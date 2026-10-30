using backend.data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace SaborNet.Service;

public class CategoriaService : ServicioBase<Categoria>
{
    public CategoriaService(AppDbContext db) : base(db)
    {
    }

    public async Task<List<Categoria>> ListarOrdenadas()
    {
        return await Db.Categorias
            .AsNoTracking()
            .OrderBy(c => c.Nombre)
            .ToListAsync();
    }

    public async Task<bool> ExisteNombre(
        string nombre, int? excluirId = null)
    {
        string nombreNormalizado = nombre.Trim().ToLower();

        return await Db.Categorias.AnyAsync(c =>
            c.Nombre.ToLower() == nombreNormalizado &&
            (!excluirId.HasValue || c.ID != excluirId.Value));
    }

    public async Task<bool> ActualizarNombre(
        int id, string nombre)
    {
        var categoria = await Db.Categorias.FindAsync(id);

        if (categoria == null)
            return false;

        categoria.Nombre = nombre.Trim();

        await Db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> TieneRecetas(int id)
    {
        return await Db.Recetas
            .AnyAsync(r => r.CategoriaID == id);
    }
}