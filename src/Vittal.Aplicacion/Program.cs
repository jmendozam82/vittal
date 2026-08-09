using Microsoft.AspNetCore.Authentication.Cookies;
using Vittal.IOC;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Session support (required by _Layout.cshtml for token and clinica_id)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.IsEssential = true;
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login/Auth/Login";
        options.LogoutPath = "/Login/Auth/Logout";
        options.AccessDeniedPath = "/Home/Error";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.None
            : CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(8); // Matches token expiry initially
    });

builder.Services.AddHttpContextAccessor();

// HTTP client para comunicar con Vittal.API
builder.Services.AddHttpClient("VittalApi", (sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    client.BaseAddress = new Uri(config["VittalApi:BaseUrl"] ?? "http://localhost:5089");
    // 180s: permite el cold start de la API en Render free tier
    client.Timeout = TimeSpan.FromSeconds(180);
}).ConfigurePrimaryHttpMessageHandler(() =>
{
    return new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
    };
});

// Register application helpers
builder.Services.AddScoped<Vittal.Aplicacion.Helpers.ApiClientHelper>();

// Register Vittal BLL + DAL services (Repository, Service, FluentValidation)
builder.Services.AddVittalServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();

app.UseSession();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// Home/Dashboard del sistema en ruta explícita. NO usar la raíz "/" porque
// ahí vive la Landing. Los redirects post-login deben apuntar a /home.
app.MapControllerRoute(
    name: "home",
    pattern: "home",
    defaults: new { controller = "Home", action = "Index", area = "" });

// Landing page como página de inicio (marketing). Los usuarios autenticados
// aterrizan en Home/Index vía redirect post-login.
app.MapControllerRoute(
    name: "landing",
    pattern: "",
    defaults: new { controller = "Landing", action = "Index", area = "Landing" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
