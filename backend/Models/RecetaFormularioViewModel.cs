using System.ComponentModel.DataAnnotations;

namespace backend.Models;

public class RecetaFormularioViewModel
{
    public int ID { get; set; }

    [Required(ErrorMessage = "El título es obligatorio.")]
    public string Titulo { get; set; } = "";
    [Required(ErrorMessage = "La descripción es obligatoria.")]
    public string Descripcion { get; set; } = "";
    [Required(ErrorMessage = "Las instrucciones son obligatorias.")]
    public string Instrucciones { get; set; } = "";
    [Required(ErrorMessage = "El tiempo es obligatorio.")]
    public int TiempoMinutos { get; set; }

    [Required(ErrorMessage = "La categoría es obligatoria.")]
    public int CategoriaID { get; set; }
    public IFormFile? Foto { get; set; }
    public string? FotoActual { get; set; }
    public List<IngredienteFormularioViewModel> Ingredientes { get; set; } = new();

}

public class IngredienteFormularioViewModel
{
    public int ID { get; set; }
    public int IngredienteID { get; set; }
    [Required(ErrorMessage = "El nombre del ingrediente es obligatorio.")]
    public string Nombre { get; set; } = "";
    [Required(ErrorMessage = "La cantidad del ingrediente es obligatoria.")]
    public decimal Cantidad { get; set; } = 0;
}