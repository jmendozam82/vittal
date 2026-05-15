using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Security.Claims;
using Vittal.API.Authorization;
using Vittal.API.Controllers;
using Vittal.API.Models;
using Vittal.BLL.Interfaces;
using Vittal.DTO.Dashboard;
using Vittal.Utility;
using Vittal.Utility.Results;

namespace Vittal.API.Tests.Controllers;

public class DashboardControllerTests
{
    private readonly Mock<IDashboardService> _serviceMock;
    private readonly Mock<ILogger<DashboardController>> _loggerMock;
    private readonly DashboardController _controller;
    private readonly Guid _clinicaId = Guid.NewGuid();
    private readonly DateTime _fecha = DateTime.UtcNow.Date;

    public DashboardControllerTests()
    {
        _serviceMock = new Mock<IDashboardService>();
        _loggerMock = new Mock<ILogger<DashboardController>>();
        _controller = new DashboardController(_serviceMock.Object, _loggerMock.Object);

        // Setup HttpContext with authenticated user claims
        var claims = new List<Claim>
        {
            new("app_clinica_id", _clinicaId.ToString()),
            new("app_usuario_id", Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };
    }

    [Fact]
    public async Task GetDashboardData_ShouldReturn200OK_WhenServiceSucceeds()
    {
        // Arrange
        var dashboardData = new DashboardConfigResponseDto
        {
            Id = Guid.NewGuid(),
            ClinicaId = _clinicaId,
            PacientesDelDia = 15,
            CitasPendientes = 8,
            PacientesEnEspera = 3,
            TiempoPromedioEspera = 12.5
        };
        var serviceResult = ServiceResult<DashboardConfigResponseDto>.Success(dashboardData);
        _serviceMock.Setup(s => s.GetDashboardDataAsync(_clinicaId, _fecha)).ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _controller.GetDashboardData(_fecha);

        // Assert
        actionResult.Should().BeOfType<OkObjectResult>();
        var okResult = actionResult as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        var response = okResult.Value as ApiResponse<DashboardConfigResponseDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.PacientesDelDia.Should().Be(15);
        response.Data.CitasPendientes.Should().Be(8);
    }

    [Fact]
    public async Task GetDashboardData_ShouldUseTodayDate_WhenNoDateProvided()
    {
        // Arrange
        var dashboardData = new DashboardConfigResponseDto
        {
            Id = Guid.NewGuid(),
            ClinicaId = _clinicaId
        };
        var serviceResult = ServiceResult<DashboardConfigResponseDto>.Success(dashboardData);
        // When fecha is null, controller uses DateTime.UtcNow.Date
        _serviceMock.Setup(s => s.GetDashboardDataAsync(_clinicaId, It.IsAny<DateTime>())).ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _controller.GetDashboardData(null);

        // Assert
        actionResult.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetConfig_ShouldReturn200OK_WhenServiceSucceeds()
    {
        // Arrange
        var config = new DashboardConfigResponseDto
        {
            Id = Guid.NewGuid(),
            ClinicaId = _clinicaId,
            MostrarPacientesDelDia = true,
            MostrarCitasPendientes = false
        };
        var serviceResult = ServiceResult<DashboardConfigResponseDto>.Success(config);
        _serviceMock.Setup(s => s.GetConfigAsync(_clinicaId)).ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _controller.GetConfig();

        // Assert
        actionResult.Should().BeOfType<OkObjectResult>();
        var okResult = actionResult as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        var response = okResult.Value as ApiResponse<DashboardConfigResponseDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data!.MostrarPacientesDelDia.Should().BeTrue();
        response.Data.MostrarCitasPendientes.Should().BeFalse();
    }

    [Fact]
    public async Task SaveConfig_ShouldReturn200OK_WhenServiceSucceeds()
    {
        // Arrange
        var request = new DashboardConfigRequestDto
        {
            MostrarPacientesDelDia = true,
            MostrarCitasPendientes = false,
            MostrarPacientesEnEspera = true,
            MostrarTiempoPromedioEspera = true,
            MostrarGraficoCitasPorHora = true,
            MostrarUltimasAlertas = true
        };
        var updatedConfig = new DashboardConfigResponseDto
        {
            Id = Guid.NewGuid(),
            ClinicaId = _clinicaId,
            MostrarPacientesDelDia = true,
            MostrarCitasPendientes = false,
            MostrarPacientesEnEspera = true,
            MostrarTiempoPromedioEspera = true,
            MostrarGraficoCitasPorHora = true,
            MostrarUltimasAlertas = true
        };
        var serviceResult = ServiceResult<DashboardConfigResponseDto>.Success(updatedConfig, "Configuración guardada exitosamente.");
        _serviceMock.Setup(s => s.SaveConfigAsync(request, _clinicaId)).ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _controller.SaveConfig(request);

        // Assert
        actionResult.Should().BeOfType<OkObjectResult>();
        var okResult = actionResult as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        var response = okResult.Value as ApiResponse<DashboardConfigResponseDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data!.MostrarPacientesDelDia.Should().BeTrue();
        response.Data.MostrarCitasPendientes.Should().BeFalse();
    }

    [Fact]
    public async Task SaveConfig_ShouldReturn400_WhenServiceReturnsValidationError()
    {
        // Arrange
        var request = new DashboardConfigRequestDto();
        var serviceResult = ServiceResult<DashboardConfigResponseDto>.Failure(
            "Error de validación", ServiceErrorType.Validation);
        _serviceMock.Setup(s => s.SaveConfigAsync(request, _clinicaId)).ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _controller.SaveConfig(request);

        // Assert
        actionResult.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = actionResult as BadRequestObjectResult;
        badRequest!.StatusCode.Should().Be(400);
    }

    [Fact]
    public void Controller_ShouldHaveAuthorizeAttribute()
    {
        // Assert
        var controllerType = typeof(DashboardController);
        var authorizeAttr = controllerType.GetCustomAttribute<AuthorizeAttribute>();
        authorizeAttr.Should().NotBeNull();
    }

    [Fact]
    public void GetDashboardData_ShouldHaveRequirePermissionAttribute()
    {
        // Assert
        var methodInfo = typeof(DashboardController).GetMethod(nameof(DashboardController.GetDashboardData));
        methodInfo.Should().NotBeNull();
        var permissionAttr = methodInfo!.GetCustomAttribute<RequirePermissionAttribute>();
        permissionAttr.Should().NotBeNull();

        // Verify constructor arguments via CustomAttributeData
        var attrData = methodInfo!.CustomAttributes
            .FirstOrDefault(a => a.AttributeType == typeof(RequirePermissionAttribute));
        attrData.Should().NotBeNull();
        attrData!.ConstructorArguments[0].Value!.Should().Be("dashboard");
        // Enum values are boxed as integers in CustomAttributeData -> compare raw int
        attrData.ConstructorArguments[1].Value!.Should().Be((int)PermissionType.Read);
    }

    [Fact]
    public void SaveConfig_ShouldHaveRequirePermissionAttribute()
    {
        // Assert
        var methodInfo = typeof(DashboardController).GetMethod(nameof(DashboardController.SaveConfig));
        methodInfo.Should().NotBeNull();
        var permissionAttr = methodInfo!.GetCustomAttribute<RequirePermissionAttribute>();
        permissionAttr.Should().NotBeNull();

        // Verify constructor arguments via CustomAttributeData
        var attrData = methodInfo!.CustomAttributes
            .FirstOrDefault(a => a.AttributeType == typeof(RequirePermissionAttribute));
        attrData.Should().NotBeNull();
        attrData!.ConstructorArguments[0].Value!.Should().Be("dashboard");
        // Enum values are boxed as integers in CustomAttributeData -> compare raw int
        attrData.ConstructorArguments[1].Value!.Should().Be((int)PermissionType.Update);
    }

    [Fact]
    public async Task GetDashboardData_ShouldReturn500_WhenServiceFails()
    {
        // Arrange
        var serviceResult = ServiceResult<DashboardConfigResponseDto>.Failure(
            "Error interno del servidor", ServiceErrorType.InternalError);
        _serviceMock.Setup(s => s.GetDashboardDataAsync(_clinicaId, _fecha)).ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _controller.GetDashboardData(_fecha);

        // Assert
        actionResult.Should().BeOfType<ObjectResult>();
        var objectResult = actionResult as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }
}
