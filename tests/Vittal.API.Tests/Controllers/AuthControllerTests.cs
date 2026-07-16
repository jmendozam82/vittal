using Xunit;
using Moq;
using Moq.Protected;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Vittal.API.Controllers;
using Vittal.API.Models;
using Vittal.BLL.Interfaces;
using Vittal.DTO.Auth;
using Vittal.DTO.Usuario;
using Vittal.Utility;
using Vittal.Utility.Results;

namespace Vittal.API.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<IUsuarioService> _usuarioServiceMock;
    private readonly Mock<IPermisoService> _permisoServiceMock;
    private readonly Mock<ILogger<AuthController>> _loggerMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _configurationMock = new Mock<IConfiguration>();
        _usuarioServiceMock = new Mock<IUsuarioService>();
        _permisoServiceMock = new Mock<IPermisoService>();
        _loggerMock = new Mock<ILogger<AuthController>>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient("SupabaseAuth")).Returns(httpClient);

        _configurationMock.Setup(c => c["Supabase:Url"]).Returns("https://test.supabase.co");
        _configurationMock.Setup(c => c["Supabase:AnonKey"]).Returns("test-anon-key");

        _controller = new AuthController(
            _httpClientFactoryMock.Object,
            _configurationMock.Object,
            _usuarioServiceMock.Object,
            _permisoServiceMock.Object,
            _loggerMock.Object);

        // Setup authenticated user for endpoints that need it
        var claims = new List<Claim>
        {
            new("app_clinica_id", Guid.NewGuid().ToString()),
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

    private void SetupHttpClientResponse(HttpResponseMessage response)
    {
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    [Fact]
    public async Task Login_ShouldReturnOk_WhenCredentialsValid()
    {
        // Arrange
        var authUserId = Guid.NewGuid();
        var clinicaId = Guid.NewGuid();
        var request = new LoginRequestDto
        {
            Email = "admin@test.com",
            Password = "Secret123"
        };

        var supabaseResponse = new SupabaseAuthResponse
        {
            AccessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.test",
            RefreshToken = "refresh-token-123",
            ExpiresIn = 3600,
            User = new SupabaseUser { Id = authUserId.ToString() }
        };

        SetupHttpClientResponse(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(
                JsonSerializer.Serialize(supabaseResponse),
                Encoding.UTF8,
                "application/json")
        });

        var usuarioResult = ServiceResult<UsuarioResponseDto>.Success(new UsuarioResponseDto
        {
            UsuarioId = Guid.NewGuid(),
            ClinicaId = clinicaId,
            Email = "admin@test.com",
            Nombres = "Admin",
            Apellidos = "Test",
            PerfilNombre = "Administrador",
            EsAdmin = true,
            EsSuperAdmin = false,
            PerfilId = Guid.NewGuid()
        });
        _usuarioServiceMock.Setup(s => s.GetByAuthUserIdAsync(authUserId)).ReturnsAsync(usuarioResult);

        // Act
        var actionResult = await _controller.Login(request);

        // Assert
        actionResult.Should().BeOfType<OkObjectResult>();
        var okResult = actionResult as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        var response = okResult.Value as ApiResponse<LoginResponseDto>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data!.AccessToken.Should().NotBeEmpty();
        response.Data.Email.Should().Be("admin@test.com");
    }

    [Fact]
    public async Task Login_ShouldReturnBadRequest_WhenCredentialsInvalid()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Email = "wrong@test.com",
            Password = "WrongPassword"
        };

        SetupHttpClientResponse(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.Unauthorized,
            Content = new StringContent(
                JsonSerializer.Serialize(new { error = "Invalid login credentials" }),
                Encoding.UTF8,
                "application/json")
        });

        // Act
        var actionResult = await _controller.Login(request);

        // Assert
        actionResult.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = actionResult as BadRequestObjectResult;
        badRequest!.StatusCode.Should().Be(400);
        var response = badRequest.Value as ApiResponse;
        response!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Login_ShouldReturnBadRequest_WhenModelInvalid()
    {
        // Arrange — empty request with no email/password
        var request = new LoginRequestDto
        {
            Email = "",
            Password = ""
        };

        SetupHttpClientResponse(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.BadRequest,
            Content = new StringContent(
                JsonSerializer.Serialize(new { error = "Email and password are required" }),
                Encoding.UTF8,
                "application/json")
        });

        // Act
        var actionResult = await _controller.Login(request);

        // Assert
        actionResult.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Refresh_ShouldReturnOk_WhenTokenValid()
    {
        // Arrange
        var request = new RefreshRequestDto
        {
            RefreshToken = "valid-refresh-token"
        };

        var supabaseResponse = new SupabaseAuthResponse
        {
            AccessToken = "new-access-token",
            RefreshToken = "new-refresh-token",
            ExpiresIn = 3600
        };

        SetupHttpClientResponse(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(
                JsonSerializer.Serialize(supabaseResponse),
                Encoding.UTF8,
                "application/json")
        });

        // Act
        var actionResult = await _controller.Refresh(request);

        // Assert
        actionResult.Should().BeOfType<OkObjectResult>();
        var okResult = actionResult as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        var response = okResult.Value as ApiResponse<SupabaseAuthResponse>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data!.AccessToken.Should().Be("new-access-token");
    }

    [Fact]
    public async Task Refresh_ShouldReturnBadRequest_WhenTokenInvalid()
    {
        // Arrange
        var request = new RefreshRequestDto
        {
            RefreshToken = "invalid-refresh-token"
        };

        SetupHttpClientResponse(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.Unauthorized,
            Content = new StringContent(
                JsonSerializer.Serialize(new { error = "Invalid refresh token" }),
                Encoding.UTF8,
                "application/json")
        });

        // Act
        var actionResult = await _controller.Refresh(request);

        // Assert
        actionResult.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = actionResult as BadRequestObjectResult;
        badRequest!.StatusCode.Should().Be(400);
        var response = badRequest.Value as ApiResponse;
        response!.Success.Should().BeFalse();
    }

    [Fact]
    public void Logout_ShouldReturnOk()
    {
        // Act
        var actionResult = _controller.Logout();

        // Assert
        actionResult.Should().BeOfType<OkObjectResult>();
        var okResult = actionResult as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        var response = okResult.Value as ApiResponse;
        response!.Success.Should().BeTrue();
        response.Message.Should().Contain("Sesión cerrada");
    }
}
