# Guía de Configuración — Vittal

## 1. Obtener las claves de Supabase

1. Ve al **Supabase Dashboard**: https://supabase.com/dashboard/project/vajpdvalavyhwxrusmwx
2. Navega a **Settings → API**
3. Copia los siguientes valores:

| Campo | Dónde encontrarlo |
|---|---|
| `Url` | Project URL (ej: `https://vajpdvalavyhwxrusmwx.supabase.co`) |
| `AnonKey` | anon / public key |
| `ServiceRoleKey` | service_role (secret) — **¡nunca exponer en frontend!** |
| `JwtSecret` | Settings → API → JWT Settings → JWT Secret |
| `ConnectionString` | Settings → Database → Connection string → URI |

---

## 2. Crear el archivo de secrets local

> ⚠️ Este archivo está en `.gitignore` — **NUNCA hacer commit de este archivo**

Crea el archivo `src/Vittal.API/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "Supabase": "Host=db.vajpdvalavyhwxrusmwx.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=TU_DB_PASSWORD;SSL Mode=Require;Trust Server Certificate=true"
  },
  "Supabase": {
    "Url": "https://vajpdvalavyhwxrusmwx.supabase.co",
    "AnonKey": "TU_ANON_KEY",
    "ServiceRoleKey": "TU_SERVICE_ROLE_KEY",
    "JwtSecret": "TU_JWT_SECRET"
  }
}
```

Crea también `src/Vittal.Aplicacion/appsettings.Development.json`:

```json
{
  "Supabase": {
    "Url": "https://vajpdvalavyhwxrusmwx.supabase.co",
    "AnonKey": "TU_ANON_KEY"
  },
  "VittalApi": {
    "BaseUrl": "https://localhost:7001"
  }
}
```

---

## 3. Ejecutar el proyecto localmente

```bash
# Terminal 1 — Iniciar el API REST
dotnet run --project src/Vittal.API

# Terminal 2 — Iniciar el Frontend MVC
dotnet run --project src/Vittal.Aplicacion

# Verificar el build completo
dotnet build Vittal.sln
```

### URLs locales

| Servicio | URL |
|---|---|
| **API REST** | https://localhost:7001 |
| **Swagger UI** | https://localhost:7001/swagger |
| **Frontend MVC** | https://localhost:7002 |
| **Supabase Dashboard** | https://supabase.com/dashboard/project/vajpdvalavyhwxrusmwx |

---

## 4. Verificar la conexión a la base de datos

En Supabase Dashboard, ve a **Table Editor** y verifica que existen las 24 tablas del sistema.

---

## 5. Información del Proyecto Supabase

| Campo | Valor |
|---|---|
| **Project Name** | vittal |
| **Project ID** | `vajpdvalavyhwxrusmwx` |
| **Region** | us-east-1 |
| **Dashboard** | https://supabase.com/dashboard/project/vajpdvalavyhwxrusmwx |
| **Plan** | Free ($0/mes) |
