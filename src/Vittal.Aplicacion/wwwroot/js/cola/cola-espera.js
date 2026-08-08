/* ───────────────────────────────────────────────────────────────
   VITTAL COLA DE ESPERA — Interactive Queue Dashboard
   src/Vittal.Aplicacion/wwwroot/js/cola/cola-espera.js
   Auto-refresh, estado transitions, premium cards
   Historia de Usuario: HU18 — Cola de Espera
   ─────────────────────────────────────────────────────────────── */

window.vittalColaEspera = (function() {

    // ── Estado global ─────────────────────────────────────────
    let config = {};
    let state = {
        citas: [],
        doctores: [],
        filterDoctor: 'todos',
        refreshInterval: null,
        isRefreshing: false,
        selectedCitaId: null,   // for modals
        stats: { atendidasHoy: 0 }
    };

    // ── Umbrales de tiempo de espera (minutos) ─────────────────
    const WAIT_THRESHOLDS = {
        warning: 15,   // ≥ 15 min → naranja
        danger:  30    // ≥ 30 min → rojo
    };

    // ── Inicialización ─────────────────────────────────────────
    function init(cfg) {
        config = cfg;

        // Opción A — modo recepción: quien no es doctor/admin/superadmin
        // (recepcionista) SOLO registra llegada y cancela; NO inicia/completa atención.
        state.esRecepcion = !config.esDoctor && !config.esAdmin && !config.esSuperAdmin;

        // Regla 6: un doctor SOLO ve y opera su propia cola.
        // Se fija el filtro a su usuarioId y se oculta el selector de doctores.
        if (config.esDoctor) {
            state.filterDoctor = config.usuarioId || 'todos';
            const filterDoctor = document.getElementById('filterDoctor');
            if (filterDoctor) {
                const wrapper = filterDoctor.closest('.cola-doctor-filter');
                if (wrapper) wrapper.style.display = 'none';
                else filterDoctor.style.display = 'none';
            }
            bindEvents();
            loadCola().then(() => {
                startAutoRefresh();
            });
            return;
        }

        // En modo recepción se oculta el selector de doctores (ve toda la cola del día)
        if (state.esRecepcion) {
            const filterDoctor = document.getElementById('filterDoctor');
            if (filterDoctor) {
                const wrapper = filterDoctor.closest('.cola-doctor-filter');
                if (wrapper) wrapper.style.display = 'none';
                else filterDoctor.style.display = 'none';
            }
        }

        bindEvents();
        loadDoctores();
        loadCola().then(() => {
            startAutoRefresh();
        });
    }

    // ── Eventos del DOM ────────────────────────────────────────
    function bindEvents() {
        // Filtro de doctor
        const filterDoctor = document.getElementById('filterDoctor');
        if (filterDoctor) {
            filterDoctor.addEventListener('change', function() {
                state.filterDoctor = this.value;
                loadCola();  // Recargar desde el servidor con el nuevo filtro
            });
        }

        // Botón refresh manual
        const btnRefresh = document.getElementById('btnRefresh');
        if (btnRefresh) {
            btnRefresh.addEventListener('click', function() {
                refreshNow();
            });
        }

        // Auto-refresh toggle
        const autoRefresh = document.getElementById('autoRefresh');
        if (autoRefresh) {
            autoRefresh.addEventListener('change', function() {
                if (this.checked) {
                    startAutoRefresh();
                } else {
                    stopAutoRefresh();
                }
            });
        }

        // Modal Llegada - confirmar
        const btnConfirmarLlegada = document.getElementById('btnConfirmarLlegada');
        if (btnConfirmarLlegada) {
            btnConfirmarLlegada.addEventListener('click', function() {
                const id = state.selectedCitaId;
                if (id) {
                    confirmLlegada(id);
                }
            });
        }

        // Modal Cancelar - confirmar
        const btnConfirmarCancelar = document.getElementById('btnConfirmarCancelar');
        if (btnConfirmarCancelar) {
            btnConfirmarCancelar.addEventListener('click', function() {
                const id = state.selectedCitaId;
                if (id) {
                    confirmCancelar(id);
                }
            });
        }
    }

    // ── Cargar doctores ────────────────────────────────────────
    async function loadDoctores() {
        try {
            const response = await fetch(config.urls.doctores);
            const result = await response.json();

            if (result.success && result.data) {
                state.doctores = result.data;
                populateDoctorFilter(result.data);
            }
        } catch (err) {
            console.error('Error loading doctors:', err);
        }
    }

    function populateDoctorFilter(doctores) {
        const select = document.getElementById('filterDoctor');
        if (!select) return;

        // Keep "Todos" option
        select.innerHTML = '<option value="todos">Todos los doctores</option>';

        doctores.forEach(function(doc) {
            const option = document.createElement('option');
            // El API devuelve usuarioId como identificador del doctor
            option.value = doc.usuarioId || doc.id || '';
            // Construir nombre completo desde la respuesta del API
            const nombre = [
                doc.nombres || doc.primerNombre || doc.primer_nombre || '',
                doc.apellidos || doc.primerApellido || doc.primer_apellido || ''
            ].filter(Boolean).join(' ') || doc.nombreCompleto || 'Doctor';
            option.textContent = nombre;
            select.appendChild(option);
        });
    }

    // ── Cargar cola ────────────────────────────────────────────
    async function loadCola() {
        try {
            let url = config.urls.cola;
            if (state.filterDoctor !== 'todos') {
                url += '?doctorId=' + encodeURIComponent(state.filterDoctor);
            }

            const response = await fetch(url);
            const result = await response.json();

            if (result.success) {
                state.citas = result.data || [];
                state.stats = result.stats || { atendidasHoy: 0 };
                updateStats(state.citas, state.stats);
                renderCola(state.citas);
                updateTotalCount(state.citas.length);
            } else {
                showToast(result.message || 'Error al cargar cola', 'error');
            }

            return result;
        } catch (err) {
            console.error('Error loading queue:', err);
            showToast('Error de conexión al cargar la cola', 'error');
            return { success: false };
        }
    }

    // ── Refresh ─────────────────────────────────────────────────
    async function refreshNow() {
        if (state.isRefreshing) return;

        const btn = document.getElementById('btnRefresh');
        if (btn) btn.classList.add('spinning');

        state.isRefreshing = true;
        await loadCola();
        state.isRefreshing = false;

        if (btn) setTimeout(function() { btn.classList.remove('spinning'); }, 600);
    }

    function startAutoRefresh() {
        stopAutoRefresh();
        state.refreshInterval = setInterval(function() {
            loadCola();
        }, config.refreshInterval || 10000);
    }

    function stopAutoRefresh() {
        if (state.refreshInterval) {
            clearInterval(state.refreshInterval);
            state.refreshInterval = null;
        }
    }

    // ── Actualizar contadores ──────────────────────────────────
    function updateStats(citas, stats) {
        var agendadas = 0, enEspera = 0, enAtencion = 0;

        citas.forEach(function(c) {
            var est = c.estado;
            if (est === 'agendada') agendadas++;
            else if (est === 'en_espera') enEspera++;
            else if (est === 'en_atencion') enAtencion++;
        });

        setText('statAgendadas', agendadas);
        setText('statEnEspera', enEspera);
        setText('statEnAtencion', enAtencion);
        // El conteo de atendidas viene del servidor (filtro propio en JsonCola)
        setText('statAtendidas', stats ? stats.atendidasHoy : 0);
    }

    function updateTotalCount(count) {
        setText('colaTotalCount', count);
    }

    function setText(id, val) {
        var el = document.getElementById(id);
        if (el) el.textContent = val;
    }

    // ── Renderizar tarjetas ────────────────────────────────────
    function renderCola(citas) {
        var grid = document.getElementById('colaGrid');
        var empty = document.getElementById('colaEmptyState');
        if (!grid) return;

        // Filtrar canceladas del grid
        var visible = citas.filter(function(c) {
            return c.estado !== 'cancelada';
        });

        if (visible.length === 0) {
            grid.innerHTML = '';
            if (empty) empty.classList.add('visible');
            return;
        }

        if (empty) empty.classList.remove('visible');

        var html = '';
        visible.forEach(function(cita) {
            html += buildCard(cita);
        });

        grid.innerHTML = html;
    }

    function buildCard(cita) {
        var id = cita.id || '';
        var estado = cita.estado || 'agendada';
        var pacienteNombre = cita.pacienteNombre || cita.paciente_nombre || 'Paciente';
        var doctorNombre = cita.doctorNombre || cita.doctor_nombre || '';
        var horaCita = formatTime(cita.horaCita || cita.hora_cita || '');
        var horaLlegada = cita.horaLlegada || cita.hora_llegada || null;

        // Obtener iniciales
        var iniciales = getInitials(pacienteNombre);

        // Computar tiempo de espera
        var waitInfo = computeWaitTime(horaLlegada);
        var waitHtml = '';
        if (estado === 'en_espera' || estado === 'en_atencion') {
            if (waitInfo.text) {
                var waitClass = waitInfo.level === 'danger' ? 'cola-wait-danger'
                    : waitInfo.level === 'warning' ? 'cola-wait-warning'
                    : 'cola-wait-normal';
                waitHtml = '<span class="cola-card-wait ' + waitClass + '">'
                    + '<i class="bi bi-hourglass-split"></i>' + waitInfo.text
                    + '</span>';
            }
        }

        // Badge de estado
        var estadoLabel = getEstadoLabel(estado);
        var estadoBadgeClass = 'cola-badge-estado estado-' + estado;

        // Botones de acción según estado
        var actionsHtml = buildActions(id, estado, pacienteNombre);

        // Nombre del doctor (si no está en filtro individual)
        var doctorHtml = doctorNombre
            ? '<span class="cola-card-doctor"><i class="bi bi-person-badge"></i>' + escapeHtml(doctorNombre) + '</span>'
            : '';

        // Sala
        var salaNombre = cita.salaNombre || cita.sala_nombre || '';
        var salaHtml = salaNombre
            ? '<span class="cola-card-doctor ms-2"><i class="bi bi-door-open"></i>' + escapeHtml(salaNombre) + '</span>'
            : '';

        return '<div class="cola-card estado-' + estado + '" data-id="' + id + '">'
            + '<div class="cola-card-header">'
            +   '<div class="cola-avatar">' + escapeHtml(iniciales) + '</div>'
            +   '<div class="cola-card-name">'
            +     '<h6>' + escapeHtml(pacienteNombre) + '</h6>'
            +     doctorHtml
            +   '</div>'
            +   '<span class="' + estadoBadgeClass + '">' + estadoLabel + '</span>'
            + '</div>'
            + '<div class="cola-card-body">'
            +   '<span class="cola-card-time"><i class="bi bi-clock"></i>' + escapeHtml(horaCita) + '</span>'
            +   waitHtml
            +   (salaNombre ? '<span class="cola-card-time"><i class="bi bi-door-open"></i>' + escapeHtml(salaNombre) + '</span>' : '')
            + '</div>'
            + '<div class="cola-card-footer">'
            +   actionsHtml
            + '</div>'
            + '</div>';
    }

    function buildActions(id, estado, pacienteNombre) {
        var html = '';
        var nombreEnc = encodeURIComponent(pacienteNombre || 'Paciente');

        // Opción A — modo recepción: SOLO "Llegó" (agendada) y "Cancelar".
        // El avance clínico (Atender/Completar) queda exclusivamente para el médico.
        if (state.esRecepcion) {
            switch (estado) {
                case 'agendada':
                    html += '<button class="cola-btn-action cola-btn-llegada" onclick="vittalColaEspera.promptLlegada(\'' + id + '\')">'
                        + '<i class="bi bi-box-arrow-in-right"></i> Llegó</button>';
                    html += '<button class="cola-btn-action cola-btn-cancelar" onclick="vittalColaEspera.promptCancelar(\'' + id + '\')" title="Cancelar cita">'
                        + '<i class="bi bi-x-lg"></i></button>';
                    break;

                case 'en_espera':
                    html += '<span class="text-muted small"><i class="bi bi-clock me-1"></i>En espera</span>';
                    html += '<button class="cola-btn-action cola-btn-cancelar" onclick="vittalColaEspera.promptCancelar(\'' + id + '\')" title="Cancelar cita">'
                        + '<i class="bi bi-x-lg"></i></button>';
                    break;

                case 'en_atencion':
                    html += '<span class="text-muted small"><i class="bi bi-play-circle me-1"></i>En atención</span>';
                    break;

                case 'atendida':
                    html += '<span class="text-muted small"><i class="bi bi-check2-all me-1"></i>Atendida</span>';
                    break;
            }

            return html;
        }

        switch (estado) {
            case 'agendada':
                html += '<button class="cola-btn-action cola-btn-llegada" onclick="vittalColaEspera.promptLlegada(\'' + id + '\')">'
                    + '<i class="bi bi-box-arrow-in-right"></i> Llegó</button>';
                html += '<button class="cola-btn-action cola-btn-cancelar" onclick="vittalColaEspera.promptCancelar(\'' + id + '\')" title="Cancelar cita">'
                    + '<i class="bi bi-x-lg"></i></button>';
                break;

            case 'en_espera':
                html += '<button class="cola-btn-action cola-btn-atender" onclick="vittalColaEspera.atender(\'' + id + '\')">'
                    + '<i class="bi bi-play-circle"></i> Atender</button>';
                html += '<button class="cola-btn-action cola-btn-cancelar" onclick="vittalColaEspera.promptCancelar(\'' + id + '\')" title="Cancelar cita">'
                    + '<i class="bi bi-x-lg"></i></button>';
                break;

            case 'en_atencion':
                html += '<a href="/LineaTiempo/LineaTiempo?citaId=' + encodeURIComponent(id) + '&paciente=' + nombreEnc + '" class="cola-btn-action cola-btn-continuar">'
                    + '<i class="bi bi-arrow-right-circle"></i> Continuar</a>';
                html += '<button class="cola-btn-action cola-btn-completar" onclick="vittalColaEspera.completar(\'' + id + '\')">'
                    + '<i class="bi bi-check-circle"></i> Completar</button>';
                html += '<button class="cola-btn-action cola-btn-cancelar" onclick="vittalColaEspera.promptCancelar(\'' + id + '\')" title="Cancelar cita">'
                    + '<i class="bi bi-x-lg"></i></button>';
                break;

            case 'atendida':
                html += '<span class="text-muted small"><i class="bi bi-check2-all me-1"></i>Atendida</span>';
                break;
        }

        return html;
    }

    // ── Transiciones de estado ─────────────────────────────────

    // Llegada: abrir modal de confirmación
    function promptLlegada(id) {
        state.selectedCitaId = id;
        var cita = findCitaById(id);
        var nombre = cita ? (cita.pacienteNombre || 'Paciente') : 'Paciente';

        var modalLabel = document.getElementById('confirmarLlegadaPaciente');
        if (modalLabel) modalLabel.textContent = nombre;

        var modal = new bootstrap.Modal(document.getElementById('modalConfirmarLlegada'));
        modal.show();
    }

    // Llegada: confirmar
    async function confirmLlegada(id) {
        try {
            // Cerrar modal
            var modalEl = document.getElementById('modalConfirmarLlegada');
            var modal = bootstrap.Modal.getInstance(modalEl);
            if (modal) modal.hide();

            var cita = findCitaById(id);
            var nombre = cita ? (cita.pacienteNombre || 'Paciente') : 'Paciente';

            var url = config.urls.llegada + '?id=' + encodeURIComponent(id);
            var response = await fetch(url, { method: 'POST' });
            var result = await response.json();

            if (result.success) {
                showToast('Llegada registrada: ' + nombre + ' está en espera', 'success');
                await loadCola();
            } else {
                showToast(result.message || 'Error al registrar llegada', 'error');
            }
        } catch (err) {
            console.error('Error en llegada:', err);
            showToast('Error de conexión al registrar llegada', 'error');
        }
    }

    // Atender
    async function atender(id) {
        try {
            var cita = findCitaById(id);
            var nombre = cita ? (cita.pacienteNombre || 'Paciente') : 'Paciente';

            var url = config.urls.atender + '?id=' + encodeURIComponent(id);
            var response = await fetch(url, { method: 'POST' });
            var result = await response.json();

            if (result.success) {
                showToast('Atención iniciada: ' + nombre, 'info');
                // Redirigir al expediente del paciente con la citaId pre-seleccionada
                if (result.expedienteId) {
                    window.location.href = '/Expedientes/Expedientes/Details/' + encodeURIComponent(result.expedienteId) + '?citaId=' + encodeURIComponent(result.citaId || id);
                } else {
                    // Fallback: si no hay expediente, ir a línea de tiempo como antes
                    var pacienteParam = encodeURIComponent(nombre);
                    window.location.href = '/LineaTiempo/LineaTiempo?citaId=' + encodeURIComponent(id) + '&paciente=' + pacienteParam;
                }
            } else {
                showToast(result.message || 'Error al iniciar atención', 'error');
            }
        } catch (err) {
            console.error('Error en atender:', err);
            showToast('Error de conexión al iniciar atención', 'error');
        }
    }

    // Completar
    async function completar(id) {
        try {
            var cita = findCitaById(id);
            var nombre = cita ? (cita.pacienteNombre || 'Paciente') : 'Paciente';

            var url = config.urls.completar + '?id=' + encodeURIComponent(id);
            var response = await fetch(url, { method: 'POST' });
            var result = await response.json();

            if (result.success) {
                showToast('Atención completada: ' + nombre, 'success');
                await loadCola();
            } else {
                showToast(result.message || 'Error al completar atención', 'error');
            }
        } catch (err) {
            console.error('Error en completar:', err);
            showToast('Error de conexión al completar atención', 'error');
        }
    }

    // Cancelar: abrir modal
    function promptCancelar(id) {
        state.selectedCitaId = id;
        var cita = findCitaById(id);
        var nombre = cita ? (cita.pacienteNombre || 'Paciente') : 'Paciente';

        var modalLabel = document.getElementById('confirmarCancelarPaciente');
        if (modalLabel) modalLabel.textContent = nombre;

        var modal = new bootstrap.Modal(document.getElementById('modalConfirmarCancelar'));
        modal.show();
    }

    // Cancelar: confirmar
    async function confirmCancelar(id) {
        try {
            var modalEl = document.getElementById('modalConfirmarCancelar');
            var modal = bootstrap.Modal.getInstance(modalEl);
            if (modal) modal.hide();

            var cita = findCitaById(id);
            var nombre = cita ? (cita.pacienteNombre || 'Paciente') : 'Paciente';

            var url = config.urls.cancelar + '?id=' + encodeURIComponent(id);
            var response = await fetch(url, { method: 'POST' });
            var result = await response.json();

            if (result.success) {
                showToast('Cita cancelada: ' + nombre, 'info');
                await loadCola();
            } else {
                showToast(result.message || 'Error al cancelar cita', 'error');
            }
        } catch (err) {
            console.error('Error en cancelar:', err);
            showToast('Error de conexión al cancelar cita', 'error');
        }
    }

    // ── Helpers ─────────────────────────────────────────────────
    function findCitaById(id) {
        for (var i = 0; i < state.citas.length; i++) {
            if (state.citas[i].id === id) return state.citas[i];
        }
        return null;
    }

    function getInitials(name) {
        if (!name) return '??';
        var parts = name.trim().split(/\s+/);
        if (parts.length === 1) return parts[0].substring(0, 2).toUpperCase();
        return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
    }

    function formatTime(time) {
        if (!time) return '--:--';
        var parts = time.toString().split(':');
        return parts[0].padStart(2, '0') + ':' + parts[1].padStart(2, '0');
    }

    function getEstadoLabel(estado) {
        var labels = {
            'agendada': 'Pendiente',
            'en_espera': 'En Espera',
            'en_atencion': 'En Atención',
            'atendida': 'Atendida',
            'cancelada': 'Cancelada'
        };
        return labels[estado] || estado;
    }

    function computeWaitTime(horaLlegada) {
        if (!horaLlegada) return { text: '', level: 'none' };

        var parts = horaLlegada.toString().split(':');
        if (parts.length < 2) return { text: '', level: 'none' };

        var llegadaMin = parseInt(parts[0], 10) * 60 + parseInt(parts[1], 10);
        var now = new Date();
        var nowMin = now.getHours() * 60 + now.getMinutes();

        var diffMin = nowMin - llegadaMin;
        if (diffMin < 0) diffMin += 1440; // pasó la medianoche

        if (diffMin < 1) return { text: '< 1 min', level: 'normal' };

        var text = '';
        if (diffMin < 60) {
            text = diffMin + ' min';
        } else {
            var h = Math.floor(diffMin / 60);
            var m = diffMin % 60;
            text = h + 'h ' + m + 'm';
        }

        var level = 'normal';
        if (diffMin >= WAIT_THRESHOLDS.danger) {
            level = 'danger';
        } else if (diffMin >= WAIT_THRESHOLDS.warning) {
            level = 'warning';
        }

        return { text: text, level: level };
    }

    function escapeHtml(text) {
        if (!text) return '';
        var div = document.createElement('div');
        div.appendChild(document.createTextNode(text));
        return div.innerHTML;
    }

    // ── Toast system ─────────────────────────────────────────────
    function showToast(message, type) {
        type = type || 'info';
        var container = document.getElementById('colaToastContainer');
        if (!container) return;

        var icons = {
            success: 'bi-check-circle-fill',
            error: 'bi-exclamation-circle-fill',
            info: 'bi-info-circle-fill'
        };

        var toast = document.createElement('div');
        toast.className = 'cola-toast cola-toast-' + type;
        toast.innerHTML = '<i class="bi ' + (icons[type] || icons.info) + '"></i>'
            + '<span>' + escapeHtml(message) + '</span>'
            + '<button class="cola-toast-close" onclick="this.parentElement.remove()">&times;</button>';

        container.appendChild(toast);

        // Auto-remover después de 4 segundos
        setTimeout(function() {
            if (toast.parentNode) {
                toast.classList.add('cola-toast-removing');
                setTimeout(function() {
                    if (toast.parentNode) toast.remove();
                }, 300);
            }
        }, 4000);
    }

    // ── API pública ──────────────────────────────────────────────
    return {
        init: init,
        refreshNow: refreshNow,
        promptLlegada: promptLlegada,
        atender: atender,
        completar: completar,
        promptCancelar: promptCancelar,
        showToast: showToast
    };

})();
