using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

public class Receta
{
    public int ID { get; set; }

    [Required]
    public string Titulo { get; set; } ="";

    [Required(ErrorMessage ="La receta DEBE tener descripcion")]
    [MinLength(50, ErrorMessage ="las recetas tienen que tener una descripcion detallada MIN 50 caracteres")]
    public string Descripcion { get; set; }="";

    [Required(ErrorMessage = "La receta debe tener instrucciones")]
    public string Instrucciones { get; set; }="";

    [Range(1, int.MaxValue, ErrorMessage = "El tiempo debe ser mayor a 0")]
    public int TiempoMinutos  {get; set;}

    [DataType(DataType.ImageUrl)]
    public string FotoPrincipal { get; set; }="";

    [DataType(DataType.DateTime)]
    public DateTime FechaDeLaReceta { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(Usuario))]
    public int UsuarioID { get; set; }

    [ForeignKey(nameof(Categoria))]
    public int CategoriaID { get; set; }

    public Usuario Usuario { get; set; } = null!;
    public Categoria Categoria { get; set; } = null!;

    public int MeGustas { get;set;} 


    public ICollection<RecetaIngrediente> RecetaIngredientes { get; set; } = new List<RecetaIngrediente>();
}