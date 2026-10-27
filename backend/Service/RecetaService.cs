
using backend.data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace SaborNet.Service;

public class RecetaService : ServicioBase<Receta>
{
    private readonly AppDbContext _db;

    public RecetaService(AppDbContext db)
        : base(db)
    {
        _db = db;
    }

    internal async Task<(List<Receta> recetas, int total)> ListarPaginado(
        int pagina, int tamano)
    {
        var consulta = _db.Set<Receta>().AsNoTracking();

        int total = await consulta.CountAsync();

        var recetas = await consulta
            .OrderByDescending(r => r.ID)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .ToListAsync();

        return (recetas, total);
    }

    public async Task<List<Receta>> MasRecientes(DateTime ultimoVisto)
    {
        return await _db.Recetas
            .Where(receta => receta.FechaDeLaReceta > ultimoVisto)
            .OrderByDescending(receta => receta.FechaDeLaReceta)
            .Take(16)
            .ToListAsync();
    }
}
