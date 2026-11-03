// INGREDIENTES



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

    // GET: /Ingredientes
    [HttpGet]
    public  async Task<IActionResult> Index()
    {
        var (items, total) = await _servicio.ListarPaginado(1, int.MaxValue, null);
        return View(items);
    }

    // GET: /Ingredientes/Listar?pagina=1&buscar=harina
    [HttpGet]
    public async Task<IActionResult> Listar(
        int pagina = 1,
        string? buscar = null)
    {
        const int tamano = 12;

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
                unidadDeMedida = i.unidadDeMedida
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
            return NotFound(new
            {
                mensaje = "Ingrediente no encontrado"
            });

        return Ok(new
        {
            id = ingrediente.ID,
            nombre = ingrediente.nombre,
            unidadDeMedida = ingrediente.unidadDeMedida
        });
    }

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

        if (string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(unidadDeMedida))
        {
            ModelState.AddModelError("", "Completá todos los campos obligatorios.");
            return View(new Ingrediente
            {
                nombre = nombre,
                unidadDeMedida = unidadDeMedida
            });
        }

        if (imagenDeIngrediente == null || imagenDeIngrediente.Length == 0)
        {
            ModelState.AddModelError("imagenDeIngrediente", "Seleccioná una imagen.");
            return View(new Ingrediente
            {
                nombre = nombre,
                unidadDeMedida = unidadDeMedida
            });
        }

        if (imagenDeIngrediente.Length > 5 * 1024 * 1024)
        {
            ModelState.AddModelError("imagenDeIngrediente", "La imagen no puede superar 5 MB.");
            return View(new Ingrediente
            {
                nombre = nombre,
                unidadDeMedida = unidadDeMedida
            });
        }

        var tipoImagen = ObtenerTipoImagen(imagenDeIngrediente);
        if (tipoImagen == null)
        {
            ModelState.AddModelError("imagenDeIngrediente", "Usá una imagen JPG o PNG.");
            return View(new Ingrediente
            {
                nombre = nombre,
                unidadDeMedida = unidadDeMedida
            });
        }

        byte[] imagenDatos = await LeerImagen(imagenDeIngrediente);

        var ingrediente = new Ingrediente
        {
            nombre = nombre,
            unidadDeMedida = unidadDeMedida,
            ImagenDeIngrediente = "",
            ImagenDatos = imagenDatos,
            ImagenTipo = tipoImagen
        };

        await _servicio.Alta(ingrediente);

        TempData["Mensaje"] = "Ingrediente creado correctamente";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
public async Task<IActionResult> Editar(int id)
{
    var ingrediente = await _servicio.BuscarUno(id);

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
    var ingredienteActual = await _servicio.BuscarUno(id);
    if (ingredienteActual == null)
        return NotFound();

    nombre = nombre?.Trim() ?? "";
    unidadDeMedida = unidadDeMedida?.Trim() ?? "";

    if (string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(unidadDeMedida))
    {
        ModelState.AddModelError("", "Completá todos los campos obligatorios.");
        return View(ingredienteActual);
    }

    byte[]? imagenDatos = null;
    string? tipoImagen = null;

    if (imagenNueva != null && imagenNueva.Length > 0)
    {
        if (imagenNueva.Length > 5 * 1024 * 1024)
        {
            ModelState.AddModelError("imagenNueva", "La imagen no puede superar 5 MB.");
            ingredienteActual.nombre = nombre;
            ingredienteActual.unidadDeMedida = unidadDeMedida;
            return View(ingredienteActual);
        }

        tipoImagen = ObtenerTipoImagen(imagenNueva);
        if (tipoImagen == null)
        {
            ModelState.AddModelError("imagenNueva", "Usá una imagen JPG o PNG.");
            ingredienteActual.nombre = nombre;
            ingredienteActual.unidadDeMedida = unidadDeMedida;
            return View(ingredienteActual);
        }

        imagenDatos = await LeerImagen(imagenNueva);
    }

    bool actualizado = await _servicio.Actualizar(
        id, nombre, unidadDeMedida, imagenDatos, tipoImagen);

    if (!actualizado)
        return NotFound();

    TempData["Mensaje"] = "Ingrediente actualizado correctamente";
    return RedirectToAction(nameof(Index));
}

    
    // POST: /Ingredientes/Eliminar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        var ingrediente = await _servicio.BuscarUno(id);

        if (ingrediente == null)
            return NotFound(new
            {
                mensaje = "Ingrediente no encontrado"
            });

        if (await _servicio.EstaEnUso(id))
            return Conflict(new
            {
                mensaje = "No se puede eliminar porque " +
                          "el ingrediente está asociado a recetas"
            });

        bool eliminado = await _servicio.Baja(id);

        if (!eliminado)
            return NotFound();

        return Ok(new
        {
            mensaje = "Ingrediente eliminado correctamente"
        });
    }

    private static string? ObtenerTipoImagen(IFormFile imagen)
    {
        string extension = Path.GetExtension(imagen.FileName).ToLowerInvariant();

        if (extension == ".jpg" || extension == ".jpeg")
            return "image/jpeg";

        if (extension == ".png")
            return "image/png";

        return null;
    }

    private static async Task<byte[]> LeerImagen(IFormFile imagen)
    {
        using var memoria = new MemoryStream();
        await imagen.CopyToAsync(memoria);
        return memoria.ToArray();
    }
}
