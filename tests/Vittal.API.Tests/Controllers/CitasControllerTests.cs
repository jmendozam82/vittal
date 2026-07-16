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
using Vittal.DTO.Cita;
using Vittal.Utility;
using Vittal.Utility.Results;

namespace Vittal.API.Tests.Controllers;

public class CitasControllerTests
{
    private readonly Mock<ICitaService> _serviceMock;
    private readonly Mock<ILogger<CitasController>> _loggerMock;
    private readonly CitasController _controller;
    private readonly Guid _clinicaId = Guid.NewGuid();
    private readonly Guid _usuarioId = Guid.NewGuid();

    public CitasControllerTests()
    {
        _serviceMock = new Mock<ICitaService>();
        _loggerMock = new Mock<ILogger<CitasController>>();
        _controller = new CitasController(_serviceMock.Object, _loggerMock.Object);

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
    public async Task GetAll_ShouldReturnOk_WhenCitasExist()
    {
        // Arrange
        var citas = new List<CitaResponseDto>
        {
            new() { Id = Guid.NewGuid(), ClinicaId = _clinicaId, PacienteId = Guid.NewGuid(), DoctorId = Guid.NewGuid(), FechaCita = DateOnly.FromDateTime(DateTime.Today), HoraCita = new TimeOnly(9, 0), Estado = "agendada", PacienteNombre = "Juan Pérez", DoctorNombre = "Dr. García" },
            new() { Id = Guid.NewGuid(), ClinicaId = _clinicaId, PacienteId = Guid.NewGuid(), DoctorId = Guid.NewGuid(), FechaCita = DateOnly.FromDateTime(DateTime.Today), HoraCita = new TimeOnly(10, 0), Estado = "agendada", PacienteNombre = "María López", DoctorNombre = "Dr. García" }
        };
        var serviceResult = ServiceResult<IEnumerable<CitaResponseDto>>.Success(citas);
        _serviceMock.Setup(s => s.GetAllAsync(_clinicaId)).ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _controller.GetAll();

        // Assert
        actionResult.Should().BeOfType<OkObjectResult>();
        var okResult = actionResult as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        var response = okResult.Value as ApiResponse<IEnumerable<CitaResponseDto>>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetById_ShouldReturnOk_WhenCitaExists()
    {
        // Arrange
        var citaId = Guid.NewGuid();
        var cita = new CitaResponseDto
        {
            Id = citaId,
            ClinicaId = _clinicaId,
            PacienteId = Guid.NewGuid(),
            DoctorId = Guid.NewGuid(),
            FechaCita = DateOnly.FromDateTime(DateTime.Today),
            HoraCita = new TimeOnly(9, 0),
            Estado = "agendada",
            PacienteNombre = "Juan Pérez",
            DoctorNombre = "Dr. García"
        };
        var serviceResult = ServiceResult<CitaResponseDto>.Success(cita);
        _serviceMock.Setup(s => s.GetByIdAsync(_clinicaId, citaId)).ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _controller.GetById(citaId);

        // Assert
        actionResult.Should().BeOfType<OkObjectResult>();
        var okResult = actionResult as OkObjectResult;
        var response = okResult!.Value as ApiResponse<CitaResponseDto>;
        response!.Success.Should().BeTrue();
        response.Data!.Id.Should().Be(citaId);
    }

    [Fact]
    public async Task Create_ShouldReturnCreated_WhenValidData()
    {
        // Arrange
        var citaId = Guid.NewGuid();
        var request = new CitaRequestDto
        {
            PacienteId = Guid.NewGuid(),
            DoctorId = Guid.NewGuid(),
            FechaCita = DateOnly.FromDateTime(DateTime.Today),
            HoraCita = new TimeOnly(9, 0),
            Estado = "agendada"
        };
        var responseDto = new CitaResponseDto
        {
            Id = citaId,
            ClinicaId = _clinicaId,
            PacienteId = request.PacienteId,
            DoctorId = request.DoctorId,
            FechaCita = request.FechaCita,
            HoraCita = request.HoraCita,
            Estado = "agendada",
            PacienteNombre = "Juan Pérez",
            DoctorNombre = "Dr. García"
        };
        var serviceResult = ServiceResult<CitaResponseDto>.Success(responseDto, "Cita creada exitosamente.");
        _serviceMock.Setup(s => s.CreateAsync(request, _clinicaId, _usuarioId)).ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _controller.Create(request);

        // Assert
        actionResult.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = actionResult as CreatedAtActionResult;
        createdResult!.StatusCode.Should().Be(201);
        createdResult.ActionName.Should().Be(nameof(CitasController.GetById));
        createdResult.RouteValues!["id"].Should().Be(citaId);
        var response = createdResult.Value as ApiResponse<CitaResponseDto>;
        response!.Success.Should().BeTrue();
        response.Data!.Id.Should().Be(citaId);
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenValidationError()
    {
        // Arrange
        var request = new CitaRequestDto
        {
            PacienteId = Guid.NewGuid(),
            DoctorId = Guid.NewGuid(),
            FechaCita = DateOnly.FromDateTime(DateTime.Today),
            HoraCita = new TimeOnly(9, 0),
            Estado = "agendada"
        };
        var serviceResult = ServiceResult<CitaResponseDto>.Failure(
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
        var citaId = Guid.NewGuid();
        var request = new CitaRequestDto
        {
            PacienteId = Guid.NewGuid(),
            DoctorId = Guid.NewGuid(),
            FechaCita = DateOnly.FromDateTime(DateTime.Today),
            HoraCita = new TimeOnly(10, 0),
            Estado = "en_atencion"
        };
        var responseDto = new CitaResponseDto
        {
            Id = citaId,
            ClinicaId = _clinicaId,
            PacienteId = request.PacienteId,
            DoctorId = request.DoctorId,
            FechaCita = request.FechaCita,
            HoraCita = request.HoraCita,
            Estado = "en_atencion",
            PacienteNombre = "Juan Pérez",
            DoctorNombre = "Dr. García"
        };
        var serviceResult = ServiceResult<CitaResponseDto>.Success(responseDto);
        _serviceMock.Setup(s => s.UpdateAsync(citaId, request, _clinicaId)).ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _controller.Update(citaId, request);

        // Assert
        actionResult.Should().BeOfType<OkObjectResult>();
        var okResult = actionResult as OkObjectResult;
        var response = okResult!.Value as ApiResponse<CitaResponseDto>;
        response!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Desactivar_ShouldReturnOk_WhenSuccessful()
    {
        // Arrange
        var citaId = Guid.NewGuid();
        var serviceResult = ServiceResult<bool>.Success(true, "Cita desactivada exitosamente.");
        _serviceMock.Setup(s => s.DeactivateAsync(_clinicaId, citaId)).ReturnsAsync(serviceResult);

        // Act
        var actionResult = await _controller.Desactivar(citaId);

        // Assert
        actionResult.Should().BeOfType<OkObjectResult>();
        var okResult = actionResult as OkObjectResult;
        var response = okResult!.Value as ApiResponse<bool>;
        response!.Success.Should().BeTrue();
        response.Data.Should().BeTrue();
    }
}
