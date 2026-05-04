# View — API Client JavaScript

> **Agente propietario:** @EspecialistaUI
> **Cuándo cargar:** Para consumir endpoints del API desde vistas Razor.
> **Prerequisito:** skills/view/SKILL.md

---

## VittalAPI — Cliente Fetch Centralizado

```javascript
// src/Vittal.Aplicacion/wwwroot/js/vittal-api.js
const VittalAPI = (() => {
  const API_BASE = window.VITTAL_API_URL || '/api';

  function getToken() {
    return document.querySelector('meta[name="vittal-token"]')?.content || '';
  }

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
      <button type="button" class="btn-close" data-bs-dismiss="alert"></button>`;
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

  function showLoading(elementId) {
    const el = document.getElementById(elementId);
    if (el) el.innerHTML = `
      <div class="d-flex justify-content-center p-4">
        <div class="vittal-spinner"></div>
      </div>`;
  }

  async function request(method, endpoint, body = null) {
    const options = {
      method,
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${getToken()}`
      }
    };

    if (body) options.body = JSON.stringify(body);

    try {
      const response = await fetch(`${API_BASE}${endpoint}`, options);

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
    get:   (endpoint)       => request('GET',  endpoint),
    post:  (endpoint, body) => request('POST', endpoint, body),
    put:   (endpoint, body) => request('PUT',  endpoint, body),
    patch: (endpoint, body) => request('PATCH',endpoint, body),
    showToast,
    showLoading
  };
})();
```

---

## Configuración Global en _Layout.cshtml

```html
<script>
  window.VITTAL_API_URL        = '@Configuration["App:ApiUrl"]';
  window.VITTAL_SUPABASE_URL   = '@Configuration["Supabase:Url"]';
  window.VITTAL_SUPABASE_ANON_KEY = '@Configuration["Supabase:AnonKey"]';
  window.VITTAL_CLINICA_ID     = '@Context.Session.GetString("ClinicaId")';
</script>
<script src="~/js/vittal-api.js"></script>
<script src="~/js/vittal-validation.js"></script>
<script src="~/js/vittal-alerts.js"></script>
```

---

## Uso Estándar

```javascript
// GET — listar
const res = await VittalAPI.get('/pacientes');
if (res?.ok) {
  const data = res.data.data || [];
  renderTabla(data);
}

// POST — crear
const payload = { primerNombre: 'Juan', primerApellido: 'Pérez' };
const res = await VittalAPI.post('/pacientes', payload);
if (res?.ok) {
  VittalAPI.showToast('Paciente creado exitosamente.', 'success');
} else {
  const errores = res?.data?.errors || [res?.data?.message || 'Error.'];
  errores.forEach(e => VittalAPI.showToast(e, 'error'));
}

// PUT — actualizar
const res = await VittalAPI.put(`/pacientes/${id}`, payload);

// PATCH — desactivar
const res = await VittalAPI.patch(`/pacientes/${id}/desactivar`);

// Loading state
VittalAPI.showLoading('tablaContainer');
```

---

## Checklist de Calidad — API Client

### Uso correcto
- [ ] Toda llamada usa `VittalAPI.get/post/put/patch` — nunca `fetch` directo
- [ ] `clinicaId` NO se pasa en el body — el API lo extrae del JWT
- [ ] Se verifica `res?.ok` antes de procesar datos
- [ ] Respuesta `null` manejada (error de red → toast)
- [ ] 401 → redirige al login (manejado por vittal-api.js)

### UX
- [ ] Toast de éxito tras operación exitosa
- [ ] Toast de error con mensaje descriptivo del servidor
- [ ] Loading spinner antes de llamadas
- [ ] Botón deshabilitado durante submit de formulario

### Toasts
- [ ] Mensajes en español
- [ ] Auto-dismiss tras 5 segundos
- [ ] Botón de cierre manual (btn-close)
- [ ] Íconos Bootstrap Icons (check-circle / exclamation-triangle)

---

*skills/view/api-client.md — Vittal v1.0.0*
