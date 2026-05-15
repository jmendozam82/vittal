using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Vittal.BLL.Interfaces;
using Vittal.BLL.Services;
using Vittal.DAL.Interfaces;
using Vittal.DTO.Expediente;
using Vittal.Entity.Models;
using Vittal.Utility.Results;

namespace Vittal.BLL.Tests.Services;

public class ExpedienteServiceTests
{
    private readonly Mock<IExpedienteRepository> _repoMock;
    private readonly Mock<ILogger<ExpedienteService>> _loggerMock;
    private readonly ExpedienteService _service;
    private readonly Guid _clinicaId = Guid.NewGuid();
    private readonly Guid _pacienteId = Guid.NewGuid();
    private readonly Guid _doctorId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public ExpedienteServiceTests()
    {
        _repoMock = new Mock<IExpedienteRepository>();
        _loggerMock = new Mock<ILogger<ExpedienteService>>();
        _service = new ExpedienteService(_repoMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnList_WhenExpedientesExist()
    {
        // Arrange
        var expedientes = new List<Expediente>
        {
            new() { Id = Guid.NewGuid(), ClinicaId = _clinicaId, PacienteId = _pacienteId, DoctorId = _doctorId, Activo = true, FechaCreacion = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), ClinicaId = _clinicaId, PacienteId = Guid.NewGuid(), DoctorId = _doctorId, Activo = true, FechaCreacion = DateTime.UtcNow }
        };
        _repoMock.Setup(r => r.GetAllAsync(_clinicaId)).ReturnsAsync(expedientes);

        // Act
        var result = await _service.GetAllAsync(_clinicaId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnEmptyList_WhenNoExpedientes()
    {
        // Arrange
        _repoMock.Setup(r => r.GetAllAsync(_clinicaId)).ReturnsAsync(new List<Expediente>());

        // Act
        var result = await _service.GetAllAsync(_clinicaId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnExpediente_WhenExists()
    {
        // Arrange
        var expedienteId = Guid.NewGuid();
        var expediente = new Expediente
        {
            Id = expedienteId,
            ClinicaId = _clinicaId,
            PacienteId = _pacienteId,
            DoctorId = _doctorId,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        _repoMock.Setup(r => r.GetByIdAsync(_clinicaId, expedienteId)).ReturnsAsync(expediente);

        // Act
        var result = await _service.GetByIdAsync(_clinicaId, expedienteId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(expedienteId);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNotFound_WhenNotExists()
    {
        // Arrange
        var expedienteId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(_clinicaId, expedienteId)).ReturnsAsync((Expediente?)null);

        // Act
        var result = await _service.GetByIdAsync(_clinicaId, expedienteId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
    }

    [Fact]
    public async Task GetByPacienteIdAsync_ShouldReturnExpediente_WhenExists()
    {
        // Arrange
        var expediente = new Expediente
        {
            Id = Guid.NewGuid(),
            ClinicaId = _clinicaId,
            PacienteId = _pacienteId,
            DoctorId = _doctorId,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        _repoMock.Setup(r => r.GetByPacienteIdAsync(_clinicaId, _pacienteId)).ReturnsAsync(expediente);

        // Act
        var result = await _service.GetByPacienteIdAsync(_clinicaId, _pacienteId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.PacienteId.Should().Be(_pacienteId);
    }

    [Fact]
    public async Task GetByPacienteIdAsync_ShouldReturnNotFound_WhenNotExists()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByPacienteIdAsync(_clinicaId, _pacienteId)).ReturnsAsync((Expediente?)null);

        // Act
        var result = await _service.GetByPacienteIdAsync(_clinicaId, _pacienteId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        result.Message.Should().Contain("no tiene expediente");
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnSuccess_WhenNoDuplicate()
    {
        // Arrange
        var request = new ExpedienteRequestDto
        {
            PacienteId = _pacienteId,
            DoctorId = _doctorId,
            NotasGenerales = "Expediente inicial"
        };
        var newId = Guid.NewGuid();
        var createdExp = new Expediente
        {
            Id = newId,
            ClinicaId = _clinicaId,
            PacienteId = _pacienteId,
            DoctorId = _doctorId,
            NotasGenerales = "Expediente inicial",
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        _repoMock.Setup(r => r.GetByPacienteIdAsync(_clinicaId, _pacienteId)).ReturnsAsync((Expediente?)null);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Expediente>())).ReturnsAsync(newId);
        _repoMock.Setup(r => r.GetByIdAsync(_clinicaId, newId)).ReturnsAsync(createdExp);

        // Act
        var result = await _service.CreateAsync(request, _clinicaId, _userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(newId);
        result.Data.PacienteId.Should().Be(_pacienteId);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnConflict_WhenDuplicatePaciente()
    {
        // Arrange
        var request = new ExpedienteRequestDto
        {
            PacienteId = _pacienteId,
            DoctorId = _doctorId
        };
        var existingExp = new Expediente
        {
            Id = Guid.NewGuid(),
            ClinicaId = _clinicaId,
            PacienteId = _pacienteId,
            DoctorId = _doctorId,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        _repoMock.Setup(r => r.GetByPacienteIdAsync(_clinicaId, _pacienteId)).ReturnsAsync(existingExp);

        // Act
        var result = await _service.CreateAsync(request, _clinicaId, _userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Conflict);
        result.Message.Should().Contain("ya tiene un expediente");
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnSuccess_WhenDataIsValid()
    {
        // Arrange
        var expedienteId = Guid.NewGuid();
        var existingExp = new Expediente
        {
            Id = expedienteId,
            ClinicaId = _clinicaId,
            PacienteId = _pacienteId,
            DoctorId = _doctorId,
            NotasGenerales = "Notas originales",
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        var request = new ExpedienteRequestDto
        {
            PacienteId = _pacienteId,
            DoctorId = _doctorId,
            NotasGenerales = "Notas actualizadas"
        };

        _repoMock.Setup(r => r.GetByIdAsync(_clinicaId, expedienteId)).ReturnsAsync(existingExp);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Expediente>())).ReturnsAsync(true);
        _repoMock.Setup(r => r.GetByIdAsync(_clinicaId, expedienteId)).ReturnsAsync(existingExp);

        // Act
        var result = await _service.UpdateAsync(expedienteId, request, _clinicaId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnNotFound_WhenNotExists()
    {
        // Arrange
        var expedienteId = Guid.NewGuid();
        var request = new ExpedienteRequestDto
        {
            PacienteId = _pacienteId,
            DoctorId = _doctorId
        };
        _repoMock.Setup(r => r.GetByIdAsync(_clinicaId, expedienteId)).ReturnsAsync((Expediente?)null);

        // Act
        var result = await _service.UpdateAsync(expedienteId, request, _clinicaId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
    }

    [Fact]
    public async Task DeactivateAsync_ShouldReturnSuccess_WhenActivo()
    {
        // Arrange
        var expedienteId = Guid.NewGuid();
        var expediente = new Expediente
        {
            Id = expedienteId,
            ClinicaId = _clinicaId,
            PacienteId = _pacienteId,
            DoctorId = _doctorId,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        _repoMock.Setup(r => r.GetByIdAsync(_clinicaId, expedienteId)).ReturnsAsync(expediente);
        _repoMock.Setup(r => r.DeactivateAsync(_clinicaId, expedienteId)).ReturnsAsync(true);

        // Act
        var result = await _service.DeactivateAsync(_clinicaId, expedienteId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateAsync_ShouldReturnNotFound_WhenNotExists()
    {
        // Arrange
        var expedienteId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(_clinicaId, expedienteId)).ReturnsAsync((Expediente?)null);

        // Act
        var result = await _service.DeactivateAsync(_clinicaId, expedienteId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
    }

    [Fact]
    public async Task DeactivateAsync_ShouldReturnValidation_WhenAlreadyInactivo()
    {
        // Arrange
        var expedienteId = Guid.NewGuid();
        var expediente = new Expediente
        {
            Id = expedienteId,
            ClinicaId = _clinicaId,
            PacienteId = _pacienteId,
            DoctorId = _doctorId,
            Activo = false,
            FechaCreacion = DateTime.UtcNow
        };
        _repoMock.Setup(r => r.GetByIdAsync(_clinicaId, expedienteId)).ReturnsAsync(expediente);

        // Act
        var result = await _service.DeactivateAsync(_clinicaId, expedienteId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Validation);
        result.Message.Should().Contain("ya está inactivo");
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
        result.Message.Should().Contain("Error interno");
    }
}
