
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaborNet.Service;

namespace backend.Controllers;

public class ValoracionComentarioController : Controller
{
    private readonly ValoracionComentarioService _servicio;

    public ValoracionComentarioController(ValoracionComentarioService servicio)
    {
        _servicio = servicio;
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Valorar(int comentarioId, bool? valoracion)
    {
        string? idUsuario = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(idUsuario, out int usuarioId))
        {
            return Challenge();
        }

        if (valoracion == null)
        {
            return BadRequest("Elegí una valoración.");
        }

        int? recetaId = await _servicio.GuardarValoracion(
            comentarioId, usuarioId, valoracion.Value);

        if (recetaId == null)
        {
            return NotFound();
        }

        return RedirectToAction("Detalle", "Recetas", new { id = recetaId });
    }
}
