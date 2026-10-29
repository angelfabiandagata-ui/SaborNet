using System.ComponentModel.DataAnnotations;

namespace backend.Models;

public class ComentarioViewModel
{

    public int ID { get; set; }

    [Required(ErrorMessage = "La receta es obligatoria")]
    public int RecetaID { get; set; }

    [Required(ErrorMessage = "El comentario no puede estar vacío")]
    [StringLength(500, ErrorMessage = "El comentario no puede superar los 500 caracteres")]

    public string comentario { get; set; } = "";

    [Required(ErrorMessage = "La calificación es obligatoria")]
    [Range(1, 5, ErrorMessage = "La calificación debe estar entre 1 y 5")]
    public int calificacion { get; set; }
}