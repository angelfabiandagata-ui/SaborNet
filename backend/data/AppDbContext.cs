using Microsoft.EntityFrameworkCore;
namespace backend.data;
using backend.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Receta> Recetas { get; set; }
    public DbSet<Categoria> Categorias{get; set;}
    public DbSet<RecetaIngrediente> RecetaIngredientes {get; set;}

    public DbSet<Ingrediente> Ingredientes {get; set;}

    public DbSet<Comentario> comentarios {get; set;}
}