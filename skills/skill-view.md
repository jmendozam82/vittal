# skill-view.md — Skill: Vistas Razor MVC y Frontend

> **Agente propietario:** @EspecialistaUI
> **Cuándo cargar este skill:** Antes de crear cualquier vista Razor (.cshtml),
> layout, componente JavaScript, formulario o integración Supabase JS en
> el proyecto Vittal.Aplicacion.
> **Prerequisito:** Haber leído CLAUDE.md y skill-controller.md.
> El endpoint de la API debe existir antes de construir la vista que lo consume.

---

## 1. Principios Fundamentales del Frontend MVC

```
1. La Vista NUNCA llama directamente al DAL, BLL ni a la BD — solo al API
2. El Controller MVC (Vittal.Aplicacion) solo coordina — nunca tiene lógica
   de negocio: obtiene datos del API y los pasa al ViewBag/ViewModel
3. Toda llamada al API incluye el JWT en el header Authorization: Bearer {token}
4. El JWT se almacena en una cookie HttpOnly segura — nunca en localStorage
5. Validación en dos niveles: jQuery Validate (cliente) + API retorna errores (servidor)
6. Diseño responsive obligatorio con Bootstrap 5.3 — mobile-first
7. Botón "Desactivar" en lugar de "Eliminar" — el color es naranja, no rojo
8. Los mensajes al usuario van en español y son descriptivos
9. Cada módulo vive en su propia Area de Vittal.Aplicacion
10. Las llamadas al API son asíncronas con fetch/axios — no recargar página completa
    excepto en navegación inicial entre módulos
```

---

## 2. Estructura del Proyecto Vittal.Aplicacion

```
src/Vittal.Aplicacion/
├── Areas/
│   ├── Login/
│   │   ├── Controllers/AuthController.cs
│   │   └── Views/Auth/
│   │       └── Login.cshtml
│   ├── Administracion/
│   │   ├── Controllers/
│   │   │   ├── PerfilesController.cs
│   │   │   ├── UsuariosController.cs
│   │   │   ├── PermisosController.cs
│   │   │   └── SalasController.cs
│   │   └── Views/
│   │       ├── Perfiles/     (Index, Create, Edit)
│   │       ├── Usuarios/     (Index, Create, Edit)
│   │       ├── Permisos/     (Index, Edit)
│   │       └── Salas/        (Index, Create, Edit)
│   ├── Catalogos/
│   │   ├── Controllers/
│   │   │   ├── PacientesController.cs
│   │   │   ├── MedicamentosController.cs
│   │   │   ├── ClinicasController.cs
│   │   │   ├── AreasController.cs
│   │   │   ├── TiposCirugiaController.cs
│   │   │   ├── CirugiasController.cs
│   │   │   ├── TiposDiagnosticoController.cs
│   │   │   ├── DiagnosticosController.cs
│   │   │   ├── TratamientosController.cs
│   │   │   ├── RecomendacionesController.cs
│   │   │   └── ExamenesController.cs
│   │   └── Views/
│   │       ├── Pacientes/    (Index, Create, Edit, Detail)
│   │       ├── Medicamentos/ (Index, Create, Edit)
│   │       └── [Un directorio por catálogo]
│   ├── ColaEspera/
│   │   ├── Controllers/ColaEsperaController.cs
│   │   └── Views/ColaEspera/Index.cshtml
│   ├── LineaTiempo/
│   │   ├── Controllers/LineaTiempoController.cs
│   │   └── Views/LineaTiempo/Index.cshtml
│   ├── Expedientes/
│   │   ├── Controllers/ExpedientesController.cs
│   │   └── Views/Expedientes/
│   │       ├── Index.cshtml
│   │       └── Detail.cshtml
│   ├── Agenda/
│   │   ├── Controllers/AgendaController.cs
│   │   └── Views/Agenda/
│   │       ├── Index.cshtml
│   │       └── Create.cshtml
│   ├── Dashboard/
│   │   ├── Controllers/DashboardController.cs
│   │   └── Views/Dashboard/Index.cshtml
│   ├── Reportes/
│   │   ├── Controllers/ReportesController.cs
│   │   └── Views/Reportes/Index.cshtml
│   └── Alertas/
│       ├── Controllers/AlertasController.cs
│       └── Views/Alertas/Index.cshtml
├── Controllers/
│   └── HomeController.cs               ← Redirección al login o dashboard
├── Helpers/
│   └── ApiClientHelper.cs              ← Cliente HTTP centralizado para llamar al API
├── Models/
│   └── ViewModels/                     ← ViewModels específicos para vistas complejas
├── Views/
│   ├── Shared/
│   │   ├── _Layout.cshtml              ← Layout principal con navbar y sidebar
│   │   ├── _LayoutLogin.cshtml         ← Layout de pantalla de login
│   │   ├── _Navbar.cshtml              ← Componente de barra de navegación
│   │   ├── _Sidebar.cshtml             ← Componente de menú lateral
│   │   ├── _Alerts.cshtml              ← Componente de alertas de espera (tiempo real)
│   │   └── Error.cshtml
│   └── _ViewImports.cshtml
└── wwwroot/
    ├── css/
    │   ├── vittal.css                  ← Estilos personalizados Vittal
    │   └── vittal-variables.css        ← Variables CSS del sistema de diseño
    ├── js/
    │   ├── vittal-api.js               ← Cliente fetch centralizado con JWT
    │   ├── vittal-alerts.js            ← Supabase Realtime para alertas
    │   ├── vittal-validation.js        ← Configuración global de jQuery Validate
    │   └── modules/
    │       ├── cola-espera.js          ← Lógica de Cola de Espera en tiempo real
    │       ├── expedientes.js          ← Lógica de Expedientes
    │       └── agenda.js               ← Lógica del calendario de Agenda
    └── lib/                            ← Bootstrap 5, jQuery, Chart.js, etc.
```

---

## 3. Sistema de Diseño Vittal

### Variables CSS (vittal-variables.css)

```css
/* src/Vittal.Aplicacion/wwwroot/css/vittal-variables.css */
:root {
  /* ── Colores de marca ──────────────────────────────────────── */
  --vittal-primary:        #1A6FA8;   /* Azul médico — acciones principales */
  --vittal-primary-dark:   #14527D;   /* Hover de primary */
  --vittal-primary-light:  #E8F3FB;   /* Fondos de cards activas */
  --vittal-secondary:      #2ECC71;   /* Verde — confirmación, éxito */
  --vittal-warning:        #F39C12;   /* Naranja — desactivar (no eliminar) */
  --vittal-danger:         #E74C3C;   /* Rojo — errores críticos */
  --vittal-info:           #3498DB;   /* Azul claro — información */

  /* ── Neutrales ──────────────────────────────────────────────── */
  --vittal-bg:             #F5F7FA;   /* Fondo general */
  --vittal-bg-card:        #FFFFFF;   /* Fondo de cards */
  --vittal-border:         #DEE2E6;   /* Bordes suaves */
  --vittal-text:           #2C3E50;   /* Texto principal */
  --vittal-text-muted:     #7F8C8D;   /* Texto secundario */
  --vittal-sidebar-bg:     #1A2535;   /* Fondo del sidebar */
  --vittal-sidebar-text:   #BDC3C7;   /* Texto del sidebar */
  --vittal-sidebar-active: #1A6FA8;   /* Item activo del sidebar */

  /* ── Estados de citas ───────────────────────────────────────── */
  --estado-agendada:       #3498DB;
  --estado-en-espera:      #F39C12;
  --estado-en-atencion:    #9B59B6;
  --estado-atendida:       #2ECC71;
  --estado-cancelada:      #95A5A6;

  /* ── Tipografía ─────────────────────────────────────────────── */
  --vittal-font:           'Inter', 'Segoe UI', system-ui, sans-serif;
  --vittal-font-mono:      'JetBrains Mono', 'Consolas', monospace;
  --vittal-radius:         8px;
  --vittal-radius-lg:      12px;
  --vittal-shadow:         0 2px 8px rgba(0,0,0,0.08);
  --vittal-shadow-lg:      0 4px 20px rgba(0,0,0,0.12);
}
```

### Clases utilitarias Vittal (vittal.css)

```css
/* src/Vittal.Aplicacion/wwwroot/css/vittal.css */

/* ── Cards ──────────────────────────────────────────────────────── */
.vittal-card {
  background: var(--vittal-bg-card);
  border-radius: var(--vittal-radius-lg);
  border: 1px solid var(--vittal-border);
  box-shadow: var(--vittal-shadow);
  padding: 1.5rem;
}

/* ── Tabla estándar ─────────────────────────────────────────────── */
.vittal-table {
  width: 100%;
  border-collapse: separate;
  border-spacing: 0;
}
.vittal-table thead th {
  background: var(--vittal-primary-light);
  color: var(--vittal-primary-dark);
  font-size: 0.78rem;
  font-weight: 600;
  letter-spacing: 0.05em;
  text-transform: uppercase;
  padding: 0.75rem 1rem;
  border-bottom: 2px solid var(--vittal-primary);
}
.vittal-table tbody tr:hover { background: var(--vittal-primary-light); }
.vittal-table tbody td {
  padding: 0.75rem 1rem;
  border-bottom: 1px solid var(--vittal-border);
  color: var(--vittal-text);
  font-size: 0.9rem;
}

/* ── Badges de estado de citas ─────────────────────────────────── */
.badge-agendada    { background: var(--estado-agendada);    color: #fff; }
.badge-en-espera   { background: var(--estado-en-espera);   color: #fff; }
.badge-en-atencion { background: var(--estado-en-atencion); color: #fff; }
.badge-atendida    { background: var(--estado-atendida);    color: #fff; }
.badge-cancelada   { background: var(--estado-cancelada);   color: #fff; }

/* ── Botones de acción ──────────────────────────────────────────── */
.btn-vittal-primary  { background: var(--vittal-primary);  color: #fff; border: none; }
.btn-vittal-primary:hover { background: var(--vittal-primary-dark); color: #fff; }
/* DESACTIVAR — naranja, nunca rojo */
.btn-vittal-deactivate { background: var(--vittal-warning); color: #fff; border: none; }
.btn-vittal-deactivate:hover { background: #D68910; color: #fff; }

/* ── Sidebar ────────────────────────────────────────────────────── */
.vittal-sidebar {
  width: 260px;
  min-height: 100vh;
  background: var(--vittal-sidebar-bg);
  position: fixed;
  top: 0; left: 0;
  display: flex;
  flex-direction: column;
  z-index: 100;
}
.vittal-sidebar .nav-link {
  color: var(--vittal-sidebar-text);
  padding: 0.65rem 1.25rem;
  border-radius: 6px;
  margin: 2px 8px;
  font-size: 0.88rem;
  display: flex;
  align-items: center;
  gap: 0.6rem;
  transition: background 0.15s;
}
.vittal-sidebar .nav-link:hover,
.vittal-sidebar .nav-link.active {
  background: var(--vittal-sidebar-active);
  color: #fff;
}
.vittal-content { margin-left: 260px; padding: 2rem; background: var(--vittal-bg); }

/* ── Loading spinner ────────────────────────────────────────────── */
.vittal-spinner {
  width: 2rem; height: 2rem;
  border: 3px solid var(--vittal-border);
  border-top-color: var(--vittal-primary);
  border-radius: 50%;
  animation: vittal-spin 0.6s linear infinite;
}
@keyframes vittal-spin { to { transform: rotate(360deg); } }

/* ── Toast de notificación ─────────────────────────────────────── */
.vittal-toast-container {
  position: fixed; top: 1rem; right: 1rem;
  z-index: 9999; display: flex;
  flex-direction: column; gap: 0.5rem;
}
```

---

## 4. Cliente API JavaScript (vittal-api.js)

```javascript
// src/Vittal.Aplicacion/wwwroot/js/vittal-api.js
// Cliente fetch centralizado. Agrega JWT automáticamente a cada request.

const VittalAPI = (() => {
  const API_BASE = window.VITTAL_API_URL || '/api'; // Configurado en _Layout.cshtml

  /**
   * Obtiene el JWT almacenado en la cookie de sesión.
   * La cookie es HttpOnly — se envía automáticamente por el browser.
   * Esta función es para uso en headers explícitos cuando sea necesario.
   */
  function getToken() {
    return document.querySelector('meta[name="vittal-token"]')?.content || '';
  }

  /**
   * Muestra un toast de éxito o error al usuario.
   */
  function showToast(message, type = 'success') {
    const container = document.getElementById('vittal-toast-container')
      || createToastContainer();

    const toast = document.createElement('div');
    toast.className = `alert alert-${type === 'success' ? 'success' : 'danger'} 
                       alert-dismissible fade show shadow-sm`;
    toast.style.cssText = 'min-width:300px; max-width:450px;';
    toast.innerHTML = `
      <i class="bi bi-${type === 'success' ? 'check-circle' : 'exclamation-triangle'} me-2"></i>
      ${message}
      <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;
    container.appendChild(toast);
    setTimeout(() => toast.remove(), 5000);
  }

  function createToastContainer() {
    const div = document.createElement('div');
    div.id = 'vittal-toast-container';
    div.className = 'vittal-toast-container';
    document.body.appendChild(div);
    return div;
  }

  /**
   * Muestra un spinner de carga en el elemento indicado.
   */
  function showLoading(elementId) {
    const el = document.getElementById(elementId);
    if (el) el.innerHTML = `
      <div class="d-flex justify-content-center p-4">
        <div class="vittal-spinner"></div>
      </div>`;
  }

  /**
   * Método base para todas las llamadas al API.
   */
  async function request(method, endpoint, body = null) {
    const options = {
      method,
      credentials: 'include',  // Envía la cookie HttpOnly con el JWT
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${getToken()}`
      }
    };

    if (body) options.body = JSON.stringify(body);

    try {
      const response = await fetch(`${API_BASE}${endpoint}`, options);

      // Token expirado → redirigir al login
      if (response.status === 401) {
        window.location.href = '/Login/Auth/Login?returnUrl=' +
          encodeURIComponent(window.location.pathname);
        return null;
      }

      const data = await response.json();
      return { ok: response.ok, status: response.status, data };

    } catch (error) {
      console.error(`[VittalAPI] Error en ${method} ${endpoint}:`, error);
      showToast('Error de conexión con el servidor. Intente nuevamente.', 'error');
      return null;
    }
  }

  return {
    get:    (endpoint)        => request('GET',    endpoint),
    post:   (endpoint, body)  => request('POST',   endpoint, body),
    put:    (endpoint, body)  => request('PUT',    endpoint, body),
    patch:  (endpoint, body)  => request('PATCH',  endpoint, body),
    showToast,
    showLoading
  };
})();
```

---

## 5. Layout Principal (_Layout.cshtml)

```html
<!-- src/Vittal.Aplicacion/Views/Shared/_Layout.cshtml -->
<!DOCTYPE html>
<html lang="es">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <meta name="vittal-token" content="@Context.Session.GetString("AccessToken")" />
  <title>@ViewData["Title"] — Vittal</title>

  <!-- Bootstrap 5 Icons -->
  <link rel="stylesheet"
        href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.css" />
  <!-- Bootstrap 5 -->
  <link rel="stylesheet"
        href="~/lib/bootstrap/dist/css/bootstrap.min.css" />
  <!-- Vittal Design System -->
  <link rel="stylesheet" href="~/css/vittal-variables.css" />
  <link rel="stylesheet" href="~/css/vittal.css" />
  <!-- Supabase JS para Realtime -->
  <script src="https://cdn.jsdelivr.net/npm/@@supabase/supabase-js@2"></script>

  @await RenderSectionAsync("Styles", required: false)
</head>
<body>

  <!-- ── Sidebar ─────────────────────────────────────────────────── -->
  <nav class="vittal-sidebar" id="vittalSidebar">
    <div class="p-3 border-bottom border-secondary">
      <div class="d-flex align-items-center gap-2">
        <i class="bi bi-heart-pulse-fill text-primary fs-4"></i>
        <span class="text-white fw-bold fs-5">Vittal</span>
      </div>
      <small class="text-muted">@Context.Session.GetString("ClinicaNombre")</small>
    </div>

    <nav class="flex-grow-1 py-2" id="sidebarMenu">
      @await Html.PartialAsync("_Sidebar")
    </nav>

    <div class="p-3 border-top border-secondary">
      <div class="d-flex align-items-center gap-2">
        <i class="bi bi-person-circle text-muted fs-5"></i>
        <div>
          <div class="text-white small">@Context.Session.GetString("NombreCompleto")</div>
          <div class="text-muted" style="font-size:0.75rem">
            @Context.Session.GetString("PerfilNombre")
          </div>
        </div>
        <a asp-area="Login" asp-controller="Auth" asp-action="Logout"
           class="btn btn-sm btn-outline-secondary ms-auto"
           title="Cerrar sesión">
          <i class="bi bi-box-arrow-right"></i>
        </a>
      </div>
    </div>
  </nav>

  <!-- ── Contenido principal ─────────────────────────────────────── -->
  <main class="vittal-content" id="vittalMain">

    <!-- Barra superior -->
    <div class="d-flex justify-content-between align-items-center mb-4">
      <div>
        <h1 class="h4 mb-0 fw-semibold" style="color:var(--vittal-text)">
          @ViewData["Title"]
        </h1>
        @if (ViewData["Breadcrumb"] is not null)
        {
          <nav aria-label="breadcrumb">
            <ol class="breadcrumb mb-0 small">
              @Html.Raw(ViewData["Breadcrumb"])
            </ol>
          </nav>
        }
      </div>
      <div id="vittal-header-actions">
        @await RenderSectionAsync("HeaderActions", required: false)
      </div>
    </div>

    <!-- Contenido del módulo -->
    @RenderBody()

    <!-- Contenedor de toasts -->
    <div class="vittal-toast-container" id="vittal-toast-container"></div>
  </main>

  <!-- ── Alertas de tiempo real ──────────────────────────────────── -->
  @await Html.PartialAsync("_Alerts")

  <!-- ── Scripts ─────────────────────────────────────────────────── -->
  <script src="~/lib/jquery/dist/jquery.min.js"></script>
  <script src="~/lib/bootstrap/dist/js/bootstrap.bundle.min.js"></script>
  <script src="~/lib/jquery-validation/dist/jquery.validate.min.js"></script>
  <script src="~/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.min.js"></script>

  <!-- API Client y configuración global -->
  <script>
    window.VITTAL_API_URL = '@Configuration["App:ApiUrl"]';
    window.VITTAL_SUPABASE_URL = '@Configuration["Supabase:Url"]';
    window.VITTAL_SUPABASE_ANON_KEY = '@Configuration["Supabase:AnonKey"]';
    window.VITTAL_CLINICA_ID = '@Context.Session.GetString("ClinicaId")';
  </script>
  <script src="~/js/vittal-api.js"></script>
  <script src="~/js/vittal-validation.js"></script>
  <script src="~/js/vittal-alerts.js"></script>

  @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

---

## 6. Plantilla Maestra de Vista Index (Listado)

```html
<!-- Areas/[Modulo]/Views/[Entidad]/Index.cshtml -->
@{
  ViewData["Title"]      = "[Nombre del módulo]";
  ViewData["Breadcrumb"] = "<li class='breadcrumb-item'><a href='/'>Inicio</a></li>" +
                            "<li class='breadcrumb-item active'>[Nombre del módulo]</li>";
}

@section HeaderActions {
  <a id="btnNuevo" class="btn btn-vittal-primary btn-sm"
     href="@Url.Action("Create", "[Entidad]")">
    <i class="bi bi-plus-lg me-1"></i> Nuevo [Entidad]
  </a>
}

<div class="vittal-card">
  <!-- Buscador y filtros -->
  <div class="row g-2 mb-3">
    <div class="col-md-5">
      <div class="input-group input-group-sm">
        <span class="input-group-text bg-white border-end-0">
          <i class="bi bi-search text-muted"></i>
        </span>
        <input type="text" id="busquedaInput" class="form-control border-start-0"
               placeholder="Buscar [entidad]..." />
      </div>
    </div>
    <div class="col-md-3">
      <select id="filtroEstado" class="form-select form-select-sm">
        <option value="">Todos los estados</option>
        <option value="activo">Activos</option>
        <option value="inactivo">Inactivos</option>
      </select>
    </div>
    <div class="col-auto ms-auto">
      <span class="text-muted small" id="contadorRegistros">Cargando...</span>
    </div>
  </div>

  <!-- Tabla de datos -->
  <div id="tablaContainer">
    <div class="d-flex justify-content-center p-4">
      <div class="vittal-spinner"></div>
    </div>
  </div>
</div>

<!-- Modal de confirmación de desactivar -->
<div class="modal fade" id="modalDesactivar" tabindex="-1" aria-hidden="true">
  <div class="modal-dialog modal-dialog-centered">
    <div class="modal-content">
      <div class="modal-header border-0 pb-0">
        <h5 class="modal-title">
          <i class="bi bi-exclamation-triangle text-warning me-2"></i>
          Confirmar desactivación
        </h5>
        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
      </div>
      <div class="modal-body">
        <p class="mb-0">
          ¿Está seguro que desea desactivar este registro?
          <br><small class="text-muted">
            El registro no será eliminado, solo quedará inactivo.
          </small>
        </p>
      </div>
      <div class="modal-footer border-0">
        <button type="button" class="btn btn-secondary btn-sm" data-bs-dismiss="modal">
          Cancelar
        </button>
        <button type="button" class="btn btn-vittal-deactivate btn-sm" id="btnConfirmarDesactivar">
          <i class="bi bi-slash-circle me-1"></i> Desactivar
        </button>
      </div>
    </div>
  </div>
</div>

@section Scripts {
<script>
  // ── Estado del módulo ───────────────────────────────────────────
  let registros   = [];
  let idSeleccionado = null;
  const modal     = new bootstrap.Modal(document.getElementById('modalDesactivar'));

  // ── Cargar datos al inicializar ─────────────────────────────────
  document.addEventListener('DOMContentLoaded', async () => {
    await cargarDatos();
    configurarBusqueda();
  });

  async function cargarDatos() {
    VittalAPI.showLoading('tablaContainer');
    const res = await VittalAPI.get('/[entidad]s');

    if (!res || !res.ok) {
      document.getElementById('tablaContainer').innerHTML =
        '<div class="alert alert-danger">Error al cargar los datos.</div>';
      return;
    }

    registros = res.data.data || [];
    renderTabla(registros);
  }

  // ── Render de tabla ─────────────────────────────────────────────
  function renderTabla(items) {
    const count = document.getElementById('contadorRegistros');
    count.textContent = `${items.length} registro${items.length !== 1 ? 's' : ''}`;

    if (items.length === 0) {
      document.getElementById('tablaContainer').innerHTML = `
        <div class="text-center py-5 text-muted">
          <i class="bi bi-inbox fs-1 d-block mb-2"></i>
          No se encontraron registros.
        </div>`;
      return;
    }

    const filas = items.map(item => `
      <tr>
        <td>[MAPEAR COLUMNAS SEGÚN LA ENTIDAD]</td>
        <td>
          <span class="badge rounded-pill ${item.activo ? 'bg-success' : 'bg-secondary'}">
            ${item.activo ? 'Activo' : 'Inactivo'}
          </span>
        </td>
        <td class="text-end">
          <a href="/[Modulo]/[Entidad]/Edit/${item.id}"
             class="btn btn-outline-primary btn-sm me-1" title="Editar">
            <i class="bi bi-pencil"></i>
          </a>
          ${item.activo ? `
            <button class="btn btn-vittal-deactivate btn-sm"
                    onclick="confirmarDesactivar('${item.id}')" title="Desactivar">
              <i class="bi bi-slash-circle"></i>
            </button>` : ''}
        </td>
      </tr>`).join('');

    document.getElementById('tablaContainer').innerHTML = `
      <div class="table-responsive">
        <table class="vittal-table">
          <thead>
            <tr>
              <th>[ENCABEZADOS DE COLUMNA]</th>
              <th>Estado</th>
              <th class="text-end">Acciones</th>
            </tr>
          </thead>
          <tbody>${filas}</tbody>
        </table>
      </div>`;
  }

  // ── Búsqueda en cliente ─────────────────────────────────────────
  function configurarBusqueda() {
    document.getElementById('busquedaInput').addEventListener('input', filtrar);
    document.getElementById('filtroEstado').addEventListener('change', filtrar);
  }

  function filtrar() {
    const termino = document.getElementById('busquedaInput').value.toLowerCase();
    const estado  = document.getElementById('filtroEstado').value;
    const filtrados = registros.filter(r => {
      const coincideTexto = JSON.stringify(r).toLowerCase().includes(termino);
      const coincideEstado = estado === '' ? true :
        (estado === 'activo' ? r.activo : !r.activo);
      return coincideTexto && coincideEstado;
    });
    renderTabla(filtrados);
  }

  // ── Desactivar ──────────────────────────────────────────────────
  function confirmarDesactivar(id) {
    idSeleccionado = id;
    modal.show();
  }

  document.getElementById('btnConfirmarDesactivar').addEventListener('click', async () => {
    if (!idSeleccionado) return;
    const res = await VittalAPI.patch(`/[entidad]s/${idSeleccionado}/desactivar`);
    modal.hide();
    if (res?.ok) {
      VittalAPI.showToast('Registro desactivado exitosamente.', 'success');
      await cargarDatos();
    } else {
      VittalAPI.showToast(res?.data?.message || 'Error al desactivar.', 'error');
    }
    idSeleccionado = null;
  });
</script>
}
```

---

## 7. Plantilla Maestra de Vista Create/Edit (Formulario)

```html
<!-- Areas/[Modulo]/Views/[Entidad]/Create.cshtml -->
@{
  ViewData["Title"]      = "Nuevo [Entidad]";
  ViewData["Breadcrumb"] = "<li class='breadcrumb-item'><a href='../'>Lista</a></li>" +
                            "<li class='breadcrumb-item active'>Nuevo</li>";
}

<div class="row justify-content-center">
  <div class="col-lg-8 col-xl-7">
    <div class="vittal-card">
      <div class="d-flex align-items-center gap-2 mb-4">
        <i class="bi bi-plus-circle text-primary fs-5"></i>
        <h5 class="mb-0">Registrar nuevo [Entidad]</h5>
      </div>

      <form id="frm[Entidad]" novalidate>
        <div class="row g-3">
          <!-- ── CAMPOS DEL FORMULARIO ── -->
          <!-- Adaptar según los campos de la entidad -->

          <!-- Ejemplo: Campo de texto obligatorio -->
          <div class="col-md-6">
            <label for="primerNombre" class="form-label fw-medium">
              Primer Nombre <span class="text-danger">*</span>
            </label>
            <input type="text" id="primerNombre" name="primerNombre"
                   class="form-control" maxlength="100"
                   data-val="true"
                   data-val-required="El primer nombre es obligatorio."
                   data-val-length="No puede superar 100 caracteres."
                   data-val-length-max="100" />
            <span class="text-danger small field-validation-valid"
                  data-valmsg-for="primerNombre"></span>
          </div>

          <!-- Ejemplo: Select con options cargados desde API -->
          <div class="col-md-6">
            <label for="doctorId" class="form-label fw-medium">
              Doctor <span class="text-danger">*</span>
            </label>
            <select id="doctorId" name="doctorId" class="form-select"
                    data-val="true"
                    data-val-required="Debe seleccionar un doctor.">
              <option value="">Seleccione un doctor...</option>
            </select>
            <span class="text-danger small field-validation-valid"
                  data-valmsg-for="doctorId"></span>
          </div>

          <!-- Ejemplo: Campo de sexo con radio buttons -->
          <div class="col-md-6">
            <label class="form-label fw-medium">
              Sexo <span class="text-danger">*</span>
            </label>
            <div class="d-flex gap-3 mt-1">
              <div class="form-check">
                <input class="form-check-input" type="radio"
                       name="sexo" id="sexoM" value="M" />
                <label class="form-check-label" for="sexoM">Masculino</label>
              </div>
              <div class="form-check">
                <input class="form-check-input" type="radio"
                       name="sexo" id="sexoF" value="F" />
                <label class="form-check-label" for="sexoF">Femenino</label>
              </div>
            </div>
          </div>

          <!-- ── FIN CAMPOS ── -->

          <!-- Botones de acción -->
          <div class="col-12 d-flex justify-content-end gap-2 pt-2 border-top mt-2">
            <a href="@Url.Action("Index", "[Entidad]")"
               class="btn btn-outline-secondary btn-sm">
              <i class="bi bi-arrow-left me-1"></i> Cancelar
            </a>
            <button type="submit" class="btn btn-vittal-primary btn-sm" id="btnGuardar">
              <span id="btnSpinner" class="spinner-border spinner-border-sm me-1 d-none"></span>
              <i class="bi bi-check-lg me-1" id="btnIcon"></i>
              Guardar [Entidad]
            </button>
          </div>
        </div>
      </form>
    </div>
  </div>
</div>

@section Scripts {
<script>
  // ── Cargar selects desde la API al inicializar ──────────────────
  document.addEventListener('DOMContentLoaded', async () => {
    await cargarDoctores();
    // await cargarOtrosCatalogos();
  });

  async function cargarDoctores() {
    const res = await VittalAPI.get('/usuarios?esDoctores=true');
    if (!res?.ok) return;
    const select = document.getElementById('doctorId');
    (res.data.data || []).forEach(u => {
      const opt = document.createElement('option');
      opt.value = u.id;
      opt.text  = `${u.nombres} ${u.apellidos}`;
      select.appendChild(opt);
    });
  }

  // ── Envío del formulario ────────────────────────────────────────
  document.getElementById('frm[Entidad]').addEventListener('submit', async (e) => {
    e.preventDefault();

    // Activar validación jQuery Validate
    if (!$(e.target).valid()) return;

    // Estado de carga en el botón
    setLoading(true);

    const payload = {
      primerNombre:  document.getElementById('primerNombre').value.trim(),
      doctorId:      document.getElementById('doctorId').value,
      sexo:          document.querySelector('input[name="sexo"]:checked')?.value || ''
      // Agregar todos los campos del formulario
    };

    const res = await VittalAPI.post('/[entidad]s', payload);
    setLoading(false);

    if (res?.ok) {
      VittalAPI.showToast('[Entidad] registrado exitosamente.', 'success');
      setTimeout(() => window.location.href = '../', 1200);
    } else {
      // Mostrar errores de validación del servidor
      const errores = res?.data?.errors || [res?.data?.message || 'Error al guardar.'];
      mostrarErrores(errores);
    }
  });

  function setLoading(loading) {
    const btn     = document.getElementById('btnGuardar');
    const spinner = document.getElementById('btnSpinner');
    const icon    = document.getElementById('btnIcon');
    btn.disabled  = loading;
    spinner.classList.toggle('d-none', !loading);
    icon.classList.toggle('d-none', loading);
  }

  function mostrarErrores(errores) {
    errores.forEach(e => VittalAPI.showToast(e, 'error'));
  }
</script>
}
```

---

## 8. Vista del Login (HU02)

```html
<!-- Areas/Login/Views/Auth/Login.cshtml -->
@{
  Layout = "~/Views/Shared/_LayoutLogin.cshtml";
  ViewData["Title"] = "Iniciar Sesión";
}

<div class="min-vh-100 d-flex align-items-center justify-content-center"
     style="background: linear-gradient(135deg, #1A2535 0%, #1A6FA8 100%);">
  <div class="card shadow-lg border-0" style="width:420px; border-radius:16px;">
    <div class="card-body p-5">

      <!-- Logo y título -->
      <div class="text-center mb-4">
        <i class="bi bi-heart-pulse-fill text-primary" style="font-size:3rem;"></i>
        <h3 class="fw-bold mt-2 mb-0" style="color:var(--vittal-text)">Vittal</h3>
        <p class="text-muted small">Sistema de Gestión Médica</p>
      </div>

      <!-- Alerta de error -->
      <div id="alertaError" class="alert alert-danger d-none" role="alert">
        <i class="bi bi-exclamation-triangle me-2"></i>
        <span id="mensajeError"></span>
      </div>

      <!-- Formulario -->
      <form id="frmLogin" novalidate>
        <div class="mb-3">
          <label for="usuario" class="form-label fw-medium small">
            Usuario <span class="text-danger">*</span>
          </label>
          <div class="input-group">
            <span class="input-group-text bg-light border-end-0">
              <i class="bi bi-person text-muted"></i>
            </span>
            <input type="email" id="usuario" name="usuario"
                   class="form-control border-start-0"
                   placeholder="correo@ejemplo.com"
                   autocomplete="username" required />
          </div>
          <div class="invalid-feedback">Ingrese su correo electrónico.</div>
        </div>

        <div class="mb-4">
          <label for="contrasena" class="form-label fw-medium small">
            Contraseña <span class="text-danger">*</span>
          </label>
          <div class="input-group">
            <span class="input-group-text bg-light border-end-0">
              <i class="bi bi-lock text-muted"></i>
            </span>
            <input type="password" id="contrasena" name="contrasena"
                   class="form-control border-start-0 border-end-0"
                   placeholder="••••••••"
                   autocomplete="current-password" required />
            <button type="button" class="input-group-text bg-light border-start-0"
                    id="togglePassword" title="Mostrar/ocultar contraseña">
              <i class="bi bi-eye" id="eyeIcon"></i>
            </button>
          </div>
          <div class="invalid-feedback">Ingrese su contraseña.</div>
        </div>

        <button type="submit" class="btn btn-vittal-primary w-100"
                id="btnLogin" style="border-radius:8px; padding:.65rem;">
          <span class="spinner-border spinner-border-sm me-2 d-none" id="loginSpinner"></span>
          <i class="bi bi-box-arrow-in-right me-1" id="loginIcon"></i>
          Iniciar Sesión
        </button>
      </form>

      <p class="text-muted text-center mt-3 mb-0" style="font-size:0.78rem;">
        &copy; @DateTime.Now.Year Vittal — Todos los derechos reservados
      </p>
    </div>
  </div>
</div>

@section Scripts {
<script>
  // Mostrar/ocultar contraseña
  document.getElementById('togglePassword').addEventListener('click', () => {
    const input = document.getElementById('contrasena');
    const icon  = document.getElementById('eyeIcon');
    const isPass = input.type === 'password';
    input.type = isPass ? 'text' : 'password';
    icon.className = isPass ? 'bi bi-eye-slash' : 'bi bi-eye';
  });

  // Envío del formulario de login
  document.getElementById('frmLogin').addEventListener('submit', async (e) => {
    e.preventDefault();

    const usuario   = document.getElementById('usuario').value.trim();
    const contrasena = document.getElementById('contrasena').value;

    if (!usuario || !contrasena) {
      document.getElementById('usuario').classList.add('is-invalid');
      document.getElementById('contrasena').classList.add('is-invalid');
      return;
    }

    setLoginLoading(true);
    ocultarError();

    const res = await fetch('/api/auth/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ usuario, contrasena })
    });

    const data = await res.json();
    setLoginLoading(false);

    if (res.ok && data.success) {
      // El servidor guarda el token en la sesión — redirigir
      window.location.href = '/Dashboard/Dashboard/Index';
    } else {
      mostrarError(data.message || 'Usuario o contraseña incorrectos.');
    }
  });

  function setLoginLoading(loading) {
    document.getElementById('btnLogin').disabled  = loading;
    document.getElementById('loginSpinner').classList.toggle('d-none', !loading);
    document.getElementById('loginIcon').classList.toggle('d-none', loading);
  }

  function mostrarError(msg) {
    document.getElementById('alertaError').classList.remove('d-none');
    document.getElementById('mensajeError').textContent = msg;
  }

  function ocultarError() {
    document.getElementById('alertaError').classList.add('d-none');
  }
</script>
}
```

---

## 9. Vista Cola de Espera con Supabase Realtime (HU18)

```html
<!-- Areas/ColaEspera/Views/ColaEspera/Index.cshtml -->
@{
  ViewData["Title"] = "Cola de Espera";
}

<div class="row g-3 mb-3">
  <div class="col-md-4">
    <label class="form-label fw-medium small text-muted">FILTRAR POR DOCTOR</label>
    <select id="filtroDoctorId" class="form-select form-select-sm">
      <option value="">Todos los doctores</option>
    </select>
  </div>
  <div class="col-auto align-self-end">
    <span class="badge bg-primary rounded-pill px-3 py-2" id="contadorCola">0 pacientes</span>
    <span class="ms-2 text-muted small">
      <i class="bi bi-circle-fill text-success me-1" style="font-size:.5rem"></i>
      Tiempo real activo
    </span>
  </div>
</div>

<div id="colaContainer" class="row g-3">
  <div class="col-12 text-center py-5">
    <div class="vittal-spinner mx-auto"></div>
  </div>
</div>

@section Scripts {
<script src="~/js/modules/cola-espera.js"></script>
}
```

```javascript
// src/Vittal.Aplicacion/wwwroot/js/modules/cola-espera.js
(async () => {
  const SUPABASE_URL     = window.VITTAL_SUPABASE_URL;
  const SUPABASE_ANON    = window.VITTAL_SUPABASE_ANON_KEY;
  const CLINICA_ID       = window.VITTAL_CLINICA_ID;

  const supabase = window.supabase.createClient(SUPABASE_URL, SUPABASE_ANON);

  let filtroDoctorId = null;

  // ── Cargar doctores en el select ────────────────────────────────
  const resDoc = await VittalAPI.get('/usuarios?esDoctores=true');
  const select = document.getElementById('filtroDoctorId');
  (resDoc?.data?.data || []).forEach(u => {
    const opt = document.createElement('option');
    opt.value = u.id;
    opt.text  = `${u.nombres} ${u.apellidos}`;
    select.appendChild(opt);
  });

  select.addEventListener('change', () => {
    filtroDoctorId = select.value || null;
    cargarCola();
  });

  // ── Cargar cola de espera ───────────────────────────────────────
  async function cargarCola() {
    const endpoint = filtroDoctorId
      ? `/citas/cola-espera?doctorId=${filtroDoctorId}`
      : '/citas/cola-espera';

    const res = await VittalAPI.get(endpoint);
    const citas = res?.data?.data || [];

    document.getElementById('contadorCola').textContent =
      `${citas.length} paciente${citas.length !== 1 ? 's' : ''}`;

    if (citas.length === 0) {
      document.getElementById('colaContainer').innerHTML = `
        <div class="col-12 text-center py-5 text-muted">
          <i class="bi bi-people fs-1 d-block mb-2 opacity-25"></i>
          No hay pacientes en espera en este momento.
        </div>`;
      return;
    }

    document.getElementById('colaContainer').innerHTML =
      citas.map(renderTarjetaPaciente).join('');
  }

  function renderTarjetaPaciente(cita) {
    const minEspera = calcularMinutosEspera(cita.horaLlegada);
    const estadoClass = {
      'agendada':   'border-primary',
      'en_espera':  'border-warning',
      'en_atencion':'border-purple'
    }[cita.estado] || 'border-secondary';

    return `
      <div class="col-md-6 col-xl-4">
        <div class="vittal-card border-start border-4 ${estadoClass} p-3">
          <div class="d-flex align-items-start gap-3">
            <div class="flex-shrink-0">
              ${cita.pacienteFotoUrl
                ? `<img src="${cita.pacienteFotoUrl}" class="rounded-circle"
                        width="48" height="48" style="object-fit:cover" alt="Foto">`
                : `<div class="rounded-circle bg-light d-flex align-items-center justify-content-center"
                        style="width:48px;height:48px">
                     <i class="bi bi-person text-muted fs-5"></i>
                   </div>`}
            </div>
            <div class="flex-grow-1 overflow-hidden">
              <div class="fw-semibold text-truncate">
                ${cita.pacientePrimerNombre} ${cita.pacientePrimerApellido}
              </div>
              <div class="text-muted small">
                <i class="bi bi-clock me-1"></i>${cita.horaCita}
                ${minEspera !== null
                  ? `<span class="ms-2 badge ${minEspera > 30 ? 'bg-danger' : 'bg-warning text-dark'}">
                       ${minEspera} min espera
                     </span>`
                  : ''}
              </div>
              <div class="small text-muted text-truncate mt-1">
                <i class="bi bi-person-badge me-1"></i>
                Dr. ${cita.doctorNombres} ${cita.doctorApellidos}
              </div>
            </div>
          </div>
          <div class="d-flex gap-2 mt-3">
            <span class="badge badge-${cita.estado.replace('_','-')} flex-grow-1 py-2">
              ${formatearEstado(cita.estado)}
            </span>
            ${cita.estado === 'agendada'
              ? `<button class="btn btn-warning btn-sm"
                         onclick="registrarLlegada('${cita.id}')">
                   <i class="bi bi-person-check me-1"></i> Llegó
                 </button>` : ''}
            ${cita.estado === 'en_espera'
              ? `<button class="btn btn-primary btn-sm"
                         onclick="atenderPaciente('${cita.id}')">
                   <i class="bi bi-arrow-right-circle me-1"></i> Atender
                 </button>` : ''}
          </div>
        </div>
      </div>`;
  }

  function calcularMinutosEspera(horaLlegada) {
    if (!horaLlegada) return null;
    const [h, m] = horaLlegada.split(':').map(Number);
    const llegada = new Date();
    llegada.setHours(h, m, 0);
    return Math.floor((Date.now() - llegada.getTime()) / 60000);
  }

  function formatearEstado(estado) {
    return {
      agendada:    'Agendada',
      en_espera:   'En espera',
      en_atencion: 'En atención',
      atendida:    'Atendida',
      cancelada:   'Cancelada'
    }[estado] || estado;
  }

  // ── Acciones ────────────────────────────────────────────────────
  window.registrarLlegada = async (citaId) => {
    const res = await VittalAPI.patch(`/citas/${citaId}/llegada`);
    if (res?.ok) VittalAPI.showToast('Llegada del paciente registrada.', 'success');
    else VittalAPI.showToast('Error al registrar llegada.', 'error');
  };

  window.atenderPaciente = async (citaId) => {
    const res = await VittalAPI.patch(`/citas/${citaId}/atender`);
    if (res?.ok) {
      VittalAPI.showToast('Paciente marcado como en atención.', 'success');
      // Redirigir al expediente del paciente
      window.location.href = `/Expedientes/Expedientes/Index?citaId=${citaId}`;
    } else {
      VittalAPI.showToast('Error al atender paciente.', 'error');
    }
  };

  // ── Supabase Realtime — actualización automática ─────────────────
  const channel = supabase
    .channel(`cola-espera-${CLINICA_ID}`)
    .on('postgres_changes', {
      event:  '*',
      schema: 'public',
      table:  'citas',
      filter: `clinica_id=eq.${CLINICA_ID}`
    }, () => {
      // Recargar la cola cuando hay cambios en la tabla citas
      cargarCola();
    })
    .subscribe();

  // ── Carga inicial ────────────────────────────────────────────────
  await cargarCola();

  // Actualización cada 60 segundos como fallback
  setInterval(cargarCola, 60000);
})();
```

---

## 10. Alertas en Tiempo Real (_Alerts.cshtml)

```html
<!-- Views/Shared/_Alerts.cshtml -->
<!-- Panel lateral de alertas de tiempo de espera excedido -->
<div id="panelAlertas" class="offcanvas offcanvas-end" tabindex="-1"
     style="width:380px;" aria-labelledby="panelAlertasLabel">
  <div class="offcanvas-header border-bottom">
    <h5 class="offcanvas-title" id="panelAlertasLabel">
      <i class="bi bi-bell-fill text-warning me-2"></i>
      Alertas de espera
    </h5>
    <button type="button" class="btn-close" data-bs-dismiss="offcanvas"></button>
  </div>
  <div class="offcanvas-body p-0">
    <div id="listaAlertas" class="p-3">
      <p class="text-muted text-center small py-4">Sin alertas activas.</p>
    </div>
  </div>
</div>

<!-- Botón flotante de alertas en el navbar -->
<div class="position-fixed" style="top:1rem; right:1.5rem; z-index:1050;">
  <button class="btn btn-warning rounded-circle shadow"
          data-bs-toggle="offcanvas"
          data-bs-target="#panelAlertas"
          style="width:44px; height:44px;"
          id="btnAlertas" title="Ver alertas de espera">
    <i class="bi bi-bell-fill"></i>
    <span class="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger d-none"
          id="contadorAlertas">0</span>
  </button>
</div>
```

```javascript
// src/Vittal.Aplicacion/wwwroot/js/vittal-alerts.js
// Suscripción Realtime a alertas de tiempo de espera excedido

(async () => {
  const CLINICA_ID = window.VITTAL_CLINICA_ID;
  if (!CLINICA_ID) return;

  const supabase = window.supabase.createClient(
    window.VITTAL_SUPABASE_URL,
    window.VITTAL_SUPABASE_ANON_KEY
  );

  let alertasActivas = [];

  function renderAlertas() {
    const lista    = document.getElementById('listaAlertas');
    const contador = document.getElementById('contadorAlertas');
    const noResueltas = alertasActivas.filter(a => !a.resuelta);

    if (noResueltas.length === 0) {
      lista.innerHTML = '<p class="text-muted text-center small py-4">Sin alertas activas.</p>';
      contador.classList.add('d-none');
      return;
    }

    contador.textContent = noResueltas.length;
    contador.classList.remove('d-none');

    // Sonido de notificación (solo en la primera alerta nueva)
    try { new Audio('/sounds/alert.mp3').play().catch(() => {}); } catch (_) {}

    lista.innerHTML = noResueltas.map(a => `
      <div class="border-start border-4 border-warning p-3 mb-2 rounded-end bg-light">
        <div class="fw-semibold small">${a.pacienteNombre}</div>
        <div class="text-muted" style="font-size:0.78rem;">
          <i class="bi bi-clock me-1"></i>Cita: ${a.horaCita}
          &nbsp;·&nbsp;
          <i class="bi bi-hourglass me-1"></i>${a.minutosEspera} min esperando
        </div>
        ${a.salaNombre
          ? `<div class="text-muted" style="font-size:0.78rem;">
               <i class="bi bi-geo me-1"></i>${a.salaNombre}
             </div>` : ''}
        <div class="text-muted" style="font-size:0.78rem;">
          <i class="bi bi-person-badge me-1"></i>Dr. ${a.doctorNombre}
        </div>
      </div>`).join('');
  }

  // Suscripción a la tabla de alertas de espera
  supabase
    .channel(`alertas-${CLINICA_ID}`)
    .on('postgres_changes', {
      event:  'INSERT',
      schema: 'public',
      table:  'alertas_espera',
      filter: `clinica_id=eq.${CLINICA_ID}`
    }, payload => {
      alertasActivas.push(payload.new);
      renderAlertas();
    })
    .subscribe();
})();
```

---

## 11. Checklist de Calidad — @EspecialistaUI (Views)

Antes de notificar al @PM que las vistas están listas:

### Estructura y área

- [ ] Vista creada en el área correcta: `Areas/[Modulo]/Views/[Entidad]/`
- [ ] Vistas mínimas del módulo creadas: `Index.cshtml` (listado) + `Create.cshtml` (alta)
- [ ] `Edit.cshtml` creado si la HU requiere edición
- [ ] Layout correcto especificado en `@{ Layout = "..."; }`
- [ ] `ViewData["Title"]` y `ViewData["Breadcrumb"]` definidos

### Diseño y UX

- [ ] Diseño responsive con Bootstrap 5 — funciona en móvil y escritorio
- [ ] Usa clases `vittal-card`, `vittal-table`, `btn-vittal-primary` del sistema de diseño
- [ ] El botón de desactivar es **naranja** (`btn-vittal-deactivate`) — nunca rojo
- [ ] Modal de confirmación antes de desactivar cualquier registro
- [ ] Estado vacío cuando no hay datos (ícono + mensaje amigable)
- [ ] Spinner de carga mientras se obtienen datos del API
- [ ] Loading state en botón de formulario durante el submit
- [ ] Todos los textos de la UI están en **español**

### Formularios y validación

- [ ] `novalidate` en el form — validación vía jQuery Validate o JS manual
- [ ] Campos obligatorios marcados con `<span class="text-danger">*</span>`
- [ ] Validación cliente activada antes del submit con `$(form).valid()`
- [ ] Errores del servidor mostrados como toasts al usuario
- [ ] Selects cargados desde el API (no hardcodeados)
- [ ] Formulario limpiado o redirigido tras éxito

### Llamadas al API

- [ ] Toda llamada al API usa `VittalAPI.get/post/put/patch` — nunca `fetch` directo
- [ ] `clinicaId` NO se pasa en el body — el API lo extrae del JWT automáticamente
- [ ] Errores de red manejados (res null → mostrar mensaje)
- [ ] Respuesta 401 → redirige al login (manejado por `vittal-api.js`)

### Tiempo real (Cola de Espera y Alertas)

- [ ] Supabase JS Client inicializado con `VITTAL_SUPABASE_URL` y `VITTAL_SUPABASE_ANON_KEY`
- [ ] Canal Realtime suscrito con filtro `clinica_id=eq.${CLINICA_ID}`
- [ ] La UI se actualiza sin recargar la página completa
- [ ] Fallback de polling cada 60 segundos para casos donde Realtime falla

---

*skill-view.md — Vittal v1.0.0 | Agente: @EspecialistaUI*
*Para contexto del proyecto: CLAUDE.md | Para controllers API: skill-controller.md*
*Para coordinación de agentes: ORCHESTRATOR.md*
*— Todos los skills completados: supabase · dal · bll · controller · view —*
