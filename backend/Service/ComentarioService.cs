
using Microsoft.EntityFrameworkCore;
using backend.data;
using backend.Models;

namespace SaborNet.Service;

public class ComentarioService : ServicioBase<Comentario>
{
    private readonly AppDbContext _db;

    public ComentarioService(AppDbContext db)
        : base(db)
    {
        _db = db;
    }

    // Editar únicamente el texto del comentario
    internal async Task<bool> ActualizarComentario(
        int id,
        string textoComentario)
    {
        var comentario = await _db.Comentarios.FindAsync(id);

        if (comentario == null)
            return false;

        comentario.comentario = textoComentario;

        await _db.SaveChangesAsync();

        return true;
    }

    // Eliminar comentario
    internal async Task<bool> EliminarComentario(int id)
    {
        var comentario = await _db.Comentarios.FindAsync(id);

        if (comentario == null)
            return false;

        _db.Comentarios.Remove(comentario);

        await _db.SaveChangesAsync();

        return true;
    }

    // Verificar que exista la receta
    internal async Task<bool> ExisteReceta(int recetaId)
    {
        return await _db.Recetas
            .AnyAsync(r => r.ID == recetaId);
    }

    // Listar comentarios con paginación
    internal async Task<
        (IEnumerable<Comentario> comentarios, int total)>
        ListarPorReceta(int recetaId, int pagina, int tamano)
    {
        var query = _db.Comentarios
            .AsNoTracking()
            .Include(c => c.Usuario)
            .Where(c => c.RecetaID == recetaId);

        var total = await query.CountAsync();

        var comentarios = await query
            .OrderByDescending(c => c.fechaCreacion)
            .ThenByDescending(c => c.ID)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .ToListAsync();

        return (comentarios, total);
    }

    // Listar todos los comentarios de una receta
    public Task<List<Comentario>> ListarPorReceta(int recetaId)
    {
        return _db.Comentarios
            .AsNoTracking()
            .Include(c => c.Usuario)
            .Where(c => c.RecetaID == recetaId)
            .OrderByDescending(c => c.fechaCreacion)
            .ToListAsync();
    }

    // Obtener un comentario por su ID
    internal async Task<Comentario?> ObtenerComentario(int id)
    {
        return await _db.Comentarios
            .Include(c => c.Usuario)
            .FirstOrDefaultAsync(c => c.ID == id);
    }
}
