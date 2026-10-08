namespace backend.Models;

public class Receta
{
    public int Id { get; set; }
    public string Titulo { get; set; }
    public string Descripcion { get; set; }
    public string Instrucciones { get; set; }
    public int TiempoMinutos  {get; set;}
    public string FotoPrincipal { get; set; }
    public int UsuarioId { get; set; }
    int CategoriaId { get; set; }
}