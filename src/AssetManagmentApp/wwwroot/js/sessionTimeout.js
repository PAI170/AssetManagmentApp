(function () {
    // En /login no hay sesion que proteger. El login siempre hace una recarga completa
    // de pagina (forceLoad:true / redirect real), asi que esta comprobacion al cargar el
    // script es suficiente: no hace falta re-evaluarla despues.
    if (window.location.pathname.toLowerCase() === '/login') {
        return;
    }

    // Estos valores deben coincidir con options.ExpireTimeSpan en Program.cs.
    var SESSION_TIMEOUT_MS = 5 * 60 * 1000;
    var WARNING_BEFORE_MS = 60 * 1000;
    var PING_INTERVAL_MS = 60 * 1000;
    var CHECK_INTERVAL_MS = 1000;

    var lastActivity = Date.now();
    var lastPing = Date.now();
    var warningShown = false;
    var countdownTimer = null;
    var overlay = null;

    function buildOverlay() {
        overlay = document.createElement('div');
        overlay.id = 'session-timeout-overlay';
        overlay.innerHTML =
            '<div class="session-timeout-card">' +
                '<div class="session-timeout-title">Tu sesión está por expirar</div>' +
                '<p class="session-timeout-text">Por inactividad, tu sesión se va a cerrar en <span id="session-timeout-countdown">60</span> segundos.</p>' +
                '<button type="button" id="session-timeout-confirm" class="btn btn-primary">Sí, quiero seguir conectado</button>' +
            '</div>';
        document.body.appendChild(overlay);
        document.getElementById('session-timeout-confirm').addEventListener('click', confirmarSesion);
    }

    function ping() {
        fetch('/account/keep-alive', { method: 'POST', credentials: 'same-origin' }).catch(function () {});
        lastPing = Date.now();
    }

    function mostrarAviso() {
        if (warningShown) {
            return;
        }
        warningShown = true;
        if (!overlay) {
            buildOverlay();
        }
        overlay.style.display = 'flex';

        var restante = Math.round(WARNING_BEFORE_MS / 1000);
        var countdownEl = document.getElementById('session-timeout-countdown');
        countdownEl.textContent = restante;

        countdownTimer = setInterval(function () {
            restante -= 1;
            if (restante <= 0) {
                clearInterval(countdownTimer);
                window.location.href = '/login';
                return;
            }
            countdownEl.textContent = restante;
        }, 1000);
    }

    function ocultarAviso() {
        warningShown = false;
        if (countdownTimer) {
            clearInterval(countdownTimer);
            countdownTimer = null;
        }
        if (overlay) {
            overlay.style.display = 'none';
        }
    }

    function confirmarSesion() {
        ocultarAviso();
        lastActivity = Date.now();
        ping();
    }

    function marcarActividad() {
        lastActivity = Date.now();
        if (warningShown) {
            confirmarSesion();
        }
    }

    ['mousemove', 'mousedown', 'keydown', 'scroll', 'touchstart'].forEach(function (evento) {
        document.addEventListener(evento, marcarActividad, { passive: true });
    });

    setInterval(function () {
        var ahora = Date.now();
        var inactivo = ahora - lastActivity;

        if (inactivo >= SESSION_TIMEOUT_MS - WARNING_BEFORE_MS) {
            mostrarAviso();
        } else if (ahora - lastPing >= PING_INTERVAL_MS) {
            ping();
        }
    }, CHECK_INTERVAL_MS);
})();
