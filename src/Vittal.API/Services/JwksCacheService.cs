using System;
using System.Collections.Generic;
using Microsoft.IdentityModel.Tokens;

namespace Vittal.API.Services;

/// <summary>
/// Cache singleton de claves JWKS para validación de tokens Supabase.
/// Se llena de forma asíncrona al startup vía JwksLoaderService.
/// Historia de Usuario: HU02 — Acceso al Sistema (Login)
/// </summary>
public class JwksCacheService
{
    private readonly object _lock = new();
    private List<SecurityKey> _keys = new();
    private bool _isLoaded;

    public IReadOnlyCollection<SecurityKey> Keys
    {
        get { lock (_lock) return _keys.AsReadOnly(); }
    }

    public bool IsLoaded
    {
        get { lock (_lock) return _isLoaded; }
    }

    public void SetKeys(List<SecurityKey> keys)
    {
        lock (_lock)
        {
            _keys = keys ?? new List<SecurityKey>();
            _isLoaded = true;
        }
    }
}
