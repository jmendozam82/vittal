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
