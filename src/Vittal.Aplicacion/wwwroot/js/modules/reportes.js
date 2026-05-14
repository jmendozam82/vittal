/**
 * reportes.js — Módulo de Reportes (HU22)
 *
 * Gestiona la generación de reportes, gráficos Chart.js,
 * exportación CSV e historial de reportes.
 *
 * Dependencias: vittal-api.js, Chart.js (CDN)
 */

(function () {
    'use strict';

    let chartInstance = null;
    let currentTipo = 'citas_atendidas';

    const DOM = {
        tabs: document.querySelectorAll('.reporte-tab'),
        btnGenerar: document.getElementById('btnGenerarReporte'),
        btnExportarCSV: document.getElementById('btnExportarCSV'),
        btnImprimir: document.getElementById('btnImprimir'),
        fechaInicio: document.getElementById('filtroFechaInicio'),
        fechaFin: document.getElementById('filtroFechaFin'),
        doctorId: document.getElementById('filtroDoctorId'),
        salaId: document.getElementById('filtroSalaId'),
        resultados: document.getElementById('reporteResultados'),
        historialContainer: document.getElementById('historialContainer'),
        historialCount: document.getElementById('historialCount')
    };

    document.addEventListener('DOMContentLoaded', function () {
        cargarDoctores();
        cargarSalas();
        cargarHistorial();

        // Tabs
        DOM.tabs.forEach(function (tab) {
            tab.addEventListener('click', function () {
                DOM.tabs.forEach(function (t) { t.classList.remove('active'); });
                this.classList.add('active');
                currentTipo = this.dataset.tipo;
            });
        });

        DOM.btnGenerar.addEventListener('click', generarReporte);
        DOM.btnExportarCSV.addEventListener('click', exportarCSV);
        DOM.btnImprimir.addEventListener('click', function () {
            window.print();
        });
    });

    async function cargarDoctores() {
        if (!DOM.doctorId) return;
        try {
            var res = await fetch('/Reportes/Reportes/JsonDoctores', {
                method: 'GET',
                headers: { 'Content-Type': 'application/json' }
            });
            var json = await res.json();
            if (json.success && json.data) {
                json.data.forEach(function (d) {
                    var opt = document.createElement('option');
                    opt.value = d.id || '';
                    opt.textContent = (d.nombres || '') + ' ' + (d.apellidos || '');
                    DOM.doctorId.appendChild(opt);
                });
            }
        } catch (err) {
            console.warn('Error cargando doctores:', err);
        }
    }

    async function cargarSalas() {
        if (!DOM.salaId) return;
        try {
            var res = await fetch('/Reportes/Reportes/JsonSalas', {
                method: 'GET',
                headers: { 'Content-Type': 'application/json' }
            });
            var json = await res.json();
            if (json.success && json.data) {
                json.data.forEach(function (d) {
                    var opt = document.createElement('option');
                    opt.value = d.id || '';
                    opt.textContent = d.nombre || 'Sala';
                    DOM.salaId.appendChild(opt);
                });
            }
        } catch (err) {
            console.warn('Error cargando salas:', err);
        }
    }

    async function generarReporte() {
        var payload = {
            tipo: currentTipo,
            fechaInicio: DOM.fechaInicio ? DOM.fechaInicio.value : '',
            fechaFin: DOM.fechaFin ? DOM.fechaFin.value : '',
            doctorId: DOM.doctorId ? DOM.doctorId.value || null : null,
            salaId: DOM.salaId ? DOM.salaId.value || null : null,
            formato: 'json'
        };

        if (!payload.fechaInicio || !payload.fechaFin) {
            VittalAPI.showToast('Debe seleccionar un rango de fechas.', 'warning');
            return;
        }

        DOM.resultados.innerHTML =
            '<div class="d-flex justify-content-center p-4"><div class="vittal-spinner" style="border-top-color:var(--vittal-primary);border:3px solid var(--vittal-border);"></div></div>';

        try {
            var res = await fetch('/Reportes/Reportes/JsonGenerar', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });
            var json = await res.json();

            if (res.ok && json.success) {
                VittalAPI.showToast(json.message || 'Reporte generado exitosamente.', 'success');
                renderizarReporte(json.data);
                await cargarHistorial();
            } else {
                VittalAPI.showToast(json.message || 'Error al generar reporte.', 'error');
                DOM.resultados.innerHTML = '<div class="empty-state"><i class="bi bi-exclamation-triangle"></i><p class="small">' +
                    escapeHtml(json.message || 'Error al generar el reporte.') + '</p></div>';
            }
        } catch (err) {
            VittalAPI.showToast('Error de conexión al generar reporte.', 'error');
            DOM.resultados.innerHTML = '<div class="empty-state"><i class="bi bi-exclamation-triangle"></i><p class="small">Error de conexión.</p></div>';
        }
    }

    function renderizarReporte(data) {
        if (!data) {
            DOM.resultados.innerHTML = '<div class="empty-state"><i class="bi bi-file-earmark-bar-graph"></i><p class="small">No hay datos para mostrar.</p></div>';
            return;
        }

        var contenidoJson = data.contenidoJson || '[]';
        var datos = [];

        try {
            datos = JSON.parse(contenidoJson);
        } catch (_) {
            datos = [];
        }

        var html = '';
        html += '<div class="d-flex align-items-center justify-content-between mb-3">';
        html += '<h6 class="mb-0 fw-semibold">' + escapeHtml(data.nombre || 'Reporte') + '</h6>';
        html += '<small class="text-muted">' + (data.fechaCreacion ? new Date(data.fechaCreacion + 'Z').toLocaleDateString('es-MX') : '') + '</small>';
        html += '</div>';

        // Gráfico
        html += '<div class="chart-wrapper">';
        html += '<canvas id="chartReporte"></canvas>';
        html += '</div>';

        // Tabla de datos
        if (datos.length > 0) {
            var columns = Object.keys(datos[0]);
            html += '<div class="table-responsive mt-3">';
            html += '<table class="vittal-table">';
            html += '<thead><tr>';
            columns.forEach(function (col) {
                html += '<th>' + escapeHtml(col) + '</th>';
            });
            html += '</tr></thead><tbody>';
            datos.forEach(function (row) {
                html += '<tr>';
                columns.forEach(function (col) {
                    html += '<td>' + escapeHtml(row[col] != null ? row[col] : '') + '</td>';
                });
                html += '</tr>';
            });
            html += '</tbody></table>';
            html += '</div>';
        } else {
            html += '<div class="empty-state"><i class="bi bi-inbox"></i><p class="small">Sin datos para este período.</p></div>';
        }

        DOM.resultados.innerHTML = html;

        // Renderizar gráfico según tipo
        if (datos.length > 0) {
            renderizarGrafico(currentTipo, datos);
        }
    }

    function renderizarGrafico(tipo, datos) {
        var ctx = document.getElementById('chartReporte');
        if (!ctx) return;

        if (chartInstance) {
            chartInstance.destroy();
            chartInstance = null;
        }

        var config = getChartConfig(tipo, datos);
        if (config) {
            chartInstance = new Chart(ctx, config);
        }
    }

    function getChartConfig(tipo, datos) {
        switch (tipo) {
            case 'citas_atendidas':
                return {
                    type: 'bar',
                    data: {
                        labels: datos.map(function (d) { return d.fecha || d.dia || ''; }),
                        datasets: [{
                            label: 'Citas Atendidas',
                            data: datos.map(function (d) { return parseInt(d.cantidad || d.total || 0); }),
                            backgroundColor: 'rgba(26, 111, 168, 0.7)',
                            borderColor: '#1A6FA8',
                            borderWidth: 1,
                            borderRadius: 6
                        }]
                    },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false,
                        plugins: { legend: { display: false } },
                        scales: {
                            y: { beginAtZero: true, ticks: { stepSize: 1 } },
                            x: { grid: { display: false } }
                        }
                    }
                };

            case 'pacientes_atendidos':
                return {
                    type: 'bar',
                    data: {
                        labels: datos.map(function (d) { return d.fecha || d.dia || ''; }),
                        datasets: [{
                            label: 'Pacientes',
                            data: datos.map(function (d) { return parseInt(d.cantidad || d.total || 0); }),
                            backgroundColor: 'rgba(46, 204, 113, 0.7)',
                            borderColor: '#2ECC71',
                            borderWidth: 1,
                            borderRadius: 6
                        }]
                    },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false,
                        plugins: { legend: { display: false } },
                        scales: {
                            y: { beginAtZero: true, ticks: { stepSize: 1 } },
                            x: { grid: { display: false } }
                        }
                    }
                };

            case 'ingresos':
                return {
                    type: 'bar',
                    data: {
                        labels: datos.map(function (d) { return d.fecha || d.mes || ''; }),
                        datasets: [{
                            label: 'Ingresos ($)',
                            data: datos.map(function (d) { return parseFloat(d.total || d.monto || 0); }),
                            backgroundColor: 'rgba(46, 204, 113, 0.7)',
                            borderColor: '#2ECC71',
                            borderWidth: 1,
                            borderRadius: 6
                        }]
                    },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false,
                        plugins: {
                            legend: { display: false },
                            tooltip: {
                                callbacks: {
                                    label: function (ctx) {
                                        return '$' + ctx.parsed.y.toFixed(2);
                                    }
                                }
                            }
                        },
                        scales: {
                            y: { beginAtZero: true },
                            x: { grid: { display: false } }
                        }
                    }
                };

            case 'tiempos_espera':
                return {
                    type: 'line',
                    data: {
                        labels: datos.map(function (d) { return d.hora || d.fecha || ''; }),
                        datasets: [{
                            label: 'Minutos de Espera',
                            data: datos.map(function (d) { return parseFloat(d.promedio || d.tiempo || 0); }),
                            borderColor: '#F39C12',
                            backgroundColor: 'rgba(243, 156, 18, 0.1)',
                            fill: true,
                            tension: 0.4,
                            pointBackgroundColor: '#F39C12'
                        }]
                    },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false,
                        plugins: { legend: { display: false } },
                        scales: {
                            y: { beginAtZero: true, title: { display: true, text: 'Minutos' } },
                            x: { grid: { display: false } }
                        }
                    }
                };

            default:
                return null;
        }
    }

    function exportarCSV() {
        var tabla = DOM.resultados.querySelector('.vittal-table');
        if (!tabla) {
            VittalAPI.showToast('No hay datos para exportar.', 'warning');
            return;
        }

        var csv = [];
        var rows = tabla.querySelectorAll('tr');

        rows.forEach(function (row) {
            var cols = row.querySelectorAll('th, td');
            var rowData = [];
            cols.forEach(function (col) {
                var text = col.textContent.trim().replace(/,/g, ';');
                rowData.push('"' + text + '"');
            });
            csv.push(rowData.join(','));
        });

        var csvContent = csv.join('\n');
        var blob = new Blob(['\uFEFF' + csvContent], { type: 'text/csv;charset=utf-8;' });
        var link = document.createElement('a');
        link.href = URL.createObjectURL(blob);
        link.download = 'reporte_' + currentTipo + '_' + new Date().toISOString().slice(0, 10) + '.csv';
        link.click();
        URL.revokeObjectURL(link.href);

        VittalAPI.showToast('CSV exportado correctamente.', 'success');
    }

    async function cargarHistorial() {
        try {
            var res = await fetch('/Reportes/Reportes/JsonHistorial', {
                method: 'GET',
                headers: { 'Content-Type': 'application/json' }
            });
            var json = await res.json();

            if (!json.success) {
                DOM.historialContainer.innerHTML = '<div class="empty-state"><i class="bi bi-inbox"></i><p class="small">Error al cargar historial.</p></div>';
                return;
            }

            var reportes = json.data || [];

            if (DOM.historialCount) {
                DOM.historialCount.textContent = reportes.length + ' reporte' + (reportes.length !== 1 ? 's' : '');
            }

            if (reportes.length === 0) {
                DOM.historialContainer.innerHTML = '<div class="empty-state"><i class="bi bi-inbox"></i><p class="small">No hay reportes generados aún.</p></div>';
                return;
            }

            DOM.historialContainer.innerHTML = reportes.map(function (r) {
                var fecha = r.fechaCreacion ? new Date(r.fechaCreacion + 'Z').toLocaleString('es-MX') : '';
                var tipo = r.tipo || '—';
                var nombre = r.nombre || 'Reporte';
                var tipoLabel = {
                    'citas_atendidas': 'Citas',
                    'pacientes_atendidos': 'Pacientes',
                    'ingresos': 'Ingresos',
                    'tiempos_espera': 'Espera'
                }[tipo] || tipo;

                return '<div class="reporte-historial-item">' +
                    '<div class="historial-info">' +
                    '<span class="historial-nombre">' + escapeHtml(nombre) + '</span>' +
                    '<span class="historial-fecha">' + fecha + '</span>' +
                    '</div>' +
                    '<div class="d-flex align-items-center gap-2">' +
                    '<span class="historial-tipo">' + escapeHtml(tipoLabel) + '</span>' +
                    '</div>' +
                    '</div>';
            }).join('');

        } catch (err) {
            console.warn('Error cargando historial:', err);
        }
    }

    function escapeHtml(text) {
        if (!text && text !== 0) return '';
        var div = document.createElement('div');
        div.textContent = String(text);
        return div.innerHTML;
    }
})();
