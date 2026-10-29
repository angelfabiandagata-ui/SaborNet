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

    internal async Task<bool> ActualizarComentario(int id, string textoComentario, int calificacion)
    {
        var comentario = await _db.Comentarios.FindAsync(id);
        if (comentario == null) return false;

        comentario.comentario = textoComentario;
        comentario.calificacion = calificacion;

        _db.Comentarios.Update(comentario);
        await _db.SaveChangesAsync();
        return true;
    }

    internal async Task<bool> EliminarComentario(int id)
    {
        var comentario = await _db.Comentarios.FindAsync(id);
        if (comentario == null) return false;

        _db.Comentarios.Remove(comentario);
        await _db.SaveChangesAsync();
        return true;
    }

    internal async Task<bool> ExisteReceta(int recetaId)
    {
        return await _db.Recetas.AnyAsync(r => r.ID == recetaId);
    }

    internal async Task<(IEnumerable<Comentario> comentarios, int total)> ListarPorReceta(int recetaId, int pagina, int tamano)
    {
        var query = _db.Comentarios
            .Include(c => c.Usuario)
            .Where(c => c.RecetaID == recetaId);

        var total = await query.CountAsync();

        var comentarios = await query
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .ToListAsync();

        return (comentarios, total);
    }

    internal async Task<Comentario?> ObtenerComentario(int id)
    {
        return await _db.Comentarios
            .Include(c => c.Usuario)
            .FirstOrDefaultAsync(c => c.ID == id);
    }

    internal async Task<(double promedio, int total)> ObtenerResumen(int recetaId)
    {
        var query = _db.Comentarios.Where(c => c.RecetaID == recetaId);

        var total = await query.CountAsync();
        if (total == 0) return (0, 0);

        var promedio = await query.AverageAsync(c => c.calificacion);

        return (promedio, total);
    }
}