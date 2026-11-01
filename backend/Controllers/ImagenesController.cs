using backend.data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

public class ImagenesController : Controller
{
    private readonly AppDbContext _db;

    public ImagenesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Receta(int id)
    {
        var receta = await _db.Recetas
            .AsNoTracking()
            .Where(r => r.ID == id)
            .Select(r => new { r.FotoDatos, r.FotoTipo })
            .FirstOrDefaultAsync();

        if (receta?.FotoDatos == null || receta.FotoTipo == null)
            return NotFound();

        return File(receta.FotoDatos, receta.FotoTipo);
    }

    [HttpGet]
    public async Task<IActionResult> Ingrediente(int id)
    {
        var ingrediente = await _db.Ingredientes
            .AsNoTracking()
            .Where(i => i.ID == id)
            .Select(i => new { i.ImagenDatos, i.ImagenTipo })
            .FirstOrDefaultAsync();

        if (ingrediente?.ImagenDatos == null || ingrediente.ImagenTipo == null)
            return NotFound();

        return File(ingrediente.ImagenDatos, ingrediente.ImagenTipo);
    }

    [HttpGet]
    public async Task<IActionResult> Avatar(int id)
    {
        var usuario = await _db.Usuarios
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new { u.AvatarDatos, u.AvatarTipo })
            .FirstOrDefaultAsync();

        if (usuario?.AvatarDatos == null || usuario.AvatarTipo == null)
            return NotFound();

        return File(usuario.AvatarDatos, usuario.AvatarTipo);
    }
}
