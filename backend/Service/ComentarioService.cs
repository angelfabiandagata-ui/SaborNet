using backend.data;
using backend.Models;

namespace SaborNet.Service;

public class ComentarioService : ServicioBase<Comentario>
{
    public ComentarioService(AppDbContext db)
        : base(db)
    {
    }
}
