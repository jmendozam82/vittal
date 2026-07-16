using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vittal.Entity;

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

    [Column("activo")]
    public bool Activo { get; set; } = true;

    [Column("fecha_creacion")]
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    [Column("fecha_modificacion")]
    public DateTime? FechaModificacion { get; set; }

    [Column("modificado_por")]
    public Guid? ModificadoPor { get; set; }
}
