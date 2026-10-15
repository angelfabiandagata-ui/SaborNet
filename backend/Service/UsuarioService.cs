using backend.data;
using backend.Models;

namespace SaborNet.Service;

public class UsuarioService : ServicioBase<Usuario>
{
    public UsuarioService(AppDbContext db)
        : base(db)
    {
    }
}
