# skill-controller.md — Backward Compatible Loader

> **NOTE:** This skill has been modularized. Load the new structure:

## New Modular Structure

Load `/skills/controller/SKILL.md` as the main entry point. It references these sub-skills:

| Sub-skill | Content |
|---|---|
| `skills/controller/SKILL.md` | Core principles, project structure, quality checklist |
| `skills/controller/api-response.md` | ApiResponse<T>, ClaimsPrincipalExtensions, ServiceResultExtensions |
| `skills/controller/permission.md` | RequirePermissionAttribute, PermissionFilter, TenantMiddleware |
| `skills/controller/controller-templates.md` | Master CRUD controller template |
| `skills/controller/auth-controller.md` | AuthController (Login, Refresh, Logout) |
| `skills/controller/business-controllers.md` | PacientesController, CitasController (specialized endpoints) |
| `skills/controller/program.md` | Program.cs full configuration (Swagger, JWT, CORS, pipeline) |

## Quick Load by Task

- **Create a new API Controller:** → `skills/controller/SKILL.md` then `skills/controller/controller-templates.md`
- **Configure auth/permissions:** → `skills/controller/api-response.md` + `skills/controller/permission.md`
- **Setup Program.cs:** → `skills/controller/program.md`
- **Implement Login:** → `skills/controller/auth-controller.md`

---

*Legacy loader — redirects to /skills/controller/SKILL.md*
