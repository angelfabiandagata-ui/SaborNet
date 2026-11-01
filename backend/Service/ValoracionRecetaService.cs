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
        int recetaId, int usuarioId, int puntaje)
    {
        if (puntaje < 1 || puntaje > 5)
        {
            return false;
        }

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
                Puntaje = puntaje
            });
        }
        else
        {
            voto.Puntaje = puntaje;
        }

        await Db.SaveChangesAsync();
        return true;
    }
}
