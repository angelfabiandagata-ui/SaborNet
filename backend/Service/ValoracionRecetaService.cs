
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

    // Guardar una única valoración por usuario y receta
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

        // Verificar si el usuario ya valoró esta receta
        bool yaVoto = await Db.ValoracionRecetas
            .AnyAsync(v =>
                v.RecetaID == recetaId &&
                v.UsuarioID == usuarioId);

        if (yaVoto)
        {
            // No permitimos modificar una valoración anterior
            return false;
        }

        // Primera valoración del usuario
        Db.ValoracionRecetas.Add(new ValoracionReceta
        {
            RecetaID = recetaId,
            UsuarioID = usuarioId,
            Puntaje = puntaje
        });

        await Db.SaveChangesAsync();

        return true;
    }

    // Consultar la valoración que hizo un usuario
    public async Task<int?> ObtenerVotoUsuario(
        int recetaId, int usuarioId)
    {
        return await Db.ValoracionRecetas
            .AsNoTracking()
            .Where(v =>
                v.RecetaID == recetaId &&
                v.UsuarioID == usuarioId)
            .Select(v => (int?)v.Puntaje)
            .FirstOrDefaultAsync();
    }

    // Obtener promedios de varias recetas
    public async Task<Dictionary<int, (double promedio, int total)>>
        ObtenerPromedios(IEnumerable<int> recetaIds)
    {
        var ids = recetaIds.Distinct().ToList();

        var resultados = await Db.ValoracionRecetas
            .AsNoTracking()
            .Where(v => ids.Contains(v.RecetaID))
            .GroupBy(v => v.RecetaID)
            .Select(g => new
            {
                RecetaID = g.Key,
                Promedio = g.Average(v => v.Puntaje),
                Total = g.Count()
            })
            .ToListAsync();

        return resultados.ToDictionary(
            x => x.RecetaID,
            x => (x.Promedio, x.Total));
    }

    // Obtener promedio de una receta
    public async Task<(double promedio, int total)>
        ObtenerPromedio(int recetaId)
    {
        var valoraciones = Db.ValoracionRecetas
            .Where(v => v.RecetaID == recetaId);

        int total = await valoraciones.CountAsync();

        if (total == 0)
        {
            return (0, 0);
        }

        double promedio = await valoraciones
            .AverageAsync(v => v.Puntaje);

        return (promedio, total);
    }
}
