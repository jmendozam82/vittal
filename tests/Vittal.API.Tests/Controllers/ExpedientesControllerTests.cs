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
using Vittal.DTO.Expediente;
using Vittal.DTO.Paciente;
using Vittal.Utility;
using Vittal.Utility.Results;

namespace Vittal.API.Tests.Controllers;

public class ExpedientesControllerTests
{
    private readonly Mock<IExpedienteService> _serviceMock;
    private readonly Mock<IPacienteService> _pacienteServiceMock;
    private readonly Mock<ILogger<ExpedientesController>> _loggerMock;
    private readonly ExpedientesController _controller;
    private readonly Guid _clinicaId = Guid.NewGuid();
    private readonly Guid _usuarioId = Guid.NewGuid();

    public ExpedientesControllerTests()
    {
        _serviceMock = new Mock<IExpedienteService>();
        _pacienteServiceMock = new Mock<IPacienteService>();
        _loggerMock = new Mock<ILogger<ExpedientesController>>();
        _controller = new ExpedientesController(_serviceMock.Object, _pacienteServiceMock.Object, _loggerMock.Object);

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
    public async Task GetAll_ShouldReturnOk_WhenExpedientesExist()
    {
        // Arrange
        var expedientes = new List<ExpedienteResponseDto>
        {
            new() { Id = Guid.NewGuid(), ClinicaId = _clinicaId, PacienteId = Guid.NewGuid(), DoctorId = Guid.NewGuid(), PacienteNombre = "Juan Pérez", DoctorNombre = "Dr. García", Activo = true },
            new() { Id = Guid.NewGuid(), ClinicaId = _clinicaId, PacienteId = Guid.NewGuid(), DoctorId = Guid.NewGuid(), PacienteNombre = "María López", DoctorNombre = "Dr. García", Activo = true }
        };
        var serviceResult = ServiceResult<IEnumerable<ExpedienteResponseDto>>.Success(expedientes);
        _serviceMock.Setup(s => s.GetAllAsync(_clinicaId, It.IsAny<Guid?>())).ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _controller.GetAll();

        // Assert
        actionResult.Should().BeOfType<OkObjectResult>();
        var okResult = actionResult as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        var response = okResult.Value as ApiResponse<IEnumerable<ExpedienteResponseDto>>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetById_ShouldReturnOk_WhenExpedienteExists()
    {
        // Arrange
        var expedienteId = Guid.NewGuid();
        var expediente = new ExpedienteResponseDto
        {
            Id = expedienteId,
            ClinicaId = _clinicaId,
            PacienteId = Guid.NewGuid(),
            DoctorId = Guid.NewGuid(),
            PacienteNombre = "Juan Pérez",
            DoctorNombre = "Dr. García",
            Activo = true
        };
        var serviceResult = ServiceResult<ExpedienteResponseDto>.Success(expediente);
        _serviceMock.Setup(s => s.GetByIdAsync(_clinicaId, expedienteId, It.IsAny<Guid?>())).ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _controller.GetById(expedienteId);

        // Assert
        actionResult.Should().BeOfType<OkObjectResult>();
        var okResult = actionResult as OkObjectResult;
        var response = okResult!.Value as ApiResponse<ExpedienteResponseDto>;
        response!.Success.Should().BeTrue();
        response.Data!.Id.Should().Be(expedienteId);
    }

    [Fact]
    public async Task Create_ShouldReturnCreated_WhenValidData()
    {
        // Arrange
        var expedienteId = Guid.NewGuid();
        var pacienteId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var request = new ExpedienteRequestDto
        {
            PacienteId = pacienteId,
            DoctorId = doctorId,
            NotasGenerales = "Expediente inicial del paciente"
        };
        var responseDto = new ExpedienteResponseDto
        {
            Id = expedienteId,
            ClinicaId = _clinicaId,
            PacienteId = pacienteId,
            DoctorId = doctorId,
            NotasGenerales = "Expediente inicial del paciente",
            PacienteNombre = "Juan Pérez",
            DoctorNombre = "Dr. García",
            Activo = true
        };
        var serviceResult = ServiceResult<ExpedienteResponseDto>.Success(responseDto, "Expediente creado exitosamente.");
        _serviceMock.Setup(s => s.CreateAsync(request, _clinicaId, _usuarioId, It.IsAny<Guid?>())).ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _controller.Create(request);

        // Assert
        actionResult.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = actionResult as CreatedAtActionResult;
        createdResult!.StatusCode.Should().Be(201);
        createdResult.ActionName.Should().Be(nameof(ExpedientesController.GetById));
        createdResult.RouteValues!["id"].Should().Be(expedienteId);
        var response = createdResult.Value as ApiResponse<ExpedienteResponseDto>;
        response!.Success.Should().BeTrue();
        response.Data!.Id.Should().Be(expedienteId);
    }

    [Fact]
    public async Task GetByPaciente_ShouldReturnOk_WhenExpedienteExists()
    {
        // Arrange
        var pacienteId = Guid.NewGuid();
        var expediente = new ExpedienteResponseDto
        {
            Id = Guid.NewGuid(),
            ClinicaId = _clinicaId,
            PacienteId = pacienteId,
            DoctorId = Guid.NewGuid(),
            PacienteNombre = "Juan Pérez",
            DoctorNombre = "Dr. García",
            Activo = true
        };
        var serviceResult = ServiceResult<ExpedienteResponseDto>.Success(expediente);
        _serviceMock.Setup(s => s.GetByPacienteIdAsync(_clinicaId, pacienteId)).ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _controller.GetByPaciente(pacienteId);

        // Assert
        actionResult.Should().BeOfType<OkObjectResult>();
        var okResult = actionResult as OkObjectResult;
        var response = okResult!.Value as ApiResponse<ExpedienteResponseDto>;
        response!.Success.Should().BeTrue();
        response.Data!.PacienteId.Should().Be(pacienteId);
    }

    [Fact]
    public async Task Desactivar_ShouldReturnOk_WhenSuccessful()
    {
        // Arrange
        var expedienteId = Guid.NewGuid();
        var serviceResult = ServiceResult<bool>.Success(true, "Expediente desactivado exitosamente.");
        _serviceMock.Setup(s => s.DeactivateAsync(_clinicaId, expedienteId)).ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _controller.Desactivar(expedienteId);

        // Assert
        actionResult.Should().BeOfType<OkObjectResult>();
        var okResult = actionResult as OkObjectResult;
        var response = okResult!.Value as ApiResponse<bool>;
        response!.Success.Should().BeTrue();
        response.Data.Should().BeTrue();
    }

    [Fact]
    public async Task GetPatientInfo_ShouldReturnOk_WhenPacienteExists()
    {
        // Arrange
        var expedienteId = Guid.NewGuid();
        var pacienteId = Guid.NewGuid();
        var expediente = new ExpedienteResponseDto
        {
            Id = expedienteId,
            ClinicaId = _clinicaId,
            PacienteId = pacienteId,
            DoctorId = Guid.NewGuid(),
            PacienteNombre = "Juan Pérez",
            DoctorNombre = "Dr. García",
            Activo = true
        };
        var expedienteResult = ServiceResult<ExpedienteResponseDto>.Success(expediente);
        _serviceMock.Setup(s => s.GetByIdAsync(_clinicaId, expedienteId, It.IsAny<Guid?>())).ReturnsAsync(expedienteResult);

        var paciente = new PacienteResponseDto
        {
            Id = pacienteId,
            ClinicaId = _clinicaId,
            DoctorId = Guid.NewGuid(),
            NombreCompleto = "Juan Pérez",
            Sexo = "M",
            TipoDocumentoIdentificacion = "CC",
            NumeroDocumentoIdentificacion = "12345678",
            Activo = true
        };
        var pacienteResult = ServiceResult<PacienteResponseDto>.Success(paciente);
        _pacienteServiceMock.Setup(s => s.GetByIdAsync(pacienteId, _clinicaId)).ReturnsAsync(pacienteResult);

        // Act
        var actionResult = await _controller.GetPatientInfo(expedienteId);

        // Assert
        actionResult.Should().BeOfType<OkObjectResult>();
        var okResult = actionResult as OkObjectResult;
        var response = okResult!.Value as ApiResponse<PacienteResponseDto>;
        response!.Success.Should().BeTrue();
        response.Data!.Id.Should().Be(pacienteId);
        response.Data.Sexo.Should().Be("M");
    }
}
