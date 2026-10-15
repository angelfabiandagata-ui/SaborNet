using backend.data;
using Microsoft.EntityFrameworkCore;

namespace SaborNet.Service;

public abstract class ServicioBase<T> where T : class
{
    protected readonly AppDbContext Db;

    protected ServicioBase(AppDbContext db)
    {
        Db = db;
    }

    public async Task<T> Alta(T entidad)
    {
        await Db.Set<T>().AddAsync(entidad);
        await Db.SaveChangesAsync();
        return entidad;
    }

    public async Task<bool> Baja(int id)
    {
        var entidad = await Db.Set<T>().FindAsync(id);
        if (entidad is null)
        {
            return false;
        }

        Db.Set<T>().Remove(entidad);
        await Db.SaveChangesAsync();
        return true;
    }

    public async Task<T> Modificacion(int id, T entidad)
    {
        var existente = await Db.Set<T>().FindAsync(id);
        if (existente is null)
        {
            throw new KeyNotFoundException($"No se encontró un registro de tipo {typeof(T).Name} con id {id}.");
        }

        Db.Entry(existente).CurrentValues.SetValues(entidad);
        await Db.SaveChangesAsync();
        return existente;
    }

    public Task<List<T>> DevolverTodos()
    {
        return Db.Set<T>().ToListAsync();
    }

    public Task<T?> BuscarUno(int id)
    {
        return Db.Set<T>().FindAsync(id).AsTask();
    }
}
