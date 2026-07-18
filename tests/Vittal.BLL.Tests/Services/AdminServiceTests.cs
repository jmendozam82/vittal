using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Vittal.BLL.Services;
using Vittal.DAL.Context;
using Vittal.DAL.Interfaces;
using Vittal.DTO.Clinica;
using Vittal.DTO.Usuario;
using Vittal.Entity;
using Vittal.Utility.Results;

namespace Vittal.BLL.Tests.Services;

public class AdminServiceTests
{
    private readonly Mock<IUsuarioRepository> _usuarioRepoMock;
    private readonly Mock<IClinicaRepository> _clinicaRepoMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<ILogger<AdminService>> _loggerMock;
    private readonly DbConnectionFactory _dbConnectionFactory;
    private readonly AdminService _service;
    private readonly Guid _clinicaId = Guid.NewGuid();
    private readonly Guid _superAdminId = Guid.NewGuid();

    public AdminServiceTests()
    {
        _usuarioRepoMock = new Mock<IUsuarioRepository>();
        _clinicaRepoMock = new Mock<IClinicaRepository>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _configMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<AdminService>>();

        _configMock.Setup(c => c["ConnectionStrings:Supabase"]).Returns("Host=localhost;Database=test;");
        _dbConnectionFactory = new DbConnectionFactory(_configMock.Object);

        _service = new AdminService(
            _dbConnectionFactory,
            _httpClientFactoryMock.Object,
            _configMock.Object,
            _loggerMock.Object,
            _usuarioRepoMock.Object,
            _clinicaRepoMock.Object);
    }

    // ── GetUsuariosByClinicaAsync ──────────────────────────────────────

    [Fact]
    public async Task GetUsuariosByClinicaAsync_ShouldReturnList_WhenUsuariosExist()
    {
        // Arrange
        var usuarios = new List<Usuario>
        {
            new() { Id = Guid.NewGuid(), ClinicaId = _clinicaId, Username = "admin1", Nombres = "Juan", Apellidos = "Pérez", Email = "juan@test.com", Activo = true, FechaCreacion = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), ClinicaId = _clinicaId, Username = "admin2", Nombres = "María", Apellidos = "García", Email = "maria@test.com", Activo = true, FechaCreacion = DateTime.UtcNow }
        };
        _usuarioRepoMock.Setup(r => r.GetAllAsync(_clinicaId)).ReturnsAsync(usuarios);

        // Act
        var result = await _service.GetUsuariosByClinicaAsync(_clinicaId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUsuariosByClinicaAsync_ShouldReturnEmptyList_WhenNoUsuarios()
    {
        // Arrange
        _usuarioRepoMock.Setup(r => r.GetAllAsync(_clinicaId)).ReturnsAsync(new List<Usuario>());

        // Act
        var result = await _service.GetUsuariosByClinicaAsync(_clinicaId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUsuariosByClinicaAsync_WithIncluirInactivos_ShouldCallCorrectMethod()
    {
        // Arrange
        _usuarioRepoMock.Setup(r => r.GetAllIncludingInactiveAsync(_clinicaId))
            .ReturnsAsync(new List<Usuario>
            {
                new() { Id = Guid.NewGuid(), ClinicaId = _clinicaId, Username = "inactive", Activo = false }
            });

        // Act
        var result = await _service.GetUsuariosByClinicaAsync(_clinicaId, incluirInactivos: true);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        _usuarioRepoMock.Verify(r => r.GetAllIncludingInactiveAsync(_clinicaId), Times.Once);
        _usuarioRepoMock.Verify(r => r.GetAllAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GetUsuariosByClinicaAsync_ShouldReturnFailure_WhenExceptionThrown()
    {
        // Arrange
        _usuarioRepoMock.Setup(r => r.GetAllAsync(_clinicaId)).ThrowsAsync(new Exception("DB error"));

        // Act
        var result = await _service.GetUsuariosByClinicaAsync(_clinicaId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("Error");
    }

    // ── ProvisionClinicaAsync ──────────────────────────────────────────

    [Fact]
    public async Task ProvisionClinicaAsync_ShouldReturnConflict_WhenNombreExiste()
    {
        // Arrange
        var dto = new ClinicaProvisionRequestDto
        {
            Nombre = "Clínica Test",
            AdminEmail = "admin@test.com",
            AdminPassword = "Pass123!",
            AdminNombres = "Juan",
            AdminApellidos = "Pérez",
            AdminUsername = "admin_test"
        };
        _clinicaRepoMock.Setup(r => r.ExistsByNameAsync("Clínica Test", It.IsAny<Guid?>())).ReturnsAsync(true);

        // Act
        var result = await _service.ProvisionClinicaAsync(dto, _superAdminId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Conflict);
        result.Message.Should().Contain("Ya existe una clínica con el nombre");
    }

    [Fact]
    public async Task ProvisionClinicaAsync_ShouldReturnFailure_WhenExceptionThrown()
    {
        // Arrange
        var dto = new ClinicaProvisionRequestDto
        {
            Nombre = "Clínica Test",
            AdminEmail = "admin@test.com",
            AdminPassword = "Pass123!",
            AdminNombres = "Juan",
            AdminApellidos = "Pérez",
            AdminUsername = "admin_test"
        };
        _clinicaRepoMock.Setup(r => r.ExistsByNameAsync("Clínica Test", It.IsAny<Guid?>())).ThrowsAsync(new Exception("DB error"));

        // Act
        var result = await _service.ProvisionClinicaAsync(dto, _superAdminId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("Error interno");
    }

    // ── CreateUsuarioAsync ─────────────────────────────────────────────

    [Fact]
    public async Task CreateUsuarioAsync_ShouldReturnConflict_WhenUsernameExists()
    {
        // Arrange
        var dto = new AdminCreateUsuarioRequestDto
        {
            ClinicaId = _clinicaId,
            Username = "existing_user",
            Nombres = "Juan",
            Apellidos = "Pérez",
            Email = "juan@test.com",
            PerfilId = Guid.NewGuid()
        };
        _usuarioRepoMock.Setup(r => r.ExistsByUsernameAsync(_clinicaId, "existing_user", It.IsAny<Guid?>())).ReturnsAsync(true);

        // Act
        var result = await _service.CreateUsuarioAsync(dto, _superAdminId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Conflict);
        result.Message.Should().Contain("nombre de usuario");
    }

    [Fact]
    public async Task CreateUsuarioAsync_ShouldReturnConflict_WhenEmailExists()
    {
        // Arrange
        var dto = new AdminCreateUsuarioRequestDto
        {
            ClinicaId = _clinicaId,
            Username = "new_user",
            Nombres = "Juan",
            Apellidos = "Pérez",
            Email = "existing@test.com",
            PerfilId = Guid.NewGuid()
        };
        _usuarioRepoMock.Setup(r => r.ExistsByUsernameAsync(_clinicaId, "new_user", It.IsAny<Guid?>())).ReturnsAsync(false);
        _usuarioRepoMock.Setup(r => r.ExistsByEmailAsync(_clinicaId, "existing@test.com", It.IsAny<Guid?>())).ReturnsAsync(true);

        // Act
        var result = await _service.CreateUsuarioAsync(dto, _superAdminId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Conflict);
        result.Message.Should().Contain("correo electrónico");
    }
}
