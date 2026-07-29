using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vittal.Entity;

namespace Vittal.DAL.Interfaces;

/// <summary>
/// Interface para repositorio de usuarios. Tabla: public.usuarios
/// </summary>
public interface IUsuarioRepository : IPaginatedRepository<Usuario>
{
    /// <summary>Obtiene usuario por su ID de Supabase Auth.</summary>
    Task<Usuario?> GetByAuthUserIdAsync(Guid authUserId);

    /// <summary>Lista todos los usuarios activos de una clínica (con JOIN a perfiles).</summary>
    Task<IEnumerable<Usuario>> GetAllAsync(Guid clinicaId);

    /// <summary>Lista TODOS los usuarios (activos + inactivos) de una clínica. Ordena activos primero.</summary>
    Task<IEnumerable<Usuario>> GetAllIncludingInactiveAsync(Guid clinicaId);

    /// <summary>Obtiene un usuario por ID validando que pertenece a la clínica.</summary>
    Task<Usuario?> GetByIdAsync(Guid id, Guid clinicaId);

    /// <summary>Inserta un nuevo usuario. Retorna el ID autogenerado.</summary>
    Task<Guid> CreateAsync(Usuario usuario);

    /// <summary>Actualiza datos del usuario (sin auth_user_id ni password).</summary>
    Task<bool> UpdateAsync(Usuario usuario);

    /// <summary>Desactiva usuario (activo = false). Nunca DELETE.</summary>
    Task<bool> DeactivateAsync(Guid id, Guid clinicaId);

    /// <summary>Reactiva usuario (activo = true).</summary>
    Task<bool> ReactivateAsync(Guid id, Guid clinicaId);

    /// <summary>Verifica si ya existe un username en la clínica. excludeId para ignorar el mismo usuario en update.</summary>
    Task<bool> ExistsByUsernameAsync(Guid clinicaId, string username, Guid? excludeId = null);

    /// <summary>Verifica si ya existe un email en la clínica.</summary>
    Task<bool> ExistsByEmailAsync(Guid clinicaId, string email, Guid? excludeId = null);

    /// <summary>Cuenta expedientes relacionados al usuario (para validación de desactivación).</summary>
    Task<int> CountExpedientesAsync(Guid usuarioId, Guid clinicaId);

    /// <summary>Cuenta citas futuras relacionadas al usuario (para validación de desactivación).</summary>
    Task<int> CountCitasAsync(Guid usuarioId, Guid clinicaId);

    /// <summary>Lista solo usuarios con es_doctor = true (para dropdowns en otros módulos).</summary>
    Task<IEnumerable<Usuario>> GetDoctoresAsync(Guid clinicaId);

    /// <summary>Verifica si ya existe un número de documento en la clínica. excludeId para ignorar el mismo usuario en update.</summary>
    Task<bool> ExistsByNumeroDocumentoAsync(Guid clinicaId, string numeroDocumento, Guid? excludeId);

    /// <summary>
    /// Busca un usuario por su email (sin filtro de clínica).
    /// Utilizado en el flujo de "Olvidó su contraseña" para identificar al usuario antes de saber su clínica.
    /// </summary>
    Task<Usuario?> GetByEmailGlobalAsync(string email);

    /// <summary>
    /// Obtiene el primer usuario administrador de una clínica específica.
    /// Utilizado para enviar notificaciones de "Olvidó su contraseña" al admin de la clínica.
    /// </summary>
    Task<Usuario?> GetAdminByClinicaAsync(Guid clinicaId);
}
