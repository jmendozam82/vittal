using Xunit;
using FluentAssertions;
using Vittal.BLL.Validators;
using Vittal.DTO.HojaCita;
using Vittal.DTO.HojaDiagnostico;
using Vittal.DTO.HojaTratamiento;
using Vittal.DTO.HojaCirugia;
using Vittal.DTO.HojaExamen;
using Vittal.DTO.HojaRecomendacion;

namespace Vittal.BLL.Tests.Validators;

#region HojaCitaRequestValidatorTests

public class HojaCitaRequestValidatorTests
{
    private readonly HojaCitaRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_WhenAllFieldsValid()
    {
        var dto = new HojaCitaRequestDto
        {
            ExpedienteId = Guid.NewGuid(),
            CitaId = Guid.NewGuid(),
            DoctorId = Guid.NewGuid(),
            MotivoConsulta = "Dolor de cabeza persistente",
            NotasConsulta = "Paciente refiere cefalea desde hace 3 días."
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_WhenExpedienteIdEmpty()
    {
        var dto = new HojaCitaRequestDto
        {
            ExpedienteId = Guid.Empty,
            CitaId = Guid.NewGuid(),
            DoctorId = Guid.NewGuid()
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ExpedienteId");
    }
}

#endregion

#region HojaDiagnosticoRequestValidatorTests

public class HojaDiagnosticoRequestValidatorTests
{
    private readonly HojaDiagnosticoRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_WhenAllFieldsValid()
    {
        var dto = new HojaDiagnosticoRequestDto
        {
            HojaCitaId = Guid.NewGuid(),
            DiagnosticoId = Guid.NewGuid(),
            Observaciones = "Diagnóstico confirmado por laboratorio."
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_WhenHojaCitaIdEmpty()
    {
        var dto = new HojaDiagnosticoRequestDto
        {
            HojaCitaId = Guid.Empty,
            DiagnosticoId = Guid.NewGuid()
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "HojaCitaId");
    }
}

#endregion

#region HojaTratamientoRequestValidatorTests

public class HojaTratamientoRequestValidatorTests
{
    private readonly HojaTratamientoRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_WhenAllFieldsValid()
    {
        var dto = new HojaTratamientoRequestDto
        {
            HojaCitaId = Guid.NewGuid(),
            MedicamentoId = Guid.NewGuid(),
            Dosis = "500mg",
            Frecuencia = "Cada 8 horas",
            Duracion = "7 días",
            Instrucciones = "Tomar con alimentos."
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_WhenHojaCitaIdEmpty()
    {
        var dto = new HojaTratamientoRequestDto
        {
            HojaCitaId = Guid.Empty,
            MedicamentoId = Guid.NewGuid()
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "HojaCitaId");
    }
}

#endregion

#region HojaCirugiaRequestValidatorTests

public class HojaCirugiaRequestValidatorTests
{
    private readonly HojaCirugiaRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_WhenAllFieldsValid()
    {
        var dto = new HojaCirugiaRequestDto
        {
            HojaCitaId = Guid.NewGuid(),
            CirugiaId = Guid.NewGuid(),
            Observaciones = "Cirugía programada para la próxima semana."
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_WhenHojaCitaIdEmpty()
    {
        var dto = new HojaCirugiaRequestDto
        {
            HojaCitaId = Guid.Empty,
            CirugiaId = Guid.NewGuid()
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "HojaCitaId");
    }
}

#endregion

#region HojaExamenRequestValidatorTests

public class HojaExamenRequestValidatorTests
{
    private readonly HojaExamenRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_WhenAllFieldsValid()
    {
        var dto = new HojaExamenRequestDto
        {
            HojaCitaId = Guid.NewGuid(),
            ExamenId = Guid.NewGuid(),
            Resultado = "Valores dentro del rango normal.",
            ArchivoUrl = "https://storage.example.com/examen.pdf"
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_WhenHojaCitaIdEmpty()
    {
        var dto = new HojaExamenRequestDto
        {
            HojaCitaId = Guid.Empty,
            ExamenId = Guid.NewGuid()
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "HojaCitaId");
    }
}

#endregion

#region HojaRecomendacionRequestValidatorTests

public class HojaRecomendacionRequestValidatorTests
{
    private readonly HojaRecomendacionRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_WhenAllFieldsValid()
    {
        var dto = new HojaRecomendacionRequestDto
        {
            HojaCitaId = Guid.NewGuid(),
            RecomendacionId = Guid.NewGuid(),
            Observaciones = "Evitar esfuerzo físico por una semana."
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_WhenHojaCitaIdEmpty()
    {
        var dto = new HojaRecomendacionRequestDto
        {
            HojaCitaId = Guid.Empty,
            RecomendacionId = Guid.NewGuid()
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "HojaCitaId");
    }
}

#endregion
