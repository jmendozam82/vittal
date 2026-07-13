using Vittal.Entity.Models;

namespace Vittal.BLL.Interfaces;

/// <summary>
/// Servicio de envío de correos electrónicos.
/// Utilizado para notificaciones al admin y otras comunicaciones del sistema.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Envía notificación al Super Admin cuando un prospecto envía contacto desde la landing.
    /// </summary>
    Task<bool> SendLandingContactNotificationAsync(ContactoLanding contacto);

    /// <summary>
    /// Envía un correo genérico con HTML personalizado.
    /// </summary>
    Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody);
}
