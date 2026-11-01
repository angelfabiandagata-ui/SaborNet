using System.ComponentModel.DataAnnotations;

namespace backend.Models;

public class ValoracionReceta
{
    public int ID { get; set; }
    public int RecetaID { get; set; }
    public int UsuarioID { get; set; }

    [Range(1, 5)]
    public int Puntaje { get; set; }

    public Receta Receta { get; set; } = null!;
    public Usuario Usuario { get; set; } = null!;
}