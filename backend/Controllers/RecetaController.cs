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
    private readonly IWebHostEnvironment _entorno;

    public RecetasController(
        RecetaService servicio,
        IWebHostEnvironment entorno)
    {
        _servicio = servicio;
        _entorno = entorno;
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

/*
    // GET: /Recetas/Detalle/5
    [HttpGet]
    public async Task<IActionResult> Detalle(int id)
    {
        var receta = await _servicio.ObtenerDetalle(id);

        if (receta == null)
            return NotFound();

        ViewBag.Ingredientes =
            await _servicio.ObtenerIngredientes(id);

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

    // GET: /Recetas/Crear
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Crear()
    {
        ViewBag.Categorias =
            await _servicio.ObtenerCategorias();

        return View(new RecetaFormularioViewModel());
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

            return View(modelo);
        }

        string? rutaFoto = null;

        try
        {
            rutaFoto = await GuardarFoto(modelo.Foto!);

            var receta = new Receta
            {
                Titulo = modelo.Titulo.Trim(),
                Descripcion = modelo.Descripcion.Trim(),
                Instrucciones = modelo.Instrucciones.Trim(),
                TiempoMinutos = modelo.TiempoMinutos,
                CategoriaID = modelo.CategoriaID,
                UsuarioID = usuarioId.Value,
                FotoPrincipal = rutaFoto
            };

            var ingredientes = modelo.Ingredientes
                .Select(i => (i.IngredienteId, i.Cantidad));

            await _servicio.CrearConIngredientes(
                receta, ingredientes);

            TempData["Mensaje"] =
                "Receta creada correctamente.";

            return RedirectToAction(nameof(Index));
        }
        catch
        {
            if (rutaFoto != null)
                EliminarFoto(rutaFoto);

            throw;
        }
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
            FotoActual = receta.FotoPrincipal,
            Ingredientes = ingredientes.Select(i =>
                new IngredienteRecetaViewModel
                {
                    IngredienteId = i.IngredienteId,
                    Cantidad = i.Cantidad
                }).ToList()
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
            modelo.FotoActual = recetaActual.FotoPrincipal;
            ViewBag.Categorias =
                await _servicio.ObtenerCategorias();

            return View(modelo);
        }

        string? nuevaFoto = null;

        try
        {
            if (modelo.Foto != null)
                nuevaFoto = await GuardarFoto(modelo.Foto);

            var datos = new Receta
            {
                Titulo = modelo.Titulo.Trim(),
                Descripcion = modelo.Descripcion.Trim(),
                Instrucciones = modelo.Instrucciones.Trim(),
                TiempoMinutos = modelo.TiempoMinutos,
                CategoriaID = modelo.CategoriaID,
                FotoPrincipal =
                    nuevaFoto ?? recetaActual.FotoPrincipal
            };

            var ingredientes = modelo.Ingredientes
                .Select(i => (i.IngredienteId, i.Cantidad));

            bool actualizado =
                await _servicio.ActualizarConIngredientes(
                    id, datos, ingredientes);

            if (!actualizado)
                return NotFound();

            if (nuevaFoto != null)
                EliminarFoto(recetaActual.FotoPrincipal);

            TempData["Mensaje"] =
                "Receta actualizada correctamente.";

            return RedirectToAction(
                nameof(Detalle), new { id });
        }
        catch
        {
            if (nuevaFoto != null)
                EliminarFoto(nuevaFoto);

            throw;
        }
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

        EliminarFoto(receta.FotoPrincipal);

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
                i.IngredienteId <= 0 ||
                i.Cantidad <= 0))
        {
            ModelState.AddModelError(
                nameof(modelo.Ingredientes),
                "Los ingredientes y cantidades deben ser válidos.");
        }

        var ids = modelo.Ingredientes
            .Select(i => i.IngredienteId)
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

    private async Task<string> GuardarFoto(IFormFile archivo)
    {
        var carpeta = Path.Combine(
            _entorno.WebRootPath, "imagenes", "recetas");

        Directory.CreateDirectory(carpeta);

        var extension = Path.GetExtension(
            archivo.FileName).ToLowerInvariant();

        // El servidor genera el nombre: no usamos el del cliente.
        var nombre = $"{Guid.NewGuid():N}{extension}";
        var ruta = Path.Combine(carpeta, nombre);

        await using var stream = new FileStream(
            ruta, FileMode.CreateNew);

        await archivo.CopyToAsync(stream);

        return $"/imagenes/recetas/{nombre}";
    }

    private void EliminarFoto(string? rutaRelativa)
    {
        if (string.IsNullOrWhiteSpace(rutaRelativa))
            return;

        const string prefijo = "/imagenes/recetas/";

        if (!rutaRelativa.StartsWith(
                prefijo, StringComparison.Ordinal))
            return;

        var nombre = rutaRelativa[prefijo.Length..];

        if (nombre != Path.GetFileName(nombre))
            return;

        var ruta = Path.Combine(
            _entorno.WebRootPath,
            "imagenes", "recetas", nombre);

        if (System.IO.File.Exists(ruta))
            System.IO.File.Delete(ruta);
    }

    */


}


