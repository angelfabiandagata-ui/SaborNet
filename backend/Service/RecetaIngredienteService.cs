using backend.data;
using backend.Models;

namespace SaborNet.Service;

public class RecetaIngredienteService : ServicioBase<RecetaIngrediente>
{
    public RecetaIngredienteService(AppDbContext db)
        : base(db)
    {
    }
}
