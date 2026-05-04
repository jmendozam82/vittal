# View — Login & Auth

> **Agente propietario:** @EspecialistaUI
> **Cuándo cargar:** Para crear la vista de Login y layout de autenticación.
> **Prerequisito:** skills/view/SKILL.md

---

## Layout de Login (_LayoutLogin.cshtml)

```html
<!DOCTYPE html>
<html lang="es">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>@ViewData["Title"] — Vittal</title>
  <link rel="stylesheet"
        href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.css" />
  <link rel="stylesheet" href="~/lib/bootstrap/dist/css/bootstrap.min.css" />
  <link rel="stylesheet" href="~/css/vittal-variables.css" />
  <link rel="stylesheet" href="~/css/vittal.css" />
  @await RenderSectionAsync("Styles", required: false)
</head>
<body>
  @RenderBody()
  <script src="~/lib/jquery/dist/jquery.min.js"></script>
  <script src="~/lib/bootstrap/dist/js/bootstrap.bundle.min.js"></script>
  <script src="~/lib/jquery-validation/dist/jquery.validate.min.js"></script>
  @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

---

## Vista de Login (Login.cshtml)

```html
<!-- Areas/Login/Views/Auth/Login.cshtml -->
@{
  Layout = "~/Views/Shared/_LayoutLogin.cshtml";
  ViewData["Title"] = "Iniciar Sesión";
}

<div class="min-vh-100 d-flex align-items-center justify-content-center"
     style="background: linear-gradient(135deg, #1A2535 0%, #1A6FA8 100%);">
  <div class="card shadow-lg border-0" style="width:420px; border-radius:16px;">
    <div class="card-body p-5">
      <div class="text-center mb-4">
        <i class="bi bi-heart-pulse-fill text-primary" style="font-size:3rem;"></i>
        <h3 class="fw-bold mt-2 mb-0" style="color:var(--vittal-text)">Vittal</h3>
        <p class="text-muted small">Sistema de Gestión Médica</p>
      </div>

      <div id="alertaError" class="alert alert-danger d-none" role="alert">
        <i class="bi bi-exclamation-triangle me-2"></i>
        <span id="mensajeError"></span>
      </div>

      <form id="frmLogin" novalidate>
        <div class="mb-3">
          <label for="usuario" class="form-label fw-medium small">
            Usuario <span class="text-danger">*</span>
          </label>
          <div class="input-group">
            <span class="input-group-text bg-light border-end-0">
              <i class="bi bi-person text-muted"></i>
            </span>
            <input type="email" id="usuario" name="usuario"
                   class="form-control border-start-0"
                   placeholder="correo@ejemplo.com"
                   autocomplete="username" required />
          </div>
          <div class="invalid-feedback">Ingrese su correo electrónico.</div>
        </div>

        <div class="mb-4">
          <label for="contrasena" class="form-label fw-medium small">
            Contraseña <span class="text-danger">*</span>
          </label>
          <div class="input-group">
            <span class="input-group-text bg-light border-end-0">
              <i class="bi bi-lock text-muted"></i>
            </span>
            <input type="password" id="contrasena" name="contrasena"
                   class="form-control border-start-0 border-end-0"
                   placeholder="••••••••"
                   autocomplete="current-password" required />
            <button type="button" class="input-group-text bg-light border-start-0"
                    id="togglePassword" title="Mostrar/ocultar contraseña">
              <i class="bi bi-eye" id="eyeIcon"></i>
            </button>
          </div>
          <div class="invalid-feedback">Ingrese su contraseña.</div>
        </div>

        <button type="submit" class="btn btn-vittal-primary w-100"
                id="btnLogin" style="border-radius:8px; padding:.65rem;">
          <span class="spinner-border spinner-border-sm me-2 d-none" id="loginSpinner"></span>
          <i class="bi bi-box-arrow-in-right me-1" id="loginIcon"></i>
          Iniciar Sesión
        </button>
      </form>

      <p class="text-muted text-center mt-3 mb-0" style="font-size:0.78rem;">
        &copy; @DateTime.Now.Year Vittal — Todos los derechos reservados
      </p>
    </div>
  </div>
</div>

@section Scripts {
<script>
  // Mostrar/ocultar contraseña
  document.getElementById('togglePassword').addEventListener('click', () => {
    const input = document.getElementById('contrasena');
    const icon  = document.getElementById('eyeIcon');
    const isPass = input.type === 'password';
    input.type = isPass ? 'text' : 'password';
    icon.className = isPass ? 'bi bi-eye-slash' : 'bi bi-eye';
  });

  // Envío del formulario
  document.getElementById('frmLogin').addEventListener('submit', async (e) => {
    e.preventDefault();
    const usuario = document.getElementById('usuario').value.trim();
    const contrasena = document.getElementById('contrasena').value;

    if (!usuario || !contrasena) {
      document.getElementById('usuario').classList.add('is-invalid');
      document.getElementById('contrasena').classList.add('is-invalid');
      return;
    }

    setLoginLoading(true);
    ocultarError();

    const res = await fetch('/api/auth/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ usuario, contrasena })
    });

    const data = await res.json();
    setLoginLoading(false);

    if (res.ok && data.success) {
      window.location.href = '/Dashboard/Dashboard/Index';
    } else {
      mostrarError(data.message || 'Usuario o contraseña incorrectos.');
    }
  });

  function setLoginLoading(loading) {
    document.getElementById('btnLogin').disabled = loading;
    document.getElementById('loginSpinner').classList.toggle('d-none', !loading);
    document.getElementById('loginIcon').classList.toggle('d-none', loading);
  }

  function mostrarError(msg) {
    document.getElementById('alertaError').classList.remove('d-none');
    document.getElementById('mensajeError').textContent = msg;
  }

  function ocultarError() {
    document.getElementById('alertaError').classList.add('d-none');
  }
</script>
}
```

---

## Checklist de Calidad — Login View

- [ ] Usa layout `_LayoutLogin.cshtml` (sin sidebar/navbar)
- [ ] Fondo con gradiente de marca (#1A2535 → #1A6FA8)
- [ ] Card centrada con sombra y bordes redondeados
- [ ] Logo + ícono bi-heart-pulse-fill
- [ ] Campos: usuario (email) + contraseña con toggle visibilidad
- [ ] Validación de campos vacíos antes del submit
- [ ] Estado de carga en botón (spinner)
- [ ] Alerta de error oculta por defecto, visible tras fallo
- [ ] Redirige a `/Dashboard/Dashboard/Index` tras éxito
- [ ] Footer con copyright y año dinámico
- [ ] Autocomplete attributes configurados (username, current-password)

---

*skills/view/login.md — Vittal v1.0.0*
