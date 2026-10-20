using System.ComponentModel.DataAnnotations;

namespace backend.Models;

public class Categoria
{
    public int ID {get; set;}

    [Required(ErrorMessage = "La categoria debe tener nombre")]
    public String Nombre {get; set;} ="";

}