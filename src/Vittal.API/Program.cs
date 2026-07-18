using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Vittal.API.Hubs;
using Vittal.API.Middleware;
using Vittal.API.Services;
using Vittal.IOC;

var builder = WebApplication.CreateBuilder(args);

// ── Kestrel: HTTPS para WSS (WebSocket Secure) ─────────────────────
// En desarrollo usa el dev-cert; en producción configurar un cert real.
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5089, listenOptions =>
    {
        listenOptions.UseHttps(); // Dev cert auto-managed by .NET
    });
});

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.Converters.Add(new DateOnlyJsonConverter());
        opts.JsonSerializerOptions.Converters.Add(new TimeOnlyJsonConverter());
    });

// Configure HttpClient for Supabase Auth
builder.Services.AddHttpClient("SupabaseAuth");

// Register IOC Dependencies
builder.Services.AddVittalServices(builder.Configuration);

// Register JWKS cache and async loader
builder.Services.AddSingleton<JwksCacheService>();
builder.Services.AddHostedService<JwksLoaderService>();

// Register SignalR short-lived token service (HMAC-SHA256, 60s lifetime)
builder.Services.AddSingleton<SignalrTokenService>();

// Configure JWT Authentication
var supabaseUrl = builder.Configuration["Supabase:Url"];
var jwtIssuer = $"{supabaseUrl}/auth/v1";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidAudience = "authenticated",
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        TryAllIssuerSigningKeys = true,
        ClockSkew = TimeSpan.FromMinutes(2)
    };

    // Log authentication for debugging
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // SignalR: lee el token del query string (WebSocket no soporta headers)
            var path = context.HttpContext.Request.Path;
            if (path.StartsWithSegments("/hubs"))
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken))
                {
                    context.Token = accessToken;
                }
            }

            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            var token = context.Token;
            if (string.IsNullOrEmpty(token))
            {
                logger.LogWarning("No Bearer token found in Authorization header");
            }
            else
            {
                // Decode JWT header for debugging — solo en Development
                var parts = token.Split('.');
                if (parts.Length >= 2 && context.HttpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment())
                {
                    try
                    {
                        var headerJson = System.Text.Encoding.UTF8.GetString(
                            Convert.FromBase64String(parts[0].Replace('-', '+').Replace('_', '/')));
                        logger.LogInformation("JWT alg: {Header}", headerJson);
                    }
                    catch { /* skip */ }
                }
            }
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("JWT Auth failed: {Message}", context.Exception?.Message);
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("JWT validated for user: {Subject}", context.Principal?.Identity?.Name);
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Vittal API", Version = "v1" });

    // Resolve conflict for nested DTOs with same names (e.g. Request, Response)
    c.CustomSchemaIds(type => type.FullName?.Replace("+", ".") ?? type.Name);

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
if (corsOrigins == null || corsOrigins.Length == 0)
{
    corsOrigins = new[] { "https://localhost:5001", "http://localhost:5000", "http://localhost:5218", "https://localhost:7106", "https://app.vittal.com" };
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", p => p.WithOrigins(corsOrigins).AllowAnyMethod().AllowAnyHeader().AllowCredentials());
});

// SignalR hubs para tiempo real
builder.Services.AddSignalR();

// API Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("global", context =>
    {
        return RateLimitPartition.GetTokenBucketLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 100,
                ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                TokensPerPeriod = 100,
                AutoReplenishment = true
            });
    });
    options.AddPolicy("auth", context =>
    {
        return RateLimitPartition.GetTokenBucketLimiter(
            $"auth_{context.Connection.RemoteIpAddress}",
            _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 5,
                ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                TokensPerPeriod = 5,
                AutoReplenishment = true
            });
    });
});

// Background service para verificación periódica de alertas
builder.Services.AddHostedService<BackgroundAlertCheckerService>();

var app = builder.Build();

// Wire up JWKS key resolution now that DI is available.
// JwksLoaderService populates JwksCacheService asynchronously at startup;
// the resolver reads from it at request time when keys are loaded.
// Also includes HMAC key for short-lived SignalR tokens.
{
    var jwksCache = app.Services.GetRequiredService<JwksCacheService>();
    var signalrTokenService = app.Services.GetRequiredService<SignalrTokenService>();
    var optionsMonitor = app.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
    var jwtParams = optionsMonitor.Get(JwtBearerDefaults.AuthenticationScheme).TokenValidationParameters;

    // Combined key resolver: JWKS (ES256 for Supabase) + HMAC (short-lived SignalR tokens)
    jwtParams.IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
    {
        var keys = jwksCache.Keys.ToList();

        // Agregar clave HMAC para tokens de corta vida de SignalR
        keys.Add(signalrTokenService.GetSigningKey());

        return keys;
    };
}

if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Correlation ID middleware — inyecta X-Correlation-ID en request/response
app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
        ?? Guid.NewGuid().ToString();
    context.Items["CorrelationId"] = correlationId;
    context.Response.Headers.Append("X-Correlation-ID", correlationId);
    await next();
});

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseMiddleware<TenantMiddleware>();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();

// SignalR Hubs
app.MapHub<AlertasHub>("/hubs/alertas");
app.MapHub<LineaTiempoHub>("/hubs/linea-tiempo");
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();

// =============================================================================
// JSON Converters para DateOnly / TimeOnly
// .NET 8 incluye soporte nativo, pero necesitamos aceptar "HH:mm" sin segundos
// que es el formato que envían los controllers MVC.
// =============================================================================

public class DateOnlyJsonConverter : JsonConverter<DateOnly>
{
    private static readonly string[] Formats = { "yyyy-MM-dd", "yyyyMMdd" };

    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value))
            throw new JsonException("DateOnly value cannot be null or empty.");
        return DateOnly.ParseExact(value, Formats, CultureInfo.InvariantCulture);
    }

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    }
}

public class TimeOnlyJsonConverter : JsonConverter<TimeOnly>
{
    // Acepta "HH:mm" (envío desde MVC) y "HH:mm:ss" (formato nativo .NET 8)
    private static readonly string[] Formats = { "HH:mm", "HH:mm:ss", "HH:mm:ss.fff" };

    public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value))
            throw new JsonException("TimeOnly value cannot be null or empty.");
        return TimeOnly.ParseExact(value, Formats, CultureInfo.InvariantCulture);
    }

    public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
    {
        // Escribir sin segundos — todos los consumidores JS lo manejan bien
        writer.WriteStringValue(value.ToString("HH:mm", CultureInfo.InvariantCulture));
    }
}
