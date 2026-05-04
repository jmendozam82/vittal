# BLL — FluentValidation Validators

> **Agente propietario:** @EspecialistaUI
> **Cuándo cargar:** Para crear validadores de DTOs de Request.
> **Prerequisito:** skills/bll/SKILL.md

---

## Plantilla Maestra de Validador

```csharp
// src/Vittal.BLL/Validators/[Entidad]RequestValidator.cs
namespace Vittal.BLL.Validators;

public class [Entidad]RequestValidator : AbstractValidator<[Entidad]RequestDto>
{
    public [Entidad]RequestValidator()
    {
        // Campos de texto obligatorios
        RuleFor(x => x.NombreCampo)
            .NotEmpty().WithMessage("El campo [Nombre] es obligatorio.")
            .MaximumLength(255).WithMessage("No puede superar 255 caracteres.")
            .Matches(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s\-']+$")
            .WithMessage("Solo permite letras y caracteres especiales del español.");

        // Email
        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("El formato del correo no es válido.")
            .MaximumLength(255).WithMessage("No puede superar 255 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Email));

        // Teléfono
        RuleFor(x => x.Celular)
            .Matches(@"^\+?[\d\s\-\(\)]{7,20}$")
            .WithMessage("El número no tiene un formato válido.")
            .When(x => !string.IsNullOrEmpty(x.Celular));

        // Sexo
        RuleFor(x => x.Sexo)
            .Must(s => s == "M" || s == "F")
            .WithMessage("Debe ser 'M' (Masculino) o 'F' (Femenino).")
            .When(x => !string.IsNullOrEmpty(x.Sexo));

        // Fecha
        RuleFor(x => x.FechaNacimiento)
            .LessThan(DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("Debe ser anterior a hoy.")
            .GreaterThan(new DateOnly(1900, 1, 1))
            .WithMessage("La fecha no es válida.")
            .When(x => x.FechaNacimiento.HasValue);

        // UUID / FK
        RuleFor(x => x.DoctorId)
            .NotEmpty().WithMessage("Debe seleccionar un doctor.");
    }
}
```

---

## PacienteRequestValidator (HU07)

```csharp
namespace Vittal.BLL.Validators;

public class PacienteRequestValidator : AbstractValidator<PacienteRequestDto>
{
    public PacienteRequestValidator()
    {
        RuleFor(x => x.PrimerNombre)
            .NotEmpty().WithMessage("El primer nombre es obligatorio.")
            .MaximumLength(100).WithMessage("No puede superar 100 caracteres.")
            .Matches(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s\-']+$")
            .WithMessage("Solo permite letras.");

        RuleFor(x => x.PrimerApellido)
            .NotEmpty().WithMessage("El primer apellido es obligatorio.")
            .MaximumLength(100).WithMessage("No puede superar 100 caracteres.")
            .Matches(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s\-']+$")
            .WithMessage("Solo permite letras.");

        RuleFor(x => x.SegundoNombre)
            .MaximumLength(100).When(x => !string.IsNullOrEmpty(x.SegundoNombre));

        RuleFor(x => x.SegundoApellido)
            .MaximumLength(100).When(x => !string.IsNullOrEmpty(x.SegundoApellido));

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Celular)
            .Matches(@"^\+?[\d\s\-\(\)]{7,20}$")
            .When(x => !string.IsNullOrEmpty(x.Celular));

        RuleFor(x => x.Sexo)
            .Must(s => s == "M" || s == "F")
            .When(x => !string.IsNullOrEmpty(x.Sexo));

        RuleFor(x => x.DoctorId)
            .NotEmpty().WithMessage("Debe seleccionar el doctor responsable.");

        RuleFor(x => x.Direccion)
            .MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Direccion));
    }
}
```

---

## UsuarioRequestValidator (HU04)

```csharp
namespace Vittal.BLL.Validators;

public class UsuarioRequestValidator : AbstractValidator<UsuarioRequestDto>
{
    public UsuarioRequestValidator()
    {
        RuleFor(x => x.Usuario)
            .NotEmpty().WithMessage("El nombre de usuario es obligatorio.")
            .MinimumLength(4).WithMessage("Al menos 4 caracteres.")
            .MaximumLength(100)
            .Matches(@"^[a-zA-Z0-9_\.\-]+$")
            .WithMessage("Solo letras, números, puntos, guiones y guiones bajos.");

        RuleFor(x => x.Contrasena)
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .MinimumLength(8).WithMessage("Al menos 8 caracteres.")
            .MaximumLength(100)
            .Matches(@"[A-Z]").WithMessage("Al menos una mayúscula.")
            .Matches(@"[0-9]").WithMessage("Al menos un número.")
            .When(x => !string.IsNullOrEmpty(x.Contrasena));

        RuleFor(x => x.Nombres)
            .NotEmpty().WithMessage("Los nombres son obligatorios.")
            .MaximumLength(255);

        RuleFor(x => x.Apellidos)
            .NotEmpty().WithMessage("Los apellidos son obligatorios.")
            .MaximumLength(255);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo es obligatorio.")
            .EmailAddress().MaximumLength(255);

        RuleFor(x => x.PerfilId)
            .NotEmpty().WithMessage("Debe seleccionar un perfil.");
    }
}
```

---

## CitaRequestValidator (HU21)

```csharp
namespace Vittal.BLL.Validators;

public class CitaRequestValidator : AbstractValidator<CitaRequestDto>
{
    public CitaRequestValidator()
    {
        RuleFor(x => x.PacienteId)
            .NotEmpty().WithMessage("Debe seleccionar un paciente.");

        RuleFor(x => x.DoctorId)
            .NotEmpty().WithMessage("Debe seleccionar un doctor.");

        RuleFor(x => x.FechaCita)
            .NotEmpty().WithMessage("La fecha es obligatoria.")
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("No puede ser en el pasado.");

        RuleFor(x => x.HoraCita)
            .NotEmpty().WithMessage("La hora es obligatoria.");

        RuleFor(x => x.Lugar)
            .NotEmpty().WithMessage("El lugar es obligatorio.")
            .MaximumLength(255);

        RuleFor(x => x.Motivo)
            .MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Motivo));
    }
}
```

---

## Checklist de Calidad — Validators

### Estructura
- [ ] Clase hereda de `AbstractValidator<[Entidad]RequestDto>`
- [ ] Constructor contiene todas las reglas
- [ ] Validador registrado en IOC via `AddValidatorsFromAssemblyContaining`

### Reglas
- [ ] Campos obligatorios con `NotEmpty`
- [ ] Longitudes máximas con `MaximumLength`
- [ ] Formatos especiales con `Matches` o `EmailAddress`
- [ ] Validaciones condicionales con `.When(x => ...)`
- [ ] UUID/FK con `NotEmpty`
- [ ] Mensajes de error en español y descriptivos

### Patrones de regex
- [ ] Nombres: `^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s\-']+$`
- [ ] Email: `EmailAddress()` built-in
- [ ] Teléfono: `^\+?[\d\s\-\(\)]{7,20}$`
- [ ] Usuario: `^[a-zA-Z0-9_\.\-]+$`
- [ ] Sexo: `Must(s => s == "M" || s == "F")`

---

*skills/bll/validators.md — Vittal v1.0.0*
