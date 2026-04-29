# Software Vittal — Sistema Médico SaaS

Vittal es una plataforma médica web multi-tenant que centraliza la gestión de citas, expedientes clínicos, diagnósticos, tratamientos, cirugías y toda la información médica de los pacientes. Está diseñada para ser adoptada por múltiples clínicas oftalmológicas como servicio SaaS.

## 🚀 Tecnologías Principales

- **Frontend:** ASP.NET Core MVC (.NET 8), Bootstrap 5.3, Vanilla JS
- **Backend API:** ASP.NET Core Web API (.NET 8), JWT, Swagger
- **Capa de Datos:** PostgreSQL 15 (Supabase), Dapper (ORM)
- **Infraestructura:** Supabase Auth, Supabase Realtime, Supabase Storage

## 🏗 Arquitectura

El sistema sigue una arquitectura estricta de **N-Capas (N-Tier) + MVC**:

- `Vittal.Aplicacion`: Frontend MVC y UI
- `Vittal.API`: Backend Web API REST
- `Vittal.BLL`: Capa de Lógica de Negocio (Services)
- `Vittal.DAL`: Capa de Acceso a Datos (Repositories)
- `Vittal.Entity`: Entidades de Dominio
- `Vittal.DTO`: Data Transfer Objects (Request/Response)
- `Vittal.IOC`: Inversión de Control (Inyección de Dependencias)

## 🤖 Desarrollo Orquestado por Agentes

El repositorio está preparado para el desarrollo mediante **Agentes Orquestados**.
Para entender el flujo de trabajo, los roles del equipo (@PM, @Arquitecto, @IngenieroDatos, @EspecialistaUI) y las convenciones del sistema, lee la documentación principal:

- [CLAUDE.md](./CLAUDE.md) - Archivo Maestro del Proyecto
- [ORCHESTRATOR.md](./ORCHESTRATOR.md) - Guía de roles y orquestación del equipo
- Carpeta `skills/` - Instrucciones detalladas de implementación por capa
