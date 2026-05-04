# Controller — Program.cs Configuration

> **Agente propietario:** @EspecialistaUI
> **Cuándo cargar:** Para configurar el pipeline del API en Program.cs.
> **Prerequisito:** skills/controller/SKILL.md

---

## Program.cs Completo

```csharp
// src/Vittal.API/Program.cs
var builder = WebApplication.CreateBuilder(args);

// ── Servicios ────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger con JWT
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Vittal API", Version = "v1",
        Description = "API REST para el sistema médico Vittal — SaaS multi-tenant"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization", Type = SecuritySchemeType.Http,
        Scheme = "bearer", BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingrese el token JWT de Supabase Auth: Bearer {token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });

    // Comentarios XML en Swagger
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) options.IncludeXmlComments(xmlPath);
});

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var supabaseUrl = builder.Configuration["Supabase:Url"]!;
        options.Authority = $"{supabaseUrl}/auth/v1";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = $"{supabaseUrl}/auth/v1",
            ValidateAudience = true,
            ValidAudience = "authenticated",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Supabase:JwtSecret"]!))
        };
    });

builder.Services.AddAuthorization();

// ── Capas de la aplicación ────────────────────────────────────────────────
builder.Services.AddVittalDAL(builder.Configuration);
builder.Services.AddVittalBLL();

// Filtro de permisos global
builder.Services.AddScoped<PermissionFilter>();
builder.Services.AddControllers(options =>
    options.Filters.AddService<PermissionFilter>());

// CORS
builder.Services.AddCors(options =>
    options.AddPolicy("VittalFrontend", policy =>
        policy.WithOrigins(
                builder.Configuration["App:FrontendUrl"] ?? "https://localhost:7001")
              .AllowAnyMethod().AllowAnyHeader().AllowCredentials()));

// ── Build ────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Pipeline ─────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Vittal API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors("VittalFrontend");
app.UseAuthentication();
app.UseMiddleware<TenantMiddleware>();   // ← DESPUÉS de Authentication
app.UseAuthorization();
app.MapControllers();

app.Run();
```

---

## Checklist de Calidad — Program.cs

### Orden del Pipeline (CRÍTICO)
- [ ] `UseAuthentication()` ANTES de `UseMiddleware<TenantMiddleware>()`
- [ ] `UseMiddleware<TenantMiddleware>()` ANTES de `UseAuthorization()`
- [ ] `UseCors()` antes de autenticación

### Swagger
- [ ] `AddSwaggerGen` con configuración Bearer JWT
- [ ] `AddSecurityDefinition` y `AddSecurityRequirement` configurados
- [ ] Comentarios XML incluidos si existe el archivo
- [ ] Swagger UI habilitado solo en Development

### Auth
- [ ] JWT Authority apunta a `{supabaseUrl}/auth/v1`
- [ ] ValidAudience = "authenticated"
- [ ] IssuerSigningKey desde `Supabase:JwtSecret`

### Servicios
- [ ] `AddVittalDAL` con configuración
- [ ] `AddVittalBLL` registrado
- [ ] `PermissionFilter` registrado como Scoped y como filtro global
- [ ] CORS con `AllowCredentials()` para cookies

---

*skills/controller/program.md — Vittal v1.0.0*
