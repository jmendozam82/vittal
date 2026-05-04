# skill-bll.md — Backward Compatible Loader

> **NOTE:** This skill has been modularized. Load the new structure:

## New Modular Structure

Load `/skills/bll/SKILL.md` as the main entry point. It references these sub-skills:

| Sub-skill | Content |
|---|---|
| `skills/bll/SKILL.md` | Core principles, project structure, NuGet packages, IOC registration |
| `skills/bll/service-result.md` | ServiceResult<T>, PagedResult, Domain Exceptions |
| `skills/bll/service-templates.md` | Interface template + Service implementation template |
| `skills/bll/validators.md` | FluentValidation templates (Paciente, Cita, Usuario) |
| `skills/bll/mapping.md` | AutoMapper profiles + mapping rules |

## Quick Load by Task

- **Implement a new Service:** → `skills/bll/SKILL.md` then `skills/bll/service-templates.md`
- **Create a validator:** → `skills/bll/validators.md`
- **Configure AutoMapper:** → `skills/bll/mapping.md`
- **Understand ServiceResult:** → `skills/bll/service-result.md`

---

*Legacy loader — redirects to /skills/bll/SKILL.md*
