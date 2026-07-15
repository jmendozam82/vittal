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
using Vittal.API.Extensions;
using Vittal.API.Models;
using Vittal.BLL.Interfaces;
using Vittal.DTO.Paciente;
using Vittal.Utility;
using Vittal.Utility.Results;

namespace Vittal.API.Tests.Controllers;

public class PacientesControllerTests
{
    private readonly Mock<IPacienteService> _serviceMock;
    private readonly Mock<ILogger<PacientesController>> _loggerMock;
    private readonly PacientesController _controller;
    private readonly Guid _clinicaId = Guid.NewGuid();
    private readonly Guid _usuarioId = Guid.NewGuid();

    public PacientesControllerTests()
    {
        _serviceMock = new Mock<IPacienteService>();
        _loggerMock = new Mock<ILogger<PacientesController>>();
        _controller = new PacientesController(_serviceMock.Object, _loggerMock.Object);

        // Setup HttpContext with authenticated user claims
        var claims = new List<Claim>
        {
            new("app_clinica_id", _clinicaId.ToString()),
            new("app_usuario_id", _usuarioId.ToString()),
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
    public async Task GetAll_ShouldReturn200OK_WhenServiceSucceeds()
    {
        // Arrange
        var pacientes = new List<PacienteResponseDto>
        {
            new() { Id = Guid.NewGuid(), ClinicaId = _clinicaId, PrimerNombre = "Juan", PrimerApellido = "Pérez", NombreCompleto = "Juan Pérez" },
            new() { Id = Guid.NewGuid(), ClinicaId = _clinicaId, PrimerNombre = "María", PrimerApellido = "García", NombreCompleto = "María García" }
        };
        var serviceResult = ServiceResult<IEnumerable<PacienteResponseDto>>.Success(pacientes);
        _serviceMock.Setup(s => s.GetAllAsync(_clinicaId, false)).ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _controller.GetAll();

        // Assert
        actionResult.Should().BeOfType<OkObjectResult>();
        var okResult = actionResult as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        var response = okResult.Value as ApiResponse<IEnumerable<PacienteResponseDto>>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetById_ShouldReturn200_WhenPacienteExists()
    {
        // Arrange
        var pacienteId = Guid.NewGuid();
        var paciente = new PacienteResponseDto
        {
            Id = pacienteId,
            ClinicaId = _clinicaId,
            PrimerNombre = "Carlos",
            PrimerApellido = "López",
            NombreCompleto = "Carlos López"
        };
        var serviceResult = ServiceResult<PacienteResponseDto>.Success(paciente);
        _serviceMock.Setup(s => s.GetByIdAsync(pacienteId, _clinicaId)).ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _controller.GetById(pacienteId);

        // Assert
        actionResult.Should().BeOfType<OkObjectResult>();
        var okResult = actionResult as OkObjectResult;
        var response = okResult!.Value as ApiResponse<PacienteResponseDto>;
        response!.Success.Should().BeTrue();
        response.Data!.Id.Should().Be(pacienteId);
    }

    [Fact]
    public async Task GetById_ShouldReturn404_WhenPacienteNotExists()
    {
        // Arrange
        var pacienteId = Guid.NewGuid();
        var serviceResult = ServiceResult<PacienteResponseDto>.Failure(
            "Paciente no encontrado", ServiceErrorType.NotFound);
        _serviceMock.Setup(s => s.GetByIdAsync(pacienteId, _clinicaId)).ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _controller.GetById(pacienteId);

        // Assert
        actionResult.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_ShouldReturn201_WhenDataIsValid()
    {
        // Arrange
        var pacienteId = Guid.NewGuid();
        var request = new PacienteRequestDto
        {
            DoctorId = Guid.NewGuid(),
            PrimerNombre = "Carlos",
            PrimerApellido = "López",
            Sexo = "M",
            TipoDocumentoIdentificacion = "CC",
            NumeroDocumentoIdentificacion = "001234567"
        };
        var responseDto = new PacienteResponseDto
        {
            Id = pacienteId,
            ClinicaId = _clinicaId,
            DoctorId = request.DoctorId,
            PrimerNombre = "Carlos",
            PrimerApellido = "López",
            Sexo = "M",
            NombreCompleto = "Carlos López",
            TipoDocumentoIdentificacion = "CC",
            NumeroDocumentoIdentificacion = "001234567"
        };
        var serviceResult = ServiceResult<PacienteResponseDto>.Success(responseDto, "Paciente creado exitosamente.");
        _serviceMock.Setup(s => s.CreateAsync(request, _clinicaId, _usuarioId)).ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _controller.Create(request);

        // Assert
        actionResult.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = actionResult as CreatedAtActionResult;
        createdResult!.StatusCode.Should().Be(201);
        createdResult.ActionName.Should().Be(nameof(PacientesController.GetById));
        createdResult.RouteValues!["id"].Should().Be(pacienteId);
        var response = createdResult.Value as ApiResponse<PacienteResponseDto>;
        response!.Success.Should().BeTrue();
        response.Data!.Id.Should().Be(pacienteId);
    }

    [Fact]
    public async Task Create_ShouldReturn400_WhenServiceReturnsValidationError()
    {
        // Arrange
        var request = new PacienteRequestDto
        {
            DoctorId = Guid.NewGuid(),
            PrimerNombre = "Carlos",
            PrimerApellido = "López"
        };
        var serviceResult = ServiceResult<PacienteResponseDto>.Failure(
            "Error de validación", ServiceErrorType.Validation);
        _serviceMock.Setup(s => s.CreateAsync(request, _clinicaId, _usuarioId)).ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _controller.Create(request);

        // Assert
        actionResult.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = actionResult as BadRequestObjectResult;
        badRequest!.StatusCode.Should().Be(400);
    }

    [Fact]
    public void Controller_ShouldHaveAuthorizeAttribute()
    {
        // Assert
        var controllerType = typeof(PacientesController);
        var authorizeAttr = controllerType.GetCustomAttribute<AuthorizeAttribute>();
        authorizeAttr.Should().NotBeNull();
    }

    [Fact]
    public void GetAll_ShouldHaveRequirePermissionAttribute()
    {
        // Assert
        var methodInfo = typeof(PacientesController).GetMethod(nameof(PacientesController.GetAll));
        methodInfo.Should().NotBeNull();
        var permissionAttr = methodInfo!.GetCustomAttribute<RequirePermissionAttribute>();
        permissionAttr.Should().NotBeNull();

        // Verify constructor arguments via CustomAttributeData
        var attrData = methodInfo!.CustomAttributes
            .FirstOrDefault(a => a.AttributeType == typeof(RequirePermissionAttribute));
        attrData.Should().NotBeNull();
        attrData!.ConstructorArguments[0].Value!.Should().Be("pacientes");
        // Enum values are boxed as integers in CustomAttributeData -> compare raw int
        attrData.ConstructorArguments[1].Value!.Should().Be((int)PermissionType.Read);
    }

    [Fact]
    public void Create_ShouldHaveRequirePermissionAttribute()
    {
        // Assert
        var methodInfo = typeof(PacientesController).GetMethod(nameof(PacientesController.Create));
        methodInfo.Should().NotBeNull();
        var permissionAttr = methodInfo!.GetCustomAttribute<RequirePermissionAttribute>();
        permissionAttr.Should().NotBeNull();

        // Verify constructor arguments via CustomAttributeData
        var attrData = methodInfo!.CustomAttributes
            .FirstOrDefault(a => a.AttributeType == typeof(RequirePermissionAttribute));
        attrData.Should().NotBeNull();
        attrData!.ConstructorArguments[0].Value!.Should().Be("pacientes");
        // Enum values are boxed as integers in CustomAttributeData -> compare raw int
        attrData.ConstructorArguments[1].Value!.Should().Be((int)PermissionType.Create);
    }

    [Fact]
    public async Task Update_ShouldReturn200_WhenServiceSucceeds()
    {
        // Arrange
        var pacienteId = Guid.NewGuid();
        var request = new PacienteRequestDto
        {
            DoctorId = Guid.NewGuid(),
            PrimerNombre = "Carlos Updated",
            PrimerApellido = "López"
        };
        var responseDto = new PacienteResponseDto
        {
            Id = pacienteId,
            ClinicaId = _clinicaId,
            PrimerNombre = "Carlos Updated",
            PrimerApellido = "López",
            NombreCompleto = "Carlos Updated López"
        };
        var serviceResult = ServiceResult<PacienteResponseDto>.Success(responseDto);
        _serviceMock.Setup(s => s.UpdateAsync(pacienteId, request, _clinicaId, _usuarioId)).ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _controller.Update(pacienteId, request);

        // Assert
        actionResult.Should().BeOfType<OkObjectResult>();
        var okResult = actionResult as OkObjectResult;
        var response = okResult!.Value as ApiResponse<PacienteResponseDto>;
        response!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Desactivar_ShouldReturn200_WhenServiceSucceeds()
    {
        // Arrange
        var pacienteId = Guid.NewGuid();
        var serviceResult = ServiceResult<bool>.Success(true, "Paciente desactivado exitosamente.");
        _serviceMock.Setup(s => s.DeactivateAsync(pacienteId, _clinicaId)).ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _controller.Desactivar(pacienteId);

        // Assert
        actionResult.Should().BeOfType<OkObjectResult>();
        var okResult = actionResult as OkObjectResult;
        var response = okResult!.Value as ApiResponse<bool>;
        response!.Success.Should().BeTrue();
        response.Data.Should().BeTrue();
    }

    [Fact]
    public async Task Search_ShouldReturn200_WhenServiceSucceeds()
    {
        // Arrange
        var pacientes = new List<PacienteResponseDto>
        {
            new() { Id = Guid.NewGuid(), ClinicaId = _clinicaId, PrimerNombre = "Juan", PrimerApellido = "Pérez", NombreCompleto = "Juan Pérez" }
        };
        var serviceResult = ServiceResult<IEnumerable<PacienteResponseDto>>.Success(pacientes);
        _serviceMock.Setup(s => s.SearchAsync(_clinicaId, "Juan")).ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _controller.Search("Juan");

        // Assert
        actionResult.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public void GetAll_WithoutPermission_ShouldRequireAuthorization()
    {
        // Verify that the [Authorize] attribute is at controller level
        var controllerType = typeof(PacientesController);
        var authorizeAttrs = controllerType.GetCustomAttributes(typeof(AuthorizeAttribute), true);
        authorizeAttrs.Should().HaveCountGreaterOrEqualTo(1);
    }
}
