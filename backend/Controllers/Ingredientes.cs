
using Microsoft.AspNetCore.Mvc;
using backend.Models;
using SaborNet.Service;
using System.ComponentModel.DataAnnotations;

namespace backend.Controllers;

public class IngredientesController : Controller
{
    private readonly IngredienteService _servicio;

    public IngredientesController(IngredienteService servicio)
    {
        _servicio = servicio;
    }

    public class IngredienteDto
    {
        [Required(ErrorMessage = "Ingresá un nombre")]
        [StringLength(100)]
        public string nombre { get; set; } = "";

        [Required(ErrorMessage = "Ingresá una unidad")]
        [StringLength(50)]
        public string unidadDeMedida { get; set; } = "";
    }

    // ==========================================
    // LISTADO PRINCIPAL
    // ==========================================

    // GET: /Ingredientes
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    // GET: /Ingredientes/Listar?pagina=1&buscar=harina
    [HttpGet]
    public async Task<IActionResult> Listar(
        int pagina = 1,
        string? buscar = null)
    {
        const int tamano = 10;

        pagina = Math.Max(1, pagina);

        var (items, total) =
            await _servicio.ListarPaginado(
                pagina, tamano, buscar);

        return Ok(new
        {
            items = items.Select(i => new
            {
                id = i.ID,
                nombre = i.nombre,
                unidadDeMedida = i.unidadDeMedida,
                imagenUrl = Url.Action("Imagen", "Ingredientes", new { id = i.ID })
            }),
            pagina,
            total,
            totalPaginas = (int)Math.Ceiling(
                (double)total / tamano)
        });
    }

    // GET: /Ingredientes/Obtener/5
    [HttpGet]
    public async Task<IActionResult> Obtener(int id)
    {
        var ingrediente = await _servicio.BuscarUno(id);

        if (ingrediente == null)
        {
            return NotFound(new
            {
                mensaje = "Ingrediente no encontrado."
            });
        }

        return Ok(new
        {
            id = ingrediente.ID,
            nombre = ingrediente.nombre,
            unidadDeMedida = ingrediente.unidadDeMedida
        });
    }
    // GET: /Ingredientes/Imagen/5
    [HttpGet]
    public async Task<IActionResult> Imagen(int id)
    {
        var ingrediente = await _servicio.BuscarUno(id);

        if (ingrediente == null ||
            ingrediente.ImagenDatos == null ||
            ingrediente.ImagenDatos.Length == 0)
        {
            return NotFound();
        }

        return File(
            ingrediente.ImagenDatos,
            ingrediente.ImagenTipo ?? "application/octet-stream"
        );
    }
    // ==========================================
    // CREAR - FORMULARIO TRADICIONAL MVC
    // ==========================================

    // GET: /Ingredientes/Crear
    [HttpGet]
    public IActionResult Crear()
    {
        return View();
    }

    // POST: /Ingredientes/Crear
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(
        string nombre,
        string unidadDeMedida,
        IFormFile? imagenDeIngrediente)
    {
        nombre = nombre?.Trim() ?? "";
        unidadDeMedida = unidadDeMedida?.Trim() ?? "";

        string? error = ValidarDatos(
            nombre,
            unidadDeMedida,
            imagenDeIngrediente,
            imagenObligatoria: true);

        if (error != null)
        {
            ModelState.AddModelError("", error);

            return View(new Ingrediente
            {
                nombre = nombre,
                unidadDeMedida = unidadDeMedida
            });
        }

        byte[] imagenDatos =
            await LeerImagen(imagenDeIngrediente!);

        var ingrediente = new Ingrediente
        {
            nombre = nombre,
            unidadDeMedida = unidadDeMedida,
            ImagenDeIngrediente = "",
            ImagenDatos = imagenDatos,
            ImagenTipo = ObtenerTipoImagen(
                imagenDeIngrediente!)
        };

        await _servicio.Alta(ingrediente);

        TempData["Mensaje"] =
            "Ingrediente creado correctamente.";

        return RedirectToAction(nameof(Index));
    }

    // ==========================================
    // CREAR - VUE.JS + AJAX
    // ==========================================

    // POST: /Ingredientes/CrearAjax
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearAjax(
        string nombre,
        string unidadDeMedida,
        IFormFile? imagenDeIngrediente)
    {
        nombre = nombre?.Trim() ?? "";
        unidadDeMedida = unidadDeMedida?.Trim() ?? "";

        string? error = ValidarDatos(
            nombre,
            unidadDeMedida,
            imagenDeIngrediente,
            imagenObligatoria: true);

        if (error != null)
        {
            return BadRequest(new
            {
                mensaje = error
            });
        }

        byte[] imagenDatos =
            await LeerImagen(imagenDeIngrediente!);

        var ingrediente = new Ingrediente
        {
            nombre = nombre,
            unidadDeMedida = unidadDeMedida,
            ImagenDeIngrediente = "",
            ImagenDatos = imagenDatos,
            ImagenTipo = ObtenerTipoImagen(
                imagenDeIngrediente!)
        };

        await _servicio.Alta(ingrediente);

        return Ok(new
        {
            mensaje = "Ingrediente creado correctamente.",
            ingrediente = new
            {
                id = ingrediente.ID,
                nombre = ingrediente.nombre,
                unidadDeMedida = ingrediente.unidadDeMedida
            }
        });
    }

    // ==========================================
    // EDITAR - FORMULARIO TRADICIONAL MVC
    // ==========================================

    // GET: /Ingredientes/Editar/5
    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var ingrediente =
            await _servicio.BuscarUno(id);

        if (ingrediente == null)
            return NotFound();

        return View(ingrediente);
    }

    // POST: /Ingredientes/Editar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(
        int id,
        string nombre,
        string unidadDeMedida,
        IFormFile? imagenNueva)
    {
        var ingredienteActual =
            await _servicio.BuscarUno(id);

        if (ingredienteActual == null)
            return NotFound();

        nombre = nombre?.Trim() ?? "";
        unidadDeMedida = unidadDeMedida?.Trim() ?? "";

        string? error = ValidarDatos(
            nombre,
            unidadDeMedida,
            imagenNueva,
            imagenObligatoria: false);

        if (error != null)
        {
            ModelState.AddModelError("", error);

            ingredienteActual.nombre = nombre;
            ingredienteActual.unidadDeMedida =
                unidadDeMedida;

            return View(ingredienteActual);
        }

        byte[]? imagenDatos = null;
        string? tipoImagen = null;

        if (imagenNueva != null &&
            imagenNueva.Length > 0)
        {
            imagenDatos =
                await LeerImagen(imagenNueva);

            tipoImagen =
                ObtenerTipoImagen(imagenNueva);
        }

        bool actualizado =
            await _servicio.Actualizar(
                id,
                nombre,
                unidadDeMedida,
                imagenDatos,
                tipoImagen);

        if (!actualizado)
            return NotFound();

        TempData["Mensaje"] =
            "Ingrediente actualizado correctamente.";

        return RedirectToAction(nameof(Index));
    }

    // ==========================================
    // EDITAR - VUE.JS + AJAX
    // ==========================================

    // POST: /Ingredientes/EditarAjax/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarAjax(
        int id,
        string nombre,
        string unidadDeMedida,
        IFormFile? imagenNueva)
    {
        var ingredienteActual =
            await _servicio.BuscarUno(id);

        if (ingredienteActual == null)
        {
            return NotFound(new
            {
                mensaje = "Ingrediente no encontrado."
            });
        }

        nombre = nombre?.Trim() ?? "";
        unidadDeMedida = unidadDeMedida?.Trim() ?? "";

        string? error = ValidarDatos(
            nombre,
            unidadDeMedida,
            imagenNueva,
            imagenObligatoria: false);

        if (error != null)
        {
            return BadRequest(new
            {
                mensaje = error
            });
        }

        byte[]? imagenDatos = null;
        string? tipoImagen = null;

        if (imagenNueva != null &&
            imagenNueva.Length > 0)
        {
            imagenDatos =
                await LeerImagen(imagenNueva);

            tipoImagen =
                ObtenerTipoImagen(imagenNueva);
        }

        bool actualizado =
            await _servicio.Actualizar(
                id,
                nombre,
                unidadDeMedida,
                imagenDatos,
                tipoImagen);

        if (!actualizado)
        {
            return NotFound(new
            {
                mensaje = "Ingrediente no encontrado."
            });
        }

        return Ok(new
        {
            mensaje = "Ingrediente actualizado correctamente.",
            ingrediente = new
            {
                id,
                nombre,
                unidadDeMedida
            }
        });
    }

    // ==========================================
    // ELIMINAR - AJAX
    // ==========================================

    // POST: /Ingredientes/Eliminar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        var ingrediente =
            await _servicio.BuscarUno(id);

        if (ingrediente == null)
        {
            return NotFound(new
            {
                mensaje = "Ingrediente no encontrado."
            });
        }

        if (await _servicio.EstaEnUso(id))
        {
            return Conflict(new
            {
                mensaje = "No se puede eliminar porque " +
                          "el ingrediente está asociado a recetas."
            });
        }

        bool eliminado =
            await _servicio.Baja(id);

        if (!eliminado)
        {
            return NotFound(new
            {
                mensaje = "Ingrediente no encontrado."
            });
        }

        return Ok(new
        {
            mensaje = "Ingrediente eliminado correctamente."
        });
    }

    // ==========================================
    // VALIDACIONES COMPARTIDAS
    // ==========================================

    private static string? ValidarDatos(
        string nombre,
        string unidadDeMedida,
        IFormFile? imagen,
        bool imagenObligatoria)
    {
        if (string.IsNullOrWhiteSpace(nombre) ||
            string.IsNullOrWhiteSpace(unidadDeMedida))
        {
            return "Completá todos los campos obligatorios.";
        }

        if (nombre.Length > 100)
        {
            return "El nombre no puede superar los 100 caracteres.";
        }

        if (unidadDeMedida.Length > 50)
        {
            return "La unidad de medida no puede superar los 50 caracteres.";
        }

        if (imagenObligatoria &&
            (imagen == null || imagen.Length == 0))
        {
            return "Seleccioná una imagen para el ingrediente.";
        }

        if (imagen != null && imagen.Length > 0)
        {
            if (imagen.Length > 5 * 1024 * 1024)
            {
                return "La imagen no puede superar los 5 MB.";
            }

            if (ObtenerTipoImagen(imagen) == null)
            {
                return "Usá una imagen JPG o PNG.";
            }
        }

        return null;
    }

    // ==========================================
    // MANEJO DE IMÁGENES
    // ==========================================

    private static string? ObtenerTipoImagen(
        IFormFile imagen)
    {
        string extension = Path
            .GetExtension(imagen.FileName)
            .ToLowerInvariant();

        if (extension == ".jpg" ||
            extension == ".jpeg")
        {
            return "image/jpeg";
        }

        if (extension == ".png")
        {
            return "image/png";
        }

        return null;
    }

    private static async Task<byte[]> LeerImagen(
        IFormFile imagen)
    {
        using var memoria = new MemoryStream();

        await imagen.CopyToAsync(memoria);

        return memoria.ToArray();
    }
}
