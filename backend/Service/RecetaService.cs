using backend.data;
using backend.Models;

namespace SaborNet.Service;

public class RecetaService : ServicioBase<Receta>
{
    public RecetaService(AppDbContext db)
        : base(db)
    {
    }
}
