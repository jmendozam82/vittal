using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vittal.Entity.Models;

namespace Vittal.DAL.Interfaces;

/// <summary>
/// Repositorio para la tabla public.modulos_sistema (catálogo global del sistema).
/// NO tiene clinicaId — es un catálogo de solo lectura compartido por todos los tenants.
/// Historia de Usuario: HU-SA01 — Super Admin Global
/// </summary>
public interface IModuloSistemaRepository
{
    /// <summary>Obtiene todos los módulos activos del sistema.</summary>
    Task<IEnumerable<ModuloSistema>> GetAllActiveAsync();
}
