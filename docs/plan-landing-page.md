# HU-L01: Landing Page Informativa — Plan de Implementación

> **Estado:** Listo para aprobación @PM (corregido por @Arquitecto)
> **Fecha de creación:** 2026-07-11
> **Última revisión:** 2026-07-11 (correcciones de @Arquitecto)
> **Sprint estimado:** Sprint 7 o paralelo al sistema
> **Días estimados:** 3 días
> **Arquitecto responsable:** @Arquitecto

### Correcciones aplicadas (2026-07-11)

| # | Problema | Corrección |
|---|---|---|
| 1 | Entity fuera de `Models/` | Movida a `Vittal.Entity/Models/` con namespace `Vittal.Entity.Models` |
| 2 | BLL Interface sin `ServiceResult<T>` | Interfaces retornan `ServiceResult<T>` siguiendo patrón existente |
| 3 | DTO sin validación | Agregados atributos `[Required]`, `[StringLength]`, `[EmailAddress]` |
| 4 | Sin `DeactivateAsync` | Agregado método a interfaz y repository |

---

## 1. Descripción de la Historia de Usuario

### HU-L01: Landing Page Informativa

**Como** prospecto (director, gerente, administrador de clínica),
**quiero** acceder a una página informativa del sistema Vittal antes de autenticarme,
**para** conocer los beneficios, funcionalidades y herramientas que ofrece el software
y decidir si solicitar una demo o acceder al sistema.

### Contexto

Vittal es un SaaS médico que necesita una fachada pública profesional para:
- Atraer prospectos (directores y gerentes de clínicas)
- Mostrar beneficios y funcionalidades del sistema
- Segmentar el mensaje por rol de usuario
- Capturar contactos interesados (formulario de contacto)
- Redirigir a usuarios existentes al login

**Decisión arquitectónica confirmada:** La landing se implementa dentro del proyecto
`Vittal.Aplicacion` como un Área más, **no como un proyecto separado**.

**Razones:**
- Un solo deploy, un solo dominio
- Mejor SEO (todo el contenido en `vittal.app/`)
- Menor complejidad operativa
- La landing accede al mismo config de Supabase
- Sigue la arquitectura N-Tier existente

---

## 2. Criterios de Aceptación

### CA-1: Página Principal (Home)
- [ ] Hero section con propuesta de valor de Vittal
- [ ] Botón "Iniciar Sesión" que redirige al área Login
- [ ] Botón "Conocer más" que navega a funcionalidades
- [ ] Diseño responsive (mobile-first)
- [ ] Tiempo de carga < 3 segundos

### CA-2: Sección de Funcionalidades
- [ ] Grid de tarjetas mostrando módulos principales:
  - Gestión de Expedientes
  - Agenda Médica
  - Cola de Espera en Tiempo Real
  - Diagnósticos y Tratamientos
  - Cirugías
  - Reportes y Dashboard
  - Alertas Configurables
- [ ] Cada tarjeta incluye: ícono, título, descripción breve
- [ ] Diseño responsive con Bootstrap 5

### CA-3: Sección de Beneficios
- [ ] Beneficios organizados por rol:
  - **Directores:** Control total, reportes, cumplimiento normativo
  - **Gerentes:** Gestión de personal, inventario, finanzas
  - **Doctores:** Acceso rápido a expedientes, agenda, cola de espera
  - **Recepcionistas:** Citas, pacientes, flujo de atención
- [ ] Diseño responsive

### CA-4: Formulario de Contacto
- [ ] Campos: Nombre completo, Email, Teléfono, Rol (select), Mensaje
- [ ] Validación client-side con jQuery Validate
- [ ] Validación server-side con FluentValidation
- [ ] Almacenamiento en tabla `contactos_landing` en BD
- [ ] Notificación por correo al admin del sistema
- [ ] Mensaje de éxito al usuario después del envío
- [ ] Protección contra spam (reCAPTCHA o similar)

### CA-5: Almacenamiento de Imágenes
- [ ] Bucket `landing` creado en Supabase Storage
- [ ] Imágenes de la landing subidas al bucket
- [ ] URLs de imágenes generadas con tokens temporales
- [ ] Imágenes optimizadas para web (WebP/AVIF)

### CA-6: Navegación
- [ ] Navbar fija con logo, enlaces y botón Login
- [ ] Footer con información de contacto y enlaces
- [ ] Smooth scroll entre secciones
- [ ] Enlace "Volver al inicio" en la página de Login existente

### CA-7: SEO
- [ ] Meta tags: title, description, keywords
- [ ] Open Graph tags para compartir en redes sociales
- [ ] Sitemap básico
- [ ] Imágenes con atributos alt

---

## 3. Decisiones Arquitectónicas

### 3.1 Ubicación en la arquitectura

```
Vittal.Aplicacion/
├── Areas/
│   ├── Landing/                    ← NUEVO (sin auth)
│   │   ├── Controllers/
│   │   │   └── LandingController.cs
│   │   └── Views/
│   │       └── Landing/
│   │           ├── Index.cshtml
│   │           ├── Funcionalidades.cshtml
│   │           ├── Beneficios.cshtml
│   │           ├── Contacto.cshtml
│   │           └── ContactoEnviado.cshtml
│   │
│   ├── Login/                      ← EXISTENTE (sin auth)
│   ├── Catalogos/                  ← EXISTENTE (con auth)
│   └── ...
│
├── Views/
│   └── Shared/
│       ├── _LayoutLanding.cshtml   ← NUEVO (layout propio)
│       └── _Layout.cshtml          ← EXISTENTE (admin)
│
└── wwwroot/
    ├── css/
    │   └── landing.css             ← NUEVO
    └── images/
        └── landing/                ← NUEVO (imágenes de la landing)
```

### 3.2 Capas N-Tier para la Landing

```
Landing View → Landing Controller → BLL ContactoService → DAL ContactoRepository → BD
                                      (solo para formulario)
```

**Nota:** La landing solo usa las capas N-Tier para el formulario de contacto.
Las demás secciones (Home, Funcionalidades, Beneficios) son presentación pura.

### 3.3 Modelo de Datos

**Excepción a la regla multi-tenant (CLAUDE.md §12):** La tabla `contactos_landing` NO tiene `clinica_id` porque es global del sistema (similar a `plantillas_especialidad`). Solo el Super Admin puede gestionar estos contactos.

```sql
-- Tabla para almacenar contactos del formulario de landing
-- NOTA: Tabla global del sistema, sin clinica_id (excepción CLAUDE.md §12)
CREATE TABLE IF NOT EXISTS contactos_landing (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    nombre_completo     VARCHAR(200) NOT NULL,
    email               VARCHAR(255) NOT NULL,
    telefono            VARCHAR(20),
    rol                 VARCHAR(50) NOT NULL,  -- director, gerente, admin, doctor, otro
    mensaje             TEXT,
    leido               BOOLEAN NOT NULL DEFAULT false,
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ
);

-- Índices
CREATE INDEX idx_contactos_landing_email ON contactos_landing(email);
CREATE INDEX idx_contactos_landing_activo ON contactos_landing(activo);
CREATE INDEX idx_contactos_landing_fecha ON contactos_landing(fecha_creacion);

-- Comentarios
COMMENT ON TABLE contactos_landing IS 'Contactos recibidos desde el formulario de la landing page';
COMMENT ON COLUMN contactos_landing.rol IS 'Rol del contactante: director, gerente, admin, doctor, otro';
```

### 3.4 RLS para contactos_landing

```sql
-- La tabla contactos_landing NO tiene clinica_id porque es global del sistema
-- Solo el Super Admin puede acceder a estos contactos
-- RLS se aplica para proteger los datos

ALTER TABLE contactos_landing ENABLE ROW LEVEL SECURITY;

-- Política para que solo el service_role pueda acceder
CREATE POLICY "service_role_only" ON contactos_landing
    FOR ALL USING (auth.role() = 'service_role');
```

### 3.5 Bucket de Supabase Storage

```sql
-- Bucket para imágenes de la landing
INSERT INTO storage.buckets (id, name, public)
VALUES ('landing', 'landing', true);

-- Política para lectura pública
CREATE POLICY "landing_public_read" ON storage.objects
    FOR SELECT USING (bucket_id = 'landing');

-- Política para escritura solo service_role
CREATE POLICY "landing_service_role_write" ON storage.objects
    FOR INSERT WITH CHECK (bucket_id = 'landing' AND auth.role() = 'service_role');
```

---

## 4. Estructura de Archivos Detallada

### 4.1 Entity

```csharp
// Vittal.Entity/Models/ContactoLanding.cs
// Siguiendo patrón existente: todas las Entities están en Models/
namespace Vittal.Entity.Models;

/// <summary>
/// Contactos recibidos desde el formulario de la landing page.
/// Tabla global del sistema (sin clinica_id).
/// </summary>
public class ContactoLanding
{
    /// <summary>Identificador único del contacto</summary>
    public Guid Id { get; set; }

    /// <summary>Nombre completo del contactante</summary>
    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>Correo electrónico del contactante</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Número de teléfono del contactante</summary>
    public string Telefono { get; set; } = string.Empty;

    /// <summary>Rol del contactante: director, gerente, admin, doctor, otro</summary>
    public string Rol { get; set; } = string.Empty;

    /// <summary>Mensaje del contactante</summary>
    public string Mensaje { get; set; } = string.Empty;

    /// <summary>Indica si el contacto ha sido leído por el admin</summary>
    public bool Leido { get; set; } = false;

    /// <summary>Estado del contacto (activo/inactivo)</summary>
    public bool Activo { get; set; } = true;

    /// <summary>Fecha y hora de creación del registro</summary>
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    /// <summary>Fecha y hora de la última modificación</summary>
    public DateTime? FechaModificacion { get; set; }
}
```

### 4.2 DTOs

```csharp
// Vittal.DTO/ContactoLanding/ContactoLandingRequestDto.cs
using System.ComponentModel.DataAnnotations;

namespace Vittal.DTO.ContactoLanding;

/// <summary>
/// DTO de entrada para el formulario de contacto de la landing page.
/// Validado con FluentValidation en BLL y atributos DataAnnotations para jQuery Validate.
/// </summary>
public class ContactoLandingRequestDto
{
    /// <summary>Nombre completo del contactante (requerido, máx. 200 caracteres)</summary>
    [Required(ErrorMessage = "El nombre completo es requerido.")]
    [StringLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres.")]
    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>Correo electrónico del contactante (requerido, formato válido)</summary>
    [Required(ErrorMessage = "El correo electrónico es requerido.")]
    [EmailAddress(ErrorMessage = "El correo electrónico no tiene un formato válido.")]
    [StringLength(255, ErrorMessage = "El correo no puede exceder 255 caracteres.")]
    public string Email { get; set; } = string.Empty;

    /// <summary>Número de teléfono (opcional, máx. 20 caracteres)</summary>
    [StringLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres.")]
    public string Telefono { get; set; } = string.Empty;

    /// <summary>Rol del contactante (requerido): director, gerente, admin, doctor, otro</summary>
    [Required(ErrorMessage = "Debe seleccionar su rol.")]
    [StringLength(50, ErrorMessage = "El rol no puede exceder 50 caracteres.")]
    public string Rol { get; set; } = string.Empty;

    /// <summary>Mensaje del contactante (requerido, máx. 2000 caracteres)</summary>
    [Required(ErrorMessage = "El mensaje es requerido.")]
    [StringLength(2000, ErrorMessage = "El mensaje no puede exceder 2000 caracteres.")]
    public string Mensaje { get; set; } = string.Empty;
}

// Vittal.DTO/ContactoLanding/ContactoLandingResponseDto.cs
namespace Vittal.DTO.ContactoLanding;

/// <summary>
/// DTO de salida para contactos de landing (vista admin).
/// </summary>
public class ContactoLandingResponseDto
{
    /// <summary>Identificador único del contacto</summary>
    public Guid Id { get; set; }

    /// <summary>Nombre completo del contactante</summary>
    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>Correo electrónico del contactante</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Rol del contactante</summary>
    public string Rol { get; set; } = string.Empty;

    /// <summary>Fecha de creación del contacto</summary>
    public DateTime FechaCreacion { get; set; }
}
```

### 4.2.1 FluentValidation (BLL)

```csharp
// Vittal.BLL/Validators/ContactoLandingRequestValidator.cs
using FluentValidation;
using Vittal.DTO.ContactoLanding;

namespace Vittal.BLL.Validators;

/// <summary>
/// Validador FluentValidation para el formulario de contacto de la landing.
/// </summary>
public class ContactoLandingRequestValidator : AbstractValidator<ContactoLandingRequestDto>
{
    public ContactoLandingRequestValidator()
    {
        RuleFor(x => x.NombreCompleto)
            .NotEmpty().WithMessage("El nombre completo es requerido.")
            .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo electrónico es requerido.")
            .EmailAddress().WithMessage("El correo electrónico no tiene un formato válido.")
            .MaximumLength(255).WithMessage("El correo no puede exceder 255 caracteres.");

        RuleFor(x => x.Telefono)
            .MaximumLength(20).WithMessage("El teléfono no puede exceder 20 caracteres.")
            .Matches(@"^[\d\s\+\-\(\)]*$").WithMessage("El teléfono solo puede contener números, espacios, guiones y paréntesis.");

        RuleFor(x => x.Rol)
            .NotEmpty().WithMessage("Debe seleccionar su rol.")
            .Must(rol => new[] { "director", "gerente", "admin", "doctor", "otro" }.Contains(rol.ToLower()))
            .WithMessage("El rol seleccionado no es válido.");

        RuleFor(x => x.Mensaje)
            .NotEmpty().WithMessage("El mensaje es requerido.")
            .MaximumLength(2000).WithMessage("El mensaje no puede exceder 2000 caracteres.");
    }
}
```

### 4.3 Interface Repository

```csharp
// Vittal.DAL/Interfaces/IContactoLandingRepository.cs
using Vittal.Entity.Models;

namespace Vittal.DAL.Interfaces;

/// <summary>
/// Interfaz del repositorio para contactos de landing.
/// Tabla global del sistema (sin clinica_id).
/// </summary>
public interface IContactoLandingRepository
{
    /// <summary>Crea un nuevo contacto de landing</summary>
    Task<Guid> CreateAsync(ContactoLanding contacto);

    /// <summary>Obtiene un contacto por ID</summary>
    Task<ContactoLanding?> GetByIdAsync(Guid id);

    /// <summary>Obtiene todos los contactos activos</summary>
    Task<IEnumerable<ContactoLanding>> GetAllAsync();

    /// <summary>Marca un contacto como leído</summary>
    Task<bool> MarkAsReadAsync(Guid id);

    /// <summary>Desactiva un contacto (no elimina) — CLAUDE.md regla #1</summary>
    Task<bool> DeactivateAsync(Guid id);
}
```

### 4.4 Repository

```csharp
// Vittal.DAL/Repositories/ContactoLandingRepository.cs
using Dapper;
using System.Data;
using Vittal.DAL.Interfaces;
using Vittal.Entity.Models;

namespace Vittal.DAL.Repositories;

/// <summary>
/// Repositorio para contactos de landing page.
/// Tabla global del sistema (sin clinica_id).
/// </summary>
public class ContactoLandingRepository : IContactoLandingRepository
{
    private readonly IDbConnection _db;

    public ContactoLandingRepository(IDbConnection db)
    {
        _db = db;
    }

    /// <inheritdoc/>
    public async Task<Guid> CreateAsync(ContactoLanding contacto)
    {
        const string sql = @"
            INSERT INTO contactos_landing (nombre_completo, email, telefono, rol, mensaje, activo, fecha_creacion)
            VALUES (@NombreCompleto, @Email, @Telefono, @Rol, @Mensaje, true, NOW())
            RETURNING id";

        return await _db.ExecuteScalarAsync<Guid>(sql, contacto);
    }

    /// <inheritdoc/>
    public async Task<ContactoLanding?> GetByIdAsync(Guid id)
    {
        const string sql = @"
            SELECT * FROM contactos_landing WHERE id = @Id AND activo = true";

        return await _db.QueryFirstOrDefaultAsync<ContactoLanding>(sql, new { Id = id });
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ContactoLanding>> GetAllAsync()
    {
        const string sql = @"
            SELECT * FROM contactos_landing 
            WHERE activo = true 
            ORDER BY fecha_creacion DESC";

        return await _db.QueryAsync<ContactoLanding>(sql);
    }

    /// <inheritdoc/>
    public async Task<bool> MarkAsReadAsync(Guid id)
    {
        const string sql = @"
            UPDATE contactos_landing 
            SET leido = true, fecha_modificacion = NOW() 
            WHERE id = @Id";

        var rows = await _db.ExecuteAsync(sql, new { Id = id });
        return rows > 0;
    }

    /// <inheritdoc/>
    public async Task<bool> DeactivateAsync(Guid id)
    {
        const string sql = @"
            UPDATE contactos_landing 
            SET activo = false, fecha_modificacion = NOW() 
            WHERE id = @Id";

        var rows = await _db.ExecuteAsync(sql, new { Id = id });
        return rows > 0;
    }
}
```

### 4.5 Interface Service

```csharp
// Vittal.BLL/Interfaces/IContactoLandingService.cs
using Vittal.DTO.ContactoLanding;
using Vittal.Utility.Results;

namespace Vittal.BLL.Interfaces;

/// <summary>
/// Interfaz del servicio BLL para contactos de landing.
/// Siguiendo patrón existente: retorna ServiceResult&lt;T&gt;.
/// </summary>
public interface IContactoLandingService
{
    /// <summary>Crea un nuevo contacto de landing</summary>
    Task<ServiceResult<ContactoLandingResponseDto>> CreateAsync(ContactoLandingRequestDto dto);

    /// <summary>Obtiene un contacto por ID</summary>
    Task<ServiceResult<ContactoLandingResponseDto>> GetByIdAsync(Guid id);

    /// <summary>Obtiene todos los contactos activos (vista admin)</summary>
    Task<ServiceResult<IEnumerable<ContactoLandingResponseDto>>> GetAllAsync();

    /// <summary>Marca un contacto como leído</summary>
    Task<ServiceResult<bool>> MarkAsReadAsync(Guid id);

    /// <summary>Desactiva un contacto (no elimina) — CLAUDE.md regla #1</summary>
    Task<ServiceResult<bool>> DeactivateAsync(Guid id);
}
```

### 4.6 Service

```csharp
// Vittal.BLL/Services/ContactoLandingService.cs
using Microsoft.Extensions.Logging;
using Vittal.BLL.Interfaces;
using Vittal.DAL.Interfaces;
using Vittal.DTO.ContactoLanding;
using Vittal.Entity.Models;
using Vittal.Utility.Results;

namespace Vittal.BLL.Services;

/// <summary>
/// Servicio BLL para contactos de landing page.
/// Implementa patrón ServiceResult&lt;T&gt; para respuestas consistentes.
/// </summary>
public class ContactoLandingService : IContactoLandingService
{
    private readonly IContactoLandingRepository _repository;
    private readonly ILogger<ContactoLandingService> _logger;

    public ContactoLandingService(
        IContactoLandingRepository repository,
        ILogger<ContactoLandingService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<ContactoLandingResponseDto>> CreateAsync(ContactoLandingRequestDto dto)
    {
        try
        {
            var contacto = new ContactoLanding
            {
                NombreCompleto = dto.NombreCompleto,
                Email = dto.Email,
                Telefono = dto.Telefono,
                Rol = dto.Rol,
                Mensaje = dto.Mensaje
            };

            var id = await _repository.CreateAsync(contacto);

            // TODO: Enviar correo de notificación al admin
            // await _emailService.SendLandingContactNotificationAsync(contacto);

            return ServiceResult<ContactoLandingResponseDto>.Success(new ContactoLandingResponseDto
            {
                Id = id,
                NombreCompleto = contacto.NombreCompleto,
                Email = contacto.Email,
                Rol = contacto.Rol,
                FechaCreacion = contacto.FechaCreacion
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear contacto de landing");
            return ServiceResult<ContactoLandingResponseDto>.Failure("Error al procesar el contacto. Intente nuevamente.");
        }
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<ContactoLandingResponseDto>> GetByIdAsync(Guid id)
    {
        var contacto = await _repository.GetByIdAsync(id);
        if (contacto == null)
            return ServiceResult<ContactoLandingResponseDto>.Failure("Contacto no encontrado.");

        return ServiceResult<ContactoLandingResponseDto>.Success(new ContactoLandingResponseDto
        {
            Id = contacto.Id,
            NombreCompleto = contacto.NombreCompleto,
            Email = contacto.Email,
            Rol = contacto.Rol,
            FechaCreacion = contacto.FechaCreacion
        });
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<IEnumerable<ContactoLandingResponseDto>>> GetAllAsync()
    {
        var contactos = await _repository.GetAllAsync();
        var result = contactos.Select(c => new ContactoLandingResponseDto
        {
            Id = c.Id,
            NombreCompleto = c.NombreCompleto,
            Email = c.Email,
            Rol = c.Rol,
            FechaCreacion = c.FechaCreacion
        });

        return ServiceResult<IEnumerable<ContactoLandingResponseDto>>.Success(result);
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<bool>> MarkAsReadAsync(Guid id)
    {
        var success = await _repository.MarkAsReadAsync(id);
        if (!success)
            return ServiceResult<bool>.Failure("Contacto no encontrado.");

        return ServiceResult<bool>.Success(true);
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<bool>> DeactivateAsync(Guid id)
    {
        var success = await _repository.DeactivateAsync(id);
        if (!success)
            return ServiceResult<bool>.Failure("Contacto no encontrado.");

        return ServiceResult<bool>.Success(true);
    }
}
```

### 4.7 Controller (sin auth)

```csharp
// Vittal.Aplicacion/Areas/Landing/Controllers/LandingController.cs
using Microsoft.AspNetCore.Mvc;
using Vittal.BLL.Interfaces;
using Vittal.DTO.ContactoLanding;

namespace Vittal.Aplicacion.Areas.Landing.Controllers;

/// <summary>
/// Controller de Landing Page — público, sin autenticación.
/// Maneja la página informativa y el formulario de contacto.
/// </summary>
[Area("Landing")]
public class LandingController : Controller
{
    private readonly IContactoLandingService _contactoService;
    private readonly ILogger<LandingController> _logger;

    public LandingController(
        IContactoLandingService contactoService,
        ILogger<LandingController> logger)
    {
        _contactoService = contactoService;
        _logger = logger;
    }

    /// <summary>Página principal de la landing</summary>
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>Sección de funcionalidades del sistema</summary>
    public IActionResult Funcionalidades()
    {
        return View();
    }

    /// <summary>Sección de beneficios por rol</summary>
    public IActionResult Beneficios()
    {
        return View();
    }

    /// <summary>Formulario de contacto — GET</summary>
    [HttpGet]
    public IActionResult Contacto()
    {
        return View();
    }

    /// <summary>Formulario de contacto — POST</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contacto(ContactoLandingRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var result = await _contactoService.CreateAsync(dto);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Error al procesar formulario de contacto: {Error}", result.Message);
            ModelState.AddModelError(string.Empty, result.Message ?? "Error al enviar el formulario. Intente nuevamente.");
            return View(dto);
        }

        return RedirectToAction(nameof(ContactoEnviado));
    }

    /// <summary>Página de confirmación después del envío</summary>
    public IActionResult ContactoEnviado()
    {
        return View();
    }
}
```

---

## 5. Configuración de Autenticación

### 5.1 Program.cs — No se requieren cambios

El middleware de autenticación ya está configurado para aplicar `[Authorize]` por controller.
El controller Landing NO tiene `[Authorize]`, por lo que es público automáticamente.

```csharp
// El orden existente en Program.cs es correcto:
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Las rutas Landing se resuelven automáticamente
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
```

### 5.2 Login — Enlace de regreso

```html
<!-- En la vista de Login.cshtml, agregar: -->
<a href="/" class="btn btn-outline-secondary btn-sm">
    ← Volver al inicio
</a>
```

---

## 6. Estimación de Tiempo

| Tarea | Días | Responsable |
|---|---|---|
| Entity, DTO, interfaces | 0.5 | @Arquitecto |
| Migración SQL + Repository | 0.5 | @IngenieroDatos |
| BLL Service | 0.5 | @EspecialistaUI |
| Controller Landing (sin auth) | 0.5 | @EspecialistaUI |
| Vistas Razor (Home, Funcionalidades, Beneficios, Contacto) | 1 | @EspecialistaUI |
| CSS personalizado + responsive | 0.5 | @EspecialistaUI |
| Configuración Storage + imágenes | 0.5 | @IngenieroDatos |
| **Total** | **3 días** | |

---

## 7. Dependencias

```
HU-L01 no depende de otras HU para su implementación.
Puede desarrollarse en paralelo con cualquier sprint.

Requisitos previos:
├── Proyecto Vittal.Aplicacion funcionando
├── Supabase configurado (BD + Storage)
└── Bootstrap 5 disponible en wwwroot
```

---

## 8. Notas Adicionales

### 8.1 SEO y Rendimiento
- Imágenes optimizadas (WebP/AVIF)
- Lazy loading en imágenes
- Meta tags completos
- Open Graph para redes sociales

### 8.2 Seguridad
- Formulario protegido con CSRF token (ValidateAntiForgeryToken)
- Rate limiting en endpoint de contacto
- Sanitización de inputs
- reCAPTCHA para prevenir spam (opcional, fase 2)

### 8.3 Futuras mejoras (fuera de alcance de HU-L01)
- Blog integrado
- Página de precios
- Testimonios de clientes
- Chat en vivo
- Multi-idioma

---

## 9. Aprobación

| Rol | Nombre | Estado | Fecha |
|---|---|---|---|
| @Arquitecto | — | ✅ Aprobado con correcciones | 2026-07-11 |
| @PM | — | **Pendiente** | — |

### Notas de la revisión del @Arquitecto

El plan fue revisado y **aprobado con correcciones**. Se identificaron 4 problemas críticos que fueron corregidos:

1. **Entity movida a `Models/`** — Namespace `Vittal.Entity.Models` consistente con el resto del proyecto
2. **BLL Interface con `ServiceResult<T>`** — Patrón existente en todas las interfaces BLL
3. **DTO con validación** — Atributos `[Required]`, `[StringLength]`, `[EmailAddress]`
4. **`DeactivateAsync` agregado** — Cumple regla #1 de no-eliminación

**Problemas moderados también corregidos:**
- Documentación XML en clases y propiedades
- Excepción `clinica_id` documentada con referencia a CLAUDE.md §12
- FluentValidation Validator declarado en el plan

**El plan está listo para aprobación del @PM.**

---

*Plan de Implementación HU-L01 — Landing Page Informativa*
*Vittal v1.0.0 | 2026-07-11*
*Corregido por @Arquitecto: 2026-07-11*
