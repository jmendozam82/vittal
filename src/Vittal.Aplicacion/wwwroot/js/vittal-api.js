/**
 * vittal-api.js — Cliente Fetch Centralizado para Vittal.API
 *
 * Proporciona métodos helper para llamar al API REST con JWT,
 * manejo centralizado de errores, toasts y loading states.
 *
 * Dependencias: Ninguna (vanilla JS)
 * Versión: 1.0.0
 */

const VittalAPI = (() => {
    'use strict';

    const API_BASE = window.VITTAL_API_URL || '/api';

    /**
     * Obtiene el JWT del meta tag.
     * @returns {string}
     */
    function getToken() {
        return document.querySelector('meta[name="vittal-token"]')?.content || '';
    }

    /**
     * Crea el contenedor de toasts si no existe.
     * @returns {HTMLElement}
     */
    function createToastContainer() {
        let container = document.getElementById('vittal-toast-container');
        if (!container) {
            container = document.createElement('div');
            container.id = 'vittal-toast-container';
            container.className = 'vittal-toast-container';
            document.body.appendChild(container);
        }
        return container;
    }

    /**
     * Muestra una notificación toast.
     * @param {string} message - Mensaje a mostrar
     * @param {'success'|'error'|'warning'|'info'} type - Tipo de toast
     * @param {number} duration - Duración en ms (default: 5000)
     */
    function showToast(message, type = 'success', duration = 5000) {
        const container = createToastContainer();
        const toast = document.createElement('div');
        toast.className = 'toast-item alert alert-dismissible fade show shadow-sm';
        toast.style.cssText = 'min-width:300px;max-width:450px;';

        const iconMap = {
            success: 'bi-check-circle',
            error: 'bi-exclamation-triangle',
            warning: 'bi-exclamation-circle',
            info: 'bi-info-circle'
        };
        const alertMap = {
            success: 'alert-success',
            error: 'alert-danger',
            warning: 'alert-warning',
            info: 'alert-info'
        };
        toast.classList.add(alertMap[type] || 'alert-info');

        toast.innerHTML = `
            <i class="bi ${iconMap[type] || 'bi-info-circle'} me-2"></i>
            ${escapeHtml(message)}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>`;

        container.appendChild(toast);

        setTimeout(() => {
            if (toast.parentNode) toast.remove();
        }, duration);
    }

    /**
     * Muestra un spinner de carga en un contenedor.
     * @param {string} elementId - ID del elemento contenedor
     */
    function showLoading(elementId) {
        const el = document.getElementById(elementId);
        if (el) {
            el.innerHTML = `
                <div class="d-flex justify-content-center p-4">
                    <div class="vittal-spinner" style="border-top-color:var(--vittal-primary);border:3px solid var(--vittal-border);"></div>
                </div>`;
        }
    }

    /**
     * Realiza una petición HTTP autenticada.
     * @param {string} method - Método HTTP
     * @param {string} endpoint - Endpoint (ej: '/pacientes')
     * @param {object|null} body - Cuerpo de la petición (para POST/PUT/PATCH)
     * @returns {Promise<{ok: boolean, status: number, data: object|null}|null>}
     */
    async function request(method, endpoint, body = null) {
        const token = getToken();
        const options = {
            method,
            credentials: 'include',
            headers: {
                'Content-Type': 'application/json'
            }
        };

        if (token) {
            options.headers['Authorization'] = `Bearer ${token}`;
        }

        if (body !== null) {
            options.body = JSON.stringify(body);
        }

        try {
            const response = await fetch(`${API_BASE}${endpoint}`, options);

            if (response.status === 401) {
                window.location.href = '/Login/Auth/Login?returnUrl=' +
                    encodeURIComponent(window.location.pathname);
                return null;
            }

            const text = await response.text();
            let data = null;
            try {
                data = text ? JSON.parse(text) : null;
            } catch (_) {
                // Respuesta no JSON
            }

            return { ok: response.ok, status: response.status, data };

        } catch (error) {
            console.error(`[VittalAPI] Error en ${method} ${endpoint}:`, error);
            showToast('Error de conexión con el servidor. Intente nuevamente.', 'error');
            return null;
        }
    }

    /**
     * Escapa HTML para prevenir XSS.
     * @param {string} text
     * @returns {string}
     */
    function escapeHtml(text) {
        if (!text && text !== 0 && text !== false) return '';
        const div = document.createElement('div');
        div.textContent = String(text);
        return div.innerHTML;
    }

    // API pública
    return {
        get:       (endpoint)       => request('GET', endpoint),
        post:      (endpoint, body) => request('POST', endpoint, body),
        put:       (endpoint, body) => request('PUT', endpoint, body),
        patch:     (endpoint, body) => request('PATCH', endpoint, body),
        del:       (endpoint)       => request('DELETE', endpoint),
        showToast,
        showLoading,
        escapeHtml,
        getToken
    };
})();
