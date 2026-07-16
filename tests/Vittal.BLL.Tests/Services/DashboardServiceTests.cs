using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Vittal.BLL.Interfaces;
using Vittal.BLL.Services;
using Vittal.DAL.Interfaces;
using Vittal.DTO.Dashboard;
using Vittal.Entity;
using Vittal.Utility.Results;

namespace Vittal.BLL.Tests.Services;

public class DashboardServiceTests
{
    private readonly Mock<IDashboardConfigRepository> _configRepoMock;
    private readonly Mock<IDashboardRepository> _dashboardRepoMock;
    private readonly Mock<INotificacionRepository> _notificacionRepoMock;
    private readonly Mock<ILogger<DashboardService>> _loggerMock;
    private readonly DashboardService _service;
    private readonly Guid _clinicaId = Guid.NewGuid();
    private readonly DateTime _fecha = DateTime.UtcNow.Date;

    public DashboardServiceTests()
    {
        _configRepoMock = new Mock<IDashboardConfigRepository>();
        _dashboardRepoMock = new Mock<IDashboardRepository>();
        _notificacionRepoMock = new Mock<INotificacionRepository>();
        _loggerMock = new Mock<ILogger<DashboardService>>();
        _service = new DashboardService(
            _configRepoMock.Object,
            _dashboardRepoMock.Object,
            _notificacionRepoMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task GetConfigAsync_ShouldReturnConfig_WhenExists()
    {
        // Arrange
        var config = new DashboardConfig
        {
            Id = Guid.NewGuid(),
            ClinicaId = _clinicaId,
            MostrarPacientesDelDia = true,
            MostrarCitasPendientes = false,
            MostrarPacientesEnEspera = true,
            MostrarTiempoPromedioEspera = true,
            MostrarGraficoCitasPorHora = false,
            MostrarUltimasAlertas = true,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        _configRepoMock.Setup(r => r.GetByClinicaIdAsync(_clinicaId)).ReturnsAsync(config);

        // Act
        var result = await _service.GetConfigAsync(_clinicaId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.MostrarPacientesDelDia.Should().BeTrue();
        result.Data.MostrarCitasPendientes.Should().BeFalse();
    }

    [Fact]
    public async Task GetConfigAsync_ShouldReturnDefault_WhenNotExists()
    {
        // Arrange
        _configRepoMock.Setup(r => r.GetByClinicaIdAsync(_clinicaId)).ReturnsAsync((DashboardConfig?)null);

        // Act
        var result = await _service.GetConfigAsync(_clinicaId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(Guid.Empty);
        result.Data.MostrarPacientesDelDia.Should().BeTrue(); // default values
        result.Data.MostrarCitasPendientes.Should().BeTrue();
    }

    [Fact]
    public async Task SaveConfigAsync_ShouldReturnSuccess_WhenCreatingNew()
    {
        // Arrange
        var request = new DashboardConfigRequestDto
        {
            MostrarPacientesDelDia = true,
            MostrarCitasPendientes = false,
            MostrarPacientesEnEspera = true,
            MostrarTiempoPromedioEspera = false,
            MostrarGraficoCitasPorHora = true,
            MostrarUltimasAlertas = true
        };
        var configId = Guid.NewGuid();

        _configRepoMock.Setup(r => r.GetByClinicaIdAsync(_clinicaId)).ReturnsAsync((DashboardConfig?)null);
        _configRepoMock.Setup(r => r.CreateOrUpdateAsync(It.IsAny<DashboardConfig>())).ReturnsAsync(configId);

        var savedConfig = new DashboardConfig
        {
            Id = configId,
            ClinicaId = _clinicaId,
            MostrarPacientesDelDia = true,
            MostrarCitasPendientes = false,
            MostrarPacientesEnEspera = true,
            MostrarTiempoPromedioEspera = false,
            MostrarGraficoCitasPorHora = true,
            MostrarUltimasAlertas = true,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        _configRepoMock.Setup(r => r.GetByClinicaIdAsync(_clinicaId)).ReturnsAsync(savedConfig);

        // Act
        var result = await _service.SaveConfigAsync(request, _clinicaId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.MostrarPacientesDelDia.Should().BeTrue();
        result.Data.MostrarCitasPendientes.Should().BeFalse();
    }

    [Fact]
    public async Task SaveConfigAsync_ShouldReturnSuccess_WhenUpdatingExisting()
    {
        // Arrange
        var configId = Guid.NewGuid();
        var existingConfig = new DashboardConfig
        {
            Id = configId,
            ClinicaId = _clinicaId,
            MostrarPacientesDelDia = false,
            MostrarCitasPendientes = false,
            MostrarPacientesEnEspera = false,
            MostrarTiempoPromedioEspera = false,
            MostrarGraficoCitasPorHora = false,
            MostrarUltimasAlertas = false,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        var request = new DashboardConfigRequestDto
        {
            MostrarPacientesDelDia = true,
            MostrarCitasPendientes = true,
            MostrarPacientesEnEspera = true,
            MostrarTiempoPromedioEspera = true,
            MostrarGraficoCitasPorHora = true,
            MostrarUltimasAlertas = true
        };

        _configRepoMock.Setup(r => r.GetByClinicaIdAsync(_clinicaId)).ReturnsAsync(existingConfig);
        _configRepoMock.Setup(r => r.CreateOrUpdateAsync(It.IsAny<DashboardConfig>())).ReturnsAsync(configId);

        var updatedConfig = new DashboardConfig
        {
            Id = configId,
            ClinicaId = _clinicaId,
            MostrarPacientesDelDia = true,
            MostrarCitasPendientes = true,
            MostrarPacientesEnEspera = true,
            MostrarTiempoPromedioEspera = true,
            MostrarGraficoCitasPorHora = true,
            MostrarUltimasAlertas = true,
            Activo = true,
            FechaCreacion = DateTime.UtcNow,
            FechaModificacion = DateTime.UtcNow
        };
        _configRepoMock.Setup(r => r.GetByClinicaIdAsync(_clinicaId)).ReturnsAsync(updatedConfig);

        // Act
        var result = await _service.SaveConfigAsync(request, _clinicaId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.MostrarPacientesDelDia.Should().BeTrue();
    }

    [Fact]
    public async Task GetDashboardDataAsync_ShouldReturnKpis_WhenAllWidgetsEnabled()
    {
        // Arrange
        var config = new DashboardConfig
        {
            Id = Guid.NewGuid(),
            ClinicaId = _clinicaId,
            MostrarPacientesDelDia = true,
            MostrarCitasPendientes = true,
            MostrarPacientesEnEspera = true,
            MostrarTiempoPromedioEspera = true,
            MostrarGraficoCitasPorHora = true,
            MostrarUltimasAlertas = true,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        _configRepoMock.Setup(r => r.GetByClinicaIdAsync(_clinicaId)).ReturnsAsync(config);
        _dashboardRepoMock.Setup(r => r.GetPacientesDelDiaAsync(_clinicaId, _fecha)).ReturnsAsync(15);
        _dashboardRepoMock.Setup(r => r.GetCitasPendientesAsync(_clinicaId, _fecha)).ReturnsAsync(8);
        _dashboardRepoMock.Setup(r => r.GetPacientesEnEsperaAsync(_clinicaId)).ReturnsAsync(3);
        _dashboardRepoMock.Setup(r => r.GetTiempoPromedioEsperaAsync(_clinicaId)).ReturnsAsync(12.5);
        _dashboardRepoMock.Setup(r => r.GetCitasPorHoraAsync(_clinicaId, _fecha))
            .ReturnsAsync(new List<DashboardGraficoDto>
            {
                new() { Etiqueta = "09:00", Valor = 5, Color = "#4F46E5" },
                new() { Etiqueta = "10:00", Valor = 8, Color = "#4F46E5" }
            });
        _dashboardRepoMock.Setup(r => r.GetUltimasAlertasAsync(_clinicaId, 5))
            .ReturnsAsync(new List<DashboardGraficoDto>
            {
                new() { Etiqueta = "Alta espera", Valor = 25, Color = "#EF4444" }
            });

        // Act
        var result = await _service.GetDashboardDataAsync(_clinicaId, _fecha);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.PacientesDelDia.Should().Be(15);
        result.Data.CitasPendientes.Should().Be(8);
        result.Data.PacientesEnEspera.Should().Be(3);
        result.Data.TiempoPromedioEspera.Should().Be(12.5);
        result.Data.CitasPorHora.Should().HaveCount(2);
        result.Data.UltimasAlertas.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetDashboardDataAsync_ShouldReturnKpis_WhenSomeWidgetsDisabled()
    {
        // Arrange
        var config = new DashboardConfig
        {
            Id = Guid.NewGuid(),
            ClinicaId = _clinicaId,
            MostrarPacientesDelDia = true,
            MostrarCitasPendientes = false,
            MostrarPacientesEnEspera = false,
            MostrarTiempoPromedioEspera = false,
            MostrarGraficoCitasPorHora = false,
            MostrarUltimasAlertas = false,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        _configRepoMock.Setup(r => r.GetByClinicaIdAsync(_clinicaId)).ReturnsAsync(config);
        _dashboardRepoMock.Setup(r => r.GetPacientesDelDiaAsync(_clinicaId, _fecha)).ReturnsAsync(10);

        // Act
        var result = await _service.GetDashboardDataAsync(_clinicaId, _fecha);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.PacientesDelDia.Should().Be(10);
        // These should remain 0 since widgets are disabled
        result.Data.CitasPendientes.Should().Be(0);
        result.Data.PacientesEnEspera.Should().Be(0);
        result.Data.TiempoPromedioEspera.Should().Be(0);
        result.Data.CitasPorHora.Should().BeEmpty();
        result.Data.UltimasAlertas.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDashboardDataAsync_ShouldHandleClinicaWithNoData()
    {
        // Arrange
        var config = new DashboardConfig
        {
            Id = Guid.NewGuid(),
            ClinicaId = _clinicaId,
            MostrarPacientesDelDia = true,
            MostrarCitasPendientes = true,
            MostrarPacientesEnEspera = true,
            MostrarTiempoPromedioEspera = true,
            MostrarGraficoCitasPorHora = true,
            MostrarUltimasAlertas = true,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        _configRepoMock.Setup(r => r.GetByClinicaIdAsync(_clinicaId)).ReturnsAsync(config);
        _dashboardRepoMock.Setup(r => r.GetPacientesDelDiaAsync(_clinicaId, _fecha)).ReturnsAsync(0);
        _dashboardRepoMock.Setup(r => r.GetCitasPendientesAsync(_clinicaId, _fecha)).ReturnsAsync(0);
        _dashboardRepoMock.Setup(r => r.GetPacientesEnEsperaAsync(_clinicaId)).ReturnsAsync(0);
        _dashboardRepoMock.Setup(r => r.GetTiempoPromedioEsperaAsync(_clinicaId)).ReturnsAsync(0.0);
        _dashboardRepoMock.Setup(r => r.GetCitasPorHoraAsync(_clinicaId, _fecha))
            .ReturnsAsync(new List<DashboardGraficoDto>());
        _dashboardRepoMock.Setup(r => r.GetUltimasAlertasAsync(_clinicaId, 5))
            .ReturnsAsync(new List<DashboardGraficoDto>());

        // Act
        var result = await _service.GetDashboardDataAsync(_clinicaId, _fecha);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.PacientesDelDia.Should().Be(0);
        result.Data.CitasPendientes.Should().Be(0);
        result.Data.PacientesEnEspera.Should().Be(0);
        result.Data.TiempoPromedioEspera.Should().Be(0);
        result.Data.CitasPorHora.Should().BeEmpty();
        result.Data.UltimasAlertas.Should().BeEmpty();
    }

    [Fact]
    public async Task GetConfigAsync_ShouldReturnFailure_WhenExceptionThrown()
    {
        // Arrange
        _configRepoMock.Setup(r => r.GetByClinicaIdAsync(_clinicaId))
            .ThrowsAsync(new Exception("DB error"));

        // Act
        var result = await _service.GetConfigAsync(_clinicaId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("Error");
    }

    [Fact]
    public async Task GetDashboardDataAsync_ShouldReturnFailure_WhenConfigFails()
    {
        // Arrange
        _configRepoMock.Setup(r => r.GetByClinicaIdAsync(_clinicaId))
            .ThrowsAsync(new Exception("DB error"));

        // Act
        var result = await _service.GetDashboardDataAsync(_clinicaId, _fecha);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("configuración");
    }
}
