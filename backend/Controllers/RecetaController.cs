// RECETAS


using System.Security.Claims;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaborNet.Service;

namespace backend.Controllers;

public class RecetasController : Controller
{
    private readonly RecetaService _servicio;
    private readonly IngredienteService _Iservicio;
    private readonly ComentarioService _comentarioServicio;
    private readonly ValoracionRecetaService _valoracionServicio;
    private readonly ValoracionComentarioService _valoracionComentarioServicio;
    public RecetasController(
        RecetaService servicio,
        IngredienteService Iservicio,
        ComentarioService comentarioServicio,
        ValoracionRecetaService valoracionServicio,
        ValoracionComentarioService valoracionComentarioServicio)
    {
        _servicio = servicio;
        _Iservicio = Iservicio;
        _comentarioServicio = comentarioServicio;
        _valoracionServicio = valoracionServicio;
        _valoracionComentarioServicio = valoracionComentarioServicio;
    }


    // GET: /Recetas
    [HttpGet]
    public async Task<IActionResult> Index(int pagina = 1)
    {
        const int tamano = 10;
        pagina = Math.Max(1, pagina);

        var (recetas, total) =
            await _servicio.ListarPaginado(pagina, tamano);

        ViewBag.PaginaActual = pagina;
        ViewBag.TotalPaginas =
            (int)Math.Ceiling((double)total / tamano);
        ViewBag.Promedios = await _valoracionServicio.ObtenerPromedios(
        recetas.Select(r => r.ID));

        return View(recetas);
    }

    // GET: /Recetas/MasPopulares
    [HttpGet]
    public async Task<IActionResult> MasPopulares(int pagina = 1)
    {
        const int tamano = 10;
        pagina = Math.Max(1, pagina);

        var (recetas, total) =
            await _servicio.ListarPaginado(pagina, tamano);

        ViewBag.PaginaActual = pagina;
        ViewBag.TotalPaginas =
            (int)Math.Ceiling((double)total / tamano);

        return View(recetas);
    }

    // GET: /Recetas/MasRecientes?ultimoVisto=2026-01-01T00:00:00
    [HttpGet]
    public async Task<IActionResult> MasRecientes(DateTime? ultimoVisto)
    {
        var recetas = await _servicio.MasRecientes(ultimoVisto);
        return View(recetas);
    }


    // GET: /Recetas/Detalle/5
    [HttpGet]

    public async Task<IActionResult> Detalle(int id)
    {
        // Obtener la receta
        var receta = await _servicio.ObtenerDetalle(id);

        if (receta == null)
            return NotFound();

        // Obtener los ingredientes
        ViewBag.Ingredientes =
            await _servicio.ObtenerIngredientes(id);

        // Obtener los comentarios
        var comentarios =
            await _comentarioServicio.ListarPorReceta(id);

        ViewBag.Comentarios = comentarios;

        // Obtener el promedio de valoraciones de la receta
        var resultado =
            await _valoracionServicio.ObtenerPromedio(id);

        ViewBag.Promedio = resultado.promedio;
        ViewBag.TotalValoraciones = resultado.total;
        ViewBag.MiValoracion = null;

        int? usuarioId = null;

        if (User.Identity?.IsAuthenticated == true)
        {
            string? idUsuario = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (int.TryParse(idUsuario, out int idUsuarioActual))
            {
                usuarioId = idUsuarioActual;

                ViewBag.MiValoracion =
                    await _valoracionServicio.ObtenerVotoUsuario(
                        id, idUsuarioActual);
            }
        }

        // Guardar los votos positivos y negativos de cada comentario
        var votosComentarios =
            new Dictionary<int, (int positivos, int negativos)>();

        // Guardar qué votó el usuario actual en cada comentario
        var misVotosComentarios =
            new Dictionary<int, bool?>();

        foreach (var comentario in comentarios)
        {
            votosComentarios[comentario.ID] =
                await _valoracionComentarioServicio.ObtenerTotales(
                    comentario.ID);

            if (usuarioId.HasValue)
            {
                misVotosComentarios[comentario.ID] =
                    await _valoracionComentarioServicio.ObtenerVotoUsuario(
                        comentario.ID, usuarioId.Value);
            }
        }

        ViewBag.VotosComentarios = votosComentarios;
        ViewBag.MisVotosComentarios = misVotosComentarios;

        return View(receta);
    }


    // GET: /Recetas/BuscarIngredientes?termino=har
    [HttpGet]
    public async Task<IActionResult> BuscarIngredientes(
        string termino)
    {
        if (string.IsNullOrWhiteSpace(termino))
            return Ok(Array.Empty<object>());

        var ingredientes = await _servicio
            .BuscarIngredientes(termino.Trim());

        return Ok(ingredientes.Select(i => new
        {
            id = i.ID,
            text = $"{i.nombre} ({i.unidadDeMedida})"
        }));
    }


[Authorize]
[HttpGet]
public async Task<IActionResult> Crear()
{
    ViewBag.Categorias = await _servicio.ObtenerCategorias();
    
    var vista = new RecetaFormularioViewModel();

    vista.Ingredientes_Basicos = await _Iservicio.RetornarIngredientesAlfabeticamente();

    return View(vista); 
}


    // POST: /Recetas/Crear
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(
        RecetaFormularioViewModel modelo)
    {
        var usuarioId = ObtenerUsuarioId();

        if (usuarioId == null)
            return Challenge();

        await ValidarFormulario(modelo, requiereFoto: true);

        if (!ModelState.IsValid)
        {
            ViewBag.Categorias =
                await _servicio.ObtenerCategorias();

            modelo.Ingredientes_Basicos =
                await _Iservicio.RetornarIngredientesAlfabeticamente();

            return View(modelo);
        }

        var (fotoDatos, fotoTipo) = await LeerImagen(modelo.Foto!);

        var receta = new Receta
        {
            Titulo = modelo.Titulo.Trim(),
            Descripcion = modelo.Descripcion.Trim(),
            Instrucciones = modelo.Instrucciones.Trim(),
            TiempoMinutos = modelo.TiempoMinutos,
            CategoriaID = modelo.CategoriaID,
            UsuarioID = usuarioId.Value,
            FotoPrincipal = "",
            FotoDatos = fotoDatos,
            FotoTipo = fotoTipo
        };

        var ingredientes = modelo.Ingredientes
            .Select(i => (i.IngredienteID, i.Cantidad));

        await _servicio.CrearConIngredientes(
            receta, ingredientes);

        TempData["Mensaje"] = "Receta creada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Recetas/Editar/5
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var receta = await _servicio.ObtenerDetalle(id);

        if (receta == null)
            return NotFound();

        if (!PuedeModificar(receta))
            return Forbid();

        var ingredientes =
            await _servicio.ObtenerIngredientes(id);

        var modelo = new RecetaFormularioViewModel
        {
            ID = receta.ID,
            Titulo = receta.Titulo,
            Descripcion = receta.Descripcion,
            Instrucciones = receta.Instrucciones,
            TiempoMinutos = receta.TiempoMinutos,
            CategoriaID = receta.CategoriaID,
            FotoActual = receta.FotoDatos != null
                ? Url.Action("Receta", "Imagenes", new { id = receta.ID })
                : receta.FotoPrincipal,
            Ingredientes = ingredientes.Select(i =>
                new IngredienteFormularioViewModel
                {
                    IngredienteID = i.IngredienteID,
                    Cantidad = i.Cantidad
                }).ToList(),
            Ingredientes_Basicos = await _Iservicio.RetornarIngredientesAlfabeticamente()
        };

        ViewBag.Categorias =
            await _servicio.ObtenerCategorias();

        return View(modelo);
    }

    // POST: /Recetas/Editar/5
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(
        int id, RecetaFormularioViewModel modelo)
    {
        var recetaActual =
            await _servicio.ObtenerDetalle(id);

        if (recetaActual == null)
            return NotFound();

        if (!PuedeModificar(recetaActual))
            return Forbid();

        await ValidarFormulario(modelo, requiereFoto: false);

        if (!ModelState.IsValid)
        {
            modelo.ID = id;
            modelo.FotoActual = recetaActual.FotoDatos != null
                ? Url.Action("Receta", "Imagenes", new { id })
                : recetaActual.FotoPrincipal;

            ViewBag.Categorias =
                await _servicio.ObtenerCategorias();

            modelo.Ingredientes_Basicos =
                await _Iservicio.RetornarIngredientesAlfabeticamente();

            return View(modelo);
        }

        byte[]? nuevaFotoDatos = null;
        string? nuevoFotoTipo = null;

        if (modelo.Foto != null)
        {
            var foto = await LeerImagen(modelo.Foto);
            nuevaFotoDatos = foto.Datos;
            nuevoFotoTipo = foto.Tipo;
        }

        var datos = new Receta
        {
            Titulo = modelo.Titulo.Trim(),
            Descripcion = modelo.Descripcion.Trim(),
            Instrucciones = modelo.Instrucciones.Trim(),
            TiempoMinutos = modelo.TiempoMinutos,
            CategoriaID = modelo.CategoriaID,
            FotoPrincipal = "",
            FotoDatos = nuevaFotoDatos,
            FotoTipo = nuevoFotoTipo
        };

        var ingredientes = modelo.Ingredientes
            .Select(i => (i.IngredienteID, i.Cantidad));

        bool actualizado =
            await _servicio.ActualizarConIngredientes(
                id, datos, ingredientes);

        if (!actualizado)
            return NotFound();

        TempData["Mensaje"] = "Receta actualizada correctamente.";
        return RedirectToAction(nameof(Detalle), new { id });
    }

    // GET: /Recetas/Eliminar/5
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Eliminar(int id)
    {
        var receta = await _servicio.ObtenerDetalle(id);

        if (receta == null)
            return NotFound();

        if (!PuedeModificar(receta))
            return Forbid();

        return View(receta);
    }

    // POST: /Recetas/Eliminar/5
    [Authorize]
    [HttpPost]
    [ActionName("Eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarConfirmado(int id)
    {
        var receta = await _servicio.ObtenerDetalle(id);

        if (receta == null)
            return NotFound();

        if (!PuedeModificar(receta))
            return Forbid();

        bool eliminado =
            await _servicio.EliminarConDependencias(id);

        if (!eliminado)
            return NotFound();

        TempData["Mensaje"] =
            "Receta eliminada correctamente.";

        return RedirectToAction(nameof(Index));
    }

    private int? ObtenerUsuarioId()
    {
        var valor = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        return int.TryParse(valor, out int id)
            ? id
            : null;
    }

    private bool PuedeModificar(Receta receta)
    {
        var usuarioId = ObtenerUsuarioId();

        return usuarioId != null &&
            (receta.UsuarioID == usuarioId.Value ||
             User.IsInRole("Administrador"));
    }

    private async Task ValidarFormulario(
        RecetaFormularioViewModel modelo,
        bool requiereFoto)
    {
        if (requiereFoto && modelo.Foto == null)
        {
            ModelState.AddModelError(
                nameof(modelo.Foto),
                "Seleccioná una fotografía.");
        }

        if (modelo.Foto != null)
        {
            if (modelo.Foto.Length == 0 ||
                modelo.Foto.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError(
                    nameof(modelo.Foto),
                    "La imagen debe pesar entre 1 byte y 5 MB.");
            }

            var extension = Path.GetExtension(
                modelo.Foto.FileName).ToLowerInvariant();

            if (extension != ".jpg" &&
                extension != ".jpeg" &&
                extension != ".png")
            {
                ModelState.AddModelError(
                    nameof(modelo.Foto),
                    "Solo se permiten JPG y PNG.");
            }
        }

        if (modelo.Ingredientes.Count == 0)
        {
            ModelState.AddModelError(
                nameof(modelo.Ingredientes),
                "Agregá al menos un ingrediente.");
        }

        if (modelo.Ingredientes.Any(i =>
                i.IngredienteID <= 0 ||
                i.Cantidad <= 0))
        {
            ModelState.AddModelError(
                nameof(modelo.Ingredientes),
                "Los ingredientes y cantidades deben ser válidos.");
        }

        var ids = modelo.Ingredientes
            .Select(i => i.IngredienteID)
            .ToList();

        if (ids.Count != ids.Distinct().Count())
        {
            ModelState.AddModelError(
                nameof(modelo.Ingredientes),
                "No repitas ingredientes.");
        }

        if (!await _servicio.ExistenIngredientes(ids))
        {
            ModelState.AddModelError(
                nameof(modelo.Ingredientes),
                "Uno o más ingredientes no existen.");
        }

        var categorias =
            await _servicio.ObtenerCategorias();

        if (!categorias.Any(c => c.ID == modelo.CategoriaID))
        {
            ModelState.AddModelError(
                nameof(modelo.CategoriaID),
                "Seleccioná una categoría válida.");
        }
    }

    private static async Task<(byte[] Datos, string Tipo)> LeerImagen(
        IFormFile archivo)
    {
        using var memoria = new MemoryStream();
        await archivo.CopyToAsync(memoria);

        string extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        string tipo = extension == ".png" ? "image/png" : "image/jpeg";

        return (memoria.ToArray(), tipo);
    }
}