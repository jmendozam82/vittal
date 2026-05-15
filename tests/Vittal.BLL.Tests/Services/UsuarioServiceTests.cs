using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Vittal.BLL.Interfaces;
using Vittal.BLL.Services;
using Vittal.DAL.Interfaces;
using Vittal.DTO.Usuario;
using Vittal.Entity.Models;
using Vittal.Utility.Results;

namespace Vittal.BLL.Tests.Services;

public class UsuarioServiceTests
{
    private readonly Mock<IUsuarioRepository> _repoMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<UsuarioService>> _loggerMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly UsuarioService _service;
    private readonly Guid _clinicaId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _perfilId = Guid.NewGuid();

    public UsuarioServiceTests()
    {
        _repoMock = new Mock<IUsuarioRepository>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<UsuarioService>>();
        _configMock = new Mock<IConfiguration>();

        // Setup configuration for Supabase
        _configMock.Setup(c => c["Supabase:Url"]).Returns("https://test.supabase.co");
        _configMock.Setup(c => c["Supabase:ServiceRoleKey"]).Returns("test-service-role-key");

        // Setup default HTTP client
        var httpClient = new HttpClient();
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        _service = new UsuarioService(
            _repoMock.Object,
            _httpClientFactoryMock.Object,
            _loggerMock.Object,
            _configMock.Object
        );
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnList_WhenUsuariosExist()
    {
        // Arrange
        var usuarios = new List<Usuario>
        {
            new() { Id = Guid.NewGuid(), ClinicaId = _clinicaId, Username = "jperez", Nombres = "Juan", Apellidos = "Pérez", Email = "juan@test.com", PerfilId = _perfilId, Activo = true, FechaCreacion = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), ClinicaId = _clinicaId, Username = "mgarcia", Nombres = "María", Apellidos = "García", Email = "maria@test.com", PerfilId = _perfilId, Activo = true, FechaCreacion = DateTime.UtcNow }
        };
        _repoMock.Setup(r => r.GetAllAsync(_clinicaId)).ReturnsAsync(usuarios);

        // Act
        var result = await _service.GetAllAsync(_clinicaId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnEmptyList_WhenNoUsuarios()
    {
        // Arrange
        _repoMock.Setup(r => r.GetAllAsync(_clinicaId)).ReturnsAsync(new List<Usuario>());

        // Act
        var result = await _service.GetAllAsync(_clinicaId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnUsuario_WhenExists()
    {
        // Arrange
        var usuarioId = Guid.NewGuid();
        var usuario = new Usuario
        {
            Id = usuarioId,
            ClinicaId = _clinicaId,
            Username = "jperez",
            Nombres = "Juan",
            Apellidos = "Pérez",
            Email = "juan@test.com",
            PerfilId = _perfilId,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        _repoMock.Setup(r => r.GetByIdAsync(usuarioId, _clinicaId)).ReturnsAsync(usuario);

        // Act
        var result = await _service.GetByIdAsync(usuarioId, _clinicaId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Username.Should().Be("jperez");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNotFound_WhenNotExists()
    {
        // Arrange
        var usuarioId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(usuarioId, _clinicaId)).ReturnsAsync((Usuario?)null);

        // Act
        var result = await _service.GetByIdAsync(usuarioId, _clinicaId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
    }

    [Fact]
    public async Task GetByAuthUserIdAsync_ShouldReturnUsuario_WhenExists()
    {
        // Arrange
        var authUserId = Guid.NewGuid();
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            ClinicaId = _clinicaId,
            AuthUserId = authUserId,
            Username = "jperez",
            Nombres = "Juan",
            Apellidos = "Pérez",
            Email = "juan@test.com",
            PerfilId = _perfilId,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        _repoMock.Setup(r => r.GetByAuthUserIdAsync(authUserId)).ReturnsAsync(usuario);

        // Act
        var result = await _service.GetByAuthUserIdAsync(authUserId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Username.Should().Be("jperez");
    }

    [Fact]
    public async Task GetByAuthUserIdAsync_ShouldReturnNotFound_WhenNotExists()
    {
        // Arrange
        var authUserId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByAuthUserIdAsync(authUserId)).ReturnsAsync((Usuario?)null);

        // Act
        var result = await _service.GetByAuthUserIdAsync(authUserId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnConflict_WhenUsernameExists()
    {
        // Arrange
        var request = new UsuarioRequestDto
        {
            Username = "jperez",
            Nombres = "Juan",
            Apellidos = "Pérez",
            Email = "juan@test.com",
            PerfilId = _perfilId
        };
        _repoMock.Setup(r => r.ExistsByUsernameAsync(_clinicaId, request.Username, null)).ReturnsAsync(true);

        // Act
        var result = await _service.CreateAsync(request, _clinicaId, _userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Conflict);
        result.Message.Should().Contain("nombre de usuario");
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnConflict_WhenEmailExists()
    {
        // Arrange
        var request = new UsuarioRequestDto
        {
            Username = "jperez",
            Nombres = "Juan",
            Apellidos = "Pérez",
            Email = "juan@test.com",
            PerfilId = _perfilId
        };
        _repoMock.Setup(r => r.ExistsByUsernameAsync(_clinicaId, request.Username, null)).ReturnsAsync(false);
        _repoMock.Setup(r => r.ExistsByEmailAsync(_clinicaId, request.Email, null)).ReturnsAsync(true);

        // Act
        var result = await _service.CreateAsync(request, _clinicaId, _userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Conflict);
        result.Message.Should().Contain("correo");
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenSupabaseAuthFails()
    {
        // Arrange - Setup with a failing HTTP client by providing an invalid URL
        var request = new UsuarioRequestDto
        {
            Username = "jperez",
            Nombres = "Juan",
            Apellidos = "Pérez",
            Email = "juan@test.com",
            Password = "Password123!",
            PerfilId = _perfilId
        };

        // Override config to point to invalid URL that will throw
        _configMock.Setup(c => c["Supabase:Url"]).Returns("https://invalid-url-that-will-fail.supabase.co");

        _repoMock.Setup(r => r.ExistsByUsernameAsync(_clinicaId, request.Username, null)).ReturnsAsync(false);
        _repoMock.Setup(r => r.ExistsByEmailAsync(_clinicaId, request.Email, null)).ReturnsAsync(false);

        // Act
        var result = await _service.CreateAsync(request, _clinicaId, _userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task DeactivateAsync_ShouldReturnSuccess_WhenNoActiveDependencies()
    {
        // Arrange
        var usuarioId = Guid.NewGuid();
        var usuario = new Usuario
        {
            Id = usuarioId,
            ClinicaId = _clinicaId,
            Username = "jperez",
            Nombres = "Juan",
            Apellidos = "Pérez",
            Email = "juan@test.com",
            PerfilId = _perfilId,
            EsDoctor = false,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        _repoMock.Setup(r => r.GetByIdAsync(usuarioId, _clinicaId)).ReturnsAsync(usuario);
        _repoMock.Setup(r => r.DeactivateAsync(usuarioId, _clinicaId)).ReturnsAsync(true);

        // Act
        var result = await _service.DeactivateAsync(usuarioId, _clinicaId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateAsync_ShouldReturnNotFound_WhenNotExists()
    {
        // Arrange
        var usuarioId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(usuarioId, _clinicaId)).ReturnsAsync((Usuario?)null);

        // Act
        var result = await _service.DeactivateAsync(usuarioId, _clinicaId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
    }

    [Fact]
    public async Task ReactivateAsync_ShouldReturnSuccess_WhenInactivo()
    {
        // Arrange
        var usuarioId = Guid.NewGuid();
        var usuario = new Usuario
        {
            Id = usuarioId,
            ClinicaId = _clinicaId,
            Username = "jperez",
            Nombres = "Juan",
            Apellidos = "Pérez",
            Email = "juan@test.com",
            PerfilId = _perfilId,
            Activo = false,
            FechaCreacion = DateTime.UtcNow
        };
        _repoMock.Setup(r => r.GetByIdAsync(usuarioId, _clinicaId)).ReturnsAsync(usuario);
        _repoMock.Setup(r => r.ReactivateAsync(usuarioId, _clinicaId)).ReturnsAsync(true);

        // Act
        var result = await _service.ReactivateAsync(usuarioId, _clinicaId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task ReactivateAsync_ShouldReturnValidation_WhenAlreadyActivo()
    {
        // Arrange
        var usuarioId = Guid.NewGuid();
        var usuario = new Usuario
        {
            Id = usuarioId,
            ClinicaId = _clinicaId,
            Username = "jperez",
            Nombres = "Juan",
            Apellidos = "Pérez",
            Email = "juan@test.com",
            PerfilId = _perfilId,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        _repoMock.Setup(r => r.GetByIdAsync(usuarioId, _clinicaId)).ReturnsAsync(usuario);

        // Act
        var result = await _service.ReactivateAsync(usuarioId, _clinicaId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Validation);
    }

    [Fact]
    public async Task GetDoctoresAsync_ShouldReturnDoctorsList()
    {
        // Arrange
        var doctores = new List<Usuario>
        {
            new() { Id = Guid.NewGuid(), ClinicaId = _clinicaId, Username = "dr1", Nombres = "Dr. Juan", Apellidos = "Pérez", Email = "drjuan@test.com", PerfilId = _perfilId, EsDoctor = true, Activo = true, FechaCreacion = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), ClinicaId = _clinicaId, Username = "dr2", Nombres = "Dra. María", Apellidos = "García", Email = "dramaria@test.com", PerfilId = _perfilId, EsDoctor = true, Activo = true, FechaCreacion = DateTime.UtcNow }
        };
        _repoMock.Setup(r => r.GetDoctoresAsync(_clinicaId)).ReturnsAsync(doctores);

        // Act
        var result = await _service.GetDoctoresAsync(_clinicaId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data!.All(u => u.EsDoctor).Should().BeTrue();
    }
}
