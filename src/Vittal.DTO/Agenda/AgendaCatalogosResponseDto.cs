using System;
using System.Collections.Generic;
using Vittal.DTO.Paciente;
using Vittal.DTO.Sala;
using Vittal.DTO.Usuario;

namespace Vittal.DTO.Agenda;

/// <summary>
/// Response DTO con los catálogos necesarios para la pantalla de agenda.
/// Agrupa los datos maestros (pacientes, doctores y salas) que se cargan
/// junto con las citas para alimentar los combos y filtros de la agenda.
/// Historia de Usuario: Agenda de citas
/// </summary>
public class AgendaCatalogosResponseDto
{
    /// <summary>Pacientes activos de la clínica para asignar citas.</summary>
    public List<PacienteResponseDto> Pacientes { get; set; } = new();

    /// <summary>Usuarios con perfil de doctor para asignar citas.</summary>
    public List<UsuarioResponseDto> Doctores { get; set; } = new();

    /// <summary>Salas activas de la clínica para asignar citas.</summary>
    public List<SalaResponseDto> Salas { get; set; } = new();
}
