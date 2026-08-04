/**
 * linea-tiempo.js — Módulo de Línea de Tiempo (HU19)
 *
 * Gestiona la visualización del timeline de atención de pacientes.
 * El avance de pasos es AUTOMÁTICO desde la Cola de Espera (Llegó/Atender/Completar):
 * el timeline solo se renderiza (sin botones manuales) y se actualiza en tiempo real
 * vía SignalR (evento "TimelineActualizada").
 *
 * Dependencias: vittal-api.js, @microsoft/signalr (CDN global en _Layout)
 */

(function () {
    'use strict';

    let activeTimerInterval = null;
    let citaIdActual = null;  // Se setea cuando se carga por cita específica

    const DOM = {
        container: document.getElementById('timelineMainContainer'),
        filtroDoctor: document.getElementById('filtroDoctorId'),
        filtroFecha: document.getElementById('filtroFecha'),
        btnRefrescar: document.getElementById('btnRefrescar'),
        fechaDisplay: document.getElementById('fechaDisplay')
    };

    document.addEventListener('DOMContentLoaded', function () {
        // Detectar si estamos en modo cita específica (desde Cola → Atender)
        var modoCita = DOM.container && DOM.container.dataset.modoCita === 'true';
        var citaId = DOM.container && DOM.container.dataset.citaId;

        if (modoCita && citaId) {
            citaIdActual = citaId;
            cargarTimelinePorCita(citaId);
        } else {
            cargarDoctores();
            cargarTimeline();
        }

        if (DOM.filtroDoctor) {
            DOM.filtroDoctor.addEventListener('change', cargarTimeline);
        }
        if (DOM.filtroFecha) {
            DOM.filtroFecha.addEventListener('change', function () {
                if (DOM.fechaDisplay) {
                    DOM.fechaDisplay.textContent = this.value;
                }
                cargarTimeline();
            });
        }
        if (DOM.btnRefrescar) {
            DOM.btnRefrescar.addEventListener('click', cargarTimeline);
        }

        conectarSignalRTimeline();
    });

    /**
     * Conecta a SignalR (hub de línea de tiempo) para refresco automático.
     * Cuando la Cola de Espera avanza un paso, la API emite "TimelineActualizada"
     * con el citaId → si estamos viendo esa cita (o la vista general), se recarga.
     */
    function conectarSignalRTimeline() {
        if (typeof signalR === 'undefined') return;
        if (!window.VITTAL_API_HUB_URL || !window.VITTAL_CLINICA_ID) return;

        var hubUrl = window.VITTAL_API_HUB_URL + '/hubs/linea-tiempo';

        async function obtenerToken() {
            try {
                var supabaseToken = VittalAPI.getToken();
                if (!supabaseToken) return null;
                var res = await fetch(window.VITTAL_API_HUB_URL + '/api/auth/signalr-token', {
                    method: 'POST',
                    headers: {
                        'Authorization': 'Bearer ' + supabaseToken,
                        'Content-Type': 'application/json'
                    }
                });
                var json = await res.json();
                return json.success && json.token ? json.token : null;
            } catch (err) {
                console.warn('[LineaTiempo] Error obteniendo token SignalR:', err);
                return null;
            }
        }

        var connection = new signalR.HubConnectionBuilder()
            .withUrl(hubUrl, {
                accessTokenFactory: obtenerToken
            })
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .build();

        connection.on('TimelineActualizada', function (citaId) {
            console.log('[LineaTiempo] Timeline actualizada para cita:', citaId);
            if (citaIdActual && citaIdActual === citaId) {
                recargarVista();
            } else if (!citaIdActual) {
                cargarTimeline(); // Vista general: refrescar siempre
            }
        });

        connection.start()
            .then(function () {
                return connection.invoke('SubscribeToClinica', window.VITTAL_CLINICA_ID);
            })
            .then(function () {
                console.log('[LineaTiempo] Suscrito al grupo timeline_' + window.VITTAL_CLINICA_ID);
            })
            .catch(function (err) {
                console.warn('[LineaTiempo] No se pudo conectar a SignalR:', err);
            });
    }

    async function cargarDoctores() {
        if (!DOM.filtroDoctor) return;
        try {
            var res = await fetch('/LineaTiempo/LineaTiempo/JsonDoctores', {
                method: 'GET',
                headers: { 'Content-Type': 'application/json' }
            });
            var json = await res.json();
            if (json.success && json.data) {
                json.data.forEach(function (d) {
                    var opt = document.createElement('option');
                    opt.value = d.usuarioId || '';
                    opt.textContent = (d.nombres || '') + ' ' + (d.apellidos || '');
                    DOM.filtroDoctor.appendChild(opt);
                });
            }
        } catch (err) {
            console.warn('Error cargando doctores:', err);
        }
    }

    async function cargarTimeline() {
        if (!DOM.container) return;

        DOM.container.innerHTML =
            '<div class="text-center py-5"><div class="vittal-spinner mx-auto"></div><p class="text-muted small mt-2">Cargando línea de tiempo...</p></div>';

        var params = [];
        if (DOM.filtroDoctor && DOM.filtroDoctor.value) {
            params.push('doctorId=' + DOM.filtroDoctor.value);
        }
        if (DOM.filtroFecha && DOM.filtroFecha.value) {
            params.push('fecha=' + DOM.filtroFecha.value);
        }
        var query = params.length > 0 ? '?' + params.join('&') : '';

        try {
            var res = await fetch('/LineaTiempo/LineaTiempo/JsonTimelineDelDia' + query, {
                method: 'GET',
                headers: { 'Content-Type': 'application/json' }
            });
            var json = await res.json();

            if (!json.success) {
                mostrarError(json.message || 'Error al cargar línea de tiempo.');
                return;
            }

            var pasos = json.data || [];
            renderizarTimeline(pasos);

        } catch (err) {
            console.error('Error cargando timeline:', err);
            mostrarError('Error de conexión al cargar la línea de tiempo.');
        }
    }

    async function cargarTimelinePorCita(citaId) {
        citaIdActual = citaId;
        if (!DOM.container) return;

        DOM.container.innerHTML =
            '<div class="text-center py-5"><div class="vittal-spinner mx-auto"></div><p class="text-muted small mt-2">Cargando línea de tiempo del paciente...</p></div>';

        try {
            var res = await fetch('/LineaTiempo/LineaTiempo/JsonTimelineByCita?citaId=' + encodeURIComponent(citaId), {
                method: 'GET',
                headers: { 'Content-Type': 'application/json' }
            });
            var json = await res.json();

            if (!json.success) {
                mostrarError(json.message || 'Error al cargar la línea de tiempo del paciente.');
                return;
            }

            var pasos = json.data || [];
            renderizarTimeline(pasos);

        } catch (err) {
            console.error('Error cargando timeline por cita:', err);
            mostrarError('Error de conexión al cargar la línea de tiempo.');
        }
    }

    function renderizarTimeline(pasos) {
        if (pasos.length === 0) {
            DOM.container.innerHTML =
                '<div class="vittal-card"><div class="empty-state"><i class="bi bi-clock-history" style="font-size:2.5rem;opacity:0.3;"></i><p class="small text-muted mt-2">No hay pacientes en atención hoy para los filtros seleccionados.</p></div></div>';
            return;
        }

        // Detectar si es modo cita específica (los filtros NO existen en ese modo)
        var esModoCita = DOM.container && DOM.container.dataset.modoCita === 'true';

        var html = '';

        if (esModoCita) {
            // ── MODO CITA ESPECÍFICA — timeline animado + lead time ──
            var totalPasos = pasos.length;
            var completados = pasos.filter(function (p) { return p.estado === 'completado'; }).length;
            var enCurso = pasos.filter(function (p) { return p.estado === 'en_sala' || p.estado === 'activo'; })[0] || null;
            var progreso = totalPasos > 0 ? Math.round((completados / totalPasos) * 100) : 0;

            html += '<div class="d-flex align-items-center gap-2 mb-3">';
            html += '<small class="text-muted fw-medium">Progreso:</small>';
            html += '<div class="timeline-progress flex-grow-1">';
            html += '<div class="progress-bar" style="width:' + progreso + '%"></div>';
            html += '</div>';
            html += '<small class="fw-bold text-primary">' + progreso + '%</small>';
            html += '</div>';

            // Stepper horizontal animado (3 pasos: Llegada → Consulta → Salida)
            html += '<div class="timeline-stepper">';
            pasos.forEach(function (paso, idx) {
                var esUltimo = idx === pasos.length - 1;
                var stateClass = paso.estado === 'completado' ? 'done'
                    : (paso.estado === 'en_sala' || paso.estado === 'activo') ? 'active'
                    : paso.estado === 'saltado' ? 'skipped' : 'pending';
                var icono = paso.estado === 'completado' ? 'bi-check-lg'
                    : (paso.estado === 'en_sala' || paso.estado === 'activo') ? 'bi-play-fill'
                    : paso.estado === 'saltado' ? 'bi-forward' : 'bi-circle';
                var horaLabel = paso.estado === 'completado'
                    ? (paso.horaSalida || paso.horaLlegada || '--:--')
                    : (paso.estado === 'en_sala' || paso.estado === 'activo') ? (paso.horaLlegada || '--:--') : '';

                html += '<div class="timeline-step ' + stateClass + '">';
                html += '<div class="step-node"><i class="bi ' + icono + '"></i></div>';
                html += '<div class="step-label">' + escapeHtml(paso.nombrePaso || 'Paso') + '</div>';
                if (horaLabel) {
                    html += '<div class="step-time">' + horaLabel.substring(0, 5) + '</div>';
                }
                if (paso.estado === 'en_sala' || paso.estado === 'activo') {
                    html += '<div class="step-live" id="step-live-' + paso.id + '">--:--</div>';
                }
                if (!esUltimo) {
                    html += '<div class="step-connector"></div>';
                }
                html += '</div>';
            });
            html += '</div>';

            // KPI Lead Time Total
            var duracionTotal = calcularDuracionTotal(pasos);
            html += '<div class="timeline-total">';
            html += '<div class="total-label">Lead Time Total</div>';
            html += '<div class="total-value" id="tiempoTotal">' + duracionTotal + '</div>';
            html += '<div class="total-sub">Desde llegada hasta salida del paciente</div>';
            html += '</div>';

            // Detalle de cada paso (solo informativo — sin botones manuales)
            html += '<div class="timeline-vertical mt-3">';
            pasos.forEach(function (paso) {
                html += renderPasoCard(paso);
            });
            html += '</div>';
        } else {
            // ── MODO GENERAL — agrupar pasos por cita/paciente ─────────
            var grupos = {};
            pasos.forEach(function (paso) {
                var key = paso.citaId || 'sin-cita';
                if (!grupos[key]) {
                    grupos[key] = {
                        citaId: key,
                        pacienteId: paso.pacienteId || '',
                        pacienteNombre: paso.pacienteNombre || '',
                        pasos: []
                    };
                }
                grupos[key].pasos.push(paso);
            });

            html += '<div class="small text-muted mb-2"><i class="bi bi-people me-1"></i>' + Object.keys(grupos).length + ' pacientes en atención</div>';

            Object.keys(grupos).forEach(function (key) {
                var grupo = grupos[key];
                var pPasos = grupo.pasos;
                var pCompletados = pPasos.filter(function (p) { return p.estado === 'completado'; }).length;
                var pProgreso = pPasos.length > 0 ? Math.round((pCompletados / pPasos.length) * 100) : 0;

                // Mostrar nombre del paciente (desde JOIN en API), o fallback a ID
                var pacienteLabel = grupo.pacienteNombre || (grupo.pacienteId ? grupo.pacienteId.substring(0, 8) : 'Paciente');

                html += '<div class="vittal-card mb-3">';
                html += '<div class="card-header d-flex align-items-center justify-content-between p-2">';
                html += '<div>';
                html += '<i class="bi bi-person-circle text-primary me-2"></i>';
                html += '<strong>Paciente #' + pacienteLabel + '</strong>';
                html += '<span class="badge bg-light text-dark ms-2">' + pProgreso + '%</span>';
                html += '</div>';
                html += '<a href="/LineaTiempo/LineaTiempo?citaId=' + encodeURIComponent(grupo.citaId) + '" class="btn btn-outline-primary btn-sm">';
                html += '<i class="bi bi-eye me-1"></i>Ver</a>';
                html += '</div>';

                html += '<div class="p-2"><div class="timeline-vertical">';
                pPasos.forEach(function (paso) {
                    html += renderPasoResumen(paso);
                });
                html += '</div></div>';
                html += '</div>';
            });
        }

        DOM.container.innerHTML = html;

        // Iniciar timer para pasos activos
        if (esModoCita) {
            iniciarTimerActivo(pasos);
        } else {
            // Activar timers por grupo
            Object.keys(grupos).forEach(function (key) {
                iniciarTimerActivo(grupos[key].pasos);
            });
        }
    }

    function renderPasoResumen(paso) {
        var estado = paso.estado || 'pendiente';
        var nombrePaso = paso.nombrePaso || 'Paso';
        var duracion = paso.duracionFormateada || '--:--';

        var estadoIcon = {
            completado: '<i class="bi bi-check-circle-fill text-success"></i>',
            en_sala: '<i class="bi bi-play-circle-fill text-primary"></i>',
            activo: '<i class="bi bi-play-circle-fill text-primary"></i>',
            saltado: '<i class="bi bi-forward-fill text-warning"></i>',
            pendiente: '<i class="bi bi-circle text-muted"></i>'
        }[estado] || '<i class="bi bi-circle text-muted"></i>';

        return '<div class="d-flex align-items-center gap-2 py-1 small">'
            + estadoIcon
            + '<span>' + escapeHtml(nombrePaso) + '</span>'
            + '<span class="ms-auto text-muted">' + duracion + '</span>'
            + '</div>';
    }

    function renderPasoCard(paso) {
        var id = paso.id || '';
        var nombrePaso = paso.nombrePaso || 'Paso';
        var orden = paso.orden || 0;
        var estado = paso.estado || 'pendiente';
        var horaLlegada = paso.horaLlegada || '--:--';
        var horaSalida = paso.horaSalida || '--:--';
        var duracion = paso.duracionFormateada || '--:--';
        var pacienteNombre = paso.pacienteNombre || '';
        var salaNombre = paso.salaNombre || null;

        if (typeof horaLlegada === 'string' && horaLlegada.length > 5) {
            horaLlegada = horaLlegada.substring(0, 5);
        }
        if (typeof horaSalida === 'string' && horaSalida.length > 5) {
            horaSalida = horaSalida.substring(0, 5);
        }

        var estadoClass = {
            completado: 'completado',
            en_sala: 'activo',
            activo: 'activo',
            saltado: 'saltado'
        }[estado] || 'pendiente';

        var icono = {
            completado: 'bi-check-lg',
            en_sala: 'bi-arrow-right-circle',
            activo: 'bi-arrow-right-circle',
            saltado: 'bi-forward',
            pendiente: 'bi-circle'
        }[estado] || 'bi-circle';

        var badgeEstado = {
            completado: 'badge-atendida',
            en_sala: 'badge-en-atencion',
            activo: 'badge-en-atencion',
            saltado: 'badge-en-espera',
            pendiente: 'badge-cancelada'
        }[estado] || 'badge-cancelada';

        var estadoTexto = {
            completado: 'Completado',
            en_sala: 'En Progreso',
            activo: 'En Progreso',
            saltado: 'Saltado',
            pendiente: 'Pendiente'
        }[estado] || 'Pendiente';

        var html = '';
        html += '<div class="timeline-paso" data-paso-id="' + id + '" data-estado="' + estado + '">';

        // Círculo
        html += '<div class="paso-circulo ' + estadoClass + '">';
        if (estado === 'completado') {
            html += '<i class="bi bi-check-lg" style="font-size:0.8rem;"></i>';
        } else if (estado === 'en_sala') {
            html += '<i class="bi bi-play-fill" style="font-size:0.7rem;margin-left:1px;"></i>';
        } else {
            html += orden;
        }
        html += '</div>';

        // Card
        html += '<div class="paso-card ' + estadoClass + '">';
        html += '<div class="paso-header">';
        html += '<i class="bi ' + icono + ' text-muted"></i>';
        html += '<span class="paso-nombre">' + escapeHtml(nombrePaso) + '</span>';
        html += '<span class="badge ' + badgeEstado + ' ms-1">' + estadoTexto + '</span>';
        html += '<span class="paso-duracion ' + (estado === 'en_sala' ? 'vivo' : '') + '" id="duracion-' + id + '">';
        html += '<i class="bi bi-hourglass me-1"></i>' + duracion;
        html += '</span>';
        html += '</div>';

        html += '<div class="d-flex align-items-center gap-3 small text-muted mb-2">';
        if (pacienteNombre) {
            html += '<span><i class="bi bi-person me-1"></i>' + escapeHtml(pacienteNombre) + '</span>';
        }
        if (salaNombre) {
            html += '<span><i class="bi bi-geo-alt me-1"></i>' + escapeHtml(salaNombre) + '</span>';
        }
        html += '<span><i class="bi bi-clock me-1"></i>' + horaLlegada + ' - ' + horaSalida + '</span>';
        html += '</div>';

        // ── Sin botones manuales: el avance es automático desde la Cola de Espera ──
        html += '<div class="paso-automatico small text-muted">';
        html += '<i class="bi bi-magic me-1"></i>Avance automático desde Cola de Espera';
        html += '</div>';

        html += '</div>'; // card
        html += '</div>'; // timeline-paso

        return html;
    }

    async function recargarVista() {
        if (citaIdActual) {
            await cargarTimelinePorCita(citaIdActual);
        } else {
            await cargarTimeline();
        }
    }

    function iniciarTimerActivo(pasos) {
        if (activeTimerInterval) {
            clearInterval(activeTimerInterval);
            activeTimerInterval = null;
        }

        var pasoActivo = null;
        for (var i = 0; i < pasos.length; i++) {
            if (pasos[i].estado === 'en_sala' || pasos[i].estado === 'activo') {
                pasoActivo = pasos[i];
                break;
            }
        }

        if (!pasoActivo) return;

        var inicio = parseHora(pasoActivo.horaLlegada);
        if (!inicio) return;

        // Si llegada es del tipo "09:05:00" o "09:05"
        var inicioDate = new Date();
        inicioDate.setHours(inicio.hours, inicio.minutes, 0, 0);

        // Calcular segundos base de pasos ya completados (estáticos)
        var segundosCompletados = 0;
        for (var i = 0; i < pasos.length; i++) {
            if (pasos[i].estado === 'completado' && pasos[i].duracionFormateada) {
                var partes = pasos[i].duracionFormateada.split(':');
                if (partes.length === 3) {
                    segundosCompletados += parseInt(partes[0]) * 3600 + parseInt(partes[1]) * 60 + parseInt(partes[2]);
                }
            }
        }

        activeTimerInterval = setInterval(function () {
            var ahora = new Date();
            var diffMs = ahora - inicioDate;
            var diffSeg = Math.floor(diffMs / 1000);
            var mins = Math.floor(diffSeg / 60);
            var segs = diffSeg % 60;
            var tiempoStr = pad(mins) + ':' + pad(segs);

            // Timer del paso activo (tarjeta de detalle)
            var duracionEl = document.getElementById('duracion-' + pasoActivo.id);
            if (duracionEl) {
                duracionEl.innerHTML = '<i class="bi bi-hourglass me-1"></i>' + tiempoStr;
                duracionEl.classList.add('vivo');
            }

            // Timer del paso activo en el stepper horizontal
            var stepLiveEl = document.getElementById('step-live-' + pasoActivo.id);
            if (stepLiveEl) {
                stepLiveEl.textContent = tiempoStr;
            }

            // Actualizar lead time total (completados + activo)
            var totalSeg = segundosCompletados + diffSeg;
            var totalEl = document.getElementById('tiempoTotal');
            if (totalEl) {
                var h = Math.floor(totalSeg / 3600);
                var m = Math.floor((totalSeg % 3600) / 60);
                var s = totalSeg % 60;
                totalEl.textContent = pad(h) + ':' + pad(m) + ':' + pad(s);
            }
        }, 1000);
    }

    function calcularDuracionTotal(pasos) {
        var totalSeg = 0;
        pasos.forEach(function (p) {
            if (p.duracionFormateada && p.duracionFormateada !== '--:--:--') {
                var partes = p.duracionFormateada.split(':');
                if (partes.length === 3) {
                    totalSeg += parseInt(partes[0]) * 3600 + parseInt(partes[1]) * 60 + parseInt(partes[2]);
                }
            }
        });
        if (totalSeg === 0) return '--:--:--';
        var h = Math.floor(totalSeg / 3600);
        var m = Math.floor((totalSeg % 3600) / 60);
        var s = totalSeg % 60;
        return pad(h) + ':' + pad(m) + ':' + pad(s);
    }

    function parseHora(hora) {
        if (!hora || hora === '--:--') return null;
        var partes = hora.split(':');
        if (partes.length >= 2) {
            return { hours: parseInt(partes[0]), minutes: parseInt(partes[1]) };
        }
        return null;
    }

    function pad(n) {
        return n < 10 ? '0' + n : '' + n;
    }

    function mostrarError(mensaje) {
        DOM.container.innerHTML =
            '<div class="alert alert-warning alert-dismissible fade show" role="alert">' +
            '<i class="bi bi-exclamation-triangle me-2"></i>' + escapeHtml(mensaje) +
            '<button type="button" class="btn-close" data-bs-dismiss="alert"></button>' +
            '</div>' +
            '<div class="text-center mt-3">' +
            '<button class="btn btn-outline-primary btn-sm" onclick="window.location.reload()">' +
            '<i class="bi bi-arrow-clockwise me-1"></i> Reintentar</button></div>';
    }

    function escapeHtml(text) {
        if (!text) return '';
        var div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
})();
