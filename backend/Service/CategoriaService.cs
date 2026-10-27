using backend.data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace SaborNet.Service;

public class CategoriaService : ServicioBase<Categoria>
{
    public CategoriaService(AppDbContext db)
        : base(db)
    {
    }

    internal async Task<bool> ActualizarNombre(int id, string nombre)
    {
        var categoria = await Db.Set<Categoria>().FindAsync(id);

        if (categoria == null)
            return false;

        categoria.Nombre = nombre;

        await Db.SaveChangesAsync();

        return true;
    }

    internal async Task<List<Categoria>> ListarOrdenadas()
    {
        return await Db.Set<Categoria>()
            .OrderBy(c => c.Nombre)
            .ToListAsync();
    }

    internal async Task<bool> TieneRecetas(int id)
    {
        throw new NotImplementedException();
    }
}