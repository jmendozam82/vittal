# View — Realtime Views (Cola de Espera & Alertas)

> **Agente propietario:** @EspecialistaUI
> **Cuándo cargar:** Para módulos con Supabase Realtime (Cola de Espera HU18, Alertas HU23).
> **Prerequisito:** skills/view/SKILL.md, skills/supabase/realtime.md

---

## Cola de Espera — Vista (Index.cshtml)

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

---

## Cola de Espera — Módulo JS (cola-espera.js)

```javascript
// src/Vittal.Aplicacion/wwwroot/js/modules/cola-espera.js
(async () => {
  const supabase = window.supabase.createClient(
    window.VITTAL_SUPABASE_URL,
    window.VITTAL_SUPABASE_ANON_KEY
  );

  let filtroDoctorId = null;

  // Cargar doctores
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
          No hay pacientes en espera.
        </div>`;
      return;
    }

    document.getElementById('colaContainer').innerHTML =
      citas.map(renderTarjetaPaciente).join('');
  }

  function renderTarjetaPaciente(cita) {
    const minEspera = calcularMinutosEspera(cita.horaLlegada);
    const estadoClass = {
      'agendada': 'border-primary',
      'en_espera': 'border-warning',
      'en_atencion': 'border-purple'
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
                     </span>` : ''}
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
      agendada: 'Agendada', en_espera: 'En espera',
      en_atencion: 'En atención', atendida: 'Atendida', cancelada: 'Cancelada'
    }[estado] || estado;
  }

  window.registrarLlegada = async (citaId) => {
    const res = await VittalAPI.patch(`/citas/${citaId}/llegada`);
    if (res?.ok) VittalAPI.showToast('Llegada registrada.', 'success');
    else VittalAPI.showToast('Error al registrar llegada.', 'error');
  };

  window.atenderPaciente = async (citaId) => {
    const res = await VittalAPI.patch(`/citas/${citaId}/atender`);
    if (res?.ok) {
      VittalAPI.showToast('Paciente en atención.', 'success');
      window.location.href = `/Expedientes/Expedientes/Index?citaId=${citaId}`;
    } else {
      VittalAPI.showToast('Error al atender paciente.', 'error');
    }
  };

  // Supabase Realtime
  const channel = supabase
    .channel(`cola-espera-${window.VITTAL_CLINICA_ID}`)
    .on('postgres_changes', {
      event: '*', schema: 'public', table: 'citas',
      filter: `clinica_id=eq.${window.VITTAL_CLINICA_ID}`
    }, () => cargarCola())
    .subscribe();

  await cargarCola();
  setInterval(cargarCola, 60000);  // Fallback
})();
```

---

## Panel de Alertas (_Alerts.cshtml)

```html
<!-- Views/Shared/_Alerts.cshtml -->
<div id="panelAlertas" class="offcanvas offcanvas-end" tabindex="-1"
     style="width:380px;" aria-labelledby="panelAlertasLabel">
  <div class="offcanvas-header border-bottom">
    <h5 class="offcanvas-title" id="panelAlertasLabel">
      <i class="bi bi-bell-fill text-warning me-2"></i>Alertas de espera
    </h5>
    <button type="button" class="btn-close" data-bs-dismiss="offcanvas"></button>
  </div>
  <div class="offcanvas-body p-0">
    <div id="listaAlertas" class="p-3">
      <p class="text-muted text-center small py-4">Sin alertas activas.</p>
    </div>
  </div>
</div>

<!-- Botón flotante -->
<div class="position-fixed" style="top:1rem; right:1.5rem; z-index:1050;">
  <button class="btn btn-warning rounded-circle shadow"
          data-bs-toggle="offcanvas" data-bs-target="#panelAlertas"
          style="width:44px; height:44px;" id="btnAlertas" title="Ver alertas">
    <i class="bi bi-bell-fill"></i>
    <span class="position-absolute top-0 start-100 translate-middle
                 badge rounded-pill bg-danger d-none" id="contadorAlertas">0</span>
  </button>
</div>
```

---

## Alertas — JS (vittal-alerts.js)

```javascript
// src/Vittal.Aplicacion/wwwroot/js/vittal-alerts.js
(async () => {
  const CLINICA_ID = window.VITTAL_CLINICA_ID;
  if (!CLINICA_ID) return;

  const supabase = window.supabase.createClient(
    window.VITTAL_SUPABASE_URL, window.VITTAL_SUPABASE_ANON_KEY);

  let alertasActivas = [];

  function renderAlertas() {
    const lista = document.getElementById('listaAlertas');
    const contador = document.getElementById('contadorAlertas');
    const noResueltas = alertasActivas.filter(a => !a.resuelta);

    if (noResueltas.length === 0) {
      lista.innerHTML = '<p class="text-muted text-center small py-4">Sin alertas activas.</p>';
      contador.classList.add('d-none');
      return;
    }

    contador.textContent = noResueltas.length;
    contador.classList.remove('d-none');
    try { new Audio('/sounds/alert.mp3').play().catch(() => {}); } catch (_) {}

    lista.innerHTML = noResueltas.map(a => `
      <div class="border-start border-4 border-warning p-3 mb-2 rounded-end bg-light">
        <div class="fw-semibold small">${a.pacienteNombre}</div>
        <div class="text-muted" style="font-size:0.78rem;">
          <i class="bi bi-clock me-1"></i>Cita: ${a.horaCita}
          &nbsp;·&nbsp;<i class="bi bi-hourglass me-1"></i>${a.minutosEspera} min esperando
        </div>
        ${a.salaNombre ? `<div class="text-muted" style="font-size:0.78rem;">
          <i class="bi bi-geo me-1"></i>${a.salaNombre}</div>` : ''}
        <div class="text-muted" style="font-size:0.78rem;">
          <i class="bi bi-person-badge me-1"></i>Dr. ${a.doctorNombre}</div>
      </div>`).join('');
  }

  supabase.channel(`alertas-${CLINICA_ID}`)
    .on('postgres_changes', {
      event: 'INSERT', schema: 'public', table: 'alertas_espera',
      filter: `clinica_id=eq.${CLINICA_ID}`
    }, payload => { alertasActivas.push(payload.new); renderAlertas(); })
    .subscribe();
})();
```

---

## Checklist de Calidad — Realtime Views

### Cola de Espera
- [ ] Tarjetas con borde izquierdo de color según estado
- [ ] Foto del paciente o placeholder con ícono
- [ ] Minutos de espera calculados y badge de color (>30 = danger)
- [ ] Botón "Llegó" solo para estado agendada
- [ ] Botón "Atender" solo para estado en_espera
- [ ] "Atender" redirige al expediente con citaId
- [ ] Supabase Realtime suscrito a tabla `citas` con filtro clinica_id
- [ ] Fallback de polling cada 60s
- [ ] Contador de pacientes visible
- [ ] Indicador "Tiempo real activo" con punto verde

### Alertas
- [ ] Offcanvas panel lateral derecho
- [ ] Botón flotante con badge de contador
- [ ] Sonido de notificación en nueva alerta
- [ ] Tarjetas con borde amarillo y datos completos
- [ ] Suscripción a INSERT en `alertas_espera` con filtro clinica_id

---

*skills/view/realtime-views.md — Vittal v1.0.0*
