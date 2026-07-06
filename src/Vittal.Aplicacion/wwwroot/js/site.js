// ──────────────────────────────────────────────────────────────────
// Site-wide JavaScript — Vittal
// ──────────────────────────────────────────────────────────────────

// ── Sidebar Collapse/Expand ───────────────────────────────────────
(function() {
    'use strict';

    const SIDEBAR_COLLAPSED_KEY = 'vittal_sidebar_collapsed';
    const sidebar = document.getElementById('mainSidebar');
    const content = document.getElementById('mainContent');
    const toggle = document.getElementById('sidebarToggle');

    const COLLAPSED_WIDTH = 62;
    const EXPANDED_WIDTH = 260;

    function applySidebarState(isCollapsed) {
        if (!sidebar || !content) return;

        if (isCollapsed) {
            sidebar.classList.add('collapsed');
            content.style.marginLeft = COLLAPSED_WIDTH + 'px';
            if (toggle) {
                toggle.title = 'Expandir menú';
                toggle.innerHTML = '<i class="bi bi-chevron-right"></i>';
            }
        } else {
            sidebar.classList.remove('collapsed');
            content.style.marginLeft = EXPANDED_WIDTH + 'px';
            if (toggle) {
                toggle.title = 'Colapsar menú';
                toggle.innerHTML = '<i class="bi bi-list"></i>';
            }
        }
    }

    // Load saved state
    const savedState = localStorage.getItem(SIDEBAR_COLLAPSED_KEY);
    if (savedState === 'true') {
        applySidebarState(true);
    }

    // Toggle click handler
    if (toggle) {
        toggle.addEventListener('click', function(e) {
            e.preventDefault();
            const isCollapsed = !sidebar.classList.contains('collapsed');
            applySidebarState(isCollapsed);
            localStorage.setItem(SIDEBAR_COLLAPSED_KEY, isCollapsed ? 'true' : 'false');
        });
    }

    // Responsive: auto-collapse on small screens
    function handleResize() {
        if (window.innerWidth < 992) {
            if (!sidebar.classList.contains('collapsed')) {
                applySidebarState(true);
                localStorage.setItem(SIDEBAR_COLLAPSED_KEY, 'true');
            }
        }
    }

    window.addEventListener('resize', handleResize);
    // Check on load
    if (window.innerWidth < 992) {
        applySidebarState(true);
    }
})();

// ── Sidebar: Acordeón suave + auto-scroll ────────────────────────────
(function() {
    'use strict';

    const menuContainer = document.getElementById('sidebarMenuContainer');
    if (!menuContainer) return;

    // Cuando Bootstrap collapse muestra un submenú, auto-scrollea para que sea visible
    menuContainer.addEventListener('shown.bs.collapse', function(e) {
        const target = e.target;
        if (!target.classList.contains('sub-nav')) return;

        // Pequeño delay para que la animación termine
        setTimeout(function() {
            // Verifica si el bottom del submenú está fuera de la vista
            const rect = target.getBoundingClientRect();
            const containerRect = menuContainer.getBoundingClientRect();
            const offsetBottom = rect.bottom - containerRect.bottom + 20;

            if (offsetBottom > 0) {
                menuContainer.scrollBy({ top: offsetBottom, behavior: 'smooth' });
            }
        }, 350); // después de la animación del collapse (~300ms)
    });

    // Al cerrar un collapse, si el scroll está muy abajo, sube suavemente
    menuContainer.addEventListener('hidden.bs.collapse', function(e) {
        const target = e.target;
        if (!target.classList.contains('sub-nav')) return;

        const containerRect = menuContainer.getBoundingClientRect();
        const activeLinks = menuContainer.querySelectorAll('.nav-link.active');
        if (activeLinks.length === 0) {
            // No hay nada activo visible, scroll al inicio suavemente
            menuContainer.scrollTo({ top: 0, behavior: 'smooth' });
        }
    });
})();

