# View — Core Skill (Design System & Structure)

> **Agente propietario:** @EspecialistaUI
> **Cuándo cargar:** Antes de crear vistas Razor, layouts o estilos CSS.
> **Prerequisito:** Haber leído CLAUDE.md y controller/SKILL.md.

---

## 1. Principios Fundamentales

```
1. La Vista NUNCA llama al DAL, BLL ni BD — solo al API
2. Controller MVC solo coordina — obtiene datos del API y pasa a ViewBag/ViewModel
3. Toda llamada al API incluye JWT en header Authorization: Bearer {token}
4. JWT en cookie HttpOnly — nunca en localStorage
5. Validación en dos niveles: jQuery Validate (cliente) + API (servidor)
6. Diseño responsive con Bootstrap 5.3 — mobile-first
7. Botón "Desactivar" en naranja, no rojo
8. Mensajes al usuario en español y descriptivos
9. Cada módulo en su propia Area de Vittal.Aplicacion
10. Llamadas al API asíncronas — no recargar página excepto navegación
```

---

## 2. Estructura del Proyecto Frontend

```
src/Vittal.Aplicacion/
├── Areas/
│   ├── Login/         ← HU02
│   ├── Administracion/ ← HU03-HU06
│   ├── Catalogos/     ← HU07-HU17
│   ├── ColaEspera/    ← HU18
│   ├── LineaTiempo/   ← HU19
│   ├── Expedientes/   ← HU20
│   ├── Agenda/        ← HU21
│   ├── Dashboard/     ← HU23
│   ├── Reportes/      ← HU22
│   └── Alertas/       ← HU23
├── Controllers/HomeController.cs
├── Helpers/ApiClientHelper.cs
├── Models/ViewModels/
├── Views/Shared/
│   ├── _Layout.cshtml
│   ├── _LayoutLogin.cshtml
│   ├── _Navbar.cshtml
│   ├── _Sidebar.cshtml
│   ├── _Alerts.cshtml
│   └── Error.cshtml
└── wwwroot/
    ├── css/vittal.css
    ├── css/vittal-variables.css
    ├── js/vittal-api.js
    ├── js/vittal-alerts.js
    ├── js/vittal-validation.js
    └── js/modules/
```

---

## 3. Sistema de Diseño — Variables CSS

```css
/* src/Vittal.Aplicacion/wwwroot/css/vittal-variables.css */
:root {
  /* Colores de marca */
  --vittal-primary:       #1A6FA8;   /* Azul médico */
  --vittal-primary-dark:  #14527D;   /* Hover primary */
  --vittal-primary-light: #E8F3FB;   /* Fondos activos */
  --vittal-secondary:     #2ECC71;   /* Verde éxito */
  --vittal-warning:       #F39C12;   /* Naranja desactivar */
  --vittal-danger:        #E74C3C;   /* Rojo errores */
  --vittal-info:          #3498DB;   /* Azul info */

  /* Neutrales */
  --vittal-bg:            #F5F7FA;
  --vittal-bg-card:       #FFFFFF;
  --vittal-border:        #DEE2E6;
  --vittal-text:          #2C3E50;
  --vittal-text-muted:    #7F8C8D;
  --vittal-sidebar-bg:    #1A2535;
  --vittal-sidebar-text:  #BDC3C7;
  --vittal-sidebar-active:#1A6FA8;

  /* Estados de citas */
  --estado-agendada:    #3498DB;
  --estado-en-espera:   #F39C12;
  --estado-en-atencion: #9B59B6;
  --estado-atendida:    #2ECC71;
  --estado-cancelada:   #95A5A6;

  /* Tipografía */
  --vittal-font:       'Inter', 'Segoe UI', system-ui, sans-serif;
  --vittal-radius:     8px;
  --vittal-radius-lg:  12px;
  --vittal-shadow:     0 2px 8px rgba(0,0,0,0.08);
  --vittal-shadow-lg:  0 4px 20px rgba(0,0,0,0.12);
}
```

---

## 4. Clases Utilitarias Vittal

```css
/* Cards */
.vittal-card {
  background: var(--vittal-bg-card);
  border-radius: var(--vittal-radius-lg);
  border: 1px solid var(--vittal-border);
  box-shadow: var(--vittal-shadow);
  padding: 1.5rem;
}

/* Tabla estándar */
.vittal-table { width: 100%; border-collapse: separate; border-spacing: 0; }
.vittal-table thead th {
  background: var(--vittal-primary-light);
  color: var(--vittal-primary-dark);
  font-size: 0.78rem; font-weight: 600;
  letter-spacing: 0.05em; text-transform: uppercase;
  padding: 0.75rem 1rem;
  border-bottom: 2px solid var(--vittal-primary);
}
.vittal-table tbody tr:hover { background: var(--vittal-primary-light); }
.vittal-table tbody td { padding: 0.75rem 1rem; border-bottom: 1px solid var(--vittal-border); }

/* Badges de estado */
.badge-agendada    { background: var(--estado-agendada);    color: #fff; }
.badge-en-espera   { background: var(--estado-en-espera);   color: #fff; }
.badge-en-atencion { background: var(--estado-en-atencion); color: #fff; }
.badge-atendida    { background: var(--estado-atendida);    color: #fff; }
.badge-cancelada   { background: var(--estado-cancelada);   color: #fff; }

/* Botones */
.btn-vittal-primary { background: var(--vittal-primary); color: #fff; border: none; }
.btn-vittal-primary:hover { background: var(--vittal-primary-dark); color: #fff; }
.btn-vittal-deactivate { background: var(--vittal-warning); color: #fff; border: none; }
.btn-vittal-deactivate:hover { background: #D68910; color: #fff; }

/* Sidebar */
.vittal-sidebar { width: 260px; min-height: 100vh; background: var(--vittal-sidebar-bg);
  position: fixed; top: 0; left: 0; z-index: 100; }
.vittal-content { margin-left: 260px; padding: 2rem; background: var(--vittal-bg); }

/* Spinner */
.vittal-spinner { width: 2rem; height: 2rem;
  border: 3px solid var(--vittal-border); border-top-color: var(--vittal-primary);
  border-radius: 50%; animation: vittal-spin 0.6s linear infinite; }
@keyframes vittal-spin { to { transform: rotate(360deg); } }

/* Toast */
.vittal-toast-container { position: fixed; top: 1rem; right: 1rem;
  z-index: 9999; display: flex; flex-direction: column; gap: 0.5rem; }
```

---

## 5. Navegación de Sub-skills — Leer según tu tarea

Este archivo contiene los principios generales. **Ahora carga el sub-skill específico para tu tarea:**

| Tu tarea | Sub-skill a cargar |
|---|---|
| Crear vistas de Login / Registro | → `skills/view/login.md` |
| Crear vistas CRUD (Index, Create, Edit) | → `skills/view/crud-templates.md` |
| Vistas con tiempo real (Cola, Alertas) | → `skills/view/realtime-views.md` |
| Configurar API Client JS (helpers, auth) | → `skills/view/api-client.md` |

---

## Checklist de Calidad — Core Views

### Estructura
- [ ] Vista en área correcta: `Areas/[Modulo]/Views/[Entidad]/`
- [ ] Layout correcto especificado
- [ ] `ViewData["Title"]` y `ViewData["Breadcrumb"]` definidos

### Diseño
- [ ] Responsive con Bootstrap 5
- [ ] Usa `vittal-card`, `vittal-table`, `btn-vittal-primary`
- [ ] Botón desactivar es naranja, nunca rojo
- [ ] Todos los textos en español

### Validación
- [ ] `novalidate` en el form
- [ ] Campos obligatorios marcados con asterisco rojo
- [ ] jQuery Validate activado antes del submit

---

*skills/view/SKILL.md — Vittal v1.0.0*
*Sub-skills: login.md | crud-templates.md | realtime-views.md | api-client.md*
