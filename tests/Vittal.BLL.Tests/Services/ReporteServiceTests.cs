using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Vittal.BLL.Services;
using Vittal.DAL.Interfaces;
using Vittal.DTO.Reporte;
using Vittal.Entity;
using Vittal.Utility.Results;

namespace Vittal.BLL.Tests.Services;

public class ReporteServiceTests
{
    private readonly Mock<IReporteRepository> _reporteRepositoryMock;
    private readonly Mock<ICitaRepository> _citaRepositoryMock;
    private readonly Mock<ILogger<ReporteService>> _loggerMock;
    private readonly ReporteService _service;
    private readonly Guid _clinicaId = Guid.NewGuid();
    private readonly Guid _usuarioId = Guid.NewGuid();

    public ReporteServiceTests()
    {
        _reporteRepositoryMock = new Mock<IReporteRepository>();
        _citaRepositoryMock = new Mock<ICitaRepository>();
        _loggerMock = new Mock<ILogger<ReporteService>>();

        _service = new ReporteService(
            _reporteRepositoryMock.Object,
            _citaRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnReports_WhenReportsExist()
    {
        // Arrange
        var reportes = new List<Reporte>
        {
            new() { Id = Guid.NewGuid(), ClinicaId = _clinicaId, Nombre = "Reporte Citas", Tipo = "citas_por_estado", Formato = "pdf", ContenidoJson = "[]", Activo = true, FechaCreacion = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), ClinicaId = _clinicaId, Nombre = "Reporte Pacientes", Tipo = "pacientes_por_dia", Formato = "csv", ContenidoJson = "[]", Activo = true, FechaCreacion = DateTime.UtcNow }
        };
        _reporteRepositoryMock.Setup(r => r.GetAllByClinicaIdAsync(_clinicaId)).ReturnsAsync(reportes);

        // Act
        var result = await _service.GetAllAsync(_clinicaId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data!.First().Nombre.Should().Be("Reporte Citas");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnEmptyList_WhenNoReports()
    {
        // Arrange
        _reporteRepositoryMock.Setup(r => r.GetAllByClinicaIdAsync(_clinicaId))
            .ReturnsAsync(new List<Reporte>());

        // Act
        var result = await _service.GetAllAsync(_clinicaId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNotFound_WhenReportDoesNotExist()
    {
        // Arrange
        var reporteId = Guid.NewGuid();
        _reporteRepositoryMock.Setup(r => r.GetByIdAsync(_clinicaId, reporteId)).ReturnsAsync((Reporte?)null);

        // Act
        var result = await _service.GetByIdAsync(_clinicaId, reporteId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnReport_WhenExists()
    {
        // Arrange
        var reporteId = Guid.NewGuid();
        var reporte = new Reporte
        {
            Id = reporteId,
            ClinicaId = _clinicaId,
            Nombre = "Reporte Test",
            Tipo = "citas_por_estado",
            Formato = "pdf",
            ContenidoJson = "[{\"estado\":\"atendida\",\"count\":5}]",
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        _reporteRepositoryMock.Setup(r => r.GetByIdAsync(_clinicaId, reporteId)).ReturnsAsync(reporte);

        // Act
        var result = await _service.GetByIdAsync(_clinicaId, reporteId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Nombre.Should().Be("Reporte Test");
    }

    [Fact]
    public async Task GenerarReporteAsync_ShouldReturnValidation_WhenInvalidTipo()
    {
        // Arrange
        var dto = new ReporteRequestDto
        {
            Tipo = "tipo_invalido",
            FechaInicio = DateTime.UtcNow.AddDays(-30),
            FechaFin = DateTime.UtcNow
        };

        // Act
        var result = await _service.GenerarReporteAsync(dto, _clinicaId, _usuarioId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Validation);
        result.Message.Should().Contain("no válido");
    }

    [Fact]
    public async Task GenerarReporteAsync_ShouldReturnValidation_WhenFechaInicioMayorQueFin()
    {
        // Arrange
        var dto = new ReporteRequestDto
        {
            Tipo = "citas_por_estado",
            FechaInicio = DateTime.UtcNow,
            FechaFin = DateTime.UtcNow.AddDays(-30)
        };

        // Act
        var result = await _service.GenerarReporteAsync(dto, _clinicaId, _usuarioId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Validation);
        result.Message.Should().Contain("fecha de inicio no puede ser mayor");
    }

    [Fact]
    public async Task GenerarReporteAsync_ShouldReturnSuccess_WhenValidRequest()
    {
        // Arrange
        var dto = new ReporteRequestDto
        {
            Tipo = "citas_por_estado",
            FechaInicio = DateTime.UtcNow.AddDays(-30),
            FechaFin = DateTime.UtcNow,
            Formato = "json"
        };
        var contenidoJson = "[{\"estado\":\"atendida\",\"count\":5},{\"estado\":\"cancelada\",\"count\":2}]";
        var reporteId = Guid.NewGuid();

        _reporteRepositoryMock.Setup(r => r.ExecuteReportQueryAsync(
            "citas_por_estado", _clinicaId, dto.FechaInicio, dto.FechaFin, null, null))
            .ReturnsAsync(contenidoJson);
        _reporteRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<Reporte>())).ReturnsAsync(reporteId);

        // Act
        var result = await _service.GenerarReporteAsync(dto, _clinicaId, _usuarioId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Tipo.Should().Be("citas_por_estado");
        result.Data!.ContenidoJson.Should().Be(contenidoJson);
    }

    [Fact]
    public async Task ExportarAsync_ShouldReturnNotFound_WhenReportDoesNotExist()
    {
        // Arrange
        var reporteId = Guid.NewGuid();
        _reporteRepositoryMock.Setup(r => r.GetByIdAsync(_clinicaId, reporteId)).ReturnsAsync((Reporte?)null);

        // Act
        var result = await _service.ExportarAsync(_clinicaId, reporteId, "pdf");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
    }

    [Fact]
    public async Task ExportarAsync_ShouldReturnValidation_WhenInvalidFormat()
    {
        // Arrange
        var reporteId = Guid.NewGuid();
        var reporte = new Reporte
        {
            Id = reporteId,
            ClinicaId = _clinicaId,
            Nombre = "Reporte Test",
            Tipo = "citas_por_estado",
            ContenidoJson = "[{\"count\":5}]",
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        _reporteRepositoryMock.Setup(r => r.GetByIdAsync(_clinicaId, reporteId)).ReturnsAsync(reporte);

        // Act
        var result = await _service.ExportarAsync(_clinicaId, reporteId, "xml");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Validation);
        result.Message.Should().Contain("no válido");
    }

    [Fact]
    public async Task ExportarAsync_ShouldReturnSuccess_WhenValidFormat()
    {
        // Arrange
        var reporteId = Guid.NewGuid();
        var contenidoJson = "[{\"estado\":\"atendida\",\"count\":5}]";
        var reporte = new Reporte
        {
            Id = reporteId,
            ClinicaId = _clinicaId,
            Nombre = "Reporte Test",
            Tipo = "citas_por_estado",
            ContenidoJson = contenidoJson,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        _reporteRepositoryMock.Setup(r => r.GetByIdAsync(_clinicaId, reporteId)).ReturnsAsync(reporte);

        // Act
        var result = await _service.ExportarAsync(_clinicaId, reporteId, "json");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Length.Should().BeGreaterThan(0);
    }
}
