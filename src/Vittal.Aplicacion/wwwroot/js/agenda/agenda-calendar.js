/* ───────────────────────────────────────────────────────────────
   VITTAL AGENDA — Interactive Calendar Engine
   src/Vittal.Aplicacion/wwwroot/js/agenda/agenda-calendar.js
   Vista Día · 5 Días · 7 Días · Mes
   Historia de Usuario: HU21 — Agenda
   ─────────────────────────────────────────────────────────────── */

const vittalAgenda = (function() {

    // ── Estado global ─────────────────────────────────────────
    let config = {};
    let state = {
        view: 'day',            // day | 5days | week | month
        currentDate: new Date(), // fecha central de navegación
        citas: [],              // todas las citas cargadas
        doctores: [],           // lista de doctores
        pacientes: [],          // lista de pacientes
        salas: [],              // lista de salas
        filterDoctor: 'todos',
        editingId: null,         // ID de cita en edición
        detallesCita: null,      // cita seleccionada para detalle
        horario: null            // horario de atención de la clínica
    };

    // ── Utilidades ─────────────────────────────────────────────
    function fmtDate(d) {
        const y = d.getFullYear();
        const m = String(d.getMonth() + 1).padStart(2, '0');
        const day = String(d.getDate()).padStart(2, '0');
        return `${y}-${m}-${day}`;
    }

    function fmtDateShort(d) {
        const dias = ['Dom', 'Lun', 'Mar', 'Mié', 'Jue', 'Vie', 'Sáb'];
        const meses = ['Ene', 'Feb', 'Mar', 'Abr', 'May', 'Jun',
                       'Jul', 'Ago', 'Sep', 'Oct', 'Nov', 'Dic'];
        return `${dias[d.getDay()]}, ${d.getDate()} ${meses[d.getMonth()]}`;
    }

    function fmtDateLong(d) {
        const dias = ['Domingo', 'Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado'];
        const meses = ['Enero', 'Febrero', 'Marzo', 'Abril', 'Mayo', 'Junio',
                       'Julio', 'Agosto', 'Septiembre', 'Octubre', 'Noviembre', 'Diciembre'];
        return `${dias[d.getDay()]}, ${d.getDate()} de ${meses[d.getMonth()]} de ${d.getFullYear()}`;
    }

    function fmtMonthYear(d) {
        const meses = ['Enero', 'Febrero', 'Marzo', 'Abril', 'Mayo', 'Junio',
                       'Julio', 'Agosto', 'Septiembre', 'Octubre', 'Noviembre', 'Diciembre'];
        return `${meses[d.getMonth()]} ${d.getFullYear()}`;
    }

    function fmtTime(iso) {
        if (!iso) return '';
        const parts = iso.split(':');
        return `${parts[0]}:${parts[1]}`;
    }

    function toDateInputValue(d) {
        return fmtDate(d);
    }

    function toTimeInputValue(t) {
        if (!t) return '';
        const parts = t.split(':');
        return `${parts[0].padStart(2,'0')}:${parts[1].padStart(2,'0')}`;
    }

    function getMonday(d) {
        const date = new Date(d);
        const day = date.getDay();
        const diff = date.getDate() - day + (day === 0 ? -6 : 1);
        date.setDate(diff);
        date.setHours(0,0,0,0);
        return date;
    }

    function addDays(d, days) {
        const result = new Date(d);
        result.setDate(result.getDate() + days);
        return result;
    }

    function isToday(d) {
        const today = new Date();
        return d.getFullYear() === today.getFullYear() &&
               d.getMonth() === today.getMonth() &&
               d.getDate() === today.getDate();
    }

    function isSameDay(a, b) {
        return a.getFullYear() === b.getFullYear() &&
               a.getMonth() === b.getMonth() &&
               a.getDate() === b.getDate();
    }

    function getEstadoLabel(estado) {
        const labels = {
            'agendada': 'Agendada',
            'en_espera': 'En Espera',
            'en_atencion': 'En Atención',
            'atendida': 'Atendida',
            'cancelada': 'Cancelada'
        };
        return labels[estado] || estado;
    }

    function getEstadoIcon(estado) {
        const icons = {
            'agendada': 'bi-calendar-check',
            'en_espera': 'bi-clock',
            'en_atencion': 'bi-play-circle',
            'atendida': 'bi-check-circle',
            'cancelada': 'bi-x-circle'
        };
        return icons[estado] || 'bi-question-circle';
    }

    function parseTimeToMinutes(timeStr) {
        if (!timeStr) return 0;
        const parts = timeStr.split(':');
        return parseInt(parts[0]) * 60 + parseInt(parts[1]);
    }

    function minutesToTimeStr(minutes) {
        const h = Math.floor(minutes / 60);
        const m = minutes % 60;
        return `${String(h).padStart(2,'0')}:${String(m).padStart(2,'0')}`;
    }

    function getPatientName(cita) {
        return cita.pacienteNombre || cita.primerNombre + ' ' + cita.primerApellido || 'Paciente';
    }

    function getDoctorName(cita) {
        return cita.doctorNombre || cita.nombres + ' ' + cita.apellidos || 'Doctor';
    }

    // ── Horario de atención ──────────────────────────────────────
    const DAY_CODE_MAP = {
        0: 'D',   // Domingo
        1: 'L',   // Lunes
        2: 'M',   // Martes
        3: 'MI',  // Miércoles
        4: 'J',   // Jueves
        5: 'V',   // Viernes
        6: 'S'    // Sábado
    };

    function isDiaAtencion(dateStr) {
        if (!state.horario || !state.horario.diasAtencion) return true;
        const d = new Date(dateStr + 'T12:00:00');
        const code = DAY_CODE_MAP[d.getDay()];
        const dias = state.horario.diasAtencion.split(',').map(s => s.trim());
        return dias.includes(code);
    }

    function isHoraEnRango(timeStr) {
        if (!state.horario || !state.horario.horarioApertura || !state.horario.horarioCierre) return true;
        const mins = parseTimeToMinutes(timeStr);
        const apertura = parseTimeToMinutes(state.horario.horarioApertura);
        const cierre = parseTimeToMinutes(state.horario.horarioCierre);
        return mins >= apertura && mins < cierre;
    }

    function getHorarioLabel() {
        if (!state.horario || !state.horario.horarioApertura || !state.horario.horarioCierre) return '';
        return `${state.horario.horarioApertura} — ${state.horario.horarioCierre}`;
    }

    /** Retorna el rango de horas [start, end) del grid según el horario de la clínica.
     *  Si no hay horario configurado, usa el rango por defecto 06–22 (6am a 10pm). */
    function getGridHourRange() {
        let startHour = 6;
        let endHour = 22;
        if (state.horario && state.horario.horarioApertura && state.horario.horarioCierre) {
            startHour = Math.floor(parseTimeToMinutes(state.horario.horarioApertura) / 60);
            endHour = Math.ceil(parseTimeToMinutes(state.horario.horarioCierre) / 60);
            // Mínimo 1 hora de rango visible
            if (endHour <= startHour) endHour = startHour + 1;
        }
        return { startHour: startHour, endHour: endHour };
    }

    // ── DOM refs ───────────────────────────────────────────────
    let $ = (id) => document.getElementById(id);
    let container, dateTitleMain, dateTitleSub;
    let filterDoctor;
    let modalCita, modalDetalle, modalDesactivar;

    // ── Inicialización ─────────────────────────────────────────
    function init(cfg) {
        config = cfg;
        container = $('agendaCalendar');
        dateTitleMain = $('dateTitleMain');
        dateTitleSub = $('dateTitleSub');
        filterDoctor = $('filterDoctor');

        // ── Perfil doctor: ocultar filtros que no aplican y fijar su doctorId ──
        if (config.esDoctor) {
            const fw = document.querySelector('.agenda-doctor-filter');
            if (fw) fw.style.display = 'none';

            const salaField = document.getElementById('citaSalaId')?.closest('.col-md-4');
            if (salaField) salaField.style.display = 'none';

            state.filterDoctor = config.usuarioId;

            // Hallazgo QA #1: ocultar el campo "Doctor" del modal y fijarlo a sí mismo.
            // El doctor no puede elegir ni cambiar el doctor de una cita.
            const doctorField = document.getElementById('citaDoctorId')?.closest('.col-md-6');
            if (doctorField) doctorField.style.display = 'none';
            const doctorSel = document.getElementById('citaDoctorId');
            if (doctorSel) {
                doctorSel.disabled = true;
                doctorSel.value = config.usuarioId;
            }
            // Ampliar el campo de paciente a ancho completo al ocultar el de doctor
            const patientField = document.getElementById('citaPacienteId')?.closest('.col-md-6');
            if (patientField) {
                patientField.classList.remove('col-md-6');
                patientField.classList.add('col-md-12');
            }
        }

        // Inicializar modales Bootstrap
        modalCita = new bootstrap.Modal($('modalCita'));
        modalDetalle = new bootstrap.Modal($('modalDetalleCita'));
        modalDesactivar = new bootstrap.Modal($('modalConfirmarDesactivar'));

        // Eventos de navegación
        $('btnPrev').addEventListener('click', () => navigate(-1));
        $('btnNext').addEventListener('click', () => navigate(1));
        $('btnToday').addEventListener('click', goToToday);

        // Eventos de cambio de vista
        document.querySelectorAll('.btn-view').forEach(btn => {
            btn.addEventListener('click', function() {
                document.querySelectorAll('.btn-view').forEach(b => b.classList.remove('active'));
                this.classList.add('active');
                state.view = this.dataset.view;
                render();
            });
        });

        // Filtro de doctor
        filterDoctor.addEventListener('change', function() {
            state.filterDoctor = this.value;
            render();
        });

        // Botón nueva cita
        $('btnNuevaCita').addEventListener('click', () => openNewCita());

        // Guardar cita
        $('btnGuardarCita').addEventListener('click', saveCita);

        // Actualizar restricciones de horario cuando cambia la fecha
        $('citaFecha').addEventListener('change', function() {
            aplicarRestriccionesHorario(this.value, $('citaHora').value);
        });

        // HU21: coherencia doctor de la cita ↔ médico asignado del paciente.
        // Al cambiar el paciente se autocompleta el doctor con el asignado del
        // paciente; al cambiar el doctor se recalcula si hace falta reasignar.
        $('citaPacienteId').addEventListener('change', function() {
            syncDoctorPacienteInfo(true);
        });
        $('citaDoctorId').addEventListener('change', function() {
            syncDoctorPacienteInfo(false);
        });

        // Desactivar cita desde modal
        $('btnDesactivarCita').addEventListener('click', () => {
            if (state.editingId) {
                const cita = state.citas.find(c => c.id === state.editingId);
                if (cita) {
                    $('confirmarDesactivarPaciente').textContent = getPatientName(cita);
                    modalCita.hide();
                    modalDesactivar.show();
                }
            }
        });

$('btnConfirmarDesactivar').addEventListener('click', async () => {
            if (state.editingId) {
                await desactivarCita(state.editingId);
                modalDesactivar.hide();
                state.editingId = null;
            }
        });

        $('btnEditarDesdeDetalle').addEventListener('click', () => {
            modalDetalle.hide();
            if (state.detallesCita) {
                openEditCita(state.detallesCita.id);
            }
        });

        // Cargar datos iniciales
        loadInitialData();
    }

    // ── Carga de datos ─────────────────────────────────────────
    async function loadInitialData() {
        try {
            const [citasRes, doctoresRes, pacientesRes, salasRes, horarioRes] = await Promise.all([
                fetch(config.urls.citas),
                fetch(config.urls.doctores),
                fetch(config.urls.pacientes),
                fetch(config.urls.salas),
                fetch(config.urls.horarioClinica)
            ]);

            const citasJson = await citasRes.json();
            const doctoresJson = await doctoresRes.json();
            const pacientesJson = await pacientesRes.json();
            const salasJson = await salasRes.json();
            const horarioJson = await horarioRes.json();

            if (citasJson.success) state.citas = citasJson.data || [];
            if (doctoresJson.success) state.doctores = doctoresJson.data || [];
            if (pacientesJson.success) state.pacientes = pacientesJson.data || [];
            if (salasJson.success) state.salas = salasJson.data || [];
            if (horarioJson.success && horarioJson.data) {
                state.horario = horarioJson.data;
            }

            // Poblar selects
            populateDoctorFilter();
            populateDoctorsSelect();
            populatePatientsSelect();
            populateSalasSelect();

            // Renderizar calendario
            render();

        } catch (err) {
            console.error('Error loading initial data:', err);
            showToast('Error al cargar datos de la agenda.', 'danger');
        }
    }

    function populateDoctorFilter() {
        const select = filterDoctor;

        // Perfil doctor: el filtro está oculto y fijado a su propio doctorId
        if (config.esDoctor) {
            select.innerHTML = '';
            return;
        }

        select.innerHTML = '<option value="todos">Todos los doctores</option>';
        state.doctores.forEach(d => {
            const name = d.nombreCompleto || d.nombres + ' ' + d.apellidos || 'Doctor';
            const opt = document.createElement('option');
            opt.value = d.id || d.usuarioId || '';
            opt.textContent = name;
            select.appendChild(opt);
        });
    }

    function populateDoctorsSelect() {
        const select = $('citaDoctorId');

        // Perfil doctor: fijar únicamente su propio doctorId (campo oculto en el modal)
        if (config.esDoctor) {
            select.innerHTML = '';
            const opt = document.createElement('option');
            const propio = state.doctores.find(d => (d.id || d.usuarioId) === config.usuarioId);
            opt.value = config.usuarioId;
            opt.textContent = (propio && (propio.nombreCompleto || propio.nombres + ' ' + propio.apellidos)) || 'Doctor';
            select.appendChild(opt);
            select.value = config.usuarioId;
            select.disabled = true;
            return;
        }

        select.innerHTML = '<option value="">-- Seleccionar doctor --</option>';
        state.doctores.forEach(d => {
            const name = d.nombreCompleto || d.nombres + ' ' + d.apellidos || 'Doctor';
            const opt = document.createElement('option');
            opt.value = d.id || d.usuarioId || '';
            opt.textContent = name;
            select.appendChild(opt);
        });
    }

    function populatePatientsSelect() {
        const select = $('citaPacienteId');
        select.innerHTML = '<option value="">-- Buscar o seleccionar paciente --</option>';
        state.pacientes.forEach(p => {
            const name = p.nombreCompleto || p.primerNombre + ' ' + p.primerApellido || 'Paciente';
            const opt = document.createElement('option');
            opt.value = p.id || '';
            opt.textContent = name;
            // HU21: datos del médico asignado del paciente para el badge y la
            // reasignación. Guid vacío (all-zeros) se trata como sin asignar.
            const docId = p.doctorId || '';
            opt.dataset.doctorId = (docId && docId !== '00000000-0000-0000-0000-000000000000') ? docId : '';
            opt.dataset.doctorNombre = p.doctorNombre || '';
            select.appendChild(opt);
        });
    }

    function populateSalasSelect() {
        const select = $('citaSalaId');
        select.innerHTML = '<option value="">-- Sin sala --</option>';
        state.salas.forEach(s => {
            const opt = document.createElement('option');
            opt.value = s.id || '';
            opt.textContent = s.nombre || 'Sala';
            select.appendChild(opt);
        });
    }

    // ── Navegación ─────────────────────────────────────────────
    function navigate(direction) {
        const delta = {
            'day': 1,
            '5days': 5,
            'week': 7,
            'month': 1
        };
        const days = delta[state.view] || 1;

        if (state.view === 'month') {
            state.currentDate.setMonth(state.currentDate.getMonth() + direction);
        } else {
            state.currentDate = addDays(state.currentDate, direction * days);
        }
        render();
    }

    function goToToday() {
        state.currentDate = new Date();
        render();
    }

    // ── RENDER ──────────────────────────────────────────────────
    function render() {
        switch (state.view) {
            case 'day':     renderDayView(); break;
            case '5days':   renderWeekView(5); break;
            case 'week':    renderWeekView(7); break;
            case 'month':   renderMonthView(); break;
        }
        updateTitle();
    }

    function updateTitle() {
        switch (state.view) {
            case 'day':
                dateTitleMain.textContent = fmtDateLong(state.currentDate);
                dateTitleSub.textContent = '';
                break;
            case '5days': {
                const monday = getMonday(state.currentDate);
                const friday = addDays(monday, 4);
                dateTitleMain.textContent = `Semana del ${fmtDateShort(monday)}`;
                dateTitleSub.textContent = `al ${fmtDateShort(friday)} · 5 días`;
                break;
            }
            case 'week': {
                const monday = getMonday(state.currentDate);
                const sunday = addDays(monday, 6);
                dateTitleMain.textContent = `Semana del ${fmtDateShort(monday)}`;
                dateTitleSub.textContent = `al ${fmtDateShort(sunday)} · 7 días`;
                break;
            }
            case 'month':
                dateTitleMain.textContent = fmtMonthYear(state.currentDate);
                dateTitleSub.textContent = `${state.currentDate.getFullYear()}`;
                break;
        }
    }

    // ── Obtener citas filtradas para rango ─────────────────────
    function getCitasForDateRange(startDate, endDate) {
        const start = new Date(startDate);
        start.setHours(0,0,0,0);
        const end = new Date(endDate);
        end.setHours(23,59,59,999);

        let filtered = state.citas.filter(c => {
            if (!c.fechaCita) return false;
            const citaDate = new Date(c.fechaCita.substring(0,10) + 'T00:00:00');
            return citaDate >= start && citaDate <= end && c.activo !== false;
        });

        // Filtrar por doctor
        if (state.filterDoctor !== 'todos') {
            filtered = filtered.filter(c => String(c.doctorId) === state.filterDoctor);
        }

        return filtered;
    }

    function getCitasForDate(date) {
        const dateStr = fmtDate(date);
        let filtered = state.citas.filter(c => {
            if (!c.fechaCita) return false;
            const cDate = c.fechaCita.substring(0,10);
            return cDate === dateStr && c.activo !== false;
        });

        if (state.filterDoctor !== 'todos') {
            filtered = filtered.filter(c => String(c.doctorId) === state.filterDoctor);
        }

        return filtered;
    }

    // ── RENDER: Vista Día ──────────────────────────────────────
    function renderDayView() {
        const citas = getCitasForDate(state.currentDate);
        const days = [state.currentDate];
        renderWeekGrid(days, citas);
    }

    // ── RENDER: Vista Semanal (5 o 7 días) ─────────────────────
    function renderWeekView(numDays) {
        const monday = getMonday(state.currentDate);
        const days = [];
        for (let i = 0; i < numDays; i++) {
            days.push(addDays(monday, i));
        }

        let allCitas = [];
        days.forEach(d => {
            allCitas = allCitas.concat(getCitasForDate(d));
        });

        renderWeekGrid(days, allCitas);
    }

    // ── RENDER: Grid Semanal ────────────────────────────────────
    function renderWeekGrid(days, citas) {
        const range = getGridHourRange();
        const hours = [];
        for (let h = range.startHour; h < range.endHour; h++) {
            hours.push(h);
        }

        const gridCols = days.length;

        let html = '';

        // Cabecera
        html += '<div class="agenda-week-header" style="grid-template-columns:' +
                `${state.view === 'day' ? '' : 'var(--agenda-sidebar-width) '}` +
                `repeat(${gridCols}, 1fr)">`;

        if (state.view !== 'day') {
            html += '<div class="day-header"></div>'; // esquina vacía
        }

        days.forEach(d => {
            const today = isToday(d) ? 'today' : '';
            html += `<div class="day-header ${today}">`;
            html += `<span>${['Dom','Lun','Mar','Mié','Jue','Vie','Sáb'][d.getDay()]}</span>`;
            html += `<span class="day-number">${d.getDate()}</span>`;
            html += '</div>';
        });
        html += '</div>';

        // Grid de tiempo
        html += '<div class="agenda-time-grid">';

        hours.forEach(h => {
            const timeStr = `${String(h).padStart(2,'0')}:00`;
            const rowClass = state.view === 'day' ? '' : 'time-row';
            html += `<div class="${rowClass}" style="display:${state.view === 'day' ? 'grid' : 'grid'};` +
                    `grid-template-columns:${state.view === 'day' ? '' : 'var(--agenda-sidebar-width) '}repeat(${gridCols}, 1fr)">`;

            if (state.view !== 'day') {
                html += `<div class="agenda-time-label">${timeStr}</div>`;
            }

            days.forEach((d, di) => {
                const today = isToday(d) ? 'today-cell' : '';
                const dateKey = fmtDate(d);
                const esDiaHorario = !state.horario || !state.horario.diasAtencion || isDiaAtencion(dateKey);
                const esHoraHorario = esDiaHorario && (!state.horario || !state.horario.horarioApertura || (h >= parseTimeToMinutes(state.horario.horarioApertura) / 60 && h < parseTimeToMinutes(state.horario.horarioCierre) / 60));
                const claseHorario = esHoraHorario ? 'hora-en-atencion' : (!esDiaHorario ? '' : 'hora-fuera-atencion');
                const cellCitas = citas.filter(c => {
                    const cDate = c.fechaCita ? c.fechaCita.substring(0,10) : '';
                    return cDate === dateKey;
                });

                html += `<div class="agenda-day-cell ${today} ${claseHorario}" ` +
                        `data-date="${dateKey}" data-hour="${h}" ` +
                        `onclick="vittalAgenda.onCellClick('${dateKey}', ${h})">`;

                // Renderizar citas que caen en esta hora
                const citasHora = cellCitas.filter(c => {
                    const hora = parseTimeToMinutes(c.horaCita);
                    return Math.floor(hora / 60) === h;
                });
                const colLayout = computeColumnLayout(citasHora);

                citasHora.forEach(c => {
                    const hora = parseTimeToMinutes(c.horaCita);
                    const horaFin = parseTimeToMinutes(c.horaFin) || hora + 30;
                    const topOffset = ((hora % 60) / 60) * 100;
                    const durationH = Math.max((horaFin - hora) / 60, 0.25);
                    const heightPct = Math.min(durationH * 100, 400);
                    const isOverlapping = durationH > 1;
                    const layout = colLayout[c.id] || { col: 0, cols: 1 };

                    html += renderCitaCard(c, topOffset, heightPct, isOverlapping, layout.col, layout.cols);
                });

                // Botón "+" para agendar otra cita en esta misma hora
                // (la clínica tiene varias salas: distintos doctores pueden
                //  atender simultáneamente en la misma hora)
                if (citasHora.length > 0) {
                    html += `<div class="agenda-cell-add" ` +
                            `onclick="event.stopPropagation(); vittalAgenda.onCellClick('${dateKey}', ${h})" ` +
                            `title="Agendar otra cita a las ${String(h).padStart(2,'0')}:00">` +
                            `<i class="bi bi-plus-lg"></i></div>`;
                }

                // Línea de hora actual (solo vista día y para hoy)
                if (state.view === 'day' && today) {
                    const now = new Date();
                    const nowMinutes = now.getHours() * 60 + now.getMinutes();
                    const currentHourStart = h * 60;
                    if (nowMinutes >= currentHourStart && nowMinutes < currentHourStart + 60) {
                        const offset = ((nowMinutes - currentHourStart) / 60) * 100;
                        html += `<div class="agenda-current-time-line" style="top:${offset}%"></div>`;
                    }
                }

                html += '</div>';
            });

            html += '</div>';
        });

        html += '</div>';

        // Botón de hora actual al final (vista día)
        if (state.view === 'day') {
            // No horas visibles—render inline
            // Inline time labels for day view (left side)
            // We'll add them via CSS
        }

        // Vacío?
        if (citas.length === 0) {
            // No mostrar empty state si hay grid, el grid se ve
        }

        // Envoltorio day-view con time labels
        if (state.view === 'day') {
            // Re-render con time labels en day view
            html = renderDayViewWithLabels(days[0], citas);
        }

        container.innerHTML = html;

        // Scroll a la hora actual (solo vista día)
        if (state.view === 'day') {
            scrollToCurrentHour();
        }
    }

    function renderDayViewWithLabels(day, citas) {
        const range = getGridHourRange();
        const hours = [];
        for (let h = range.startHour; h < range.endHour; h++) {
            hours.push(h);
        }
        const dateKey = fmtDate(day);
        const today = isToday(day);

        let html = '';

        // Cabecera minimalista
        html += '<div class="agenda-week-header" style="grid-template-columns:var(--agenda-sidebar-width) 1fr">';
        html += '<div class="day-header"></div>';
        const todayClass = today ? 'today' : '';
        html += `<div class="day-header ${todayClass}">`;
        const dias = ['Domingo', 'Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado'];
        html += `<span>${dias[day.getDay()]}</span>`;
        html += `<span class="day-number">${day.getDate()}</span>`;
        html += '</div></div>';

        // Grid
        html += '<div class="agenda-time-grid">';

        hours.forEach(h => {
            const timeStr = `${String(h).padStart(2,'0')}:00`;
            const isCurrentHour = today && new Date().getHours() === h;

            html += `<div class="time-row" style="display:grid;grid-template-columns:var(--agenda-sidebar-width) 1fr" data-hour="${h}">`;
            html += `<div class="agenda-time-label">${timeStr}</div>`;

            const esDiaHorario = !state.horario || !state.horario.diasAtencion || isDiaAtencion(dateKey);
            const esHoraHorario = esDiaHorario && (!state.horario || !state.horario.horarioApertura || (h >= parseTimeToMinutes(state.horario.horarioApertura) / 60 && h < parseTimeToMinutes(state.horario.horarioCierre) / 60));
            const claseHorario = esHoraHorario ? 'hora-en-atencion' : (!esDiaHorario ? '' : 'hora-fuera-atencion');

            html += `<div class="agenda-day-cell ${today ? 'today-cell' : ''} ${claseHorario}" ` +
                    `data-date="${dateKey}" data-hour="${h}" ` +
                    `onclick="vittalAgenda.onCellClick('${dateKey}', ${h})">`;

            const hourCitas = citas.filter(c => {
                const cDate = c.fechaCita ? c.fechaCita.substring(0,10) : '';
                const cHora = parseTimeToMinutes(c.horaCita);
                const cHoraFin = parseTimeToMinutes(c.horaFin) || cHora + 30;
                return cDate === dateKey && Math.floor(cHora / 60) === h;
            });

            const colLayout = computeColumnLayout(hourCitas);

            hourCitas.forEach(c => {
                const cHora = parseTimeToMinutes(c.horaCita);
                const cHoraFin = parseTimeToMinutes(c.horaFin) || cHora + 30;
                const topOffset = ((cHora % 60) / 60) * 100;
                const durationH = Math.max((cHoraFin - cHora) / 60, 0.25);
                const heightPct = Math.min(durationH * 100, 400);
                const layout = colLayout[c.id] || { col: 0, cols: 1 };
                html += renderCitaCard(c, topOffset, heightPct, durationH > 1.5, layout.col, layout.cols);
            });

            // Botón "+" para agendar otra cita en esta misma hora
            if (hourCitas.length > 0) {
                html += `<div class="agenda-cell-add" ` +
                        `onclick="event.stopPropagation(); vittalAgenda.onCellClick('${dateKey}', ${h})" ` +
                        `title="Agendar otra cita a las ${String(h).padStart(2,'0')}:00">` +
                        `<i class="bi bi-plus-lg"></i></div>`;
            }

            if (isCurrentHour) {
                const now = new Date();
                const offset = ((now.getMinutes()) / 60) * 100;
                html += `<div class="agenda-current-time-line" style="top:${offset}%"></div>`;
            }

            html += '</div></div>';
        });

        html += '</div>';
        return html;
    }

    // ── Renderizar tarjeta de cita ──────────────────────────────
    /**
     * Asigna columnas a citas que se solapan en el mismo rango horario.
     * La clínica tiene varias salas con doctores que atienden a la vez,
     * por lo que varias citas pueden coincidir en la misma hora.
     * Retorna { [id]: { col, cols } } donde col = índice de columna y
     * cols = total de columnas del grupo solapado.
     */
    function computeColumnLayout(citas) {
        const eventos = citas.map(c => ({
            id: c.id,
            start: parseTimeToMinutes(c.horaCita),
            end: parseTimeToMinutes(c.horaFin) || parseTimeToMinutes(c.horaCita) + 30
        })).sort((a, b) => a.start - b.start || (b.end - a.end));

        const columnEnds = [];   // hora de fin de la última cita de cada columna
        const asignadas = {};    // id -> índice de columna

        eventos.forEach(ev => {
            let col = columnEnds.findIndex(end => end <= ev.start);
            if (col === -1) {
                col = columnEnds.length;
                columnEnds.push(ev.end);
            } else {
                columnEnds[col] = ev.end;
            }
            asignadas[ev.id] = col;
        });

        const totalCols = Math.max(columnEnds.length, 1);
        const layout = {};
        citas.forEach(c => {
            layout[c.id] = { col: asignadas[c.id] ?? 0, cols: totalCols };
        });
        return layout;
    }

    function renderCitaCard(c, topOffset, heightPct, compact, colIndex, colCount) {
        const estado = c.estado || 'agendada';
        const horaInicio = fmtTime(c.horaCita);
        const horaFin = c.horaFin ? fmtTime(c.horaFin) : '';
        const paciente = getPatientName(c);
        const doctor = getDoctorName(c);
        const sala = c.salaNombre || '';
        const id = c.id || '';
        const safePaciente = escapeHtml(paciente);
        const safeDoctor = escapeHtml(doctor);
        const safeSala = escapeHtml(sala);
        const estadoLabel = getEstadoLabel(estado);

        const heightStyle = heightPct > 0 ? `height:${heightPct}%` : 'height:30px';
        const topStyle = `top:${topOffset}%`;

        // ── Layout en columnas paralelas para citas simultáneas ──
        // Varios doctores pueden atender a la vez en distintas salas.
        // Si hay N citas que se solapan, cada una ocupa 100/N % de ancho.
        const idx = colIndex || 0;
        const total = colCount || 1;
        const leftPct = (idx / total) * 100;
        const widthPct = (100 / total) - 0.4; // pequeño margen entre columnas
        const columnStyle = `left:calc(${leftPct}% + 3px); width:calc(${widthPct}% - 3px);`;

        // Doctor + sala en una línea compacta
        let doctorLine = '';
        if (safeDoctor && safeSala) {
            doctorLine = `${safeDoctor} · ${safeSala}`;
        } else if (safeDoctor) {
            doctorLine = safeDoctor;
        } else if (safeSala) {
            doctorLine = safeSala;
        }

        return `
            <div class="agenda-card estado-${estado}"
                 style="${topStyle}; ${heightStyle}; ${columnStyle}"
                 onclick="event.stopPropagation(); vittalAgenda.onCitaClick('${id}')"
                 title="${safePaciente} — ${horaInicio}${horaFin ? ' a ' + horaFin : ''} | ${doctorLine} | ${estadoLabel}">
                <div class="card-row-main">
                    <span class="card-time">${horaInicio}${horaFin ? '—' + horaFin : ''}</span>
                    <span class="card-patient">${safePaciente}</span>
                    <span class="card-estado-badge badge-estado-${estado}">${estadoLabel}</span>
                </div>
                ${(!compact && doctorLine) ? `<div class="card-row-sub">${doctorLine}</div>` : ''}
            </div>
        `;
    }

    // ── RENDER: Vista Mes ──────────────────────────────────────
    function renderMonthView() {
        const year = state.currentDate.getFullYear();
        const month = state.currentDate.getMonth();
        const firstDay = new Date(year, month, 1);
        const lastDay = new Date(year, month + 1, 0);
        const startPad = firstDay.getDay(); // 0=Dom

        const daysInMonth = lastDay.getDate();
        const prevMonthDays = new Date(year, month, 0).getDate();

        // Fechas para grid (6 semanas x 7 días)
        const gridDays = [];
        const totalCells = Math.ceil((startPad + daysInMonth) / 7) * 7;

        for (let i = 0; i < totalCells; i++) {
            let d;
            if (i < startPad) {
                d = new Date(year, month - 1, prevMonthDays - startPad + i + 1);
            } else if (i >= startPad + daysInMonth) {
                d = new Date(year, month + 1, i - startPad - daysInMonth + 1);
            } else {
                d = new Date(year, month, i - startPad + 1);
            }
            gridDays.push(d);
        }

        const citas = getCitasForDateRange(
            gridDays[0],
            gridDays[gridDays.length - 1]
        );

        let html = '';

        // Cabecera de días de la semana
        const dayNames = ['Dom', 'Lun', 'Mar', 'Mié', 'Jue', 'Vie', 'Sáb'];
        html += '<div class="agenda-month-header">';
        dayNames.forEach(name => {
            html += `<div class="month-day-name">${name}</div>`;
        });
        html += '</div>';

        // Grid de días
        html += '<div class="agenda-month-grid">';

        gridDays.forEach(d => {
            const today = isToday(d) ? 'today-cell' : '';
            const isOtherMonth = d.getMonth() !== month;
            const dateKey = fmtDate(d);
            const dayCitas = citas.filter(c => {
                const cDate = c.fechaCita ? c.fechaCita.substring(0,10) : '';
                return cDate === dateKey;
            });

            html += `<div class="agenda-month-cell ${today} ${isOtherMonth ? 'other-month-cell' : ''}" ` +
                    `data-date="${dateKey}" onclick="vittalAgenda.onMonthCellClick('${dateKey}')">`;
            html += `<div class="month-day-number">${d.getDate()}</div>`;

            // Mostrar hasta 3 citas por día
            const maxDisplay = 3;
            dayCitas.slice(0, maxDisplay).forEach(c => {
                const hora = fmtTime(c.horaCita);
                const paciente = getPatientName(c);
                const estado = c.estado || 'agendada';
                const id = c.id || '';

                html += `<div class="agenda-month-card estado-${estado}" ` +
                        `onclick="event.stopPropagation();vittalAgenda.onCitaClick('${id}')">`;
                html += `<span class="mc-time">${hora}</span>`;
                html += `<span class="mc-patient">${escapeHtml(paciente)}</span>`;
                html += '</div>';
            });

            if (dayCitas.length > maxDisplay) {
                html += `<div class="agenda-card-more" onclick="event.stopPropagation();vittalAgenda.goToDay('${dateKey}')">`;
                html += `+${dayCitas.length - maxDisplay} más</div>`;
            }

            html += '</div>';
        });

        html += '</div>';

        container.innerHTML = html;
    }

    // ── Scroll a hora actual ────────────────────────────────────
    function scrollToCurrentHour() {
        const now = new Date();
        const hour = now.getHours();
        const targetRow = container.querySelector(`[data-hour="${hour}"]`);
        if (targetRow) {
            setTimeout(() => {
                targetRow.scrollIntoView({ block: 'center', behavior: 'smooth' });
            }, 100);
        }
    }

    // ── Event handlers ──────────────────────────────────────────
    function onCellClick(dateStr, hour) {
        // Bloquear creación de citas en fechas pasadas
        const hoyStr = fmtDate(new Date());
        if (dateStr < hoyStr) {
            showToast('No se pueden agendar citas en fechas pasadas.', 'warning');
            return;
        }
        const ahora = new Date();
        // Si es hoy, bloquear solo horas COMPLETAMENTE pasadas (no por minutos)
        if (dateStr === hoyStr && hour < ahora.getHours()) {
            showToast('Esta hora ya pasó. Seleccione una hora futura.', 'warning');
            return;
        }
        // Pre-fill de hora: si es la hora actual de hoy, usar el siguiente slot de 5 minutos
        let timeStr = `${String(hour).padStart(2,'0')}:00`;
        if (dateStr === hoyStr && hour === ahora.getHours()) {
            const minSlot = Math.ceil(ahora.getMinutes() / 5) * 5;
            if (minSlot < 60) {
                timeStr = `${String(hour).padStart(2,'0')}:${String(minSlot).padStart(2,'0')}`;
            } else {
                timeStr = `${String(hour + 1).padStart(2,'0')}:00`;
            }
        }
        openNewCitaAt(dateStr, timeStr);
    }

    function onMonthCellClick(dateStr) {
        state.currentDate = new Date(dateStr + 'T12:00:00');
        document.querySelectorAll('.btn-view').forEach(b => b.classList.remove('active'));
        document.querySelector('[data-view="day"]').classList.add('active');
        state.view = 'day';
        render();
    }

    function onCitaClick(id) {
        const cita = state.citas.find(c => c.id === id);
        if (!cita) return;
        state.detallesCita = cita;
        showDetalleCita(cita);
    }

    function goToDay(dateStr) {
        state.currentDate = new Date(dateStr + 'T12:00:00');
        document.querySelectorAll('.btn-view').forEach(b => b.classList.remove('active'));
        document.querySelector('[data-view="day"]').classList.add('active');
        state.view = 'day';
        render();
    }

    // ── Mostrar detalle de cita ─────────────────────────────────
    function showDetalleCita(cita) {
        const estadoLabels = {
            'agendada': '<span class="badge" style="background:var(--estado-agendada)">Agendada</span>',
            'en_espera': '<span class="badge" style="background:var(--estado-en-espera)">En Espera</span>',
            'en_atencion': '<span class="badge" style="background:var(--estado-en-atencion)">En Atención</span>',
            'atendida': '<span class="badge" style="background:var(--estado-atendida)">Atendida</span>',
            'cancelada': '<span class="badge" style="background:var(--estado-cancelada)">Cancelada</span>'
        };
        const estadoBadge = estadoLabels[cita.estado] || cita.estado;
        const fecha = cita.fechaCita ? cita.fechaCita.substring(0,10) : '';
        const hora = fmtTime(cita.horaCita);
        const horaFin = cita.horaFin ? fmtTime(cita.horaFin) : '';
        const paciente = getPatientName(cita);
        const doctor = getDoctorName(cita);
        const sala = cita.salaNombre || '—';
        const lugar = cita.lugar || '—';
        const motivo = cita.motivo || 'Sin motivo registrado';
        const notas = cita.notas || 'Sin notas';

        const body = `
            <div class="d-flex align-items-center justify-content-between mb-3">
                <h6 class="mb-0">${escapeHtml(paciente)}</h6>
                ${estadoBadge}
            </div>
            <table class="table table-sm table-borderless">
                <tr>
                    <td class="text-muted" style="width:90px"><i class="bi bi-calendar3 me-1"></i>Fecha</td>
                    <td><strong>${fecha}</strong></td>
                </tr>
                <tr>
                    <td class="text-muted"><i class="bi bi-clock me-1"></i>Hora</td>
                    <td><strong>${hora}${horaFin ? ' — ' + horaFin : ''}</strong></td>
                </tr>
                <tr>
                    <td class="text-muted"><i class="bi bi-person-badge me-1"></i>Doctor</td>
                    <td>${escapeHtml(doctor)}</td>
                </tr>
                <tr>
                    <td class="text-muted"><i class="bi bi-door-open me-1"></i>Sala</td>
                    <td>${escapeHtml(sala)}</td>
                </tr>
                <tr>
                    <td class="text-muted"><i class="bi bi-geo-alt me-1"></i>Lugar</td>
                    <td>${escapeHtml(lugar)}</td>
                </tr>
                <tr>
                    <td class="text-muted"><i class="bi bi-chat-text me-1"></i>Motivo</td>
                    <td>${escapeHtml(motivo)}</td>
                </tr>
                <tr>
                    <td class="text-muted"><i class="bi bi-sticky me-1"></i>Notas</td>
                    <td>${escapeHtml(notas)}</td>
                </tr>
            </table>
        `;

        $('detalleCitaBody').innerHTML = body;

        // ── Ocultar botón Editar en citas ya atendidas (consulta finalizada) ──
        const esAtendida = cita.estado === 'atendida';
        $('btnEditarDesdeDetalle').classList.toggle('d-none', esAtendida);

        modalDetalle.show();
    }

    // ── CRUD: Nueva cita ────────────────────────────────────────
    function openNewCita() {
        openNewCitaAt(fmtDate(state.currentDate), '08:00');
    }

    function openNewCitaAt(dateStr, timeStr) {
        state.editingId = null;
        $('modalCitaTitleText').textContent = 'Nueva Cita';
        $('btnGuardarTexto').textContent = 'Guardar Cita';
        $('citaId').value = '';
        $('formCita').reset();
        $('formCita').classList.remove('was-validated');
        $('citaFecha').value = dateStr;
        $('citaHora').value = timeStr;
        $('citaHoraFin').value = '';
        $('citaEstado').value = 'agendada';
        $('btnDesactivarCita').classList.add('d-none');

        // ── Restringir fecha mínima a hoy ────────────────────────
        $('citaFecha').min = fmtDate(new Date());

        // ── Validar horario de atención ──────────────────────────
        aplicarRestriccionesHorario(dateStr, timeStr);

        // Perfil doctor: su doctorId siempre fijado a sí mismo
        if (config.esDoctor) {
            $('citaDoctorId').value = config.usuarioId;
        }

        // HU21: ocultar/limpiar badge y checkbox de reasignación al abrir nueva cita
        syncDoctorPacienteInfo(false);

        modalCita.show();
    }

    function openEditCita(id) {
        const cita = state.citas.find(c => c.id === id);
        if (!cita) return;

        // ── Bloquear edición de citas ya atendidas (consulta finalizada) ──
        if (cita.estado === 'atendida') {
            showToast('La cita ya fue atendida y no se puede modificar.', 'warning');
            return;
        }

        state.editingId = id;
        $('modalCitaTitleText').textContent = 'Editar Cita';
        $('btnGuardarTexto').textContent = 'Actualizar Cita';
        $('citaId').value = id;
        $('formCita').classList.remove('was-validated');

        $('citaPacienteId').value = cita.pacienteId || '';
        $('citaDoctorId').value = config.esDoctor ? config.usuarioId : (cita.doctorId || '');
        $('citaFecha').value = cita.fechaCita ? cita.fechaCita.substring(0,10) : '';
        $('citaHora').value = toTimeInputValue(cita.horaCita);
        $('citaHoraFin').value = toTimeInputValue(cita.horaFin);
        $('citaSalaId').value = cita.salaId || '';
        $('citaLugar').value = cita.lugar || '';
        $('citaEstado').value = cita.estado || 'agendada';
        $('citaMotivo').value = cita.motivo || '';
        $('citaNotas').value = cita.notas || '';
        $('btnDesactivarCita').classList.remove('d-none');

        // ── Validar horario de atención ──────────────────────────
        aplicarRestriccionesHorario($('citaFecha').value, $('citaHora').value);

        // HU21: recalcular badge y checkbox de reasignación con los datos de la cita
        syncDoctorPacienteInfo(false);

        modalCita.show();
    }

    /**
     * Aplica restricciones de horario de atención en el modal de citas.
     * - Muestra badge con horario y días de atención
     * - Muestra advertencia si el día no es de atención
     * - NO usa min/max HTML5 (causa mensajes nativos confusos)
     */
    function aplicarRestriccionesHorario(dateStr, currentTime) {
        const horaInput = $('citaHora');
        const horaFinInput = $('citaHoraFin');
        const fechaInput = $('citaFecha');
        const badge = $('horarioBadge');
        const badgeValores = $('horarioBadgeValores');
        const badgeDiasValores = $('horarioBadgeDiasValores');

        // Remover advertencia previa
        const prevWarning = document.getElementById('horarioWarning');
        if (prevWarning) prevWarning.remove();

        if (!state.horario) {
            badge.classList.add('d-none');
            return;
        }

        const apertura = state.horario.horarioApertura;
        const cierre = state.horario.horarioCierre;
        const diasAtencion = state.horario.diasAtencion;

        // Mostrar badge de horario
        if (apertura && cierre) {
            badge.classList.remove('d-none');
            badgeValores.textContent = `${apertura} — ${cierre}`;
            badgeDiasValores.textContent = diasAtencion || 'Todos';
        } else {
            badge.classList.add('d-none');
        }

        // Verificar si el día seleccionado es de atención
        if (diasAtencion && dateStr) {
            const esDiaAtencion = isDiaAtencion(dateStr);
            if (!esDiaAtencion) {
                const warning = document.createElement('div');
                warning.id = 'horarioWarning';
                warning.className = 'alert alert-warning py-2 px-3 mb-2 small';
                warning.innerHTML = `<i class="bi bi-exclamation-triangle me-1"></i>` +
                    `La clínica <strong>no atiende</strong> este día. ` +
                    `Días de atención: <strong>${diasAtencion}</strong>. ` +
                    `La cita se creará fuera del horario habitual.`;
                $('formCita').prepend(warning);
            }
        }

        // Actualizar placeholder con horario
        if (apertura && cierre) {
            horaInput.placeholder = apertura;
            horaFinInput.placeholder = cierre;
        }
    }

    // ── HU21: coherencia doctor de cita ↔ médico asignado del paciente ──
    /**
     * Sincroniza la información del médico asignado del paciente con el doctor
     * seleccionado en la cita:
     * - Muestra un badge con el médico asignado del paciente (si tiene).
     * - Si el paciente tiene médico asignado y el doctor de la cita difiere,
     *   muestra (y marca) el checkbox de reasignación del médico tratante.
     * - Al cambiar de paciente (autofillDoctor=true) autocompleta el doctor
     *   de la cita con el asignado del paciente.
     * - Perfil doctor: no aplica (el doctor se fija a sí mismo y el campo está oculto).
     */
    function syncDoctorPacienteInfo(autofillDoctor) {
        if (config.esDoctor) return;

        const pacienteSel = $('citaPacienteId');
        const doctorSel = $('citaDoctorId');
        const infoDiv = $('doctorAssignadoInfo');
        const nombreSpan = $('doctorAssignadoNombre');
        const wrapChk = $('cambiarDoctorPacienteWrap');
        const chk = $('cambiarDoctorPaciente');

        if (!infoDiv || !wrapChk || !chk) return;

        const opt = pacienteSel.selectedOptions && pacienteSel.selectedOptions[0];
        const pacienteDoctorId = opt ? (opt.dataset.doctorId || '') : '';
        const pacienteDoctorNombre = opt ? (opt.dataset.doctorNombre || '') : '';

        // Sin paciente seleccionado o sin médico asignado: ocultar badge y checkbox
        if (!opt || !opt.value || !pacienteDoctorId) {
            infoDiv.classList.add('d-none');
            wrapChk.classList.add('d-none');
            chk.checked = false;
            return;
        }

        // Badge informativo con el médico asignado del paciente
        nombreSpan.textContent = pacienteDoctorNombre || '—';
        infoDiv.classList.remove('d-none');

        // Autocompletar el doctor de la cita con el asignado del paciente
        if (autofillDoctor) {
            doctorSel.value = pacienteDoctorId;
        }

        // Checkbox de reasignación: solo si el doctor elegido difiere del asignado
        const doctorSeleccionado = doctorSel.value || '';
        if (doctorSeleccionado && doctorSeleccionado !== pacienteDoctorId) {
            wrapChk.classList.remove('d-none');
            chk.checked = true;
        } else {
            wrapChk.classList.add('d-none');
            chk.checked = false;
        }
    }

    // ── Guardar cita (crear o actualizar) ───────────────────────
    async function saveCita() {
        const form = $('formCita');
        if (!form.checkValidity()) {
            form.classList.add('was-validated');
            return;
        }

        // ── Validación: no agendar en fechas pasadas ─────────────
        const fechaVal = $('citaFecha').value;
        const horaVal = $('citaHora').value;
        const horaFinVal = $('citaHoraFin').value;
        const hoyStr = fmtDate(new Date());

        if (!state.editingId && fechaVal < hoyStr) {
            showToast('No se pueden agendar citas en fechas pasadas. Seleccione el día de hoy o una fecha futura.', 'warning');
            return;
        }

        // Si es hoy, validar que la hora no sea en el pasado (precisión de segundos)
        if (!state.editingId && fechaVal === hoyStr && horaVal) {
            const ahora = new Date();
            const horaElegida = new Date(`${fechaVal}T${horaVal}:00`);
            if (horaElegida < ahora) {
                const ahoraStr = `${String(ahora.getHours()).padStart(2,'0')}:${String(ahora.getMinutes()).padStart(2,'0')}:${String(ahora.getSeconds()).padStart(2,'0')}`;
                showToast(`La hora ${horaVal} ya pasó. Seleccione una hora posterior a las ${ahoraStr}.`, 'warning');
                return;
            }
        }

        if (state.horario && state.horario.horarioApertura && state.horario.horarioCierre) {
            const apertura = state.horario.horarioApertura;
            const cierre = state.horario.horarioCierre;

            // Validar hora_fin > hora_cita
            if (horaFinVal && parseTimeToMinutes(horaFinVal) <= parseTimeToMinutes(horaVal)) {
                showToast('La hora de fin debe ser posterior a la hora de inicio.', 'warning');
                return;
            }

            // Validar día de atención
            if (fechaVal && !isDiaAtencion(fechaVal)) {
                const diaNombre = fmtDateLong(new Date(fechaVal + 'T12:00:00'));
                showToast(`La clínica no atiende el ${diaNombre}. Días de atención: ${state.horario.diasAtencion}`, 'warning');
                return;
            }

            // Validar hora dentro del rango
            if (!isHoraEnRango(horaVal)) {
                showToast(`La hora ${horaVal} está fuera del horario de atención (${apertura} — ${cierre}). Seleccione una hora entre ${apertura} y ${cierre}.`, 'warning');
                return;
            }
        }

        const id = $('citaId').value;
        const isEdit = id !== '';

        const data = {
            pacienteId: $('citaPacienteId').value || '00000000-0000-0000-0000-000000000000',
            doctorId: config.esDoctor ? config.usuarioId : ($('citaDoctorId').value || '00000000-0000-0000-0000-000000000000'),
            cambiarDoctorPaciente: $('cambiarDoctorPaciente').checked,
            salaId: $('citaSalaId').value || null,
            fechaCita: $('citaFecha').value,
            horaCita: $('citaHora').value,
            horaFin: $('citaHoraFin').value || null,
            lugar: $('citaLugar').value || null,
            motivo: $('citaMotivo').value || null,
            estado: $('citaEstado').value,
            notas: $('citaNotas').value || null
        };

        const btn = $('btnGuardarCita');
        const originalText = btn.innerHTML;
        btn.disabled = true;
        btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span> Guardando...';

        try {
            let res;
            if (isEdit) {
                res = await fetch(`${config.urls.actualizar}?id=${id}`, {
                    method: 'PUT',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(data)
                });
            } else {
                res = await fetch(config.urls.crear, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(data)
                });
            }

            const json = await res.json();

            if (res.ok && json.success) {
                showToast(json.message || (isEdit ? 'Cita actualizada' : 'Cita creada'), 'success');
                // Opción D: aviso preventivo — el paciente no tiene expediente.
                // Se creará automáticamente al atenderlo en la Cola de Espera.
                if (!isEdit && json.sinExpediente) {
                    showToast('Este paciente no tiene expediente. Se creará automáticamente cuando inicie su atención en la Cola de Espera.', 'warning');
                }
                modalCita.hide();
                await refreshCitas();
            } else {
                showToast(json.message || 'Error al guardar', 'danger');
            }
        } catch (err) {
            showToast('Error de conexión al guardar.', 'danger');
        } finally {
            btn.disabled = false;
            btn.innerHTML = originalText;
        }
    }

    // ── Desactivar cita ─────────────────────────────────────────
    async function desactivarCita(id) {
        try {
            const res = await fetch(`${config.urls.desactivar}?id=${id}`, {
                method: 'PATCH',
                headers: { 'Content-Type': 'application/json' }
            });
            const json = await res.json();
            if (res.ok && json.success) {
                showToast(json.message, 'warning');
                await refreshCitas();
            } else {
                showToast(json.message || 'Error al desactivar', 'danger');
            }
        } catch (err) {
            showToast('Error de conexión.', 'danger');
        }
    }

    // ── Recargar citas ──────────────────────────────────────────
    async function refreshCitas() {
        try {
            const res = await fetch(config.urls.citas);
            const json = await res.json();
            if (json.success) {
                state.citas = json.data || [];
                render();
            }
        } catch (err) {
            console.error('Error refreshing citas:', err);
        }
    }

    // ── Toast ───────────────────────────────────────────────────
    function showToast(message, type) {
        const container = $('toastContainer') || createToastContainer();
        const toast = document.createElement('div');
        toast.className = `alert alert-${type} alert-dismissible fade show shadow-sm agenda-toast`;
        toast.innerHTML = `${message}<button type="button" class="btn-close" data-bs-dismiss="alert"></button>`;
        container.appendChild(toast);
        setTimeout(() => toast.remove(), 5000);
    }

    function createToastContainer() {
        const div = document.createElement('div');
        div.id = 'toastContainer';
        document.body.appendChild(div);
        return div;
    }

    // ── Helpers ─────────────────────────────────────────────────
    function escapeHtml(text) {
        if (!text) return '';
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    // ── API pública ─────────────────────────────────────────────
    return {
        init,
        onCellClick,
        onMonthCellClick,
        onCitaClick,
        goToDay,
        openNewCita,
        openEditCita,
        refreshCitas,
        aplicarRestriccionesHorario
    };

})();
