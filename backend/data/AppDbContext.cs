using Microsoft.EntityFrameworkCore;
namespace backend.data;

using backend.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Receta> Recetas { get; set; }
    public DbSet<Categoria> Categorias { get; set; }
    public DbSet<RecetaIngrediente> RecetaIngredientes { get; set; }

    public DbSet<Ingrediente> Ingredientes { get; set; }

    public DbSet<Comentario> Comentarios { get; set; }

    public DbSet<ValoracionComentario> ValoracionComentarios { get; set; }
    public DbSet<ValoracionReceta> ValoracionRecetas { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Comentario>(entity =>
{
    entity.ToTable("comentarios");

    entity.Property(c => c.calificacion)
          .HasColumnName("califiacion");
});


        modelBuilder.Entity<RecetaIngrediente>()
    .HasKey(ri => ri.ID);

        modelBuilder.Entity<RecetaIngrediente>()
            .Property(ri => ri.ID)
            .ValueGeneratedOnAdd();

        modelBuilder.Entity<RecetaIngrediente>()
        .HasIndex(ri => new { ri.RecetaID, ri.IngredienteID })
        .IsUnique();

        modelBuilder.Entity<RecetaIngrediente>()
            .HasOne(ri => ri.Receta)
            .WithMany(r => r.RecetaIngredientes)
            .HasForeignKey(ri => ri.RecetaID);

        modelBuilder.Entity<RecetaIngrediente>()
            .HasOne(ri => ri.Ingrediente)
            .WithMany(i => i.RecetaIngredientes)
            .HasForeignKey(ri => ri.IngredienteID);

        modelBuilder.Entity<ValoracionComentario>()
            .HasIndex(v => new { v.ComentarioID, v.UsuarioID })
            .IsUnique();

        modelBuilder.Entity<ValoracionReceta>()
            .HasIndex(v => new { v.RecetaID, v.UsuarioID })
            .IsUnique();
    }
}