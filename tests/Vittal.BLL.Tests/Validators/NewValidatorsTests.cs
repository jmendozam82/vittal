using Xunit;
using FluentAssertions;
using Vittal.BLL.Validators;
using Vittal.DTO.Auth;
using Vittal.DTO.Constancia;
using Vittal.DTO.ExpedienteArchivo;
using Vittal.DTO.Permiso;
using Vittal.DTO.SignosVitalesHoja;
using Vittal.DTO.Usuario;
using Vittal.DTO.Clinica;
using Vittal.DTO.Dashboard;
using Vittal.DTO.ConfiguracionAlerta;
using Vittal.DTO.LineaTiempo;
using Vittal.DTO.Reporte;
using Vittal.DTO.ContactoLanding;

namespace Vittal.BLL.Tests.Validators;

#region LoginRequestValidatorTests

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_WhenAllFieldsValid()
    {
        var dto = new LoginRequestDto
        {
            Email = "admin@mediccore.com",
            Password = "Segura123"
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_WhenPasswordEmpty()
    {
        var dto = new LoginRequestDto
        {
            Email = "admin@mediccore.com",
            Password = string.Empty
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }
}

#endregion

#region RefreshRequestValidatorTests

public class RefreshRequestValidatorTests
{
    private readonly RefreshRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_WhenAllFieldsValid()
    {
        var dto = new RefreshRequestDto
        {
            RefreshToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.validtoken"
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_WhenRefreshTokenEmpty()
    {
        var dto = new RefreshRequestDto
        {
            RefreshToken = string.Empty
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RefreshToken");
    }
}

#endregion

#region ConstanciaRequestValidatorTests

public class ConstanciaRequestValidatorTests
{
    private readonly ConstanciaRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_WhenAllFieldsValid()
    {
        var dto = new ConstanciaRequestDto
        {
            ExpedienteId = Guid.NewGuid(),
            DoctorId = Guid.NewGuid(),
            TipoConstancia = "ASISTENCIA",
            Contenido = "El paciente asistió a consulta médica el día de hoy."
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_WhenTipoConstanciaInvalido()
    {
        var dto = new ConstanciaRequestDto
        {
            ExpedienteId = Guid.NewGuid(),
            DoctorId = Guid.NewGuid(),
            TipoConstancia = "INVALIDO",
            Contenido = "Contenido de la constancia."
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TipoConstancia");
    }

    [Fact]
    public void Should_Fail_WhenContenidoEmpty()
    {
        var dto = new ConstanciaRequestDto
        {
            ExpedienteId = Guid.NewGuid(),
            DoctorId = Guid.NewGuid(),
            TipoConstancia = "ASISTENCIA",
            Contenido = string.Empty
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Contenido");
    }
}

#endregion

#region ExpedienteArchivoRequestValidatorTests

public class ExpedienteArchivoRequestValidatorTests
{
    private readonly ExpedienteArchivoRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_WhenAllFieldsValid()
    {
        var dto = new ExpedienteArchivoRequestDto
        {
            ExpedienteId = Guid.NewGuid(),
            NombreArchivo = "resultado_laboratorio.pdf",
            TipoMime = "application/pdf",
            StoragePath = "expedientes/clinica1/paciente1/archivo.pdf",
            TamanoBytes = 1024000
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_WhenTipoMimeInvalido()
    {
        var dto = new ExpedienteArchivoRequestDto
        {
            ExpedienteId = Guid.NewGuid(),
            NombreArchivo = "archivo.rar",
            TipoMime = "application/x-rar-compressed",
            StoragePath = "expedientes/clinica1/paciente1/archivo.rar"
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TipoMime");
    }

    [Fact]
    public void Should_Fail_WhenStoragePathEmpty()
    {
        var dto = new ExpedienteArchivoRequestDto
        {
            ExpedienteId = Guid.NewGuid(),
            NombreArchivo = "resultado.pdf",
            TipoMime = "application/pdf",
            StoragePath = string.Empty
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "StoragePath");
    }
}

#endregion

#region PermisoUpdateRequestValidatorTests

public class PermisoUpdateRequestValidatorTests
{
    private readonly PermisoUpdateRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_WhenAllFieldsValid()
    {
        var dto = new PermisoUpdateRequestDto
        {
            Permisos = new List<PermisoItemUpdateDto>
            {
                new()
                {
                    ModuloId = Guid.NewGuid(),
                    PuedeLeer = true,
                    PuedeCrear = true,
                    PuedeActualizar = false
                }
            }
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_WhenPermisosListEmpty()
    {
        var dto = new PermisoUpdateRequestDto
        {
            Permisos = new List<PermisoItemUpdateDto>()
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Permisos");
    }
}

#endregion

#region SignosVitalesHojaRequestValidatorTests

public class SignosVitalesHojaRequestValidatorTests
{
    private readonly SignosVitalesHojaRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_WhenAllFieldsValid()
    {
        var dto = new SignosVitalesHojaRequestDto
        {
            HojaCitaId = Guid.NewGuid(),
            SalaId = Guid.NewGuid(),
            TipoSignoVitalId = Guid.NewGuid(),
            Valor = 120.5m,
            Unidad = "mmHg"
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_WhenValorFueraDeRango()
    {
        var dto = new SignosVitalesHojaRequestDto
        {
            HojaCitaId = Guid.NewGuid(),
            SalaId = Guid.NewGuid(),
            TipoSignoVitalId = Guid.NewGuid(),
            Valor = -5m
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Valor");
    }

    [Fact]
    public void Should_Fail_WhenHojaCitaIdEmpty()
    {
        var dto = new SignosVitalesHojaRequestDto
        {
            HojaCitaId = Guid.Empty,
            SalaId = Guid.NewGuid(),
            TipoSignoVitalId = Guid.NewGuid(),
            Valor = 36.5m
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "HojaCitaId");
    }
}

#endregion

#region MiPerfilUpdateRequestValidatorTests

public class MiPerfilUpdateRequestValidatorTests
{
    private readonly MiPerfilUpdateRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_WhenAllFieldsValid()
    {
        var dto = new MiPerfilUpdateRequestDto
        {
            Nombres = "Juan Carlos",
            Apellidos = "Pérez García",
            Email = "juan@correo.com",
            Sexo = "M",
            Celular = "+52 555 123456"
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_WhenNombresEmpty()
    {
        var dto = new MiPerfilUpdateRequestDto
        {
            Nombres = string.Empty,
            Apellidos = "Pérez García",
            Email = "juan@correo.com"
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Nombres");
    }
}

#endregion

#region AdminCreateUsuarioRequestValidatorTests

public class AdminCreateUsuarioRequestValidatorTests
{
    private readonly AdminCreateUsuarioRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_WhenAllFieldsValid()
    {
        var dto = new AdminCreateUsuarioRequestDto
        {
            ClinicaId = Guid.NewGuid(),
            Username = "dr.garcia",
            Nombres = "Carlos García",
            Apellidos = "López Martínez",
            Email = "garcia@mediccore.com",
            Password = "Segura123",
            PerfilId = Guid.NewGuid(),
            Sexo = "M"
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_WhenEmailInvalido()
    {
        var dto = new AdminCreateUsuarioRequestDto
        {
            ClinicaId = Guid.NewGuid(),
            Username = "dr.garcia",
            Nombres = "Carlos García",
            Apellidos = "López Martínez",
            Email = "no-es-email",
            Password = "Segura123",
            PerfilId = Guid.NewGuid()
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Should_Fail_WhenUsernameEmpty()
    {
        var dto = new AdminCreateUsuarioRequestDto
        {
            ClinicaId = Guid.NewGuid(),
            Username = string.Empty,
            Nombres = "Carlos García",
            Apellidos = "López Martínez",
            Email = "garcia@mediccore.com",
            Password = "Segura123",
            PerfilId = Guid.NewGuid()
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Username");
    }
}

#endregion

#region ClinicaProvisionRequestValidatorTests

public class ClinicaProvisionRequestValidatorTests
{
    private readonly ClinicaProvisionRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_WhenAllFieldsValid()
    {
        var dto = new ClinicaProvisionRequestDto
        {
            Nombre = "Clínica Nueva Salud",
            Direccion = "Av. Principal #123",
            TiempoEsperaMinutos = 30,
            AdminEmail = "admin@nuevasalud.com",
            AdminPassword = "Segura123",
            AdminNombres = "María López",
            AdminApellidos = "García Ruiz",
            AdminUsername = "admin.nuevasalud"
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_WhenAdminEmailEmpty()
    {
        var dto = new ClinicaProvisionRequestDto
        {
            Nombre = "Clínica Nueva Salud",
            TiempoEsperaMinutos = 30,
            AdminEmail = string.Empty,
            AdminPassword = "Segura123",
            AdminNombres = "María López",
            AdminApellidos = "García Ruiz",
            AdminUsername = "admin.nuevasalud"
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AdminEmail");
    }

    [Fact]
    public void Should_Fail_WhenAdminPasswordTooShort()
    {
        var dto = new ClinicaProvisionRequestDto
        {
            Nombre = "Clínica Nueva Salud",
            TiempoEsperaMinutos = 30,
            AdminEmail = "admin@nuevasalud.com",
            AdminPassword = "12345",
            AdminNombres = "María López",
            AdminApellidos = "García Ruiz",
            AdminUsername = "admin.nuevasalud"
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AdminPassword");
    }
}

#endregion

#region DashboardConfigRequestValidatorTests

public class DashboardConfigRequestValidatorTests
{
    private readonly DashboardConfigRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_WithValidConfig()
    {
        var dto = new DashboardConfigRequestDto
        {
            MostrarPacientesDelDia = true,
            MostrarCitasPendientes = true,
            Layout = "grid-2col"
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_WhenLayoutTooLong()
    {
        var dto = new DashboardConfigRequestDto
        {
            Layout = new string('X', 51)
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Layout");
    }
}

#endregion

#region ConfiguracionAlertaRequestValidatorTests

public class ConfiguracionAlertaRequestValidatorTests
{
    private readonly ConfiguracionAlertaRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_WithValidConfig()
    {
        var dto = new ConfiguracionAlertaRequestDto
        {
            TiempoEsperaMaximoMinutos = 15,
            IntervaloRevisionSegundos = 30,
            Activo = true,
            NotificacionSonido = true
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_WhenTiempoEsperaInvalido()
    {
        var dto = new ConfiguracionAlertaRequestDto
        {
            TiempoEsperaMaximoMinutos = 0,
            IntervaloRevisionSegundos = 30
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TiempoEsperaMaximoMinutos");
    }
}

#endregion

#region LineaTiempoRequestValidatorTests

public class LineaTiempoRequestValidatorTests
{
    private readonly LineaTiempoRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_WithValidConfig()
    {
        var dto = new LineaTiempoRequestDto
        {
            PasoId = Guid.NewGuid(),
            Accion = "iniciar",
            Observacion = "Paciente ingresó a la sala de espera."
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_WhenAccionEmpty()
    {
        var dto = new LineaTiempoRequestDto
        {
            PasoId = Guid.NewGuid(),
            Accion = string.Empty
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Accion");
    }
}

#endregion

#region ReporteRequestValidatorTests

public class ReporteRequestValidatorTests
{
    private readonly ReporteRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_WithValidConfig()
    {
        var dto = new ReporteRequestDto
        {
            Tipo = "pacientes_por_dia",
            FechaInicio = DateTime.UtcNow.AddDays(-30),
            FechaFin = DateTime.UtcNow,
            Formato = "PDF"
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_WhenTipoEmpty()
    {
        var dto = new ReporteRequestDto
        {
            Tipo = string.Empty,
            FechaInicio = DateTime.UtcNow.AddDays(-30),
            FechaFin = DateTime.UtcNow,
            Formato = "PDF"
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Tipo");
    }
}

#endregion

#region ContactoLandingRequestValidatorTests

public class ContactoLandingRequestValidatorTests
{
    private readonly ContactoLandingRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_WhenAllFieldsValid()
    {
        var dto = new ContactoLandingRequestDto
        {
            NombreCompleto = "Juan Pérez García",
            Email = "juan@empresa.com",
            Telefono = "+52 555 123456",
            Rol = "director",
            Mensaje = "Me interesa implementar el sistema en mi clínica."
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_WhenEmailInvalido()
    {
        var dto = new ContactoLandingRequestDto
        {
            NombreCompleto = "Juan Pérez García",
            Email = "no-es-email",
            Rol = "director",
            Mensaje = "Mensaje de prueba."
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }
}

#endregion
