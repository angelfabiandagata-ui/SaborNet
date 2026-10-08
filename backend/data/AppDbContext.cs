using Microsoft.EntityFrameworkCore;
namespace backend.data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Receta> Recetas { get; set; }
}