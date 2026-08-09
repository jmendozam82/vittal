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
    private readonly Mock<ILogger<DashboardService>> _loggerMock;
    private readonly DashboardService _service;
    private readonly Guid _clinicaId = Guid.NewGuid();
    private readonly DateTime _fecha = DateTime.UtcNow.Date;

    public DashboardServiceTests()
    {
        _configRepoMock = new Mock<IDashboardConfigRepository>();
        _dashboardRepoMock = new Mock<IDashboardRepository>();
        _loggerMock = new Mock<ILogger<DashboardService>>();
        _service = new DashboardService(
            _configRepoMock.Object,
            _dashboardRepoMock.Object,
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
            MostrarCitasPorMedico = false,
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
            MostrarCitasPorMedico = true,
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
            MostrarCitasPorMedico = true,
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
            MostrarCitasPorMedico = false,
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
            MostrarCitasPorMedico = true,
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
            MostrarCitasPorMedico = true,
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
            MostrarCitasPorMedico = true,
            MostrarUltimasAlertas = true,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        _configRepoMock.Setup(r => r.GetByClinicaIdAsync(_clinicaId)).ReturnsAsync(config);
        _dashboardRepoMock.Setup(r => r.GetPacientesDelDiaAsync(_clinicaId, _fecha)).ReturnsAsync(15);
        _dashboardRepoMock.Setup(r => r.GetCitasPendientesAsync(_clinicaId, _fecha)).ReturnsAsync(8);
        _dashboardRepoMock.Setup(r => r.GetPacientesEnEsperaAsync(_clinicaId)).ReturnsAsync(3);
        _dashboardRepoMock.Setup(r => r.GetPacientesEnAtencionAsync(_clinicaId)).ReturnsAsync(2);
        _dashboardRepoMock.Setup(r => r.GetCitasCanceladasAsync(_clinicaId, _fecha)).ReturnsAsync(1);
        _dashboardRepoMock.Setup(r => r.GetTiempoPromedioEsperaAsync(_clinicaId, _fecha)).ReturnsAsync(12.5);
        _dashboardRepoMock.Setup(r => r.GetCitasPorHoraAsync(_clinicaId, _fecha))
            .ReturnsAsync(new List<DashboardCitaPorHoraDto>
            {
                new() { Etiqueta = "09:00", Agendadas = 2, EnEspera = 1, EnAtencion = 0, Atendidas = 2, Canceladas = 1 },
                new() { Etiqueta = "10:00", Agendadas = 1, EnEspera = 0, EnAtencion = 2, Atendidas = 5, Canceladas = 0 }
            });
        _dashboardRepoMock.Setup(r => r.GetCitasPorMedicoAsync(_clinicaId, _fecha))
            .ReturnsAsync(new List<DashboardCitaPorMedicoDto>
            {
                new() { DoctorNombre = "Dr. Jose Reyes", Atendidas = 6, Pendientes = 2 },
                new() { DoctorNombre = "Dra. Maria Lopez", Atendidas = 4, Pendientes = 3 }
            });

        // Act
        var result = await _service.GetDashboardDataAsync(_clinicaId, _fecha);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.PacientesDelDia.Should().Be(15);
        result.Data.CitasPendientes.Should().Be(8);
        result.Data.PacientesEnEspera.Should().Be(3);
        result.Data.PacientesEnAtencion.Should().Be(2);
        result.Data.CitasCanceladas.Should().Be(1);
        result.Data.TiempoPromedioEspera.Should().Be(12.5);
        result.Data.CitasPorHora.Should().HaveCount(2);
        result.Data.CitasPorHora[0].Canceladas.Should().Be(1);
        result.Data.CitasPorHora[0].Atendidas.Should().Be(2);
        result.Data.CitasPorMedico.Should().HaveCount(2);
        result.Data.CitasPorMedico[0].Atendidas.Should().Be(6);
        result.Data.CitasPorMedico[0].Pendientes.Should().Be(2);
        // El dashboard ya no consulta notificaciones (panel eliminado)
        result.Data.UltimasAlertas.Should().BeEmpty();
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
            MostrarCitasPorMedico = false,
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
        result.Data.CitasPorMedico.Should().BeEmpty();
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
            MostrarCitasPorMedico = true,
            MostrarUltimasAlertas = true,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        _configRepoMock.Setup(r => r.GetByClinicaIdAsync(_clinicaId)).ReturnsAsync(config);
        _dashboardRepoMock.Setup(r => r.GetPacientesDelDiaAsync(_clinicaId, _fecha)).ReturnsAsync(0);
        _dashboardRepoMock.Setup(r => r.GetCitasPendientesAsync(_clinicaId, _fecha)).ReturnsAsync(0);
        _dashboardRepoMock.Setup(r => r.GetPacientesEnEsperaAsync(_clinicaId)).ReturnsAsync(0);
        _dashboardRepoMock.Setup(r => r.GetPacientesEnAtencionAsync(_clinicaId)).ReturnsAsync(0);
        _dashboardRepoMock.Setup(r => r.GetCitasCanceladasAsync(_clinicaId, _fecha)).ReturnsAsync(0);
        _dashboardRepoMock.Setup(r => r.GetTiempoPromedioEsperaAsync(_clinicaId, _fecha)).ReturnsAsync(0.0);
        _dashboardRepoMock.Setup(r => r.GetCitasPorHoraAsync(_clinicaId, _fecha))
            .ReturnsAsync(new List<DashboardCitaPorHoraDto>());
        _dashboardRepoMock.Setup(r => r.GetCitasPorMedicoAsync(_clinicaId, _fecha))
            .ReturnsAsync(new List<DashboardCitaPorMedicoDto>());

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
        result.Data.CitasPorMedico.Should().BeEmpty();
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
