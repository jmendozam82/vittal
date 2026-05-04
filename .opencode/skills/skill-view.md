# skill-view.md — Backward Compatible Loader

> **NOTE:** This skill has been modularized. Load the new structure:

## New Modular Structure

Load `/skills/view/SKILL.md` as the main entry point. It references these sub-skills:

| Sub-skill | Content |
|---|---|
| `skills/view/SKILL.md` | Core principles, project structure, design system (CSS vars, utility classes) |
| `skills/view/login.md` | Login view template + _LayoutLogin.cshtml |
| `skills/view/crud-templates.md` | Index listing template + Create form template + deactivation modal |
| `skills/view/realtime-views.md` | Cola de Espera view + JS module + Alertas panel + JS |
| `skills/view/api-client.md` | VittalAPI JavaScript client + toast system + loading states |

## Quick Load by Task

- **Create CRUD views:** → `skills/view/SKILL.md` then `skills/view/crud-templates.md`
- **Implement Login:** → `skills/view/login.md`
- **Setup realtime (Cola/Alerts):** → `skills/view/realtime-views.md`
- **API client usage:** → `skills/view/api-client.md`

---

*Legacy loader — redirects to /skills/view/SKILL.md*
