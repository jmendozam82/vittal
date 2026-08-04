using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Vittal.BLL.Services;
using Vittal.DAL.Interfaces;
using Vittal.Entity;
using Vittal.Utility.Results;

namespace Vittal.BLL.Tests.Services;

public class LineaTiempoServiceTests
{
    private readonly Mock<ILineaTiempoRepository> _repositoryMock;
    private readonly Mock<ICitaRepository> _citaRepositoryMock;
    private readonly Mock<ILogger<LineaTiempoService>> _loggerMock;
    private readonly LineaTiempoService _service;
    private readonly Guid _clinicaId = Guid.NewGuid();
    private readonly Guid _usuarioId = Guid.NewGuid();

    public LineaTiempoServiceTests()
    {
        _repositoryMock = new Mock<ILineaTiempoRepository>();
        _citaRepositoryMock = new Mock<ICitaRepository>();
        _loggerMock = new Mock<ILogger<LineaTiempoService>>();

        _service = new LineaTiempoService(
            _repositoryMock.Object,
            _citaRepositoryMock.Object,
            _loggerMock.Object);
    }

    // ── GetTimelineByCitaAsync ─────────────────────────────────────────

    [Fact]
    public async Task GetTimelineByCitaAsync_ShouldReturnSteps_WhenCitaExists()
    {
        // Arrange
        var citaId = Guid.NewGuid();
        var pasos = new List<LineaTiempo>
        {
            new() { Id = Guid.NewGuid(), ClinicaId = _clinicaId, CitaId = citaId, PacienteId = Guid.NewGuid(), NombrePaso = "Llegada", Orden = 1, Estado = "completado", HoraLlegada = new TimeSpan(9, 0, 0), HoraSalida = new TimeSpan(9, 5, 0), PacienteNombre = "Juan Pérez" },
            new() { Id = Guid.NewGuid(), ClinicaId = _clinicaId, CitaId = citaId, PacienteId = Guid.NewGuid(), NombrePaso = "Consulta", Orden = 2, Estado = "en_sala", HoraLlegada = new TimeSpan(9, 5, 0), PacienteNombre = "Juan Pérez" }
        };
        _repositoryMock.Setup(r => r.GetByCitaIdAsync(_clinicaId, citaId)).ReturnsAsync(pasos);

        // Act
        var result = await _service.GetTimelineByCitaAsync(_clinicaId, citaId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data!.First().NombrePaso.Should().Be("Llegada");
        result.Data!.Last().Estado.Should().Be("en_sala");
    }

    [Fact]
    public async Task GetTimelineByCitaAsync_ShouldReturnEmpty_WhenNoSteps()
    {
        // Arrange
        var citaId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByCitaIdAsync(_clinicaId, citaId))
            .ReturnsAsync(new List<LineaTiempo>());

        // Act
        var result = await _service.GetTimelineByCitaAsync(_clinicaId, citaId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    // ── GetTimelineDelDiaAsync ─────────────────────────────────────────

    [Fact]
    public async Task GetTimelineDelDiaAsync_ShouldReturnTimeline_WhenDataExists()
    {
        // Arrange
        var fecha = DateTime.UtcNow.Date;
        var doctorId = Guid.NewGuid();
        var pasos = new List<LineaTiempo>
        {
            new() { Id = Guid.NewGuid(), ClinicaId = _clinicaId, CitaId = Guid.NewGuid(), PacienteId = Guid.NewGuid(), NombrePaso = "Consulta", Orden = 3, Estado = "completado", PacienteNombre = "Ana López", SalaNombre = "Sala 1" }
        };
        _repositoryMock.Setup(r => r.GetByClinicaAndDateAsync(_clinicaId, doctorId, fecha)).ReturnsAsync(pasos);

        // Act
        var result = await _service.GetTimelineDelDiaAsync(_clinicaId, doctorId, fecha);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data!.First().NombrePaso.Should().Be("Consulta");
    }

    // ── IniciarPasoAsync ───────────────────────────────────────────────

    [Fact]
    public async Task IniciarPasoAsync_ShouldReturnSuccess_WhenPasoIsPendiente()
    {
        // Arrange
        var pasoId = Guid.NewGuid();
        var paso = new LineaTiempo
        {
            Id = pasoId,
            ClinicaId = _clinicaId,
            CitaId = Guid.NewGuid(),
            PacienteId = Guid.NewGuid(),
            NombrePaso = "Llegada",
            Orden = 1,
            Estado = "pendiente"
        };
        _repositoryMock.Setup(r => r.GetByIdAsync(_clinicaId, pasoId)).ReturnsAsync(paso);
        _repositoryMock.Setup(r => r.UpdateEstadoAsync(_clinicaId, pasoId, "en_sala", It.IsAny<TimeSpan?>())).ReturnsAsync(true);

        // Act
        var result = await _service.IniciarPasoAsync(_clinicaId, pasoId, _usuarioId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Estado.Should().Be("en_sala");
        result.Data!.HoraLlegada.Should().NotBeNull();
    }

    [Fact]
    public async Task IniciarPasoAsync_ShouldReturnNotFound_WhenPasoDoesNotExist()
    {
        // Arrange
        var pasoId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(_clinicaId, pasoId)).ReturnsAsync((LineaTiempo?)null);

        // Act
        var result = await _service.IniciarPasoAsync(_clinicaId, pasoId, _usuarioId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
    }

    [Fact]
    public async Task IniciarPasoAsync_ShouldReturnValidation_WhenPasoIsNotPendiente()
    {
        // Arrange
        var pasoId = Guid.NewGuid();
        var paso = new LineaTiempo
        {
            Id = pasoId,
            ClinicaId = _clinicaId,
            CitaId = Guid.NewGuid(),
            PacienteId = Guid.NewGuid(),
            NombrePaso = "Consulta",
            Orden = 2,
            Estado = "en_sala"
        };
        _repositoryMock.Setup(r => r.GetByIdAsync(_clinicaId, pasoId)).ReturnsAsync(paso);

        // Act
        var result = await _service.IniciarPasoAsync(_clinicaId, pasoId, _usuarioId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Validation);
        result.Message.Should().Contain("Solo se pueden iniciar pasos pendientes");
    }

    // ── FinalizarPasoAsync ─────────────────────────────────────────────

    [Fact]
    public async Task FinalizarPasoAsync_ShouldReturnSuccess_WhenPasoIsEnSala()
    {
        // Arrange
        var pasoId = Guid.NewGuid();
        var citaId = Guid.NewGuid();
        var paso = new LineaTiempo
        {
            Id = pasoId,
            ClinicaId = _clinicaId,
            CitaId = citaId,
            PacienteId = Guid.NewGuid(),
            NombrePaso = "Consulta",
            Orden = 3,
            Estado = "en_sala",
            HoraLlegada = new TimeSpan(9, 0, 0)
        };
        _repositoryMock.Setup(r => r.GetByIdAsync(_clinicaId, pasoId)).ReturnsAsync(paso);
        _repositoryMock.Setup(r => r.UpdateEstadoAsync(_clinicaId, pasoId, "completado", It.IsAny<TimeSpan?>())).ReturnsAsync(true);
        _repositoryMock.Setup(r => r.GetByCitaIdAsync(_clinicaId, citaId)).ReturnsAsync(new List<LineaTiempo> { paso });

        // Act
        var result = await _service.FinalizarPasoAsync(_clinicaId, pasoId, _usuarioId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Estado.Should().Be("completado");
        result.Data!.HoraSalida.Should().NotBeNull();
    }

    [Fact]
    public async Task FinalizarPasoAsync_ShouldReturnNotFound_WhenPasoDoesNotExist()
    {
        // Arrange
        var pasoId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(_clinicaId, pasoId)).ReturnsAsync((LineaTiempo?)null);

        // Act
        var result = await _service.FinalizarPasoAsync(_clinicaId, pasoId, _usuarioId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
    }

    [Fact]
    public async Task FinalizarPasoAsync_ShouldReturnValidation_WhenPasoIsNotEnSala()
    {
        // Arrange
        var pasoId = Guid.NewGuid();
        var paso = new LineaTiempo
        {
            Id = pasoId,
            ClinicaId = _clinicaId,
            CitaId = Guid.NewGuid(),
            PacienteId = Guid.NewGuid(),
            NombrePaso = "Llegada",
            Orden = 1,
            Estado = "pendiente"
        };
        _repositoryMock.Setup(r => r.GetByIdAsync(_clinicaId, pasoId)).ReturnsAsync(paso);

        // Act
        var result = await _service.FinalizarPasoAsync(_clinicaId, pasoId, _usuarioId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Validation);
        result.Message.Should().Contain("Solo se pueden finalizar pasos en atención");
    }

    // ── GenerarPasosParaCitaAsync ──────────────────────────────────────

    [Fact]
    public async Task GenerarPasosParaCitaAsync_ShouldReturnNotFound_WhenCitaDoesNotExist()
    {
        // Arrange
        var citaId = Guid.NewGuid();
        _citaRepositoryMock.Setup(r => r.GetByIdAsync(_clinicaId, citaId)).ReturnsAsync((Cita?)null);

        // Act
        var result = await _service.GenerarPasosParaCitaAsync(_clinicaId, citaId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        result.Message.Should().Contain("Cita no encontrada");
    }

    [Fact]
    public async Task GenerarPasosParaCitaAsync_ShouldReturnConflict_WhenPasosAlreadyExist()
    {
        // Arrange
        var citaId = Guid.NewGuid();
        var cita = new Cita
        {
            Id = citaId,
            ClinicaId = _clinicaId,
            PacienteId = Guid.NewGuid(),
            DoctorId = Guid.NewGuid(),
            Estado = "agendada",
            PacienteNombre = "Test Patient",
            SalaNombre = "Sala 1"
        };
        var pasosExistentes = new List<LineaTiempo>
        {
            new() { Id = Guid.NewGuid(), ClinicaId = _clinicaId, CitaId = citaId, NombrePaso = "Llegada", Orden = 1, Estado = "pendiente" }
        };

        _citaRepositoryMock.Setup(r => r.GetByIdAsync(_clinicaId, citaId)).ReturnsAsync(cita);
        _repositoryMock.Setup(r => r.GetByCitaIdAsync(_clinicaId, citaId)).ReturnsAsync(pasosExistentes);

        // Act
        var result = await _service.GenerarPasosParaCitaAsync(_clinicaId, citaId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Conflict);
        result.Message.Should().Contain("ya tiene pasos");
    }

    [Fact]
    public async Task GenerarPasosParaCitaAsync_ShouldGenerateThreeAutomaticSteps()
    {
        // Arrange
        var citaId = Guid.NewGuid();
        var cita = new Cita
        {
            Id = citaId,
            ClinicaId = _clinicaId,
            PacienteId = Guid.NewGuid(),
            DoctorId = Guid.NewGuid(),
            Estado = "agendada",
            PacienteNombre = "Test Patient",
            SalaNombre = "Sala 1"
        };

        _citaRepositoryMock.Setup(r => r.GetByIdAsync(_clinicaId, citaId)).ReturnsAsync(cita);
        _repositoryMock.Setup(r => r.GetByCitaIdAsync(_clinicaId, citaId)).ReturnsAsync(new List<LineaTiempo>());
        _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<LineaTiempo>()))
            .ReturnsAsync((LineaTiempo p) => { p.Id = Guid.NewGuid(); return p.Id; });

        // Act
        var result = await _service.GenerarPasosParaCitaAsync(_clinicaId, citaId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(3);
        result.Data!.Select(p => p.NombrePaso).Should().ContainInOrder("Llegada", "Consulta", "Salida");
        result.Data!.All(p => p.Orden == Array.IndexOf(new[] { "Llegada", "Consulta", "Salida" }, p.NombrePaso) + 1)
            .Should().BeTrue();
        result.Data!.All(p => p.Estado == "pendiente").Should().BeTrue();
    }
}
