/**
 * linea-tiempo.js — Módulo de Línea de Tiempo (HU19)
 *
 * Gestiona la visualización del timeline de atención de pacientes,
 * con iniciar/finalizar/saltar pasos y timer en vivo.
 *
 * Dependencias: vittal-api.js
 */

(function () {
    'use strict';

    let activeTimerInterval = null;

    const DOM = {
        container: document.getElementById('timelineMainContainer'),
        filtroDoctor: document.getElementById('filtroDoctorId'),
        filtroFecha: document.getElementById('filtroFecha'),
        btnRefrescar: document.getElementById('btnRefrescar'),
        fechaDisplay: document.getElementById('fechaDisplay')
    };

    document.addEventListener('DOMContentLoaded', function () {
        cargarDoctores();
        cargarTimeline();

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
    });

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
                    opt.value = d.id || '';
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

    function renderizarTimeline(pasos) {
        if (pasos.length === 0) {
            DOM.container.innerHTML =
                '<div class="vittal-card"><div class="empty-state"><i class="bi bi-clock-history" style="font-size:2.5rem;opacity:0.3;"></i><p class="small text-muted mt-2">No hay pacientes en atención hoy para los filtros seleccionados.</p></div></div>';
            return;
        }

        var totalPasos = pasos.length;
        var completados = pasos.filter(function (p) { return p.estado === 'completado'; }).length;
        var progreso = totalPasos > 0 ? Math.round((completados / totalPasos) * 100) : 0;

        var html = '';

        // Barra de progreso
        html += '<div class="d-flex align-items-center gap-2 mb-3">';
        html += '<small class="text-muted fw-medium">Progreso:</small>';
        html += '<div class="timeline-progress flex-grow-1">';
        html += '<div class="progress-bar" style="width:' + progreso + '%"></div>';
        html += '</div>';
        html += '<small class="fw-bold text-primary">' + progreso + '%</small>';
        html += '</div>';

        // Timeline
        html += '<div class="timeline-vertical">';

        pasos.forEach(function (paso) {
            html += renderPasoCard(paso);
        });

        html += '</div>';

        // Tiempo total
        var duracionTotal = calcularDuracionTotal(pasos);
        html += '<div class="timeline-total">';
        html += '<div class="total-label">Tiempo Total</div>';
        html += '<div class="total-value" id="tiempoTotal">' + duracionTotal + '</div>';
        html += '</div>';

        DOM.container.innerHTML = html;

        // Iniciar timer para pasos activos
        iniciarTimerActivo(pasos);

        // Binding de botones
        bindPasoButtons();
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
            activo: 'activo',
            saltado: 'saltado'
        }[estado] || 'pendiente';

        var icono = {
            completado: 'bi-check-lg',
            activo: 'bi-arrow-right-circle',
            saltado: 'bi-forward',
            pendiente: 'bi-circle'
        }[estado] || 'bi-circle';

        var badgeEstado = {
            completado: 'badge-atendida',
            activo: 'badge-en-atencion',
            saltado: 'badge-en-espera',
            pendiente: 'badge-cancelada'
        }[estado] || 'badge-cancelada';

        var estadoTexto = {
            completado: 'Completado',
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
        } else if (estado === 'activo') {
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
        html += '<span class="paso-duracion ' + (estado === 'activo' ? 'vivo' : '') + '" id="duracion-' + id + '">';
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

        html += '<div class="paso-acciones">';
        if (estado === 'pendiente') {
            html += '<button class="btn btn-primary btn-sm btn-iniciar-paso" data-paso-id="' + id + '">';
            html += '<i class="bi bi-play-fill me-1"></i>Iniciar</button> ';
            html += '<button class="btn btn-outline-warning btn-sm btn-saltar-paso" data-paso-id="' + id + '">';
            html += '<i class="bi bi-forward me-1"></i>Saltar</button>';
        } else if (estado === 'activo') {
            html += '<button class="btn btn-success btn-sm btn-finalizar-paso" data-paso-id="' + id + '">';
            html += '<i class="bi bi-check-lg me-1"></i>Finalizar</button>';
        }
        html += '</div>';

        html += '</div>'; // card
        html += '</div>'; // timeline-paso

        return html;
    }

    function bindPasoButtons() {
        document.querySelectorAll('.btn-iniciar-paso').forEach(function (btn) {
            btn.addEventListener('click', function () {
                iniciarPaso(this.dataset.pasoId, this);
            });
        });
        document.querySelectorAll('.btn-finalizar-paso').forEach(function (btn) {
            btn.addEventListener('click', function () {
                finalizarPaso(this.dataset.pasoId, this);
            });
        });
        document.querySelectorAll('.btn-saltar-paso').forEach(function (btn) {
            btn.addEventListener('click', function () {
                saltarPaso(this.dataset.pasoId, this);
            });
        });
    }

    async function iniciarPaso(pasoId, btn) {
        btn.disabled = true;
        btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>';

        try {
            var res = await fetch('/LineaTiempo/LineaTiempo/JsonIniciarPaso?pasoId=' + pasoId, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' }
            });
            var json = await res.json();

            if (res.ok && json.success) {
                VittalAPI.showToast('Paso iniciado correctamente.', 'success');
                await cargarTimeline();
            } else {
                VittalAPI.showToast(json.message || 'Error al iniciar paso.', 'error');
                btn.disabled = false;
                btn.innerHTML = '<i class="bi bi-play-fill me-1"></i>Iniciar';
            }
        } catch (err) {
            VittalAPI.showToast('Error de conexión.', 'error');
            btn.disabled = false;
            btn.innerHTML = '<i class="bi bi-play-fill me-1"></i>Iniciar';
        }
    }

    async function finalizarPaso(pasoId, btn) {
        btn.disabled = true;
        btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>';

        try {
            var res = await fetch('/LineaTiempo/LineaTiempo/JsonFinalizarPaso?pasoId=' + pasoId, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' }
            });
            var json = await res.json();

            if (res.ok && json.success) {
                VittalAPI.showToast('Paso finalizado correctamente.', 'success');
                await cargarTimeline();
            } else {
                VittalAPI.showToast(json.message || 'Error al finalizar paso.', 'error');
                btn.disabled = false;
                btn.innerHTML = '<i class="bi bi-check-lg me-1"></i>Finalizar';
            }
        } catch (err) {
            VittalAPI.showToast('Error de conexión.', 'error');
            btn.disabled = false;
            btn.innerHTML = '<i class="bi bi-check-lg me-1"></i>Finalizar';
        }
    }

    async function saltarPaso(pasoId, btn) {
        if (!confirm('¿Está seguro de saltar este paso?')) return;

        btn.disabled = true;
        btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>';

        try {
            var res = await fetch('/LineaTiempo/LineaTiempo/JsonSaltarPaso?pasoId=' + pasoId, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' }
            });
            var json = await res.json();

            if (res.ok && json.success) {
                VittalAPI.showToast('Paso saltado correctamente.', 'success');
                await cargarTimeline();
            } else {
                VittalAPI.showToast(json.message || 'Error al saltar paso.', 'error');
                btn.disabled = false;
                btn.innerHTML = '<i class="bi bi-forward me-1"></i>Saltar';
            }
        } catch (err) {
            VittalAPI.showToast('Error de conexión.', 'error');
            btn.disabled = false;
            btn.innerHTML = '<i class="bi bi-forward me-1"></i>Saltar';
        }
    }

    function iniciarTimerActivo(pasos) {
        if (activeTimerInterval) {
            clearInterval(activeTimerInterval);
            activeTimerInterval = null;
        }

        var pasoActivo = null;
        for (var i = 0; i < pasos.length; i++) {
            if (pasos[i].estado === 'activo') {
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

        activeTimerInterval = setInterval(function () {
            var ahora = new Date();
            var diffMs = ahora - inicioDate;
            var diffSeg = Math.floor(diffMs / 1000);
            var mins = Math.floor(diffSeg / 60);
            var segs = diffSeg % 60;
            var tiempoStr = pad(mins) + ':' + pad(segs);

            var duracionEl = document.getElementById('duracion-' + pasoActivo.id);
            if (duracionEl) {
                duracionEl.innerHTML = '<i class="bi bi-hourglass me-1"></i>' + tiempoStr;
                duracionEl.classList.add('vivo');
            }

            // Actualizar tiempo total
            var totalEl = document.getElementById('tiempoTotal');
            if (totalEl) {
                totalEl.textContent = '00:' + pad(mins) + ':' + pad(segs);
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
