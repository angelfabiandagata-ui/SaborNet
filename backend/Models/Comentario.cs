using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

public class Comentario
{
    public int ID {get; set;}

    [Required(ErrorMessage = "El Comentario no puede estar vacio")]
    public String comentario{get; set;} ="";

    [Range(1, 5, ErrorMessage = "La calificacion debe estar entre 1 y 5")]
    public int califiacion {get; set;}

    [Required]
    public DateTime fechaCreacion {get; set;}


    // claves foraneas
    [ForeignKey(nameof(Usuario))]
    public int UsuarioID{get; set;}

    [ForeignKey(nameof(Receta))]
    public int RecetaID{get; set;}

    public Usuario Usuario { get; set; } = null!;
    public Receta Receta { get; set; } = null!;
}