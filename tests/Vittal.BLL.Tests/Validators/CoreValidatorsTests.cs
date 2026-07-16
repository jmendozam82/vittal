using Xunit;
using FluentAssertions;
using FluentValidation;
using Vittal.BLL.Validators;
using Vittal.DTO.Paciente;
using Vittal.DTO.Usuario;
using Vittal.DTO.Cita;
using Vittal.DTO.Clinica;
using Vittal.DTO.Perfil;
using Vittal.DTO.Sala;
using Vittal.DTO.Expediente;

namespace Vittal.BLL.Tests.Validators;

#region PacienteRequestValidatorTests

public class PacienteRequestValidatorTests
{
    private readonly PacienteRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_WhenAllFieldsValid()
    {
        var dto = new PacienteRequestDto
        {
            PrimerNombre = "Juan",
            PrimerApellido = "Pérez",
            DoctorId = Guid.NewGuid(),
            TipoDocumentoIdentificacion = "CC",
            NumeroDocumentoIdentificacion = "1234567890",
            Email = "juan@correo.com",
            Sexo = "M",
            FechaNacimiento = new DateOnly(1990, 5, 15)
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_WhenPrimerNombreEmpty()
    {
        var dto = new PacienteRequestDto
        {
            PrimerNombre = string.Empty,
            PrimerApellido = "Pérez",
            DoctorId = Guid.NewGuid(),
            TipoDocumentoIdentificacion = "CC",
            NumeroDocumentoIdentificacion = "1234567890"
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PrimerNombre");
    }

    [Fact]
    public void Should_Fail_WhenTipoDocumentoInvalido()
    {
        var dto = new PacienteRequestDto
        {
            PrimerNombre = "Juan",
            PrimerApellido = "Pérez",
            DoctorId = Guid.NewGuid(),
            TipoDocumentoIdentificacion = "XX",
            NumeroDocumentoIdentificacion = "1234567890"
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TipoDocumentoIdentificacion");
    }

    [Fact]
    public void Should_Fail_WhenEmailInvalido()
    {
        var dto = new PacienteRequestDto
        {
            PrimerNombre = "Juan",
            PrimerApellido = "Pérez",
            DoctorId = Guid.NewGuid(),
            TipoDocumentoIdentificacion = "CC",
            NumeroDocumentoIdentificacion = "1234567890",
            Email = "no-es-email"
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Should_Fail_WhenDoctorIdEmpty()
    {
        var dto = new PacienteRequestDto
        {
            PrimerNombre = "Juan",
            PrimerApellido = "Pérez",
            DoctorId = Guid.Empty,
            TipoDocumentoIdentificacion = "CC",
            NumeroDocumentoIdentificacion = "1234567890"
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DoctorId");
    }
}

#endregion

#region UsuarioRequestValidatorTests

public class UsuarioRequestValidatorTests
{
    private readonly UsuarioRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_WhenAllFieldsValid()
    {
        var dto = new UsuarioRequestDto
        {
            Username = "juan.perez",
            Nombres = "Juan Carlos",
            Apellidos = "Pérez García",
            Email = "juan@correo.com",
            Password = "Pass123",
            PerfilId = Guid.NewGuid(),
            Sexo = "M",
            TipoDocumentoIdentificacion = "CC",
            NumeroDocumentoIdentificacion = "1234567890"
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_WhenUsernameEmpty()
    {
        var dto = new UsuarioRequestDto
        {
            Username = string.Empty,
            Nombres = "Juan Carlos",
            Apellidos = "Pérez García",
            Email = "juan@correo.com",
            Password = "Pass123",
            PerfilId = Guid.NewGuid(),
            TipoDocumentoIdentificacion = "CC",
            NumeroDocumentoIdentificacion = "1234567890"
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Username");
    }

    [Fact]
    public void Should_Fail_WhenPasswordSinMayuscula()
    {
        var dto = new UsuarioRequestDto
        {
            Username = "testuser",
            Nombres = "Juan Carlos",
            Apellidos = "Pérez García",
            Email = "juan@correo.com",
            Password = "password123",
            PerfilId = Guid.NewGuid(),
            TipoDocumentoIdentificacion = "CC",
            NumeroDocumentoIdentificacion = "1234567890"
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void Should_Fail_WhenTipoDocumentoInvalido()
    {
        var dto = new UsuarioRequestDto
        {
            Username = "testuser",
            Nombres = "Juan Carlos",
            Apellidos = "Pérez García",
            Email = "juan@correo.com",
            Password = "Pass123",
            PerfilId = Guid.NewGuid(),
            TipoDocumentoIdentificacion = "XX",
            NumeroDocumentoIdentificacion = "1234567890"
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TipoDocumentoIdentificacion");
    }
}

#endregion

#region CitaRequestValidatorTests

public class CitaRequestValidatorTests
{
    private readonly CitaRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_WhenAllFieldsValid()
    {
        var dto = new CitaRequestDto
        {
            PacienteId = Guid.NewGuid(),
            DoctorId = Guid.NewGuid(),
            FechaCita = DateOnly.FromDateTime(DateTime.Today),
            HoraCita = new TimeOnly(10, 0),
            Estado = "agendada"
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_WhenPacienteIdEmpty()
    {
        var dto = new CitaRequestDto
        {
            PacienteId = Guid.Empty,
            DoctorId = Guid.NewGuid(),
            FechaCita = DateOnly.FromDateTime(DateTime.Today),
            HoraCita = new TimeOnly(10, 0),
            Estado = "agendada"
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PacienteId");
    }

    [Fact]
    public void Should_Fail_WhenFechaCitaInPasado()
    {
        var dto = new CitaRequestDto
        {
            PacienteId = Guid.NewGuid(),
            DoctorId = Guid.NewGuid(),
            FechaCita = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
            HoraCita = new TimeOnly(10, 0),
            Estado = "agendada"
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FechaCita");
    }
}

#endregion

#region ClinicaRequestValidatorTests

public class ClinicaRequestValidatorTests
{
    private readonly ClinicaRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_WhenAllFieldsValid()
    {
        var dto = new ClinicaRequestDto
        {
            Nombre = "Clínica MedicCore",
            TiempoEsperaMinutos = 30,
            Email = "info@medicore.com",
            Telefono = "+52 555 1234567"
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_WhenNombreEmpty()
    {
        var dto = new ClinicaRequestDto
        {
            Nombre = string.Empty,
            TiempoEsperaMinutos = 30
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Nombre");
    }

    [Fact]
    public void Should_Fail_WhenTiempoEsperaFueraRango()
    {
        var dto = new ClinicaRequestDto
        {
            Nombre = "Clínica MedicCore",
            TiempoEsperaMinutos = 500
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TiempoEsperaMinutos");
    }
}

#endregion

#region PerfilRequestValidatorTests

public class PerfilRequestValidatorTests
{
    private readonly PerfilRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_WhenAllFieldsValid()
    {
        var dto = new PerfilRequestDto
        {
            Nombre = "Administrador",
            Descripcion = "Perfil con acceso completo al sistema."
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_WhenNombreTooShort()
    {
        var dto = new PerfilRequestDto
        {
            Nombre = "AB"
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Nombre");
    }

    [Fact]
    public void Should_Fail_WhenDescripcionTooLong()
    {
        var dto = new PerfilRequestDto
        {
            Nombre = "Administrador",
            Descripcion = new string('X', 501)
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Descripcion");
    }
}

#endregion

#region SalaRequestValidatorTests

public class SalaRequestValidatorTests
{
    private readonly SalaRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_WhenAllFieldsValid()
    {
        var dto = new SalaRequestDto
        {
            Nombre = "Sala de Cardiología",
            Descripcion = "Sala equipada para consultas de cardiología."
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_WhenNombreEmpty()
    {
        var dto = new SalaRequestDto
        {
            Nombre = string.Empty
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Nombre");
    }

    [Fact]
    public void Should_Fail_WhenNombreTooShort()
    {
        var dto = new SalaRequestDto
        {
            Nombre = "AB"
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Nombre");
    }
}

#endregion

#region ExpedienteRequestValidatorTests

public class ExpedienteRequestValidatorTests
{
    private readonly ExpedienteRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_WhenAllFieldsValid()
    {
        var dto = new ExpedienteRequestDto
        {
            PacienteId = Guid.NewGuid(),
            DoctorId = Guid.NewGuid(),
            NotasGenerales = "Expediente inicial del paciente."
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_WhenPacienteIdEmpty()
    {
        var dto = new ExpedienteRequestDto
        {
            PacienteId = Guid.Empty,
            DoctorId = Guid.NewGuid()
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PacienteId");
    }

    [Fact]
    public void Should_Fail_WhenDoctorIdEmpty()
    {
        var dto = new ExpedienteRequestDto
        {
            PacienteId = Guid.NewGuid(),
            DoctorId = Guid.Empty
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DoctorId");
    }
}

#endregion
