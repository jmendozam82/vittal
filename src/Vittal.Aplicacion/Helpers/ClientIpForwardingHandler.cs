using Microsoft.AspNetCore.Http;

namespace Vittal.Aplicacion.Helpers
{
    /// <summary>
    /// Reenvía la IP real del cliente a Vittal.API en el header X-Forwarded-For.
    ///
    /// Sin esto, el API (detrás del proxy de Render) ve la IP del contenedor del web
    /// para TODAS las llamadas, y el rate limiter de login particiona por esa IP
    /// compartida → "Too Many Requests" para todos los usuarios a la vez.
    ///
    /// Render pone la IP real del cliente en X-Forwarded-For al llegar al web;
    /// se reenvía ese valor (primer hop) o el RemoteIpAddress como fallback
    /// en cada request saliente hacia la API.
    /// </summary>
    public class ClientIpForwardingHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ClientIpForwardingHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext != null)
            {
                var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();

                var clientIp = !string.IsNullOrWhiteSpace(forwardedFor)
                    ? forwardedFor.Split(',')[0].Trim()
                    : httpContext.Connection.RemoteIpAddress?.ToString();

                if (!string.IsNullOrWhiteSpace(clientIp))
                {
                    request.Headers.Remove("X-Forwarded-For");
                    request.Headers.TryAddWithoutValidation("X-Forwarded-For", clientIp);
                }
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}