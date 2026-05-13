using System.ComponentModel.DataAnnotations;





namespace Vittal.DTO.Sala;





/// <summary>


/// Request DTO para crear o editar una sala/área.


/// No expone campos de auditoría — el servidor los maneja automáticamente.


/// </summary>


public class SalaRequestDto


{


    [Required(ErrorMessage = "El nombre de la sala es obligatorio")]


    [StringLength(100, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 100 caracteres")]


    public string Nombre { get; set; } = string.Empty;





    [StringLength(500, ErrorMessage = "La descripcion no puede exceder 500 caracteres")]


    public string? Descripcion { get; set; }


}


