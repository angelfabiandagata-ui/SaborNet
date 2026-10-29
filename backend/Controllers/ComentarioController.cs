// COMENTARIOS

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
            calificacion = c.calificacion,
            fecha = c.fechaCreacion,
            usuarioId = c.UsuarioID,
            autor = c.Usuario.Nombre,
            avatar = c.Usuario.AvatarUrl
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

    // GET: /Comentarios/Resumen?recetaId=5
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Resumen(int recetaId)
    {
        if (!await _servicio.ExisteReceta(recetaId))
            return NotFound();

        var (promedio, total) =
            await _servicio.ObtenerResumen(recetaId);

        return Ok(new
        {
            recetaId,
            promedio,
            total
        });
    }

    // POST: /Comentarios/Crear
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(
        [FromBody] ComentarioViewModel modelo)
    {
        int? usuarioId = ObtenerUsuarioId();

        if (usuarioId == null)
            return Challenge();

        if (modelo == null)
            return BadRequest(new { mensaje = "Datos inválidos" });

        modelo.comentario = modelo.comentario?.Trim() ?? "";

        ModelState.Clear();

        if (!TryValidateModel(modelo))
            return BadRequest(ModelState);

        if (!await _servicio.ExisteReceta(modelo.RecetaID))
        {
            return NotFound(new
            {
                mensaje = "La receta no existe"
            });
        }

        var comentario = new Comentario
        {
            comentario = modelo.comentario,
            calificacion = modelo.calificacion,
            fechaCreacion = DateTime.UtcNow,
            UsuarioID = usuarioId.Value,
            RecetaID = modelo.RecetaID
        };

        await _servicio.Alta(comentario);

        await NotificarCambio(
            comentario.RecetaID, "creado", comentario.ID);

        return Ok(new
        {
            mensaje = "Comentario publicado correctamente",
            id = comentario.ID
        });
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
                modelo.comentario,
                modelo.calificacion);

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
