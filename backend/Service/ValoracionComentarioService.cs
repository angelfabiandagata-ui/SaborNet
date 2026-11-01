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
}
