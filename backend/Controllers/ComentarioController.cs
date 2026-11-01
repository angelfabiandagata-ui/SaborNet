
using System.Security.Claims;
using backend.Hubs;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SaborNet.Service;

namespace backend.Controllers;

public class ComentariosController : Controller
{
    private readonly ComentarioService _servicio;
    private readonly IHubContext<ComentariosHub> _hub;

    public ComentariosController(
        ComentarioService servicio,
        IHubContext<ComentariosHub> hub)
    {
        _servicio = servicio;
        _hub = hub;
    }

    // GET: /Comentarios/Listar?recetaId=5&pagina=1
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Listar(
        int recetaId, int pagina = 1)
    {
        if (!await _servicio.ExisteReceta(recetaId))
            return NotFound();

        const int tamano = 10;
        pagina = Math.Max(1, pagina);

        var (comentarios, total) =
            await _servicio.ListarPorReceta(
                recetaId, pagina, tamano);

        var resultado = comentarios.Select(c => new
        {
            id = c.ID,
            texto = c.comentario,
            fecha = c.fechaCreacion,
            usuarioId = c.UsuarioID,
            autor = c.Usuario.Nombre,
            avatar = c.Usuario.AvatarDatos != null
                ? $"/Imagenes/Avatar/{c.UsuarioID}"
                : c.Usuario.AvatarUrl
        });

        return Ok(new
        {
            items = resultado,
            pagina,
            total,
            totalPaginas = (int)Math.Ceiling(
                (double)total / tamano)
        });
    }

    // POST: /Comentarios/Crear
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(ComentarioViewModel modelo)
    {
        int? usuarioId = ObtenerUsuarioId();

        if (usuarioId == null)
            return Challenge();

        modelo.comentario = modelo.comentario?.Trim() ?? "";

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (!await _servicio.ExisteReceta(modelo.RecetaID))
            return NotFound();

        var comentario = new Comentario
        {
            comentario = modelo.comentario,
            fechaCreacion = DateTime.UtcNow,
            UsuarioID = usuarioId.Value,
            RecetaID = modelo.RecetaID
        };

        await _servicio.Alta(comentario);

        return RedirectToAction(
            "Detalle", "Recetas", new { id = comentario.RecetaID });
    }

    // POST: /Comentarios/Editar/8
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(
        int id,
        [FromBody] ComentarioViewModel modelo)
    {
        var comentario =
            await _servicio.ObtenerComentario(id);

        if (comentario == null)
            return NotFound();

        if (comentario.UsuarioID != ObtenerUsuarioId())
            return Forbid();

        if (modelo == null)
            return BadRequest(new { mensaje = "Datos inválidos" });

        modelo.comentario = modelo.comentario?.Trim() ?? "";

        // La receta no se puede cambiar al editar.
        modelo.RecetaID = comentario.RecetaID;

        ModelState.Clear();

        if (!TryValidateModel(modelo))
            return BadRequest(ModelState);

        bool actualizado =
            await _servicio.ActualizarComentario(
                id,
                modelo.comentario);

        if (!actualizado)
            return NotFound();

        await NotificarCambio(
            comentario.RecetaID, "editado", id);

        return Ok(new
        {
            mensaje = "Comentario actualizado correctamente"
        });
    }

    // POST: /Comentarios/Eliminar/8
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        var comentario =
            await _servicio.ObtenerComentario(id);

        if (comentario == null)
            return NotFound();

        bool esAutor =
            comentario.UsuarioID == ObtenerUsuarioId();

        bool esAdministrador =
            User.IsInRole("Administrador");

        if (!esAutor && !esAdministrador)
            return Forbid();

        bool eliminado =
            await _servicio.EliminarComentario(id);

        if (!eliminado)
            return NotFound();

        await NotificarCambio(
            comentario.RecetaID, "eliminado", id);

        return Ok(new
        {
            mensaje = "Comentario eliminado correctamente"
        });
    }

    private int? ObtenerUsuarioId()
    {
        string? valor = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        return int.TryParse(valor, out int id)
            ? id
            : null;
    }

    private async Task NotificarCambio(
        int recetaId, string accion, int comentarioId)
    {
        await _hub.Clients
            .Group($"receta-{recetaId}")
            .SendAsync("ComentarioActualizado", new
            {
                recetaId,
                accion,
                comentarioId
            });
    }
}
