using System.ComponentModel.DataAnnotations;

namespace backend.Models;

public class Ingrediente
{
    public int ID { get; set; }

    [Required(ErrorMessage = "El ingrediente debe tener nombre")]
    public string nombre { get; set; } = "";

    [Required(ErrorMessage = "El ingrediente debe tener unidad de medida")]
    public string unidadDeMedida { get; set; } = "";

    [Required(ErrorMessage = "El ingrediente debe tener una imagen")]
    public string ImagenDeIngrediente { get; set; } = ""; 

    public ICollection<RecetaIngrediente> RecetaIngredientes { get; set; } = new List<RecetaIngrediente>();
}
