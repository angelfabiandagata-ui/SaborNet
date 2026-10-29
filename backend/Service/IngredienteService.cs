using Microsoft.EntityFrameworkCore;
using backend.data;
using backend.Models;

namespace SaborNet.Service;

public class IngredienteService : ServicioBase<Ingrediente>
{
    private readonly AppDbContext _db;

    public IngredienteService(AppDbContext db)
        : base(db)
    {
        _db = db;
    }

    internal async Task<bool> Actualizar(int id, string nombre, string unidadDeMedida)
    {
        var ingrediente = await _db.Ingredientes.FindAsync(id);
        if (ingrediente == null) return false;

        ingrediente.nombre = nombre;
        ingrediente.unidadDeMedida = unidadDeMedida;

        _db.Ingredientes.Update(ingrediente);
        await _db.SaveChangesAsync();
        return true;
    }

    internal async Task<bool> EstaEnUso(int id)
    {
        return await _db.Set<RecetaIngrediente>().AnyAsync(ri => ri.IngredienteID == id);
    }

    internal async Task<(IEnumerable<Ingrediente> items, int total)> ListarPaginado(int pagina, int tamano, string? buscar)
    {
        var query = _db.Ingredientes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            query = query.Where(i => i.nombre.ToLower().Contains(buscar.ToLower()));
        }

        var totalItems = await query.CountAsync();

        var items = await query
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .ToListAsync();

        return (items, totalItems);
    }
}