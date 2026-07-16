using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Vittal.API.Services;

/// <summary>
/// Background service que carga las claves JWKS de Supabase de forma asíncrona al startup.
/// Reintenta automáticamente si falla la primera vez.
/// Historia de Usuario: HU02 — Acceso al Sistema (Login)
/// </summary>
public class JwksLoaderService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<JwksLoaderService> _logger;
    private readonly string _jwksUrl;

    public JwksLoaderService(IServiceProvider serviceProvider, ILogger<JwksLoaderService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        // Leer la URL de JWKS desde la configuración
        using var scope = serviceProvider.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
        var supabaseUrl = configuration["Supabase:Url"];
        _jwksUrl = $"{supabaseUrl}/auth/v1/.well-known/jwks.json";
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("JWKS loader starting. Fetching keys from {Url}", _jwksUrl);

        var maxRetries = 5;
        var retryDelay = TimeSpan.FromSeconds(2);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var keys = await FetchJwksKeysAsync(_jwksUrl, cancellationToken);

                var cache = _serviceProvider.GetRequiredService<JwksCacheService>();
                cache.SetKeys(keys);

                _logger.LogInformation("JWKS loaded successfully: {Count} key(s) found (attempt {Attempt})", keys.Count, attempt);
                return;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("JWKS loading was cancelled during startup");
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("JWKS fetch attempt {Attempt}/{MaxRetries} failed: {Message}",
                    attempt, maxRetries, ex.Message);

                if (attempt < maxRetries)
                {
                    _logger.LogInformation("Retrying JWKS fetch in {Delay} seconds...", retryDelay.TotalSeconds);
                    await Task.Delay(retryDelay, cancellationToken);
                }
                else
                {
                    _logger.LogError("All JWKS fetch attempts failed. JWT validation will fail for ES256 tokens.");
                }
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("JWKS loader stopped");
        return Task.CompletedTask;
    }

    private static async Task<List<SecurityKey>> FetchJwksKeysAsync(string jwksUrl, CancellationToken cancellationToken)
    {
        var keys = new List<SecurityKey>();

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        var response = await client.GetStringAsync(jwksUrl, cancellationToken);
        var doc = JsonDocument.Parse(response);
        var jsonKeys = doc.RootElement.GetProperty("keys");

        foreach (var key in jsonKeys.EnumerateArray())
        {
            var rawText = key.GetRawText();
            var jsonWebKey = new JsonWebKey(rawText);
            keys.Add(jsonWebKey);
        }

        return keys;
    }
}
