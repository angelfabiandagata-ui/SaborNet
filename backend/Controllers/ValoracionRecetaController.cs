using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaborNet.Service;

namespace backend.Controllers;

public class ValoracionRecetaController : Controller
{
    private readonly ValoracionRecetaService _servicio;

    public ValoracionRecetaController(ValoracionRecetaService servicio)
    {
        _servicio = servicio;
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Valorar(int recetaId, bool? valoracion)
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

        bool guardado = await _servicio.GuardarValoracion(
            recetaId, usuarioId, valoracion.Value);

        if (!guardado)
        {
            return NotFound();
        }

        return RedirectToAction("Detalle", "Recetas", new { id = recetaId });
    }
}
