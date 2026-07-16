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
using Vittal.DTO.Clinica;
using Vittal.DTO.Medicamento;
using Vittal.DTO.Cirugia;
using Vittal.DTO.Perfil;
using Vittal.Utility;
using Vittal.Utility.Results;

namespace Vittal.API.Tests.Controllers;

// ═══════════════════════════════════════════════════════════════════
// ClinicasController Tests
// ═══════════════════════════════════════════════════════════════════
public class ClinicasControllerTests
{
    private readonly Mock<IClinicaService> _serviceMock;
    private readonly Mock<IAdminService> _adminServiceMock;
    private readonly Mock<ILogger<ClinicasController>> _loggerMock;
    private readonly ClinicasController _controller;
    private readonly Guid _clinicaId = Guid.NewGuid();

    public ClinicasControllerTests()
    {
        _serviceMock = new Mock<IClinicaService>();
        _adminServiceMock = new Mock<IAdminService>();
        _loggerMock = new Mock<ILogger<ClinicasController>>();
        _controller = new ClinicasController(_serviceMock.Object, _adminServiceMock.Object, _loggerMock.Object);

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
    public async Task ClinicasController_GetAll_ShouldReturnOk()
    {
        // Arrange
        var clinicas = new List<ClinicaResponseDto>
        {
            new() { Id = Guid.NewGuid(), Nombre = "Clínica Central", Activo = true },
            new() { Id = Guid.NewGuid(), Nombre = "Clínica Norte", Activo = true }
        };
        var serviceResult = ServiceResult<IEnumerable<ClinicaResponseDto>>.Success(clinicas);
        _serviceMock.Setup(s => s.GetAllAsync(false)).ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _controller.GetAll();

        // Assert
        actionResult.Should().BeOfType<OkObjectResult>();
        var okResult = actionResult as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        var response = okResult.Value as ApiResponse<IEnumerable<ClinicaResponseDto>>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task ClinicasController_Create_ShouldReturnCreated()
    {
        // Arrange
        var clinicaId = Guid.NewGuid();
        var request = new ClinicaRequestDto
        {
            Nombre = "Nueva Clínica",
            Direccion = "Calle Principal 123",
            Telefono = "555-0100",
            Email = "info@nuevaclinica.com",
            TiempoEsperaMinutos = 30
        };
        var responseDto = new ClinicaResponseDto
        {
            Id = clinicaId,
            Nombre = "Nueva Clínica",
            Direccion = "Calle Principal 123",
            Telefono = "555-0100",
            Email = "info@nuevaclinica.com",
            TiempoEsperaMinutos = 30,
            Activo = true
        };
        var serviceResult = ServiceResult<ClinicaResponseDto>.Success(responseDto, "Clínica creada exitosamente.");
        _serviceMock.Setup(s => s.CreateAsync(request)).ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _controller.Create(request);

        // Assert
        actionResult.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = actionResult as CreatedAtActionResult;
        createdResult!.StatusCode.Should().Be(201);
        createdResult.ActionName.Should().Be(nameof(ClinicasController.GetById));
        var response = createdResult.Value as ApiResponse<ClinicaResponseDto>;
        response!.Success.Should().BeTrue();
        response.Data!.Nombre.Should().Be("Nueva Clínica");
    }

    [Fact]
    public async Task ClinicasController_Create_ShouldReturnConflict_WhenDuplicate()
    {
        // Arrange
        var request = new ClinicaRequestDto
        {
            Nombre = "Clínica Existente",
            TiempoEsperaMinutos = 30
        };
        var serviceResult = ServiceResult<ClinicaResponseDto>.Failure(
            "Ya existe una clínica con ese nombre", ServiceErrorType.Conflict);
        _serviceMock.Setup(s => s.CreateAsync(request)).ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _controller.Create(request);

        // Assert
        actionResult.Should().BeOfType<ConflictObjectResult>();
        var conflictResult = actionResult as ConflictObjectResult;
        conflictResult!.StatusCode.Should().Be(409);
    }
}

// ═══════════════════════════════════════════════════════════════════
// MedicamentosController Tests
// ═══════════════════════════════════════════════════════════════════
public class MedicamentosControllerTests
{
    private readonly Mock<IMedicamentoService> _serviceMock;
    private readonly Mock<ILogger<MedicamentosController>> _loggerMock;
    private readonly MedicamentosController _controller;
    private readonly Guid _clinicaId = Guid.NewGuid();

    public MedicamentosControllerTests()
    {
        _serviceMock = new Mock<IMedicamentoService>();
        _loggerMock = new Mock<ILogger<MedicamentosController>>();
        _controller = new MedicamentosController(_serviceMock.Object, _loggerMock.Object);

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
    public async Task MedicamentosController_GetAll_ShouldReturnOk()
    {
        // Arrange
        var medicamentos = new List<MedicamentoResponseDto>
        {
            new() { Id = Guid.NewGuid(), ClinicaId = _clinicaId, Nombre = "Paracetamol", Concentracion = "500mg", Activo = true, NombreCompleto = "Paracetamol 500mg" },
            new() { Id = Guid.NewGuid(), ClinicaId = _clinicaId, Nombre = "Ibuprofeno", Concentracion = "400mg", Activo = true, NombreCompleto = "Ibuprofeno 400mg" }
        };
        var serviceResult = ServiceResult<IEnumerable<MedicamentoResponseDto>>.Success(medicamentos);
        _serviceMock.Setup(s => s.GetAllAsync(_clinicaId, false)).ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _controller.GetAll();

        // Assert
        actionResult.Should().BeOfType<OkObjectResult>();
        var okResult = actionResult as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        var response = okResult.Value as ApiResponse<IEnumerable<MedicamentoResponseDto>>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task MedicamentosController_Create_ShouldReturnCreated()
    {
        // Arrange
        var medicamentoId = Guid.NewGuid();
        var request = new MedicamentoRequestDto
        {
            Nombre = "Paracetamol",
            Concentracion = "500mg",
            UnidadMedida = "tabletas"
        };
        var responseDto = new MedicamentoResponseDto
        {
            Id = medicamentoId,
            ClinicaId = _clinicaId,
            Nombre = "Paracetamol",
            Concentracion = "500mg",
            UnidadMedida = "tabletas",
            Activo = true,
            NombreCompleto = "Paracetamol 500mg"
        };
        var serviceResult = ServiceResult<MedicamentoResponseDto>.Success(responseDto, "Medicamento creado exitosamente.");
        _serviceMock.Setup(s => s.CreateAsync(request, _clinicaId, It.IsAny<Guid>())).ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _controller.Create(request);

        // Assert
        actionResult.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = actionResult as CreatedAtActionResult;
        createdResult!.StatusCode.Should().Be(201);
        var response = createdResult.Value as ApiResponse<MedicamentoResponseDto>;
        response!.Success.Should().BeTrue();
        response.Data!.Nombre.Should().Be("Paracetamol");
    }
}

// ═══════════════════════════════════════════════════════════════════
// CirugiasController Tests
// ═══════════════════════════════════════════════════════════════════
public class CirugiasControllerTests
{
    private readonly Mock<ICirugiaService> _serviceMock;
    private readonly Mock<ILogger<CirugiasController>> _loggerMock;
    private readonly CirugiasController _controller;
    private readonly Guid _clinicaId = Guid.NewGuid();

    public CirugiasControllerTests()
    {
        _serviceMock = new Mock<ICirugiaService>();
        _loggerMock = new Mock<ILogger<CirugiasController>>();
        _controller = new CirugiasController(_serviceMock.Object, _loggerMock.Object);

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
    public async Task CirugiasController_GetAll_ShouldReturnOk()
    {
        // Arrange
        var cirugias = new List<CirugiaResponseDto>
        {
            new() { Id = Guid.NewGuid(), ClinicaId = _clinicaId, TipoCirugiaId = Guid.NewGuid(), TipoCirugiaNombre = "General", Nombre = "Apendicectomía", Activo = true, NombreCompleto = "Apendicectomía (General)" },
            new() { Id = Guid.NewGuid(), ClinicaId = _clinicaId, TipoCirugiaId = Guid.NewGuid(), TipoCirugiaNombre = "Cardíaca", Nombre = "By-Pass", Activo = true, NombreCompleto = "By-Pass (Cardíaca)" }
        };
        var serviceResult = ServiceResult<IEnumerable<CirugiaResponseDto>>.Success(cirugias);
        _serviceMock.Setup(s => s.GetAllAsync(_clinicaId, false)).ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _controller.GetAll();

        // Assert
        actionResult.Should().BeOfType<OkObjectResult>();
        var okResult = actionResult as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        var response = okResult.Value as ApiResponse<IEnumerable<CirugiaResponseDto>>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task CirugiasController_Create_ShouldReturnCreated()
    {
        // Arrange
        var cirugiaId = Guid.NewGuid();
        var tipoCirugiaId = Guid.NewGuid();
        var request = new CirugiaRequestDto
        {
            TipoCirugiaId = tipoCirugiaId,
            Nombre = "Apendicectomía",
            Descripcion = "Extirpación del apéndice"
        };
        var responseDto = new CirugiaResponseDto
        {
            Id = cirugiaId,
            ClinicaId = _clinicaId,
            TipoCirugiaId = tipoCirugiaId,
            TipoCirugiaNombre = "General",
            Nombre = "Apendicectomía",
            Descripcion = "Extirpación del apéndice",
            Activo = true,
            NombreCompleto = "Apendicectomía (General)"
        };
        var serviceResult = ServiceResult<CirugiaResponseDto>.Success(responseDto, "Cirugía creada exitosamente.");
        _serviceMock.Setup(s => s.CreateAsync(request, _clinicaId, It.IsAny<Guid>())).ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _controller.Create(request);

        // Assert
        actionResult.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = actionResult as CreatedAtActionResult;
        createdResult!.StatusCode.Should().Be(201);
        var response = createdResult.Value as ApiResponse<CirugiaResponseDto>;
        response!.Success.Should().BeTrue();
        response.Data!.Nombre.Should().Be("Apendicectomía");
    }
}

// ═══════════════════════════════════════════════════════════════════
// PerfilesController Tests
// ═══════════════════════════════════════════════════════════════════
public class PerfilesControllerTests
{
    private readonly Mock<IPerfilService> _serviceMock;
    private readonly Mock<ILogger<PerfilesController>> _loggerMock;
    private readonly PerfilesController _controller;
    private readonly Guid _clinicaId = Guid.NewGuid();

    public PerfilesControllerTests()
    {
        _serviceMock = new Mock<IPerfilService>();
        _loggerMock = new Mock<ILogger<PerfilesController>>();
        _controller = new PerfilesController(_serviceMock.Object, _loggerMock.Object);

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
    public async Task PerfilesController_GetAll_ShouldReturnOk()
    {
        // Arrange
        var perfiles = new List<PerfilResponseDto>
        {
            new() { Id = Guid.NewGuid(), Nombre = "Administrador", EsAdmin = true, Activo = true, CantidadPermisos = 15, CantidadUsuarios = 2 },
            new() { Id = Guid.NewGuid(), Nombre = "Doctor", EsAdmin = false, Activo = true, CantidadPermisos = 8, CantidadUsuarios = 5 }
        };
        var serviceResult = ServiceResult<IEnumerable<PerfilResponseDto>>.Success(perfiles);
        _serviceMock.Setup(s => s.GetAllAsync(_clinicaId, false)).ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _controller.GetAll();

        // Assert
        actionResult.Should().BeOfType<OkObjectResult>();
        var okResult = actionResult as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        var response = okResult.Value as ApiResponse<IEnumerable<PerfilResponseDto>>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data.Should().HaveCount(2);
    }
}
