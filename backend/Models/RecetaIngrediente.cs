using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

public class RecetaIngrediente
{
    public int ID { get; set; }

    [ForeignKey(nameof(Receta))]
    public int RecetaId { get; set; }
    public Receta Receta { get; set; } = null!;

    [ForeignKey(nameof(Ingrediente))]
    public int IngredienteId { get; set; }
    public Ingrediente Ingrediente { get; set; } = null!;

     [Required] 
    public decimal Cantidad { get; set; }
}