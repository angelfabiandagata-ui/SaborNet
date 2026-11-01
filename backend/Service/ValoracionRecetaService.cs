using backend.data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace SaborNet.Service;

public class ValoracionRecetaService : ServicioBase<ValoracionReceta>
{
    public ValoracionRecetaService(AppDbContext db)
        : base(db)
    {
    }

    public async Task<bool> GuardarValoracion(
        int recetaId, int usuarioId, bool valoracion)
    {
        var receta = await Db.Recetas.FindAsync(recetaId);

        if (receta == null)
        {
            return false;
        }

        var voto = await Db.ValoracionRecetas
            .SingleOrDefaultAsync(v =>
                v.RecetaID == recetaId &&
                v.UsuarioID == usuarioId);

        if (voto == null)
        {
            Db.ValoracionRecetas.Add(new ValoracionReceta
            {
                RecetaID = recetaId,
                UsuarioID = usuarioId,
                Valoracion = valoracion
            });
        }
        else
        {
            voto.Valoracion = valoracion;
        }

        await Db.SaveChangesAsync();
        return true;
    }
}
