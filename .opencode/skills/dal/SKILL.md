# DAL — Core Skill

> **Agente propietario:** @IngenieroDatos
> **Cuándo cargar:** Antes de implementar Repositories, interfaces o queries con Dapper.
> **Prerequisito:** Haber leído CLAUDE.md, supabase/SKILL.md. La tabla en PostgreSQL debe existir.

---

## 1. Principios Fundamentales

```
1. DAL NUNCA contiene lógica de negocio — solo operaciones de datos
2. Toda query SIEMPRE filtra por clinica_id
3. No existe DeleteAsync — solo DeactivateAsync
4. Queries usan parámetros Dapper (@Param) — nunca interpolación
5. Repository depende de su interfaz — nunca al revés
6. Un Repository por entidad principal
7. Transacciones cuando involucra múltiples tablas
8. Errores de BD mapeados a excepciones de dominio
9. Async/await en todos los métodos
```

---

## 2. Estructura del Proyecto

```
src/Vittal.DAL/
├── Connections/
│   ├── IDbConnectionFactory.cs
│   └── SupabaseConnectionFactory.cs
├── Interfaces/    ← Definidas por @Arquitecto
├── Repositories/  ← Implementadas por @IngenieroDatos
└── Exceptions/
    ├── RepositoryException.cs
    ├── DuplicateEntityException.cs
    └── TenantViolationException.cs
```

---

## 3. NuGet Packages

```xml
<PackageReference Include="Dapper" Version="2.1.35" />
<PackageReference Include="Npgsql" Version="8.0.3" />
<PackageReference Include="Dapper.NodaTime" Version="2.0.0" />
```

---

## 4. Registro en IOC

```csharp
public static IServiceCollection AddVittalDAL(this IServiceCollection services)
{
    services.AddSingleton<IDbConnectionFactory, SupabaseConnectionFactory>();
    services.AddScoped<IPacienteRepository, PacienteRepository>();
    // ... más repositorios
    return services;
}
```

---

## 5. Navegación de Sub-skills — Leer según tu tarea

Este archivo contiene los principios generales. **Ahora carga el sub-skill específico para tu tarea:**

| Tu tarea | Sub-skill a cargar |
|---|---|
| Configurar conexión a BD (IDbConnectionFactory) | → `skills/dal/connection.md` |
| Implementar Repository (template CRUD completo) | → `skills/dal/repository-templates.md` |
| Definir interfaces de Repository | → `skills/dal/repositories-core.md` |

---

## Checklist de Calidad — DAL Core

- [ ] IDbConnectionFactory como Singleton
- [ ] Repositorios como Scoped
- [ ] Toda query filtra por clinica_id
- [ ] No existe DeleteAsync en ningún Repository
- [ ] Queries usan parámetros Dapper, no interpolación
- [ ] Async/await en todos los métodos

---

*skills/dal/SKILL.md — Vittal v1.0.0*
*Sub-skills: connection.md | repository-templates.md | repositories-core.md*
