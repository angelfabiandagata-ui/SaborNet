
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

internal async Task<List<Receta>> ListarPaginado(
    int? lastId,
    int tamano,
    string? termino = null,
    bool anterior = false)
{
    var consulta = _db.Set<Receta>().AsNoTracking();

    if (!string.IsNullOrWhiteSpace(termino))
    {
        var patron = $"%{termino.Trim()}%";
        consulta = consulta.Where(r =>
            EF.Functions.ILike(r.Titulo, patron) ||
            EF.Functions.ILike(r.Descripcion, patron));
    }

    if (lastId.HasValue)
        consulta = consulta.Where(r => anterior
            ? r.ID > lastId.Value
            : r.ID < lastId.Value);

    return await (anterior
        ? consulta.OrderBy(r => r.ID)
        : consulta.OrderByDescending(r => r.ID))
        .Take(tamano + 1)
        .ToListAsync();
}
internal async Task<List<Receta>> ListarMisRecetasPaginado(
    int usuarioId,
    int? lastId,
    int tamano,
    string? termino = null,
    bool anterior = false)
{
    var consulta = _db.Set<Receta>()
        .AsNoTracking()
        .Where(r => r.UsuarioID == usuarioId);

    if (!string.IsNullOrWhiteSpace(termino))
    {
        var patron = $"%{termino.Trim()}%";

        consulta = consulta.Where(r =>
            EF.Functions.ILike(r.Titulo, patron) ||
            EF.Functions.ILike(r.Descripcion, patron));
    }

    if (lastId.HasValue)
        consulta = consulta.Where(r => anterior
            ? r.ID > lastId.Value
            : r.ID < lastId.Value);

    return await (anterior
        ? consulta.OrderBy(r => r.ID)
        : consulta.OrderByDescending(r => r.ID))
        .Take(tamano + 1)
        .ToListAsync();
}

internal async Task<List<Receta>> ListarPopularesPaginado(
    int? lastId,
    int tamano,
    bool anterior = false)
{
    var valoraciones = _db.Set<ValoracionReceta>();
    var consulta = _db.Recetas.AsNoTracking().Select(r => new
    {
        Receta = r,
        Promedio = valoraciones
            .Where(v => v.RecetaID == r.ID)
            .Average(v => (double?)v.Puntaje) ?? 0,
        Cantidad = valoraciones.Count(v => v.RecetaID == r.ID)
    });

    if (lastId.HasValue)
    {
        var cursor = await consulta
            .Where(r => r.Receta.ID == lastId.Value)
            .Select(r => new
            {
                r.Promedio,
                r.Cantidad,
                r.Receta.FechaDeLaReceta,
                r.Receta.ID
            })
            .FirstOrDefaultAsync();

        if (cursor != null)
        {
            if (anterior)
                consulta = consulta.Where(r =>
                    r.Promedio > cursor.Promedio ||
                    (r.Promedio == cursor.Promedio &&
                     (r.Cantidad > cursor.Cantidad ||
                      (r.Cantidad == cursor.Cantidad &&
                       (r.Receta.FechaDeLaReceta > cursor.FechaDeLaReceta ||
                        (r.Receta.FechaDeLaReceta == cursor.FechaDeLaReceta &&
                         r.Receta.ID > cursor.ID))))));
            else
                consulta = consulta.Where(r =>
                    r.Promedio < cursor.Promedio ||
                    (r.Promedio == cursor.Promedio &&
                     (r.Cantidad < cursor.Cantidad ||
                      (r.Cantidad == cursor.Cantidad &&
                       (r.Receta.FechaDeLaReceta < cursor.FechaDeLaReceta ||
                        (r.Receta.FechaDeLaReceta == cursor.FechaDeLaReceta &&
                         r.Receta.ID < cursor.ID))))));
        }
    }

    var ordenada = anterior
        ? consulta.OrderBy(r => r.Promedio)
            .ThenBy(r => r.Cantidad)
            .ThenBy(r => r.Receta.FechaDeLaReceta)
            .ThenBy(r => r.Receta.ID)
        : consulta.OrderByDescending(r => r.Promedio)
            .ThenByDescending(r => r.Cantidad)
            .ThenByDescending(r => r.Receta.FechaDeLaReceta)
            .ThenByDescending(r => r.Receta.ID);

    return await ordenada
        .Take(tamano + 1)
        .Select(r => r.Receta)
        .ToListAsync();
}

public async Task<List<Receta>> MasRecientes(
    int? lastId, int tamano = 16, bool anterior = false)
{
    var consulta = _db.Recetas.AsNoTracking();

    if (lastId.HasValue)
    {
        var cursor = await _db.Recetas
            .Where(r => r.ID == lastId.Value)
            .Select(r => new { r.FechaDeLaReceta, r.ID })
            .FirstOrDefaultAsync();

        if (cursor != null)
            consulta = consulta.Where(r => anterior
                ? r.FechaDeLaReceta > cursor.FechaDeLaReceta ||
                    (r.FechaDeLaReceta == cursor.FechaDeLaReceta &&
                        r.ID > cursor.ID)
                : r.FechaDeLaReceta < cursor.FechaDeLaReceta ||
                    (r.FechaDeLaReceta == cursor.FechaDeLaReceta &&
                        r.ID < cursor.ID));
    }

    return await (anterior
        ? consulta.OrderBy(r => r.FechaDeLaReceta).ThenBy(r => r.ID)
        : consulta.OrderByDescending(r => r.FechaDeLaReceta)
            .ThenByDescending(r => r.ID))
        .Take(tamano + 1)
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
            .AsNoTracking()
            .Where(i => EF.Functions.ILike(i.nombre, $"%{termino}%"))
            .OrderBy(i => i.nombre)
            .Take(20)
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
