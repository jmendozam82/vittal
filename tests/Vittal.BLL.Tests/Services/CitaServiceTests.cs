using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Vittal.BLL.Interfaces;
using Vittal.BLL.Services;
using Vittal.DAL.Interfaces;
using Vittal.DTO.Cita;
using Vittal.DTO.Clinica;
using Vittal.Entity;
using Vittal.Utility.Results;

namespace Vittal.BLL.Tests.Services;

public class CitaServiceTests
{
    private readonly Mock<ICitaRepository> _repoMock;
    private readonly Mock<ILineaTiempoService> _lineaTiempoMock;
    private readonly Mock<IClinicaService> _clinicaServiceMock;
    private readonly Mock<ILogger<CitaService>> _loggerMock;
    private readonly CitaService _service;
    private readonly Guid _clinicaId = Guid.NewGuid();
    private readonly Guid _doctorId = Guid.NewGuid();
    private readonly Guid _pacienteId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public CitaServiceTests()
    {
        _repoMock = new Mock<ICitaRepository>();
        _lineaTiempoMock = new Mock<ILineaTiempoService>();
        _clinicaServiceMock = new Mock<IClinicaService>();
        _loggerMock = new Mock<ILogger<CitaService>>();

        // Default: clinic has no schedule configured (validation skipped)
        _clinicaServiceMock
            .Setup(s => s.GetByIdAsync(_clinicaId))
            .ReturnsAsync(ServiceResult<ClinicaResponseDto>.Success(new ClinicaResponseDto
            {
                Id = _clinicaId,
                HorarioApertura = null,
                HorarioCierre = null,
                DiasAtencion = null
            }));

        // Default: no hay solapamiento de horario para el doctor
        _repoMock
            .Setup(r => r.ExisteCitaSolapadaAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateOnly>(),
                It.IsAny<TimeOnly>(), It.IsAny<TimeOnly?>(), It.IsAny<Guid?>()))
            .ReturnsAsync(false);

        _service = new CitaService(_repoMock.Object, _lineaTiempoMock.Object, _clinicaServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnList_WhenCitasExist()
    {
        // Arrange
        var citas = new List<Cita>
        {
            new() { Id = Guid.NewGuid(), ClinicaId = _clinicaId, PacienteId = _pacienteId, DoctorId = _doctorId, FechaCita = DateOnly.FromDateTime(DateTime.UtcNow), HoraCita = new TimeOnly(9, 0, 0), Estado = "agendada", Activo = true, FechaCreacion = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), ClinicaId = _clinicaId, PacienteId = _pacienteId, DoctorId = _doctorId, FechaCita = DateOnly.FromDateTime(DateTime.UtcNow), HoraCita = new TimeOnly(10, 0, 0), Estado = "agendada", Activo = true, FechaCreacion = DateTime.UtcNow }
        };
        _repoMock.Setup(r => r.GetAllAsync(_clinicaId)).ReturnsAsync(citas);

        // Act
        var result = await _service.GetAllAsync(_clinicaId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnEmptyList_WhenNoCitas()
    {
        // Arrange
        _repoMock.Setup(r => r.GetAllAsync(_clinicaId)).ReturnsAsync(new List<Cita>());

        // Act
        var result = await _service.GetAllAsync(_clinicaId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnCita_WhenExists()
    {
        // Arrange
        var citaId = Guid.NewGuid();
        var cita = new Cita
        {
            Id = citaId,
            ClinicaId = _clinicaId,
            PacienteId = _pacienteId,
            DoctorId = _doctorId,
            FechaCita = DateOnly.FromDateTime(DateTime.UtcNow),
            HoraCita = new TimeOnly(9, 0, 0),
            Estado = "agendada",
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        _repoMock.Setup(r => r.GetByIdAsync(_clinicaId, citaId)).ReturnsAsync(cita);

        // Act
        var result = await _service.GetByIdAsync(_clinicaId, citaId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(citaId);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNotFound_WhenNotExists()
    {
        // Arrange
        var citaId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(_clinicaId, citaId)).ReturnsAsync((Cita?)null);

        // Act
        var result = await _service.GetByIdAsync(_clinicaId, citaId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnSuccess_WhenDataIsValid()
    {
        // Arrange
        var request = new CitaRequestDto
        {
            PacienteId = _pacienteId,
            DoctorId = _doctorId,
            FechaCita = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            HoraCita = new TimeOnly(9, 0, 0),
            Estado = "agendada"
        };
        var newId = Guid.NewGuid();
        var createdCita = new Cita
        {
            Id = newId,
            ClinicaId = _clinicaId,
            PacienteId = _pacienteId,
            DoctorId = _doctorId,
            FechaCita = request.FechaCita,
            HoraCita = request.HoraCita,
            Estado = "agendada",
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Cita>())).ReturnsAsync(newId);
        _repoMock.Setup(r => r.GetByIdAsync(_clinicaId, newId)).ReturnsAsync(createdCita);

        // Act
        var result = await _service.CreateAsync(request, _clinicaId, _userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(newId);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnFailure_WhenExceptionThrown()
    {
        // Arrange
        var request = new CitaRequestDto
        {
            PacienteId = _pacienteId,
            DoctorId = _doctorId,
            FechaCita = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            HoraCita = new TimeOnly(9, 0, 0),
            Estado = "agendada"
        };
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Cita>())).ThrowsAsync(new Exception("DB error"));

        // Act
        var result = await _service.CreateAsync(request, _clinicaId, _userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnSuccess_WhenDataIsValid()
    {
        // Arrange
        var citaId = Guid.NewGuid();
        var existingCita = new Cita
        {
            Id = citaId,
            ClinicaId = _clinicaId,
            PacienteId = _pacienteId,
            DoctorId = _doctorId,
            FechaCita = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            HoraCita = new TimeOnly(9, 0, 0),
            Estado = "agendada",
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        var request = new CitaRequestDto
        {
            PacienteId = _pacienteId,
            DoctorId = _doctorId,
            FechaCita = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
            HoraCita = new TimeOnly(10, 0, 0),
            Estado = "agendada"
        };

        _repoMock.Setup(r => r.GetByIdAsync(_clinicaId, citaId)).ReturnsAsync(existingCita);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Cita>())).ReturnsAsync(true);
        _repoMock.Setup(r => r.GetByIdAsync(_clinicaId, citaId)).ReturnsAsync(existingCita);

        // Act
        var result = await _service.UpdateAsync(citaId, request, _clinicaId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnNotFound_WhenNotExists()
    {
        // Arrange
        var citaId = Guid.NewGuid();
        var request = new CitaRequestDto
        {
            PacienteId = _pacienteId,
            DoctorId = _doctorId,
            FechaCita = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            HoraCita = new TimeOnly(9, 0, 0),
            Estado = "agendada"
        };
        _repoMock.Setup(r => r.GetByIdAsync(_clinicaId, citaId)).ReturnsAsync((Cita?)null);

        // Act
        var result = await _service.UpdateAsync(citaId, request, _clinicaId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
    }

    [Fact]
    public async Task DeactivateAsync_ShouldReturnSuccess_WhenActivo()
    {
        // Arrange
        var citaId = Guid.NewGuid();
        var existingCita = new Cita
        {
            Id = citaId,
            ClinicaId = _clinicaId,
            PacienteId = _pacienteId,
            DoctorId = _doctorId,
            FechaCita = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            HoraCita = new TimeOnly(9, 0, 0),
            Estado = "agendada",
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        _repoMock.Setup(r => r.GetByIdAsync(_clinicaId, citaId)).ReturnsAsync(existingCita);
        _repoMock.Setup(r => r.DeactivateAsync(_clinicaId, citaId)).ReturnsAsync(true);

        // Act
        var result = await _service.DeactivateAsync(_clinicaId, citaId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateAsync_ShouldReturnNotFound_WhenNotExists()
    {
        // Arrange
        var citaId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(_clinicaId, citaId)).ReturnsAsync((Cita?)null);

        // Act
        var result = await _service.DeactivateAsync(_clinicaId, citaId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
    }

    [Fact]
    public async Task DeactivateAsync_ShouldReturnFailure_WhenCitaAtendida()
    {
        // Arrange
        var citaId = Guid.NewGuid();
        var atendidaCita = new Cita
        {
            Id = citaId,
            ClinicaId = _clinicaId,
            PacienteId = _pacienteId,
            DoctorId = _doctorId,
            FechaCita = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            HoraCita = new TimeOnly(9, 0, 0),
            Estado = "atendida",
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        _repoMock.Setup(r => r.GetByIdAsync(_clinicaId, citaId)).ReturnsAsync(atendidaCita);

        // Act
        var result = await _service.DeactivateAsync(_clinicaId, citaId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Validation);
        result.Message.Should().Contain("no se puede desactivar");
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnFailure_WhenCitaAtendida()
    {
        // Arrange
        var citaId = Guid.NewGuid();
        var atendidaCita = new Cita
        {
            Id = citaId,
            ClinicaId = _clinicaId,
            PacienteId = _pacienteId,
            DoctorId = _doctorId,
            FechaCita = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            HoraCita = new TimeOnly(9, 0, 0),
            Estado = "atendida",
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        var request = new CitaRequestDto
        {
            PacienteId = _pacienteId,
            DoctorId = _doctorId,
            FechaCita = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            HoraCita = new TimeOnly(10, 0, 0),
            Estado = "atendida"
        };
        _repoMock.Setup(r => r.GetByIdAsync(_clinicaId, citaId)).ReturnsAsync(atendidaCita);

        // Act
        var result = await _service.UpdateAsync(citaId, request, _clinicaId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Validation);
        result.Message.Should().Contain("no se puede modificar");
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Cita>()), Times.Never);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnFailure_WhenExceptionThrown()
    {
        // Arrange
        _repoMock.Setup(r => r.GetAllAsync(_clinicaId)).ThrowsAsync(new Exception("DB error"));

        // Act
        var result = await _service.GetAllAsync(_clinicaId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("Error");
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnFailure_WhenDoctorHasOverlappingCita()
    {
        // Arrange — el mismo doctor ya tiene una cita en ese horario
        var request = new CitaRequestDto
        {
            PacienteId = _pacienteId,
            DoctorId = _doctorId,
            FechaCita = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            HoraCita = new TimeOnly(9, 0, 0),
            HoraFin = new TimeOnly(9, 30, 0),
            Estado = "agendada"
        };
        _repoMock
            .Setup(r => r.ExisteCitaSolapadaAsync(
                _clinicaId, _doctorId, request.FechaCita, request.HoraCita, request.HoraFin, null))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CreateAsync(request, _clinicaId, _userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Validation);
        result.Message.Should().Contain("El doctor ya tiene una cita");
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<Cita>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnSuccess_WhenDifferentDoctorsSameTime()
    {
        // Arrange — otro doctor (distinto) agendando en la misma hora SÍ es válido
        var otroDoctorId = Guid.NewGuid();
        var request = new CitaRequestDto
        {
            PacienteId = _pacienteId,
            DoctorId = otroDoctorId,
            FechaCita = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            HoraCita = new TimeOnly(9, 0, 0),
            HoraFin = new TimeOnly(9, 30, 0),
            Estado = "agendada"
        };
        var newId = Guid.NewGuid();
        var createdCita = new Cita
        {
            Id = newId,
            ClinicaId = _clinicaId,
            PacienteId = _pacienteId,
            DoctorId = otroDoctorId,
            FechaCita = request.FechaCita,
            HoraCita = request.HoraCita,
            Estado = "agendada",
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Cita>())).ReturnsAsync(newId);
        _repoMock.Setup(r => r.GetByIdAsync(_clinicaId, newId)).ReturnsAsync(createdCita);

        // Act
        var result = await _service.CreateAsync(request, _clinicaId, _userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<Cita>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnFailure_WhenDoctorHasOverlappingCita()
    {
        // Arrange
        var citaId = Guid.NewGuid();
        var existingCita = new Cita
        {
            Id = citaId,
            ClinicaId = _clinicaId,
            PacienteId = _pacienteId,
            DoctorId = _doctorId,
            FechaCita = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
            HoraCita = new TimeOnly(8, 0, 0),
            Estado = "agendada",
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        var request = new CitaRequestDto
        {
            PacienteId = _pacienteId,
            DoctorId = _doctorId,
            FechaCita = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
            HoraCita = new TimeOnly(9, 0, 0),
            HoraFin = new TimeOnly(9, 30, 0),
            Estado = "agendada"
        };
        _repoMock.Setup(r => r.GetByIdAsync(_clinicaId, citaId)).ReturnsAsync(existingCita);
        // Existe otra cita del mismo doctor que se solapa (excluyendo la que se edita)
        _repoMock
            .Setup(r => r.ExisteCitaSolapadaAsync(
                _clinicaId, _doctorId, request.FechaCita, request.HoraCita, request.HoraFin, citaId))
            .ReturnsAsync(true);

        // Act
        var result = await _service.UpdateAsync(citaId, request, _clinicaId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Validation);
        result.Message.Should().Contain("El doctor ya tiene una cita");
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Cita>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldExcludeSelf_WhenCheckingOverlap()
    {
        // Arrange — la cita se mantiene en su mismo horario (no debe chocar consigo misma)
        var citaId = Guid.NewGuid();
        var existingCita = new Cita
        {
            Id = citaId,
            ClinicaId = _clinicaId,
            PacienteId = _pacienteId,
            DoctorId = _doctorId,
            FechaCita = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
            HoraCita = new TimeOnly(9, 0, 0),
            Estado = "agendada",
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        var request = new CitaRequestDto
        {
            PacienteId = _pacienteId,
            DoctorId = _doctorId,
            FechaCita = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
            HoraCita = new TimeOnly(9, 0, 0),
            HoraFin = new TimeOnly(9, 30, 0),
            Estado = "agendada"
        };
        _repoMock.Setup(r => r.GetByIdAsync(_clinicaId, citaId)).ReturnsAsync(existingCita);
        _repoMock
            .Setup(r => r.ExisteCitaSolapadaAsync(
                _clinicaId, _doctorId, request.FechaCita, request.HoraCita, request.HoraFin, citaId))
            .ReturnsAsync(false);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Cita>())).ReturnsAsync(true);
        _repoMock.Setup(r => r.GetByIdAsync(_clinicaId, citaId)).ReturnsAsync(existingCita);

        // Act
        var result = await _service.UpdateAsync(citaId, request, _clinicaId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Cita>()), Times.Once);
    }
}
