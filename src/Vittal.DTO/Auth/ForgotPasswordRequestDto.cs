namespace Vittal.DTO.Auth;

/// <summary>
/// DTO de solicitud para "Olvidó su contraseña".
/// El usuario ingresa su email y el sistema notifica al administrador de su clínica.
/// </summary>
public class ForgotPasswordRequestDto
{
    /// <summary>Correo electrónico del usuario que solicita restablecer su contraseña.</summary>
    public string Email { get; set; } = string.Empty;
}
