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
        _repoMock.Setup(r => r.DeactivateAsync(_clinicaId, citaId)).ReturnsAsync(false);

        // Act
        var result = await _service.DeactivateAsync(_clinicaId, citaId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
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
}
