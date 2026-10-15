using backend.data;
using backend.Models;

namespace SaborNet.Service;

public class CategoriaService : ServicioBase<Categoria>
{
    public CategoriaService(AppDbContext db)
        : base(db)
    {
    }
}