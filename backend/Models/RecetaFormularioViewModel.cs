using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http; // Asegura el soporte de IFormFile

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

    [Range(1, int.MaxValue, ErrorMessage = "El tiempo debe ser mayor a 0 minutos.")]
    public int TiempoMinutos { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "La categoría es obligatoria.")]
    public int CategoriaID { get; set; }

    public IFormFile? Foto { get; set; }
    public string? FotoActual { get; set; }

    public List<IngredienteFormularioViewModel> Ingredientes { get; set; } = new();
    public List<Ingrediente> Ingredientes_Basicos { get; set; } = new();
}

public class IngredienteFormularioViewModel
{
    public int ID { get; set; } 

    [Range(1, int.MaxValue, ErrorMessage = "Debes seleccionar un ingrediente válido.")]
    public int IngredienteID { get; set; }

    public string? Nombre { get; set; } = "";

    [Range(0.01, double.MaxValue, ErrorMessage = "La cantidad debe ser mayor a cero.")]
    public decimal Cantidad { get; set; } = 0;
}
