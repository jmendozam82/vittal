using Xunit;
using FluentAssertions;
using Vittal.BLL.Validators;
using Vittal.DTO.Medicamento;
using Vittal.DTO.TipoCirugia;
using Vittal.DTO.Cirugia;
using Vittal.DTO.TipoDiagnostico;
using Vittal.DTO.Diagnostico;
using Vittal.DTO.Tratamiento;
using Vittal.DTO.Recomendacion;
using Vittal.DTO.Examen;

namespace Vittal.BLL.Tests.Validators;

#region MedicamentoRequestValidatorTests

public class MedicamentoRequestValidatorTests
{
    private readonly MedicamentoRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_WhenAllFieldsValid()
    {
        var dto = new MedicamentoRequestDto
        {
            Nombre = "Paracetamol",
            Descripcion = "Analgésico y antipirético.",
            Concentracion = "500mg",
            UnidadMedida = "tabletas"
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_WhenNombreEmpty()
    {
        var dto = new MedicamentoRequestDto
        {
            Nombre = string.Empty
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Nombre");
    }
}

#endregion

#region TipoCirugiaRequestValidatorTests

public class TipoCirugiaRequestValidatorTests
{
    private readonly TipoCirugiaRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_WhenAllFieldsValid()
    {
        var dto = new TipoCirugiaRequestDto
        {
            Nombre = "Cirugía Mayor",
            Descripcion = "Procedimientos quirúrgicos de alta complejidad."
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_WhenNombreEmpty()
    {
        var dto = new TipoCirugiaRequestDto
        {
            Nombre = string.Empty
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Nombre");
    }
}

#endregion

#region CirugiaRequestValidatorTests

public class CirugiaRequestValidatorTests
{
    private readonly CirugiaRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_WhenAllFieldsValid()
    {
        var dto = new CirugiaRequestDto
        {
            TipoCirugiaId = Guid.NewGuid(),
            Nombre = "Apendicectomía",
            Descripcion = "Extirpación del apéndice."
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_WhenNombreEmpty()
    {
        var dto = new CirugiaRequestDto
        {
            TipoCirugiaId = Guid.NewGuid(),
            Nombre = string.Empty
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Nombre");
    }
}

#endregion

#region TipoDiagnosticoRequestValidatorTests

public class TipoDiagnosticoRequestValidatorTests
{
    private readonly TipoDiagnosticoRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_WhenAllFieldsValid()
    {
        var dto = new TipoDiagnosticoRequestDto
        {
            Nombre = "Enfermedad Crónica",
            Descripcion = "Diagnosticos de condición crónica."
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_WhenNombreEmpty()
    {
        var dto = new TipoDiagnosticoRequestDto
        {
            Nombre = string.Empty
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Nombre");
    }
}

#endregion

#region DiagnosticoRequestValidatorTests

public class DiagnosticoRequestValidatorTests
{
    private readonly DiagnosticoRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_WhenAllFieldsValid()
    {
        var dto = new DiagnosticoRequestDto
        {
            Nombre = "Diabetes Mellitus Tipo 2",
            TipoDiagnosticoId = Guid.NewGuid(),
            CodigoCie10 = "E11.9"
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_WhenNombreEmpty()
    {
        var dto = new DiagnosticoRequestDto
        {
            Nombre = string.Empty,
            TipoDiagnosticoId = Guid.NewGuid()
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Nombre");
    }
}

#endregion

#region TratamientoRequestValidatorTests

public class TratamientoRequestValidatorTests
{
    private readonly TratamientoRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_WhenAllFieldsValid()
    {
        var dto = new TratamientoRequestDto
        {
            Nombre = "Terapia Física",
            Descripcion = "Rehabilitación muscular programada."
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_WhenNombreEmpty()
    {
        var dto = new TratamientoRequestDto
        {
            Nombre = string.Empty
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Nombre");
    }
}

#endregion

#region RecomendacionRequestValidatorTests

public class RecomendacionRequestValidatorTests
{
    private readonly RecomendacionRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_WhenAllFieldsValid()
    {
        var dto = new RecomendacionRequestDto
        {
            Nombre = "Reposo en casa",
            Descripcion = "Descanso recomendado por 3 días."
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_WhenNombreEmpty()
    {
        var dto = new RecomendacionRequestDto
        {
            Nombre = string.Empty
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Nombre");
    }
}

#endregion

#region ExamenRequestValidatorTests

public class ExamenRequestValidatorTests
{
    private readonly ExamenRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_WhenAllFieldsValid()
    {
        var dto = new ExamenRequestDto
        {
            Nombre = "Hemograma Completo",
            Descripcion = "Examen de laboratorio general."
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_WhenNombreEmpty()
    {
        var dto = new ExamenRequestDto
        {
            Nombre = string.Empty
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Nombre");
    }
}

#endregion
