using System.ComponentModel.DataAnnotations;

namespace backend.Models;

public class Ingrediente
{
    public int ID{get; set;}

    [Required(ErrorMessage = "El ingrediente debe tener nombre")]
    public String nombre {get; set;} ="";

    [Required(ErrorMessage = "El ingrediente debe tener unidad de medida")]
    public String unidadDeMedida {get; set;} ="";
}