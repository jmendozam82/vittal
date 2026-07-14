/**
 * vittal-alerts.js — Notificaciones en Tiempo Real y Alertas del Sistema
 *
 * Gestiona la suscripción a SignalR para notificaciones push,
 * badge de contador en navbar, toast de nuevas alertas.
 *
 * Dependencias: vittal-api.js, @microsoft/signalr (CDN)
 * Versión: 1.0.0
 */

(function () {
    'use strict';

    const CLINICA_ID = window.VITTAL_CLINICA_ID || '';

    let connection = null;
    let reconnectInterval = null;

    /**
     * Inicializa la conexión SignalR para notificaciones en tiempo real.
     */
    function initSignalR() {
        if (typeof signalR === 'undefined') {
            console.warn('[VittalAlertas] SignalR no está disponible. Fallback a polling.');
            iniciarPollingFallback();
            return;
        }

        const hubUrl = (window.VITTAL_API_HUB_URL || '') + '/hubs/alertas';
        connection = new signalR.HubConnectionBuilder()
            .withUrl(hubUrl, {
                accessTokenFactory: () => VittalAPI.getToken()
            })
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .build();

        // Evento: nueva notificación
        connection.on('NuevaNotificacion', function (notificacion) {
            console.log('[VittalAlertas] Nueva notificación:', notificacion);
            actualizarBadge();
            mostrarToastNotificacion(notificacion);
        });

        // Evento: alerta de espera
        connection.on('NuevaAlerta', function (alerta) {
            console.log('[VittalAlertas] Nueva alerta:', alerta);
            actualizarBadge();
            mostrarToastAlerta(alerta);
            reproducirSonido();
        });

        // Estado de conexión
        connection.onreconnecting(function () {
            console.log('[VittalAlertas] Reconectando...');
        });

        connection.onreconnected(function () {
            console.log('[VittalAlertas] Reconectado.');
            actualizarBadge();
        });

        connection.onclose(function () {
            console.log('[VittalAlertas] Conexión cerrada.');
            iniciarPollingFallback();
        });

        // Iniciar conexión
        connection.start()
            .then(function () {
                console.log('[VittalAlertas] Conectado a SignalR.');
                // Unirse al grupo de la clínica para recibir alertas
                if (CLINICA_ID) {
                    connection.invoke('JoinGroup', CLINICA_ID)
                        .then(function () {
                            console.log('[VittalAlertas] Unido al grupo clinica_' + CLINICA_ID);
                        })
                        .catch(function (err) {
                            console.error('[VittalAlertas] Error al unirse al grupo:', err);
                        });
                }
                actualizarBadge();
            })
            .catch(function (err) {
                console.error('[VittalAlertas] Error de conexión SignalR:', err);
                iniciarPollingFallback();
            });
    }

    /**
     * Actualiza el badge de notificaciones no leídas en el navbar.
     */
    async function actualizarBadge() {
        try {
            const badge = document.getElementById('notifBadge');
            if (!badge) return;

            const res = await fetch('/Dashboard/Dashboard/JsonNotificacionesNoLeidas', {
                method: 'GET',
                headers: { 'Content-Type': 'application/json' }
            });

            if (!res.ok) return;

            const json = await res.json();
            if (json.success) {
                const count = json.count || 0;
                badge.textContent = count;
                badge.classList.toggle('d-none', count === 0);

                // Animación de badge si hay nuevas
                if (count > 0) {
                    badge.style.animation = 'none';
                    setTimeout(function () {
                        badge.style.animation = 'badge-pop 0.3s ease';
                    }, 10);
                }
            }
        } catch (err) {
            console.warn('[VittalAlertas] Error actualizando badge:', err);
        }
    }

    /**
     * Muestra un toast cuando llega una nueva notificación.
     */
    function mostrarToastNotificacion(notif) {
        if (!notif) return;
        var titulo = notif.titulo || notif.titulo || 'Notificación';
        var mensaje = notif.mensaje || '';
        VittalAPI.showToast(titulo + (mensaje ? ': ' + mensaje : ''), 'info', 6000);
    }

    /**
     * Muestra un toast cuando llega una alerta de tiempo de espera.
     */
    function mostrarToastAlerta(alerta) {
        if (!alerta) return;
        var paciente = alerta.pacienteNombre || 'Paciente';
        var minutos = alerta.minutosEspera || 0;
        VittalAPI.showToast(
            '⏰ ' + paciente + ' lleva ' + minutos + ' min de espera',
            'warning',
            8000
        );
    }

    /**
     * Reproduce un sonido de alerta.
     */
    function reproducirSonido() {
        try {
            var audio = new Audio('/sounds/alert.mp3');
            audio.volume = 0.5;
            audio.play().catch(function () {
                // Autoplay bloqueado por el navegador — silencioso
            });
        } catch (_) {
            // Audio no soportado
        }
    }

    /**
     * Polling de respaldo cada 15s si SignalR no está disponible.
     */
    function iniciarPollingFallback() {
        if (reconnectInterval) return;
        console.log('[VittalAlertas] Iniciando polling fallback cada 15s.');
        reconnectInterval = setInterval(actualizarBadge, 15000);
    }

    // ── Inicializar cuando el DOM esté listo ──────────────
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initSignalR);
    } else {
        initSignalR();
    }

    // Exponer funciones globales para uso en vistas
    window.VittalAlertas = {
        actualizarBadge: actualizarBadge,
        mostrarToastNotificacion: mostrarToastNotificacion,
        mostrarToastAlerta: mostrarToastAlerta
    };
})();
