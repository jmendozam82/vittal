# AGENTS.md — Vittal Project Master Rules

> CRITICAL: This file is automatically loaded by opencode.
> Based on CLAUDE.md — do NOT modify without @PM approval.

## Quick Overview
- **Project**: Vittal Medical System (SaaS + BaaS)
- **Client**: MedicCore (Clínicas Médicas)
- **Stack**: .NET 8, ASP.NET Core MVC + Web API, Supabase (PostgreSQL)
- **Architecture**: N-Tier + MVC, strict layer separation
- **Language**: Spanish (UI/DB), English (code)
- **Multi-tenant**: Every table MUST have `clinica_id` field
- **Specialty model**: `sala_id` = specialty discriminator | `clinica_id` = RLS only (see CLAUDE.md §4.1)

## Mandatory Instructions

**READ THE COMPLETE PROJECT DOCUMENTATION:**
@CLAUDE.md

## Skill References

Skills are now modular. Load the main entry point — it references sub-skills:
- **BLL generation**: `/skills/bll/SKILL.md` (sub-skills: service-result, service-templates, validators, mapping)
- **DAL generation**: `/skills/dal/SKILL.md` (sub-skills: connection, repository-templates, repositories-core)
- **API Controllers**: `/skills/controller/SKILL.md` (sub-skills: api-response, permission, controller-templates, auth, business, program)
- **Razor Views**: `/skills/view/SKILL.md` (sub-skills: login, crud-templates, realtime-views, api-client)
- **SQL Migrations**: `/skills/supabase/SKILL.md` (sub-skills: migrations-core, migrations-business, storage, realtime)

Legacy loaders for backward compatibility:
- `/skills/skill-bll.md` → loads `/skills/bll/SKILL.md`
- `/skills/skill-dal.md` → loads `/skills/dal/SKILL.md`
- `/skills/skill-controller.md` → loads `/skills/controller/SKILL.md`
- `/skills/skill-view.md` → loads `/skills/view/SKILL.md`
- `/skills/skill-supabase.md` → loads `/skills/supabase/SKILL.md`

## MCP Server References

Use MCP (Model Context Protocol) servers for enhanced capabilities:

- **@supabase** (MCP): Search Supabase documentation, RLS policies, PostgREST API
  - When to use: "@Arquitecto needs Supabase docs" or "Configure RLS policies. use supabase"
  - Access: Enabled for @Arquitecto and @IngenieroDatos

- **@github** (MCP via Docker): Search GitHub code examples, repos, issues
  - When to use: "Find C# Dapper examples. use github" or "Search ASP.NET patterns. use github"
  - Access: Enabled for @Arquitecto, @IngenieroDatos, @EspecialistaUI
  - Prerequisite: Docker installed

- **@n8n** (MCP): Automate workflows, integrate with external APIs
  - When to use: "Create n8n workflow for alerts. use n8n"
  - Access: Enabled for @EspecialistaUI (HU23 Alertas)

**Important**: MCP tools are disabled globally to save context. Each agent enables only the MCP servers it needs.

## Essential Rules (Summary)

1. **Multi-tenant**: Every table MUST have `clinica_id` — NO exceptions
2. **Specialty by Room**: `sala_id` = specialty discriminator in `tipos_antecedente`, `tipos_signo_vital`, `antecedentes_paciente`, `signos_vitales_hoja`. `clinica_id` = RLS ONLY in these tables
3. **No delete**: Only deactivate (`activo = false`) — applies to all catalogs
4. **N-tier flow**: View → MVC Controller → API → BLL → DAL → DB (never skip layers)
5. **API responses**: Always use `ApiResponse<T>`, never return Entity directly
6. **Service registration**: Always register in `Vittal.IOC/DependencyInjection.cs`
7. **Permissions**: Verify `READ`, `CREATE`, `UPDATE` before any operation (NO DELETE)
8. **RLS**: Enable Row Level Security on all business tables
9. **Audit fields**: Every entity has `fecha_creacion` and `fecha_modificacion`

## Project Structure (Quick Reference)

```
src/
├── Vittal.Aplicacion/   ← Frontend MVC (Areas/)
├── Vittal.API/           ← Backend Web API
├── Vittal.BLL/           ← Business Logic Layer (Interfaces/ & Services/)
├── Vittal.DAL/           ← Data Access Layer (Interfaces/ & Repositories/)
├── Vittal.Entity/         ← Domain Entities
├── Vittal.DTO/           ← Data Transfer Objects
├── Vittal.IOC/           ← Dependency Injection
└── Vittal.Utility/       ← Shared Helpers
```

## Development Workflow

When @PM assigns a new module:
1. **@Arquitecto** → Define N-tier structure (Entity, DTO, interfaces)
2. **@IngenieroDatos** → SQL migration + Repository (DAL) with Dapper
3. **@EspecialistaUI** → API Controller + BLL Service + Razor Views
4. **@PM** → Review integration, test complete flow

## Module Checklist

Before marking task complete, verify:
- [ ] SQL migration with `clinica_id` and RLS in `/supabase/migrations/`
- [ ] Entity in `Vittal.Entity/`
- [ ] DTOs (Request/Response) in `Vittal.DTO/`
- [ ] Repository + Interface in `Vittal.DAL/`
- [ ] Interface in `Vittal.BLL/Interfaces/` and Service in `Vittal.BLL/Services/`
- [ ] API Controller with Swagger in `Vittal.API/`
- [ ] Registration in `Vittal.IOC/DependencyInjection.cs`
- [ ] Razor Views in `Vittal.Aplicacion/Areas/`
- [ ] FluentValidation in BLL, jQuery Validate in View
- [ ] Permission check in API Controller
- [ ] Filter by `clinica_id` in all queries
- [ ] Field `activo` respected (no delete, only deactivate)

## Naming Conventions

| Element | Convention | Example |
|---------|------------|---------|
| C# Classes | PascalCase | `PacienteService` |
| C# Interfaces | IPascalCase | `IPacienteService` |
| C# Methods | PascalCase | `GetAllAsync` |
| C# Variables | camelCase | `pacienteId` |
| SQL Tables | snake_case plural | `pacientes` |
| SQL Columns | snake_case | `clinica_id` |
| DTOs/Entities | PascalCase.cs | `PacienteService.cs` |

## Database Standards

- **IDs**: `UUID` with `gen_random_uuid()` default
- **Tenant field**: `clinica_id UUID NOT NULL REFERENCES clinicas(id)`
- **Soft delete**: `activo BOOLEAN NOT NULL DEFAULT true`
- **Timestamps**: `TIMESTAMPTZ` (always UTC)
- **RLS**: Always enable with `clinica_id` policy

---

*AGENTS.md — Vittal v1.0.0 | Auto-loaded by opencode*
*References: CLAUDE.md (full documentation)*
