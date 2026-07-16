using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Vittal.API.Controllers;
using Vittal.API.Models;
using Vittal.BLL.Interfaces;
using Vittal.DTO.Usuario;
using Vittal.Utility;
using Vittal.Utility.Results;

namespace Vittal.API.Tests.Controllers;

public class UsuariosControllerTests
{
    private readonly Mock<IUsuarioService> _serviceMock;
    private readonly Mock<ILogger<UsuariosController>> _loggerMock;
    private readonly UsuariosController _controller;
    private readonly Guid _clinicaId = Guid.NewGuid();
    private readonly Guid _usuarioId = Guid.NewGuid();

    public UsuariosControllerTests()
    {
        _serviceMock = new Mock<IUsuarioService>();
        _loggerMock = new Mock<ILogger<UsuariosController>>();
        _controller = new UsuariosController(_serviceMock.Object, _loggerMock.Object);

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
    public async Task GetAll_ShouldReturnOk_WhenUsuariosExist()
    {
        // Arrange
        var usuarios = new List<UsuarioResponseDto>
        {
            new() { UsuarioId = Guid.NewGuid(), ClinicaId = _clinicaId, Username = "admin", Nombres = "Juan", Apellidos = "Pérez", Email = "juan@test.com", PerfilNombre = "Admin" },
            new() { UsuarioId = Guid.NewGuid(), ClinicaId = _clinicaId, Username = "doctor1", Nombres = "María", Apellidos = "García", Email = "maria@test.com", PerfilNombre = "Doctor" }
        };
        var serviceResult = ServiceResult<IEnumerable<UsuarioResponseDto>>.Success(usuarios);
        _serviceMock.Setup(s => s.GetAllAsync(_clinicaId, false)).ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _controller.GetAll();

        // Assert
        actionResult.Should().BeOfType<OkObjectResult>();
        var okResult = actionResult as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        var response = okResult.Value as ApiResponse<IEnumerable<UsuarioResponseDto>>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetById_ShouldReturnOk_WhenUsuarioExists()
    {
        // Arrange
        var usuarioId = Guid.NewGuid();
        var usuario = new UsuarioResponseDto
        {
            UsuarioId = usuarioId,
            ClinicaId = _clinicaId,
            Username = "admin",
            Nombres = "Juan",
            Apellidos = "Pérez",
            Email = "juan@test.com",
            PerfilNombre = "Admin"
        };
        var serviceResult = ServiceResult<UsuarioResponseDto>.Success(usuario);
        _serviceMock.Setup(s => s.GetByIdAsync(usuarioId, _clinicaId)).ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _controller.GetById(usuarioId);

        // Assert
        actionResult.Should().BeOfType<OkObjectResult>();
        var okResult = actionResult as OkObjectResult;
        var response = okResult!.Value as ApiResponse<UsuarioResponseDto>;
        response!.Success.Should().BeTrue();
        response.Data!.UsuarioId.Should().Be(usuarioId);
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenUsuarioDoesNotExist()
    {
        // Arrange
        var usuarioId = Guid.NewGuid();
        var serviceResult = ServiceResult<UsuarioResponseDto>.Failure(
            "Usuario no encontrado", ServiceErrorType.NotFound);
        _serviceMock.Setup(s => s.GetByIdAsync(usuarioId, _clinicaId)).ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _controller.GetById(usuarioId);

        // Assert
        actionResult.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_ShouldReturnCreated_WhenValidData()
    {
        // Arrange
        var usuarioId = Guid.NewGuid();
        var request = new UsuarioRequestDto
        {
            Username = "newdoctor",
            Nombres = "Carlos",
            Apellidos = "López",
            Email = "carlos@test.com",
            Password = "Secret123",
            PerfilId = Guid.NewGuid(),
            TipoDocumentoIdentificacion = "CC",
            NumeroDocumentoIdentificacion = "1234567890"
        };
        var responseDto = new UsuarioResponseDto
        {
            UsuarioId = usuarioId,
            ClinicaId = _clinicaId,
            Username = "newdoctor",
            Nombres = "Carlos",
            Apellidos = "López",
            Email = "carlos@test.com",
            PerfilNombre = "Doctor"
        };
        var serviceResult = ServiceResult<UsuarioResponseDto>.Success(responseDto, "Usuario creado exitosamente.");
        _serviceMock.Setup(s => s.CreateAsync(request, _clinicaId, _usuarioId)).ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _controller.Create(request);

        // Assert
        actionResult.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = actionResult as CreatedAtActionResult;
        createdResult!.StatusCode.Should().Be(201);
        createdResult.ActionName.Should().Be(nameof(UsuariosController.GetById));
        var response = createdResult.Value as ApiResponse<UsuarioResponseDto>;
        response!.Success.Should().BeTrue();
        response.Data!.UsuarioId.Should().Be(usuarioId);
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenServiceFails()
    {
        // Arrange
        var request = new UsuarioRequestDto
        {
            Username = "newdoctor",
            Nombres = "Carlos",
            Apellidos = "López",
            Email = "carlos@test.com",
            Password = "Secret123",
            PerfilId = Guid.NewGuid(),
            TipoDocumentoIdentificacion = "CC",
            NumeroDocumentoIdentificacion = "1234567890"
        };
        var serviceResult = ServiceResult<UsuarioResponseDto>.Failure(
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
    public async Task Update_ShouldReturnOk_WhenValidData()
    {
        // Arrange
        var usuarioId = Guid.NewGuid();
        var request = new UsuarioRequestDto
        {
            Username = "updateduser",
            Nombres = "Carlos Updated",
            Apellidos = "López",
            Email = "carlos@test.com",
            PerfilId = Guid.NewGuid(),
            TipoDocumentoIdentificacion = "CC",
            NumeroDocumentoIdentificacion = "1234567890"
        };
        var responseDto = new UsuarioResponseDto
        {
            UsuarioId = usuarioId,
            ClinicaId = _clinicaId,
            Username = "updateduser",
            Nombres = "Carlos Updated",
            Apellidos = "López",
            Email = "carlos@test.com"
        };
        var serviceResult = ServiceResult<UsuarioResponseDto>.Success(responseDto);
        _serviceMock.Setup(s => s.UpdateAsync(usuarioId, request, _clinicaId, _usuarioId)).ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _controller.Update(usuarioId, request);

        // Assert
        actionResult.Should().BeOfType<OkObjectResult>();
        var okResult = actionResult as OkObjectResult;
        var response = okResult!.Value as ApiResponse<UsuarioResponseDto>;
        response!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Deactivate_ShouldReturnOk_WhenSuccessful()
    {
        // Arrange
        var usuarioId = Guid.NewGuid();
        var serviceResult = ServiceResult<bool>.Success(true, "Usuario desactivado exitosamente.");
        _serviceMock.Setup(s => s.DeactivateAsync(usuarioId, _clinicaId)).ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _controller.Desactivar(usuarioId);

        // Assert
        actionResult.Should().BeOfType<OkObjectResult>();
        var okResult = actionResult as OkObjectResult;
        var response = okResult!.Value as ApiResponse<bool>;
        response!.Success.Should().BeTrue();
        response.Data.Should().BeTrue();
    }

    [Fact]
    public async Task Reactivate_ShouldReturnOk_WhenSuccessful()
    {
        // Arrange
        var usuarioId = Guid.NewGuid();
        var serviceResult = ServiceResult<bool>.Success(true, "Usuario reactivado exitosamente.");
        _serviceMock.Setup(s => s.ReactivateAsync(usuarioId, _clinicaId)).ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _controller.Reactivar(usuarioId);

        // Assert
        actionResult.Should().BeOfType<OkObjectResult>();
        var okResult = actionResult as OkObjectResult;
        var response = okResult!.Value as ApiResponse<bool>;
        response!.Success.Should().BeTrue();
        response.Data.Should().BeTrue();
    }
}
