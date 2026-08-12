/**
 * vittal-session.js — Control de Sesión por Inactividad
 *
 * Cierra la sesión automáticamente cuando el usuario está inactivo 60 minutos.
 * Al detectar la inactividad, llama al Logout (limpia cookies y sesión del
 * servidor) y redirige al Login para que el usuario vea la pantalla de acceso.
 *
 * Requisito de negocio: en un entorno clínico no se deben mantener sesiones
 * abiertas sin actividad. El cierre es directo (sin aviso previo).
 *
 * Dependencias: Ninguna (vanilla JS). Se carga solo en páginas autenticadas
 * (ver _Layout.cshtml). NO se carga en Login/Landing.
 * Versión: 1.0.0
 */

(function () {
    'use strict';

    // 60 minutos de inactividad (alineado con ExpireTimeSpan de Program.cs)
    const INACTIVITY_TIMEOUT_MS = 60 * 60 * 1000;
    // Verificación periódica cada 30 segundos
    const CHECK_INTERVAL_MS = 30 * 1000;

    let lastActivity = Date.now();

    // Cualquier interacción del usuario reinicia el contador
    const ACTIVITY_EVENTS = ['mousemove', 'mousedown', 'keydown', 'touchstart', 'scroll', 'click', 'wheel'];

    function onActivity() {
        lastActivity = Date.now();
    }

    ACTIVITY_EVENTS.forEach(function (ev) {
        document.addEventListener(ev, onActivity, { passive: true });
    });

    // Verificación periódica de inactividad
    setInterval(function () {
        if (window.__vittalSessionClosing) return; // ya está cerrando
        var elapsed = Date.now() - lastActivity;
        if (elapsed >= INACTIVITY_TIMEOUT_MS) {
            cerrarSesionPorInactividad();
        }
    }, CHECK_INTERVAL_MS);

    /**
     * Cierra la sesión por inactividad: llama al Logout del servidor
     * (limpia cookies auth + vittal_jwt + session) y redirige al Login.
     */
    function cerrarSesionPorInactividad() {
        window.__vittalSessionClosing = true;
        console.log('[VittalSession] Sesión cerrada por inactividad (60 min sin actividad).');

        fetch('/Login/Auth/Logout', {
            method: 'POST',
            credentials: 'same-origin'
        })
            .catch(function () {
                // Si el servidor no responde, igual redirigimos al Login
            })
            .finally(function () {
                window.location.href = '/Login/Auth/Login?expired=1';
            });
    }

    // Exponer para debugging / pruebas
    window.VittalSession = {
        lastActivityMs: function () { return lastActivity; },
        timeoutMs: INACTIVITY_TIMEOUT_MS,
        forceLogout: cerrarSesionPorInactividad
    };
})();
