# BLL — AutoMapper Mapping Profiles

> **Agente propietario:** @EspecialistaUI
> **Cuándo cargar:** Para configurar mapeos entre Entity y DTO.
> **Prerequisito:** skills/bll/SKILL.md

---

## VittalMappingProfile

```csharp
// src/Vittal.BLL/Mappings/VittalMappingProfile.cs
namespace Vittal.BLL.Mappings;

public class VittalMappingProfile : Profile
{
    public VittalMappingProfile()
    {
        // ── Paciente ─────────────────────────────────────────────────────
        CreateMap<Paciente, PacienteResponseDto>()
            .ForMember(dest => dest.NombreCompleto,
                opt => opt.MapFrom(src =>
                    $"{src.PrimerNombre} {src.SegundoNombre} {src.PrimerApellido} {src.SegundoApellido}"
                    .Replace("  ", " ").Trim()));

        CreateMap<PacienteRequestDto, Paciente>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ClinicaId, opt => opt.Ignore())
            .ForMember(dest => dest.Activo, opt => opt.Ignore())
            .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
            .ForMember(dest => dest.FechaModificacion, opt => opt.Ignore());

        // ── Usuario ──────────────────────────────────────────────────────
        CreateMap<Usuario, UsuarioResponseDto>()
            .ForMember(dest => dest.NombreCompleto,
                opt => opt.MapFrom(src => $"{src.Nombres} {src.Apellidos}".Trim()));

        CreateMap<UsuarioRequestDto, Usuario>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ClinicaId, opt => opt.Ignore())
            .ForMember(dest => dest.AuthUserId, opt => opt.Ignore())
            .ForMember(dest => dest.Activo, opt => opt.Ignore())
            .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
            .ForMember(dest => dest.FechaModificacion, opt => opt.Ignore());

        // ── Perfil ───────────────────────────────────────────────────────
        CreateMap<Perfil, PerfilResponseDto>();
        CreateMap<PerfilRequestDto, Perfil>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ClinicaId, opt => opt.Ignore())
            .ForMember(dest => dest.Activo, opt => opt.Ignore())
            .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
            .ForMember(dest => dest.FechaModificacion, opt => opt.Ignore());

        // ── Cita ─────────────────────────────────────────────────────────
        CreateMap<Cita, CitaResponseDto>();
        CreateMap<CitaRequestDto, Cita>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ClinicaId, opt => opt.Ignore())
            .ForMember(dest => dest.Estado, opt => opt.Ignore())
            .ForMember(dest => dest.Activo, opt => opt.Ignore())
            .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
            .ForMember(dest => dest.FechaModificacion, opt => opt.Ignore());

        // ── Expediente ───────────────────────────────────────────────────
        CreateMap<Expediente, ExpedienteResponseDto>();
        CreateMap<HojaCita, HojaCitaResponseDto>();

        // ── Catálogos simples (patrón idéntico) ──────────────────────────
        CreateMap<Medicamento, MedicamentoResponseDto>();
        CreateMap<MedicamentoRequestDto, Medicamento>()
            .IgnoreAuditFields();
        // Repetir para: TipoCirugia, Cirugia, TipoDiagnostico,
        // Diagnostico, Tratamiento, Recomendacion, Examen, Sala
    }
}

// Extensión para ignorar campos de auditoría en catálogos simples
public static class MappingExtensions
{
    public static IMappingExpression<TSrc, TDest> IgnoreAuditFields<TSrc, TDest>(
        this IMappingExpression<TSrc, TDest> map)
    {
        map.ForMember("Id", opt => opt.Ignore());
        map.ForMember("ClinicaId", opt => opt.Ignore());
        map.ForMember("Activo", opt => opt.Ignore());
        map.ForMember("FechaCreacion", opt => opt.Ignore());
        map.ForMember("FechaModificacion", opt => opt.Ignore());
        return map;
    }
}
```

---

## Reglas de Mapeo

### Entity → ResponseDto
- Mapeo directo en la mayoría de los casos
- Campos calculados con `MapFrom` (ej: `NombreCompleto`)
- NUNCA mapear contraseñas, tokens ni datos sensibles

### RequestDto → Entity
- **Siempre ignorar:** `Id`, `ClinicaId`, `Activo`, `FechaCreacion`, `FechaModificacion`
- `Id` lo genera la BD
- `ClinicaId` se asigna desde el JWT en el Service
- `Activo` se establece en el Repository
- Fechas de auditoría las maneja la BD o el Repository

---

## Checklist de Calidad — Mapping

- [ ] AutoMapper registrado via `AddAutoMapper(typeof(VittalMappingProfile).Assembly)`
- [ ] `Entity → ResponseDto` definido para cada entidad
- [ ] `RequestDto → Entity` ignora campos de auditoría
- [ ] Campos calculados (`NombreCompleto`) usan `MapFrom`
- [ ] `AuthUserId` ignorado en mapeo de Usuario (lo maneja Supabase Auth)
- [ ] `Estado` ignorado en mapeo de Cita (lo asigna el Service)
- [ ] No se mapean contraseñas en texto plano

---

*skills/bll/mapping.md — Vittal v1.0.0*
