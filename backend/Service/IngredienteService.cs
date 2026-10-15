using backend.data;
using backend.Models;

namespace SaborNet.Service;

public class IngredienteService : ServicioBase<Ingrediente>
{
    public IngredienteService(AppDbContext db)
        : base(db)
    {
    }
}
