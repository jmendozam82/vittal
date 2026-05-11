using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vittal.Entity.Models;

[Table("permisos")]
public class Permiso
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("clinica_id")]
    public Guid ClinicaId { get; set; }

    [Required]
    [Column("perfil_id")]
    public Guid PerfilId { get; set; }

    [Required]
    [Column("modulo_id")]
    public Guid ModuloId { get; set; }

    [Column("puede_leer")]
    public bool PuedeLeer { get; set; } = false;

    [Column("puede_crear")]
    public bool PuedeCrear { get; set; } = false;

    [Column("puede_actualizar")]
    public bool PuedeActualizar { get; set; } = false;

    [Column("fecha_modificacion")]
    public DateTime FechaModificacion { get; set; } = DateTime.UtcNow;

    [Column("modificado_por")]
    public Guid? ModificadoPor { get; set; }
}
