# BLL — Core Skill

> **Agente propietario:** @EspecialistaUI
> **Cuándo cargar:** Antes de implementar servicios, validadores o lógica de negocio.
> **Prerequisito:** Haber leído CLAUDE.md, dal/SKILL.md. Interfaces del Repository deben existir.

---

## 1. Principios Fundamentales

```
1. BLL es el único lugar con lógica de negocio — ni DAL ni Controllers deciden
2. BLL NUNCA retorna Entities — siempre transforma a DTOs
3. BLL NUNCA llama a la BD — usa interfaces del DAL (I*Repository)
4. Toda operación de escritura extrae clinicaId del parámetro — nunca lo asume
5. Validaciones con FluentValidation — no en el Controller
6. Captura excepciones del DAL → ServiceResult<T>
7. Un Service por entidad/módulo principal
8. Async/await en todos los métodos
```

---

## 2. Estructura del Proyecto

```
src/Vittal.BLL/
├── Common/
│   ├── ServiceResult.cs
│   └── PagedResult.cs
├── Exceptions/
│   ├── BusinessException.cs
│   └── NotFoundException.cs
├── Interfaces/    ← Definidas por @Arquitecto
├── Services/      ← Implementadas por @EspecialistaUI
├── Validators/    ← FluentValidation por DTO de Request
└── Mappings/
    └── VittalMappingProfile.cs  ← AutoMapper
```

---

## 3. NuGet Packages

```xml
<PackageReference Include="FluentValidation" Version="11.9.2" />
<PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="11.9.2" />
<PackageReference Include="AutoMapper" Version="13.0.1" />
<PackageReference Include="AutoMapper.Extensions.Microsoft.DependencyInjection" Version="12.0.1" />
```

---

## 4. Registro en IOC

```csharp
// DependencyInjection.cs
public static IServiceCollection AddVittalBLL(this IServiceCollection services)
{
    services.AddAutoMapper(typeof(VittalMappingProfile).Assembly);
    services.AddValidatorsFromAssemblyContaining<PacienteRequestValidator>();

    services.AddScoped<IPacienteService, PacienteService>();
    // ... más servicios
    return services;
}
```

---

## 5. Navegación de Sub-skills — Leer según tu tarea

Este archivo contiene los principios generales. **Ahora carga el sub-skill específico para tu tarea:**

| Tu tarea | Sub-skill a cargar |
|---|---|
| Crear ServiceResult / PagedResult | → `skills/bll/service-result.md` |
| Implementar BLL Service (estructura completa) | → `skills/bll/service-templates.md` |
| Crear validadores FluentValidation | → `skills/bll/validators.md` |
| Configurar AutoMapper (Entity ↔ DTO) | → `skills/bll/mapping.md` |

---

## Checklist de Calidad — BLL Core

- [ ] Service retorna DTOs, nunca Entities
- [ ] Usa interfaces del DAL, no implementaciones
- [ ] clinicaId viene del parámetro, nunca del DTO
- [ ] Async/await en todos los métodos
- [ ] Service registrado como Scoped en IOC
- [ ] Validador registrado via AddValidatorsFromAssemblyContaining
- [ ] AutoMapper registrado via AddAutoMapper

---

*skills/bll/SKILL.md — Vittal v1.0.0*
*Sub-skills: service-result.md | service-templates.md | validators.md | mapping.md*
