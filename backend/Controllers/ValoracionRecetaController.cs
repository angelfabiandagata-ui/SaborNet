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
    public async Task<IActionResult> Valorar(int recetaId, int? puntaje)
    {
        string? idUsuario = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(idUsuario, out int usuarioId))
        {
            return Challenge();
        }

        if (puntaje == null || puntaje < 1 || puntaje > 5)
        {
            return BadRequest("El puntaje debe ser de 1 a 5.");
        }

        bool guardado = await _servicio.GuardarValoracion(
            recetaId, usuarioId, puntaje.Value);

        if (!guardado)
        {
            return NotFound();
        }

        return RedirectToAction("Detalle", "Recetas", new { id = recetaId });
    }
}
