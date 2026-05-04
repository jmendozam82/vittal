# Controller — Core Skill (API REST)

> **Agente propietario:** @EspecialistaUI
> **Cuándo cargar:** Antes de implementar Controllers en Vittal.API.
> **Prerequisito:** Haber leído CLAUDE.md, bll/SKILL.md, dal/SKILL.md. El Service debe existir.

---

## 1. Principios Fundamentales

```
1. Controller NUNCA contiene lógica de negocio — solo orquesta
2. NUNCA accede directamente al DAL o BD
3. Todo endpoint protegido con [Authorize] (excepto Login)
4. clinicaId SIEMPRE se extrae del JWT via User.GetClinicaId()
5. Toda respuesta usa ApiResponse<T> como wrapper
6. Cada endpoint verifica permiso (READ, CREATE, UPDATE)
7. Códigos HTTP semánticamente correctos
8. Swagger documenta cada endpoint con ProducesResponseType
9. Métodos siempre async Task<IActionResult>
10. Usa ToActionResult() para traducir ServiceResult → HTTP
```

---

## 2. Estructura del Proyecto

```
src/Vittal.API/
├── Attributes/RequirePermissionAttribute.cs
├── Controllers/[Entidad]sController.cs
├── Extensions/
│   ├── ClaimsPrincipalExtensions.cs
│   └── ServiceResultExtensions.cs
├── Filters/PermissionFilter.cs
├── Middleware/TenantMiddleware.cs
├── Models/ApiResponse.cs
├── appsettings.json
└── Program.cs
```

---

## 4. Navegación de Sub-skills — Leer según tu tarea

Este archivo contiene los principios generales. **Ahora carga el sub-skill específico para tu tarea:**

| Tu tarea | Sub-skill a cargar |
|---|---|
| Configurar ApiResponse<T> wrapper | → `skills/controller/api-response.md` |
| Implementar RequirePermission / filtros | → `skills/controller/permission.md` |
| Estructura base de API Controller | → `skills/controller/controller-templates.md` |
| Auth (Login, Logout, Refresh) | → `skills/controller/auth-controller.md` |
| Controllers de negocio (CRUD por entidad) | → `skills/controller/business-controllers.md` |
| Configurar Program.cs (middleware, JWT, Swagger) | → `skills/controller/program.md` |

---

## 3. Checklist de Calidad — Controller Core

- [ ] `[ApiController]`, `[Route("api/[controller]")]`, `[Authorize]`, `[Produces("application/json")]`
- [ ] Constructor recibe Service + Logger
- [ ] `clinicaId = User.GetClinicaId()` en cada método
- [ ] `[RequirePermission]` en cada endpoint
- [ ] `[ProducesResponseType]` en cada endpoint
- [ ] `/// <summary>` en español para Swagger
- [ ] **No existe [HttpDelete]** en ningún Controller

---

*skills/controller/SKILL.md — Vittal v1.0.0*
*Sub-skills: api-response.md | permission.md | controller-templates.md | auth-controller.md | business-controllers.md | program.md*
