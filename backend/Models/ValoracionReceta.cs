namespace backend.Models;

public class ValoracionReceta
{
    public int ID { get; set; }
    public int RecetaID { get; set; }
    public int UsuarioID { get; set; }
    public bool Valoracion { get; set; }

    public Receta Receta { get; set; } = null!;
    public Usuario Usuario { get; set; } = null!;
}