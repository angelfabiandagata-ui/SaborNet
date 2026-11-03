
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
    int pagina,
    int tamano,
    string? termino = null)
{
    var consulta = _db.Set<Receta>().AsNoTracking();

    if (!string.IsNullOrWhiteSpace(termino))
    {
        var patron = $"%{termino.Trim()}%";
        consulta = consulta.Where(r =>
            EF.Functions.ILike(r.Titulo, patron) ||
            EF.Functions.ILike(r.Descripcion, patron));
    }

    int total = await consulta.CountAsync();

    var recetas = await consulta
        .OrderByDescending(r => r.ID)
        .Skip((pagina - 1) * tamano)
        .Take(tamano)
        .ToListAsync();

    return (recetas, total);
}

internal async Task<(List<Receta> recetas, int total)> ListarPopularesPaginado(
    int pagina,
    int tamano)
{
    var consulta = _db.Set<Receta>().AsNoTracking();
    var valoraciones = _db.Set<ValoracionReceta>();

    int total = await consulta.CountAsync();

    var recetas = await consulta
        .OrderByDescending(r => valoraciones
            .Where(v => v.RecetaID == r.ID)
            .Average(v => (double?)v.Puntaje) ?? 0)
        .ThenByDescending(r => valoraciones.Count(v => v.RecetaID == r.ID))
        .ThenByDescending(r => r.FechaDeLaReceta)
        .Skip((pagina - 1) * tamano)
        .Take(tamano)
        .ToListAsync();

    return (recetas, total);
}

    public async Task<List<Receta>> MasRecientes(DateTime? ultimoVisto)
    {
        if (ultimoVisto.HasValue)
        {
            ultimoVisto = ultimoVisto.Value.Kind switch
            {
                DateTimeKind.Local =>
                    ultimoVisto.Value.ToUniversalTime(),

                DateTimeKind.Unspecified =>
                    DateTime.SpecifyKind(
                        ultimoVisto.Value,
                        DateTimeKind.Utc),

                _ => ultimoVisto.Value
            };
        }

        var consulta = _db.Recetas.AsNoTracking();

        if (ultimoVisto.HasValue)
        {
            consulta = consulta.Where(receta =>
                receta.FechaDeLaReceta > ultimoVisto.Value);
        }

        return await consulta
            .OrderByDescending(receta => receta.FechaDeLaReceta)
            .Take(16)
            .ToListAsync();
    }

    internal async Task<Receta?> ObtenerDetalle(int id)
    {
        return await _db.Recetas
            .Include(r => r.Categoria)
            .Include(r => r.Usuario)
            .FirstOrDefaultAsync(r => r.ID == id);
    }

    internal async Task<List<RecetaIngrediente>> ObtenerIngredientes(int id)
    {
        return await _db.Set<RecetaIngrediente>()
            .Include(ri => ri.Ingrediente)
            .Where(ri => ri.RecetaID == id)
            .ToListAsync();
    }

    internal async Task<IEnumerable<Ingrediente>> BuscarIngredientes(
        string termino)
    {
        return await _db.Set<Ingrediente>()
            .Where(i => i.nombre.Contains(termino))
            .ToListAsync();
    }

    internal async Task<List<Categoria>> ObtenerCategorias()
    {
        return await _db.Set<Categoria>().ToListAsync();
    }

    internal async Task CrearConIngredientes(
        Receta receta,
        IEnumerable<(int IngredienteId, decimal Cantidad)> ingredientes)
    {
        using var transaction =
            await _db.Database.BeginTransactionAsync();

        try
        {
            _db.Recetas.Add(receta);

            await _db.SaveChangesAsync();

            foreach (var item in ingredientes)
            {
                var recetaIngrediente = new RecetaIngrediente
                {
                    RecetaID = receta.ID,
                    IngredienteID = item.IngredienteId,
                    Cantidad = item.Cantidad
                };

                _db.Set<RecetaIngrediente>().Add(recetaIngrediente);
            }

            await _db.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    internal async Task<bool> ActualizarConIngredientes(
        int id,
        Receta datos,
        IEnumerable<(int IngredienteId, decimal Cantidad)> ingredientes)
    {
        var receta = await _db.Recetas
            .Include(r => r.RecetaIngredientes)
            .FirstOrDefaultAsync(r => r.ID == id);

        if (receta == null)
            return false;

        var ingredientesNuevos = ingredientes.ToList();

        // Actualizar los datos generales.
        receta.Titulo = datos.Titulo;
        receta.Descripcion = datos.Descripcion;
        receta.Instrucciones = datos.Instrucciones;
        receta.TiempoMinutos = datos.TiempoMinutos;
        receta.CategoriaID = datos.CategoriaID;
        if (datos.FotoDatos != null)
        {
            receta.FotoPrincipal = "";
            receta.FotoDatos = datos.FotoDatos;
            receta.FotoTipo = datos.FotoTipo;
        }

        // Obtener los ingredientes actuales.
        var existentes = receta.RecetaIngredientes.ToList();

        var idsNuevos = ingredientesNuevos
            .Select(i => i.IngredienteId)
            .ToHashSet();

        // Quitar ingredientes que ya no están.
        foreach (var existente in existentes)
        {
            if (!idsNuevos.Contains(existente.IngredienteID))
            {
                _db.Set<RecetaIngrediente>().Remove(existente);
            }
        }

        // Agregar ingredientes nuevos o actualizar cantidades.
        foreach (var item in ingredientesNuevos)
        {
            var existente = existentes.FirstOrDefault(i =>
                i.IngredienteID == item.IngredienteId);

            if (existente != null)
            {
                existente.Cantidad = item.Cantidad;
            }
            else
            {
                _db.Set<RecetaIngrediente>().Add(
                    new RecetaIngrediente
                    {
                        RecetaID = id,
                        IngredienteID = item.IngredienteId,
                        Cantidad = item.Cantidad
                    });
            }
        }

        await _db.SaveChangesAsync();

        return true;
    }

    internal async Task<bool> EliminarConDependencias(int id)
    {
        var receta = await _db.Recetas.FindAsync(id);

        if (receta == null)
            return false;

        // 1. Eliminar los comentarios asociados a la receta.
        var comentarios = await _db.Set<Comentario>()
            .Where(c => c.RecetaID == id)
            .ToListAsync();

        _db.Set<Comentario>().RemoveRange(comentarios);

        // 2. Eliminar las relaciones con los ingredientes.
        // Los ingredientes del catálogo NO se eliminan.
        var ingredientesAsociados = await _db.Set<RecetaIngrediente>()
            .Where(ri => ri.RecetaID == id)
            .ToListAsync();

        _db.Set<RecetaIngrediente>().RemoveRange(ingredientesAsociados);

        // 3. Eliminar la receta.
        _db.Recetas.Remove(receta);

        // 4. Guardar los cambios en PostgreSQL.
        await _db.SaveChangesAsync();

        return true;
    }

    internal async Task<bool> ExistenIngredientes(List<int> ids)
    {
        var count = await _db.Set<Ingrediente>()
            .CountAsync(i => ids.Contains(i.ID));

        return count == ids.Count;
    }
}
