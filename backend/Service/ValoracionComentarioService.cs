
using backend.data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace SaborNet.Service;

public class ValoracionComentarioService : ServicioBase<ValoracionComentario>
{
    public ValoracionComentarioService(AppDbContext db)
        : base(db)
    {
    }

    // Guardar o actualizar voto positivo/negativo
    public async Task<int?> GuardarValoracion(
        int comentarioId, int usuarioId, bool valoracion)
    {
        var comentario = await Db.Comentarios.FindAsync(comentarioId);

        if (comentario == null)
        {
            return null;
        }

        var voto = await Db.ValoracionComentarios
            .SingleOrDefaultAsync(v =>
                v.ComentarioID == comentarioId &&
                v.UsuarioID == usuarioId);

        if (voto == null)
        {
            Db.ValoracionComentarios.Add(new ValoracionComentario
            {
                ComentarioID = comentarioId,
                UsuarioID = usuarioId,
                Valoracion = valoracion
            });
        }
        else
        {
            voto.Valoracion = valoracion;
        }

        await Db.SaveChangesAsync();

        return comentario.RecetaID;
    }

    // Consultar qué votó un usuario en un comentario
    public async Task<bool?> ObtenerVotoUsuario(
        int comentarioId, int usuarioId)
    {
        return await Db.ValoracionComentarios
            .AsNoTracking()
            .Where(v =>
                v.ComentarioID == comentarioId &&
                v.UsuarioID == usuarioId)
            .Select(v => (bool?)v.Valoracion)
            .FirstOrDefaultAsync();
    }

    // Obtener cantidad de votos positivos y negativos
    public async Task<(int positivos, int negativos)>
        ObtenerTotales(int comentarioId)
    {
        var votos = Db.ValoracionComentarios
            .AsNoTracking()
            .Where(v => v.ComentarioID == comentarioId);

        int positivos = await votos.CountAsync(v => v.Valoracion);
        int negativos = await votos.CountAsync(v => !v.Valoracion);

        return (positivos, negativos);
    }
}
