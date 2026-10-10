/* =====================================================================================
   SwiftShip - the JavaScript of the public pages and of the app shell.

   There is no framework here on purpose: the project is an ASP.NET Core MVC application
   and the pages are rendered on the server. This file only does the things a server cannot
   do: remember the theme, open the sidebar on a phone, count the KPI numbers up, filter a
   table as you type, and draw the charts of the reports.
   ===================================================================================== */

(function () {
    'use strict';

    /* ------------------------------------------------------------ theme
       The choice is remembered in localStorage. The first time a visitor arrives we follow
       the preference of his operating system instead of guessing. Every read and write is
       wrapped in try/catch, because private browsing throws when localStorage is touched. */
    var THEME_KEY = 'swiftship-theme';

    function readStoredTheme() {
        try { return window.localStorage.getItem(THEME_KEY); } catch (e) { return null; }
    }

    function storeTheme(theme) {
        try { window.localStorage.setItem(THEME_KEY, theme); } catch (e) { /* nothing we can do */ }
    }

    function systemTheme() {
        try {
            return window.matchMedia('(prefers-color-scheme: light)').matches ? 'light' : 'dark';
        } catch (e) {
            return 'dark';
        }
    }

    function applyTheme(theme) {
        document.documentElement.setAttribute('data-theme', theme);
        var buttons = document.querySelectorAll('.theme-toggle');
        for (var i = 0; i < buttons.length; i++) {
            buttons[i].setAttribute('aria-label', theme === 'light' ? 'Switch to dark mode' : 'Switch to light mode');
            buttons[i].textContent = theme === 'light' ? '\u263E' : '\u2600';
        }
    }

    function currentTheme() {
        return document.documentElement.getAttribute('data-theme') === 'light' ? 'light' : 'dark';
    }

    applyTheme(readStoredTheme() || systemTheme());

    document.addEventListener('click', function (event) {
        var button = event.target.closest ? event.target.closest('.theme-toggle') : null;
        if (!button) return;
        var next = currentTheme() === 'light' ? 'dark' : 'light';
        applyTheme(next);
        storeTheme(next);
        redrawCharts();
    });

    /* ------------------------------------------------------------ toast
       The controller puts a message in TempData, the layout renders it hidden, and this
       turns it into a toast that disappears on its own. */
    window.swiftshipToast = function (kind, text) {
        var host = document.getElementById('toasts');
        if (!host) return;
        var toast = document.createElement('div');
        toast.className = 'toast ' + (kind || 'info');
        toast.setAttribute('role', 'status');
        toast.textContent = text;
        host.appendChild(toast);
        window.setTimeout(function () {
            toast.style.opacity = '0';
            toast.style.transform = 'translateY(10px)';
            window.setTimeout(function () { toast.remove(); }, 300);
        }, 4200);
    };

    /* ------------------------------------------------------------ sidebar on a phone */
    window.swiftshipToggleSidebar = function () {
        var sidebar = document.querySelector('.sidebar');
        if (!sidebar) return;
        sidebar.classList.toggle('open');
        var existing = document.querySelector('.scrim');
        if (sidebar.classList.contains('open') && !existing) {
            var scrim = document.createElement('div');
            scrim.className = 'scrim';
            scrim.addEventListener('click', window.swiftshipToggleSidebar);
            document.body.appendChild(scrim);
        } else if (existing) {
            existing.remove();
        }
    };

    /* ------------------------------------------------------------ count-up KPI cards
       data-count holds the final number; the animation only runs once, when the card
       becomes visible. */
    function countUp(el) {
        var target = parseFloat(el.getAttribute('data-count') || '0');
        var decimals = parseInt(el.getAttribute('data-decimals') || '0', 10);
        var duration = 900;
        var start = null;

        function step(ts) {
            if (start === null) start = ts;
            var progress = Math.min((ts - start) / duration, 1);
            var eased = 1 - Math.pow(1 - progress, 3);
            el.textContent = (target * eased).toFixed(decimals);
            if (progress < 1) window.requestAnimationFrame(step);
        }
        window.requestAnimationFrame(step);
    }

    function runCountUps() {
        var nodes = document.querySelectorAll('[data-count]');
        for (var i = 0; i < nodes.length; i++) {
            if (nodes[i].getAttribute('data-done') === '1') continue;
            nodes[i].setAttribute('data-done', '1');
            countUp(nodes[i]);
        }
    }

    /* ------------------------------------------------------------ filter a table as you type */
    window.swiftshipFilterTable = function (input, tableId) {
        var term = (input.value || '').toLowerCase();
        var table = document.getElementById(tableId);
        if (!table) return;
        var rows = table.querySelectorAll('tbody tr');
        for (var i = 0; i < rows.length; i++) {
            rows[i].style.display = rows[i].textContent.toLowerCase().indexOf(term) >= 0 ? '' : 'none';
        }
    };

    /* ------------------------------------------------------------ charts (Chart.js)
       The data is written into the page by the Razor view as JSON, so this file never has
       to know what a shipment is. */
    var charts = [];

    function colours() {
        var style = getComputedStyle(document.documentElement);
        return {
            accent: style.getPropertyValue('--accent').trim() || '#C25400',
            muted: style.getPropertyValue('--muted').trim() || '#9FB0C8',
            border: style.getPropertyValue('--border').trim() || 'rgba(255,255,255,.1)',
            success: style.getPropertyValue('--success').trim() || '#16A34A',
            info: style.getPropertyValue('--info').trim() || '#2563EB',
            warning: style.getPropertyValue('--warning').trim() || '#F59E0B'
        };
    }

    function redrawCharts() {
        for (var i = 0; i < charts.length; i++) {
            var entry = charts[i];
            entry.chart.destroy();
            drawChart(entry.canvas);
        }
    }

    function drawChart(canvas) {
        if (typeof window.Chart === 'undefined') return null;

        var labels = JSON.parse(canvas.getAttribute('data-labels') || '[]');
        var series = JSON.parse(canvas.getAttribute('data-series') || '[]');
        var type = canvas.getAttribute('data-type') || 'bar';
        var c = colours();
        var palette = [c.accent, c.info, c.success, c.warning, c.muted];

        var datasets = [];
        for (var i = 0; i < series.length; i++) {
            var colour = palette[i % palette.length];
            datasets.push({
                label: series[i].name,
                data: series[i].values,
                backgroundColor: type === 'line' ? colour + '33' : colour,
                borderColor: colour,
                borderWidth: type === 'line' ? 3 : 0,
                fill: type === 'line',
                tension: 0.35,
                pointRadius: type === 'line' ? 3 : 0
            });
        }

        var chart = new window.Chart(canvas.getContext('2d'), {
            type: type,
            data: { labels: labels, datasets: datasets },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                animation: { duration: 700 },
                plugins: {
                    legend: { labels: { color: c.muted, font: { family: 'Manrope' } } }
                },
                scales: (type === 'pie' || type === 'doughnut') ? {} : {
                    x: { ticks: { color: c.muted }, grid: { color: c.border } },
                    y: { ticks: { color: c.muted }, grid: { color: c.border }, beginAtZero: true }
                }
            }
        });

        charts.push({ canvas: canvas, chart: chart });
        return chart;
    }

    function initCharts() {
        var nodes = document.querySelectorAll('canvas[data-chart]');
        for (var i = 0; i < nodes.length; i++) drawChart(nodes[i]);
    }

    /* ------------------------------------------------------------ client-side validation
       The server always validates again; this only spares the user a round trip. */
    function initValidation() {
        var forms = document.querySelectorAll('form[data-validate]');
        for (var i = 0; i < forms.length; i++) {
            forms[i].addEventListener('submit', function (event) {
                var form = event.target;
                var ok = true;
                var required = form.querySelectorAll('[data-required]');
                for (var j = 0; j < required.length; j++) {
                    var field = required[j];
                    var empty = !(field.value || '').trim();
                    field.classList.toggle('input-invalid', empty);
                    if (empty) ok = false;
                }
                var numbers = form.querySelectorAll('[data-positive]');
                for (var k = 0; k < numbers.length; k++) {
                    var value = parseFloat(numbers[k].value);
                    var bad = isNaN(value) || value <= 0;
                    numbers[k].classList.toggle('input-invalid', bad);
                    if (bad) ok = false;
                }
                if (!ok) {
                    event.preventDefault();
                    window.swiftshipToast('error', 'Please check the highlighted fields.');
                }
            });
        }
    }

    /* ------------------------------------------------------------ start */
    document.addEventListener('DOMContentLoaded', function () {
        runCountUps();
        initCharts();
        initValidation();

        var flash = document.getElementById('flash-message');
        if (flash) {
            window.swiftshipToast(flash.getAttribute('data-kind') || 'info', flash.textContent.trim());
            flash.remove();
        }
    });
})();
