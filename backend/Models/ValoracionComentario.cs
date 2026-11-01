namespace backend.Models;

public class ValoracionComentario
{
    public int ID { get; set; }
    public int ComentarioID { get; set; }
    public int UsuarioID { get; set; }
    public bool Valoracion { get; set; }

    public Comentario Comentario { get; set; } = null!;
    public Usuario Usuario { get; set; } = null!;
}