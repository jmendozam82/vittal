# skill-dal.md — Backward Compatible Loader

> **NOTE:** This skill has been modularized. Load the new structure:

## New Modular Structure

Load `/skills/dal/SKILL.md` as the main entry point. It references these sub-skills:

| Sub-skill | Content |
|---|---|
| `skills/dal/SKILL.md` | Core principles, project structure, NuGet packages, IOC registration |
| `skills/dal/connection.md` | IDbConnectionFactory, SupabaseConnectionFactory, GuidTypeHandler, DAL Exceptions |
| `skills/dal/repository-templates.md` | Interface template + Repository implementation template (Dapper) |
| `skills/dal/repositories-core.md` | Implemented repositories: Paciente, Cita, Permiso, Expediente |

## Quick Load by Task

- **Create a new Repository:** → `skills/dal/SKILL.md` then `skills/dal/repository-templates.md`
- **Configure connection:** → `skills/dal/connection.md`
- **Reference existing repos:** → `skills/dal/repositories-core.md`

---

*Legacy loader — redirects to /skills/dal/SKILL.md*
