using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Vittal.BLL.Interfaces;
using Vittal.BLL.Services;
using Vittal.DAL.Interfaces;
using Vittal.DTO.Alerta;
using Vittal.DTO.ConfiguracionAlerta;
using Vittal.Entity;
using Vittal.Utility.Results;

namespace Vittal.BLL.Tests.Services;

public class AlertaEsperaServiceTests
{
    private readonly Mock<IAlertaEsperaRepository> _repositoryMock;
    private readonly Mock<ICitaRepository> _citaRepositoryMock;
    private readonly Mock<IConfiguracionAlertaService> _configServiceMock;
    private readonly Mock<INotificacionService> _notificacionServiceMock;
    private readonly Mock<ILogger<AlertaEsperaService>> _loggerMock;
    private readonly AlertaEsperaService _service;
    private readonly Guid _clinicaId = Guid.NewGuid();
    private readonly Guid _usuarioId = Guid.NewGuid();

    public AlertaEsperaServiceTests()
    {
        _repositoryMock = new Mock<IAlertaEsperaRepository>();
        _citaRepositoryMock = new Mock<ICitaRepository>();
        _configServiceMock = new Mock<IConfiguracionAlertaService>();
        _notificacionServiceMock = new Mock<INotificacionService>();
        _loggerMock = new Mock<ILogger<AlertaEsperaService>>();

        _service = new AlertaEsperaService(
            _repositoryMock.Object,
            _citaRepositoryMock.Object,
            _configServiceMock.Object,
            _notificacionServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_WithClinicaId_ShouldReturnAlerts()
    {
        // Arrange
        var alertas = new List<AlertaEspera>
        {
            new() { Id = Guid.NewGuid(), ClinicaId = _clinicaId, CitaId = Guid.NewGuid(), PacienteId = Guid.NewGuid(), DoctorId = Guid.NewGuid(), MinutosEspera = 35, Resuelta = false, PacienteNombre = "Juan Pérez", DoctorNombre = "Dr. García", FechaAlerta = DateTime.UtcNow }
        };
        _repositoryMock.Setup(r => r.GetAllByClinicaIdAsync(_clinicaId, null)).ReturnsAsync(alertas);

        // Act
        var result = await _service.GetAllAsync(_clinicaId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data!.First().PacienteNombre.Should().Be("Juan Pérez");
        result.Data!.First().MinutosEspera.Should().Be(35);
    }

    [Fact]
    public async Task GetAllAsync_EmptyClinica_ShouldReturnEmptyList()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetAllByClinicaIdAsync(_clinicaId, null))
            .ReturnsAsync(new List<AlertaEspera>());

        // Act
        var result = await _service.GetAllAsync(_clinicaId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetNoResueltasAsync_ShouldReturnUnresolvedAlerts()
    {
        // Arrange
        var alertas = new List<AlertaEspera>
        {
            new() { Id = Guid.NewGuid(), ClinicaId = _clinicaId, CitaId = Guid.NewGuid(), PacienteId = Guid.NewGuid(), DoctorId = Guid.NewGuid(), MinutosEspera = 45, Resuelta = false, PacienteNombre = "Ana López", DoctorNombre = "Dr. Martínez", FechaAlerta = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), ClinicaId = _clinicaId, CitaId = Guid.NewGuid(), PacienteId = Guid.NewGuid(), DoctorId = Guid.NewGuid(), MinutosEspera = 60, Resuelta = false, PacienteNombre = "Pedro Ruiz", DoctorNombre = "Dr. Sánchez", FechaAlerta = DateTime.UtcNow }
        };
        _repositoryMock.Setup(r => r.GetNoResueltasAsync(_clinicaId)).ReturnsAsync(alertas);

        // Act
        var result = await _service.GetNoResueltasAsync(_clinicaId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data!.All(a => a.Resuelta == false).Should().BeTrue();
    }

    [Fact]
    public async Task ResolverAlertaAsync_ShouldReturnSuccess_WhenAlertExists()
    {
        // Arrange
        var alertaId = Guid.NewGuid();
        var dto = new AlertaEsperaResolveDto { AlertaId = alertaId, NotasResolucion = "Paciente fue atendido" };
        _repositoryMock.Setup(r => r.MarcarResueltaAsync(_clinicaId, alertaId)).ReturnsAsync(true);

        // Act
        var result = await _service.ResolverAlertaAsync(_clinicaId, dto, _usuarioId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();
        result.Message.Should().Contain("resuelta exitosamente");
    }

    [Fact]
    public async Task ResolverAlertaAsync_ShouldReturnNotFound_WhenAlertDoesNotExist()
    {
        // Arrange
        var alertaId = Guid.NewGuid();
        var dto = new AlertaEsperaResolveDto { AlertaId = alertaId };
        _repositoryMock.Setup(r => r.MarcarResueltaAsync(_clinicaId, alertaId)).ReturnsAsync(false);

        // Act
        var result = await _service.ResolverAlertaAsync(_clinicaId, dto, _usuarioId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        result.Message.Should().Contain("no encontrada");
    }

    [Fact]
    public async Task VerificarTiemposEsperaAsync_ShouldGenerateAlert_WhenExceedsTime()
    {
        // Arrange
        var configResult = ServiceResult<ConfiguracionAlertaResponseDto>.Success(new ConfiguracionAlertaResponseDto
        {
            ClinicaId = _clinicaId,
            TiempoEsperaMaximoMinutos = 30,
            Activo = true
        });
        _configServiceMock.Setup(s => s.GetAsync(_clinicaId)).ReturnsAsync(configResult);

        var citaId = Guid.NewGuid();
        var pacienteId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var horaCita = new TimeOnly(9, 0, 0);
        var horaActual = TimeOnly.FromDateTime(DateTime.UtcNow);
        var minutosEspera = (int)(horaActual - horaCita).TotalMinutes;

        var citasEnEspera = new List<Cita>
        {
            new() { Id = citaId, ClinicaId = _clinicaId, PacienteId = pacienteId, DoctorId = doctorId, HoraCita = horaCita, Estado = "en_espera", Activo = true, PacienteNombre = "Test Patient", DoctorNombre = "Dr. Test" }
        };
        _citaRepositoryMock.Setup(r => r.GetCitasEnEsperaAsync(_clinicaId)).ReturnsAsync(citasEnEspera);
        _repositoryMock.Setup(r => r.ExisteAlertaNoResueltaParaCitaAsync(_clinicaId, citaId)).ReturnsAsync(false);
        _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<AlertaEspera>())).ReturnsAsync(Guid.NewGuid());
        _notificacionServiceMock.Setup(s => s.CreateAsync(It.IsAny<Notificacion>()))
            .ReturnsAsync(ServiceResult<DTO.Notificacion.NotificacionResponseDto>.Success(new DTO.Notificacion.NotificacionResponseDto()));

        // Act
        var result = await _service.VerificarTiemposEsperaAsync(_clinicaId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        if (minutosEspera >= 30)
        {
            result.Data.Should().BeGreaterThanOrEqualTo(1);
            _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<AlertaEspera>()), Times.Once);
        }
        else
        {
            result.Data.Should().Be(0);
        }
    }

    [Fact]
    public async Task VerificarTiemposEsperaAsync_ShouldReturnZero_WhenAlertasDisabled()
    {
        // Arrange
        var configResult = ServiceResult<ConfiguracionAlertaResponseDto>.Success(new ConfiguracionAlertaResponseDto
        {
            ClinicaId = _clinicaId,
            TiempoEsperaMaximoMinutos = 30,
            Activo = false
        });
        _configServiceMock.Setup(s => s.GetAsync(_clinicaId)).ReturnsAsync(configResult);

        // Act
        var result = await _service.VerificarTiemposEsperaAsync(_clinicaId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(0);
        result.Message.Should().Contain("deshabilitadas");
    }
}
