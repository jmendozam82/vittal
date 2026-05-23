using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Vittal.API.Hubs;
using Vittal.API.Middleware;
using Vittal.IOC;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure HttpClient for Supabase Auth
builder.Services.AddHttpClient("SupabaseAuth");

// Register IOC Dependencies
builder.Services.AddVittalServices(builder.Configuration);

// Configure JWT Authentication
var supabaseUrl = builder.Configuration["Supabase:Url"];
var jwtIssuer = $"{supabaseUrl}/auth/v1";
var jwksUrl = $"{supabaseUrl}/auth/v1/.well-known/jwks.json";

// Fetch JWKS synchronously at startup so keys are available for validation
var jwksKeys = FetchJwksKeys(jwksUrl);

if (jwksKeys.Count == 0)
{
    Console.WriteLine($"WARNING: Could not fetch JWKS from {jwksUrl}");
    Console.WriteLine("JWT validation will fail for ES256 tokens.");
}
else
{
    Console.WriteLine($"JWKS loaded successfully: {jwksKeys.Count} key(s) found");
}

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
        IssuerSigningKeys = jwksKeys,
        TryAllIssuerSigningKeys = true,
        ClockSkew = TimeSpan.FromMinutes(5)
    };

    // Log authentication for debugging
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            var token = context.Token;
            if (string.IsNullOrEmpty(token))
            {
                logger.LogWarning("No Bearer token found in Authorization header");
            }
            else
            {
                // Decode JWT header for debugging
                var parts = token.Split('.');
                if (parts.Length >= 2)
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
    corsOrigins = new[] { "https://localhost:5001", "http://localhost:5000", "https://app.vittal.com" };
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", p => p.WithOrigins(corsOrigins).AllowAnyMethod().AllowAnyHeader().AllowCredentials());
});

// SignalR hubs para tiempo real
builder.Services.AddSignalR();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseMiddleware<TenantMiddleware>();
app.UseAuthorization();
app.MapControllers();

// SignalR Hubs
app.MapHub<AlertasHub>("/hubs/alertas");
app.MapHub<LineaTiempoHub>("/hubs/linea-tiempo");

app.Run();

// =============================================================================
// Helper: Fetch JWKS public keys at startup (synchronous)
// =============================================================================
static List<SecurityKey> FetchJwksKeys(string jwksUrl)
{
    var keys = new List<SecurityKey>();
    try
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        var response = client.GetStringAsync(jwksUrl).Result;
        var doc = JsonDocument.Parse(response);
        var jsonKeys = doc.RootElement.GetProperty("keys");

        foreach (var key in jsonKeys.EnumerateArray())
        {
            var rawText = key.GetRawText();
            var jsonWebKey = new JsonWebKey(rawText);
            keys.Add(jsonWebKey);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error fetching JWKS from {jwksUrl}: {ex.Message}");
    }
    return keys;
}
