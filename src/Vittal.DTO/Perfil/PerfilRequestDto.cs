using System.ComponentModel.DataAnnotations;





namespace Vittal.DTO.Perfil;





/// <summary>


/// Request DTO para crear o editar un perfil.


/// No expone campos de auditoría — el servidor los maneja automáticamente.


/// </summary>


public class PerfilRequestDto


{


    [Required(ErrorMessage = "El nombre del perfil es obligatorio")]


    [StringLength(100, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 100 caracteres")]


    public string Nombre { get; set; } = string.Empty;





    [StringLength(500, ErrorMessage = "La descripcion no puede exceder 500 caracteres")]


    public string? Descripcion { get; set; }





    /// <summary>


    /// Si true, el perfil tiene acceso total sin verificar permisos específicos.


    /// </summary>


    public bool EsAdmin { get; set; }


}


