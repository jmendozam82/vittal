using Xunit;
using Moq;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Vittal.BLL.Interfaces;
using Vittal.BLL.Services;
using Vittal.DAL.Interfaces;
using Vittal.DTO.Paciente;
using Vittal.Entity;
using Vittal.Utility.Results;

namespace Vittal.BLL.Tests.Services;

public class PacienteServiceTests
{
    private readonly Mock<IPacienteRepository> _repoMock;
    private readonly Mock<IExpedienteRepository> _expedienteRepoMock;
    private readonly Mock<ILogger<PacienteService>> _loggerMock;
    private readonly Mock<IValidator<PacienteRequestDto>> _validatorMock;
    private readonly PacienteService _service;
    private readonly Guid _clinicaId = Guid.NewGuid();
    private readonly Guid _doctorId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public PacienteServiceTests()
    {
        _repoMock = new Mock<IPacienteRepository>();
        _expedienteRepoMock = new Mock<IExpedienteRepository>();
        _loggerMock = new Mock<ILogger<PacienteService>>();
        _validatorMock = new Mock<IValidator<PacienteRequestDto>>();
        _validatorMock.Setup(v => v.ValidateAsync(It.IsAny<PacienteRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        _service = new PacienteService(_repoMock.Object, _expedienteRepoMock.Object, _loggerMock.Object, _validatorMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnList_WhenPacientesExist()
    {
        // Arrange
        var pacientes = new List<Paciente>
        {
            new() { Id = Guid.NewGuid(), ClinicaId = _clinicaId, DoctorId = _doctorId, PrimerNombre = "Juan", PrimerApellido = "Pérez", Activo = true, FechaCreacion = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), ClinicaId = _clinicaId, DoctorId = _doctorId, PrimerNombre = "María", PrimerApellido = "García", Activo = true, FechaCreacion = DateTime.UtcNow }
        };
        _repoMock.Setup(r => r.GetAllAsync(_clinicaId)).ReturnsAsync(pacientes);

        // Act
        var result = await _service.GetAllAsync(_clinicaId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data.Should().Contain(p => p.PrimerNombre == "Juan");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnEmptyList_WhenNoPacientes()
    {
        // Arrange
        _repoMock.Setup(r => r.GetAllAsync(_clinicaId)).ReturnsAsync(new List<Paciente>());

        // Act
        var result = await _service.GetAllAsync(_clinicaId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WithIncluirInactivos_ShouldCallCorrectMethod()
    {
        // Arrange
        _repoMock.Setup(r => r.GetAllIncludingInactiveAsync(_clinicaId))
            .ReturnsAsync(new List<Paciente>
            {
                new() { Id = Guid.NewGuid(), ClinicaId = _clinicaId, PrimerNombre = "Inactivo", Activo = false }
            });

        // Act
        var result = await _service.GetAllAsync(_clinicaId, incluirInactivos: true);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        _repoMock.Verify(r => r.GetAllIncludingInactiveAsync(_clinicaId), Times.Once);
        _repoMock.Verify(r => r.GetAllAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnPaciente_WhenExists()
    {
        // Arrange
        var pacienteId = Guid.NewGuid();
        var paciente = new Paciente
        {
            Id = pacienteId,
            ClinicaId = _clinicaId,
            DoctorId = _doctorId,
            PrimerNombre = "Carlos",
            PrimerApellido = "López",
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        _repoMock.Setup(r => r.GetByIdAsync(pacienteId, _clinicaId)).ReturnsAsync(paciente);

        // Act
        var result = await _service.GetByIdAsync(pacienteId, _clinicaId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(pacienteId);
        result.Data.PrimerNombre.Should().Be("Carlos");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNotFound_WhenNotExists()
    {
        // Arrange
        var pacienteId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(pacienteId, _clinicaId)).ReturnsAsync((Paciente?)null);

        // Act
        var result = await _service.GetByIdAsync(pacienteId, _clinicaId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        result.Message.Should().Contain("no encontrado");
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnSuccess_WhenDataIsValid()
    {
        // Arrange
        var request = new PacienteRequestDto
        {
            DoctorId = _doctorId,
            PrimerNombre = "Carlos",
            PrimerApellido = "López",
            Sexo = "M",
            TipoDocumentoIdentificacion = "CC",
            NumeroDocumentoIdentificacion = "001234567"
        };
        var newId = Guid.NewGuid();
        var createdPaciente = new Paciente
        {
            Id = newId,
            ClinicaId = _clinicaId,
            DoctorId = _doctorId,
            PrimerNombre = "Carlos",
            PrimerApellido = "López",
            Sexo = "M",
            TipoDocumentoIdentificacion = "CC",
            NumeroDocumentoIdentificacion = "001234567",
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        _repoMock.Setup(r => r.ExistsByEmailAsync(_clinicaId, It.IsAny<string>(), null)).ReturnsAsync(false);
        _repoMock.Setup(r => r.ExistsByCelularAsync(_clinicaId, It.IsAny<string>(), null)).ReturnsAsync(false);
        _repoMock.Setup(r => r.ExistsByNumeroDocumentoAsync(_clinicaId, It.IsAny<string>(), null)).ReturnsAsync(false);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Paciente>())).ReturnsAsync(newId);
        _repoMock.Setup(r => r.GetByIdAsync(newId, _clinicaId)).ReturnsAsync(createdPaciente);

        // Act
        var result = await _service.CreateAsync(request, _clinicaId, _userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(newId);
        result.Data.PrimerNombre.Should().Be("Carlos");
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnConflict_WhenEmailExists()
    {
        // Arrange
        var request = new PacienteRequestDto
        {
            DoctorId = _doctorId,
            PrimerNombre = "Carlos",
            PrimerApellido = "López",
            Email = "carlos@example.com",
            TipoDocumentoIdentificacion = "CC",
            NumeroDocumentoIdentificacion = "12345678"
        };
        _repoMock.Setup(r => r.ExistsByNumeroDocumentoAsync(_clinicaId, It.IsAny<string>(), null)).ReturnsAsync(false);
        _repoMock.Setup(r => r.ExistsByEmailAsync(_clinicaId, request.Email!, null)).ReturnsAsync(true);

        // Act
        var result = await _service.CreateAsync(request, _clinicaId, _userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Conflict);
        result.Message.Should().Contain("correo");
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnConflict_WhenCelularExists()
    {
        // Arrange
        var request = new PacienteRequestDto
        {
            DoctorId = _doctorId,
            PrimerNombre = "Carlos",
            PrimerApellido = "López",
            Celular = "555-1234",
            TipoDocumentoIdentificacion = "CC",
            NumeroDocumentoIdentificacion = "12345678"
        };
        _repoMock.Setup(r => r.ExistsByNumeroDocumentoAsync(_clinicaId, It.IsAny<string>(), null)).ReturnsAsync(false);
        _repoMock.Setup(r => r.ExistsByEmailAsync(_clinicaId, It.IsAny<string>(), null)).ReturnsAsync(false);
        _repoMock.Setup(r => r.ExistsByCelularAsync(_clinicaId, request.Celular!, null)).ReturnsAsync(true);

        // Act
        var result = await _service.CreateAsync(request, _clinicaId, _userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Conflict);
        result.Message.Should().Contain("celular");
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnValidation_WhenInvalidTipoDocumento()
    {
        // Arrange
        var request = new PacienteRequestDto
        {
            DoctorId = _doctorId,
            PrimerNombre = "Carlos",
            PrimerApellido = "López",
            Sexo = "M",
            TipoDocumentoIdentificacion = "XX"
        };

        // Setup validator to fail for this specific request
        var validationFailures = new List<FluentValidation.Results.ValidationFailure>
        {
            new("TipoDocumentoIdentificacion", "Debe ser CC, CR o PA.")
        };
        _validatorMock.Setup(v => v.ValidateAsync(
                It.Is<PacienteRequestDto>(r => r.TipoDocumentoIdentificacion == "XX"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(validationFailures));

        // Act
        var result = await _service.CreateAsync(request, _clinicaId, _userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Validation);
        result.Message.Should().Contain("CC, CR o PA");
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnConflict_WhenNumeroDocumentoExists()
    {
        // Arrange
        var request = new PacienteRequestDto
        {
            DoctorId = _doctorId,
            PrimerNombre = "Carlos",
            PrimerApellido = "López",
            Sexo = "M",
            TipoDocumentoIdentificacion = "CC",
            NumeroDocumentoIdentificacion = "001234567"
        };
        _repoMock.Setup(r => r.ExistsByNumeroDocumentoAsync(_clinicaId, "001234567", null)).ReturnsAsync(true);

        // Act
        var result = await _service.CreateAsync(request, _clinicaId, _userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Conflict);
        result.Message.Should().Contain("número de documento");
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnSuccess_WhenDataIsValid()
    {
        // Arrange
        var pacienteId = Guid.NewGuid();
        var existingPaciente = new Paciente
        {
            Id = pacienteId,
            ClinicaId = _clinicaId,
            DoctorId = _doctorId,
            PrimerNombre = "Carlos",
            PrimerApellido = "López",
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        var request = new PacienteRequestDto
        {
            DoctorId = _doctorId,
            PrimerNombre = "Carlos Updated",
            PrimerApellido = "López Updated",
            Sexo = "M",
            TipoDocumentoIdentificacion = "CC",
            NumeroDocumentoIdentificacion = "001234567"
        };

        _repoMock.Setup(r => r.GetByIdAsync(pacienteId, _clinicaId)).ReturnsAsync(existingPaciente);
        _repoMock.Setup(r => r.ExistsByEmailAsync(_clinicaId, It.IsAny<string>(), pacienteId)).ReturnsAsync(false);
        _repoMock.Setup(r => r.ExistsByCelularAsync(_clinicaId, It.IsAny<string>(), pacienteId)).ReturnsAsync(false);
        _repoMock.Setup(r => r.ExistsByNumeroDocumentoAsync(_clinicaId, It.IsAny<string>(), pacienteId)).ReturnsAsync(false);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Paciente>())).ReturnsAsync(true);
        _repoMock.Setup(r => r.GetByIdAsync(pacienteId, _clinicaId)).ReturnsAsync(existingPaciente);

        // Act
        var result = await _service.UpdateAsync(pacienteId, request, _clinicaId, _userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnNotFound_WhenNotExists()
    {
        // Arrange
        var pacienteId = Guid.NewGuid();
        var request = new PacienteRequestDto
        {
            DoctorId = _doctorId,
            PrimerNombre = "Carlos",
            PrimerApellido = "López"
        };
        _repoMock.Setup(r => r.GetByIdAsync(pacienteId, _clinicaId)).ReturnsAsync((Paciente?)null);

        // Act
        var result = await _service.UpdateAsync(pacienteId, request, _clinicaId, _userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
    }

    [Fact]
    public async Task DeactivateAsync_ShouldReturnSuccess_WhenActivo()
    {
        // Arrange
        var pacienteId = Guid.NewGuid();
        var paciente = new Paciente
        {
            Id = pacienteId,
            ClinicaId = _clinicaId,
            DoctorId = _doctorId,
            PrimerNombre = "Carlos",
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        _repoMock.Setup(r => r.GetByIdAsync(pacienteId, _clinicaId)).ReturnsAsync(paciente);
        _repoMock.Setup(r => r.DeactivateAsync(pacienteId, _clinicaId)).ReturnsAsync(true);

        // Act
        var result = await _service.DeactivateAsync(pacienteId, _clinicaId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateAsync_ShouldReturnNotFound_WhenNotExists()
    {
        // Arrange
        var pacienteId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(pacienteId, _clinicaId)).ReturnsAsync((Paciente?)null);

        // Act
        var result = await _service.DeactivateAsync(pacienteId, _clinicaId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
    }

    [Fact]
    public async Task DeactivateAsync_ShouldReturnValidation_WhenAlreadyInactivo()
    {
        // Arrange
        var pacienteId = Guid.NewGuid();
        var paciente = new Paciente
        {
            Id = pacienteId,
            ClinicaId = _clinicaId,
            DoctorId = _doctorId,
            Activo = false,
            FechaCreacion = DateTime.UtcNow
        };
        _repoMock.Setup(r => r.GetByIdAsync(pacienteId, _clinicaId)).ReturnsAsync(paciente);

        // Act
        var result = await _service.DeactivateAsync(pacienteId, _clinicaId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Validation);
        result.Message.Should().Contain("ya está inactivo");
    }

    [Fact]
    public async Task ReactivateAsync_ShouldReturnSuccess_WhenInactivo()
    {
        // Arrange
        var pacienteId = Guid.NewGuid();
        var paciente = new Paciente
        {
            Id = pacienteId,
            ClinicaId = _clinicaId,
            DoctorId = _doctorId,
            Activo = false,
            FechaCreacion = DateTime.UtcNow
        };
        _repoMock.Setup(r => r.GetByIdAsync(pacienteId, _clinicaId)).ReturnsAsync(paciente);
        _repoMock.Setup(r => r.ReactivateAsync(pacienteId, _clinicaId)).ReturnsAsync(true);

        // Act
        var result = await _service.ReactivateAsync(pacienteId, _clinicaId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnResults_WhenMatchFound()
    {
        // Arrange — SearchAsync now delegates to repo SQL search (ILIKE)
        var pacientes = new List<Paciente>
        {
            new() { Id = Guid.NewGuid(), ClinicaId = _clinicaId, DoctorId = _doctorId, PrimerNombre = "Juan", PrimerApellido = "Pérez", Activo = true, FechaCreacion = DateTime.UtcNow }
        };
        _repoMock.Setup(r => r.SearchAsync(_clinicaId, "Juan", 20)).ReturnsAsync(pacientes);

        // Act
        var result = await _service.SearchAsync(_clinicaId, "Juan");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data!.First()!.PrimerNombre.Should().Be("Juan");
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnEmpty_WhenNoMatch()
    {
        // Arrange — SQL search returns empty when no ILIKE match
        _repoMock.Setup(r => r.SearchAsync(_clinicaId, "XYZ", 20)).ReturnsAsync(new List<Paciente>());

        // Act
        var result = await _service.SearchAsync(_clinicaId, "XYZ");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnEmpty_WhenTermIsTooShort()
    {
        // Act
        var result = await _service.SearchAsync(_clinicaId, "a");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeEmpty();
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
