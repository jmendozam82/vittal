using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Vittal.BLL.Interfaces;
using Vittal.Entity;

namespace Vittal.BLL.Services;

/// <summary>
/// Servicio de envío de correos electrónicos using System.Net.Mail.
/// Configuración SMTP en appsettings.json → sección "Smtp".
/// </summary>
public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<bool> SendLandingContactNotificationAsync(ContactoLanding contacto)
    {
        try
        {
            var adminEmail = _configuration["Smtp:AdminEmail"]
                ?? throw new InvalidOperationException("Smtp:AdminEmail no está configurado.");

            var subject = $"🔔 Nuevo contacto desde la Landing — {contacto.NombreCompleto}";

            var htmlBody = BuildLandingContactHtml(contacto);

            return await SendEmailAsync(adminEmail, subject, htmlBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar notificación de landing para {Email}", contacto.Email);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        try
        {
            // Lee de variables de entorno primero (producción/Render),
            // luego de appsettings (desarrollo local)
            var host = GetConfig("Smtp:Host", "SMTP_HOST") ?? "smtp.gmail.com";
            var port = int.Parse(GetConfig("Smtp:Port", "SMTP_PORT") ?? "587");
            var enableSsl = bool.Parse(GetConfig("Smtp:EnableSsl", "SMTP_ENABLE_SSL") ?? "true");
            var username = GetConfig("Smtp:Username", "SMTP_USERNAME")
                ?? throw new InvalidOperationException("SMTP_USERNAME no está configurado.");
            var password = GetConfig("Smtp:Password", "SMTP_PASSWORD")
                ?? throw new InvalidOperationException("SMTP_PASSWORD no está configurado.");
            var fromEmail = GetConfig("Smtp:FromEmail", "SMTP_FROM_EMAIL")
                ?? throw new InvalidOperationException("SMTP_FROM_EMAIL no está configurado.");
            var fromName = GetConfig("Smtp:FromName", "SMTP_FROM_NAME") ?? "Vittal";

            using var message = new MailMessage();
            message.From = new MailAddress(fromEmail, fromName);
            message.To.Add(new MailAddress(toEmail));
            message.Subject = subject;
            message.Body = htmlBody;
            message.IsBodyHtml = true;

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                Credentials = new NetworkCredential(username, password),
                Timeout = 30000 // 30 segundos
            };

            await client.SendMailAsync(message);

            _logger.LogInformation(
                "Correo enviado exitosamente a {ToEmail} — Asunto: {Subject}",
                toEmail, subject);

            return true;
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex, "Error SMTP al enviar correo a {ToEmail}: {Message}",
                toEmail, ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al enviar correo a {ToEmail}", toEmail);
            return false;
        }
    }

    /// <summary>
    /// Genera el HTML del correo de notificación de contacto de landing.
    /// </summary>
    private static string BuildLandingContactHtml(ContactoLanding contacto)
    {
        var rolDisplay = contacto.Rol?.ToLowerInvariant() switch
        {
            "director" => "Director(a) de Clínica",
            "gerente" => "Gerente de Clínica",
            "admin" => "Administrador(a)",
            "doctor" => "Doctor(a)",
            _ => contacto.Rol
        };

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background: #f4f6f9; margin: 0; padding: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background: #fff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.08); }}
        .header {{ background: linear-gradient(135deg, #0F1A2E, #1A6FA8); padding: 24px 30px; color: #fff; }}
        .header h1 {{ margin: 0; font-size: 20px; font-weight: 700; }}
        .header p {{ margin: 6px 0 0; font-size: 13px; opacity: 0.8; }}
        .body {{ padding: 30px; }}
        .field {{ margin-bottom: 18px; }}
        .field-label {{ font-size: 12px; font-weight: 600; color: #6c757d; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 4px; }}
        .field-value {{ font-size: 15px; color: #2C3E50; line-height: 1.5; }}
        .field-value.mensaje {{ background: #f8fafc; padding: 14px; border-radius: 8px; border-left: 3px solid #1A6FA8; white-space: pre-wrap; }}
        .badge {{ display: inline-block; background: #E8F3FB; color: #1A6FA8; padding: 3px 10px; border-radius: 20px; font-size: 12px; font-weight: 600; }}
        .footer {{ padding: 20px 30px; background: #f8fafc; text-align: center; font-size: 12px; color: #999; border-top: 1px solid #eee; }}
        .footer a {{ color: #1A6FA8; text-decoration: none; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔔 Nuevo Contacto desde la Landing</h1>
            <p>Un prospecto ha enviado un mensaje a través del formulario de contacto.</p>
        </div>
        <div class='body'>
            <div class='field'>
                <div class='field-label'>Nombre Completo</div>
                <div class='field-value'><strong>{WebUtility.HtmlEncode(contacto.NombreCompleto)}</strong></div>
            </div>
            <div class='field'>
                <div class='field-label'>Correo Electrónico</div>
                <div class='field-value'><a href='mailto:{WebUtility.HtmlEncode(contacto.Email)}'>{WebUtility.HtmlEncode(contacto.Email)}</a></div>
            </div>
            <div class='field'>
                <div class='field-label'>Teléfono</div>
                <div class='field-value'>{WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(contacto.Telefono) ? "No proporcionado" : contacto.Telefono)}</div>
            </div>
            <div class='field'>
                <div class='field-label'>Rol / Cargo</div>
                <div class='field-value'><span class='badge'>{WebUtility.HtmlEncode(rolDisplay)}</span></div>
            </div>
            <div class='field'>
                <div class='field-label'>Mensaje</div>
                <div class='field-value mensaje'>{WebUtility.HtmlEncode(contacto.Mensaje)}</div>
            </div>
        </div>
        <div class='footer'>
            <p>Este correo fue enviado automáticamente por <a href='#'>Vittal Software</a>.</p>
            <p>Fecha: {contacto.FechaCreacion:dd/MM/yyyy HH:mm:ss} UTC</p>
        </div>
    </div>
</body>
</html>";
    }

    /// <summary>
    /// Lee un valor de configuración: primero variable de entorno (producción),
    /// luego IConfiguration (desarrollo). Permite desplegar en Render sin appsettings.Development.json.
    /// </summary>
    private string? GetConfig(string configKey, string envVarName)
    {
        // 1. Variable de entorno tiene prioridad (producción/Render)
        var envValue = Environment.GetEnvironmentVariable(envVarName);
        if (!string.IsNullOrWhiteSpace(envValue))
            return envValue;

        // 2. Fallback a IConfiguration (desarrollo local)
        return _configuration[configKey];
    }
}
