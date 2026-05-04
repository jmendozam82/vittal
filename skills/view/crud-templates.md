# View — CRUD Templates (Index, Create, Edit)

> **Agente propietario:** @EspecialistaUI
> **Cuándo cargar:** Para crear vistas de listado, alta y edición de catálogos.
> **Prerequisito:** skills/view/SKILL.md

---

## Vista Index (Listado)

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
  let registros = [];
  let idSeleccionado = null;
  const modal = new bootstrap.Modal(document.getElementById('modalDesactivar'));

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
        <td>[MAPEAR COLUMNAS]</td>
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
              <th>[ENCABEZADOS]</th>
              <th>Estado</th>
              <th class="text-end">Acciones</th>
            </tr>
          </thead>
          <tbody>${filas}</tbody>
        </table>
      </div>`;
  }

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

  function confirmarDesactivar(id) { idSeleccionado = id; modal.show(); }

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

## Vista Create (Formulario)

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
          <!-- Campos del formulario: adaptar según entidad -->
          <!-- Ejemplo: Campo texto obligatorio -->
          <div class="col-md-6">
            <label for="nombre" class="form-label fw-medium">
              Nombre <span class="text-danger">*</span>
            </label>
            <input type="text" id="nombre" name="nombre"
                   class="form-control" maxlength="100"
                   data-val="true"
                   data-val-required="El nombre es obligatorio." />
            <span class="text-danger small field-validation-valid"
                  data-valmsg-for="nombre"></span>
          </div>

          <!-- Ejemplo: Select desde API -->
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
  document.addEventListener('DOMContentLoaded', async () => {
    // await cargarCatalogos();  // Ejemplo: cargarDoctores()
  });

  document.getElementById('frm[Entidad]').addEventListener('submit', async (e) => {
    e.preventDefault();
    if (!$(e.target).valid()) return;

    setLoading(true);
    const payload = {
      // Mapear campos del formulario
    };

    const res = await VittalAPI.post('/[entidad]s', payload);
    setLoading(false);

    if (res?.ok) {
      VittalAPI.showToast('[Entidad] registrado exitosamente.', 'success');
      setTimeout(() => window.location.href = '../', 1200);
    } else {
      const errores = res?.data?.errors || [res?.data?.message || 'Error al guardar.'];
      errores.forEach(e => VittalAPI.showToast(e, 'error'));
    }
  });

  function setLoading(loading) {
    const btn = document.getElementById('btnGuardar');
    const spinner = document.getElementById('btnSpinner');
    const icon = document.getElementById('btnIcon');
    btn.disabled = loading;
    spinner.classList.toggle('d-none', !loading);
    icon.classList.toggle('d-none', loading);
  }
</script>
}
```

---

## Checklist de Calidad — CRUD Views

### Index
- [ ] Buscador con filtrado en cliente
- [ ] Filtro de estado (activos/inactivos)
- [ ] Contador de registros visible
- [ ] Estado vacío con ícono + mensaje amigable
- [ ] Spinner de carga mientras se obtienen datos
- [ ] Botón "Editar" con icono bi-pencil
- [ ] Botón "Desactivar" naranja (btn-vittal-deactivate)
- [ ] Modal de confirmación antes de desactivar
- [ ] Badge de estado (bg-success/bg-secondary)

### Create/Edit
- [ ] `novalidate` en el form
- [ ] Asterisco rojo en campos obligatorios
- [ ] Validación jQuery Validate: `$(form).valid()`
- [ ] Selects cargados desde API, no hardcodeados
- [ ] Spinner en botón durante submit
- [ ] Toast de éxito + redirección tras guardar
- [ ] Errores del servidor como toasts
- [ ] Botón Cancelar con flecha izquierda

---

*skills/view/crud-templates.md — Vittal v1.0.0*
