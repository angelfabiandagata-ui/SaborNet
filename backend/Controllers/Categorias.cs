// CATEGORIAS


using Microsoft.AspNetCore.Mvc;
using backend.Models;
using SaborNet.Service;

namespace backend.Controllers;

public class CategoriasController : Controller
{
    private readonly CategoriaService _categoriaService;

    public CategoriasController(
        CategoriaService categoriaService)
    {
        _categoriaService = categoriaService;
    }

    // GET: /Categorias
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var categorias =
            await _categoriaService.ListarOrdenadas();

        return View(categorias);
    }

    // GET: /Categorias/Crear
    [HttpGet]
    public IActionResult Crear()
    {
        return View(new Categoria());
    }

    // POST: /Categorias/Crear
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(
        [Bind("Nombre")] Categoria categoria)
    {
        categoria.Nombre = categoria.Nombre.Trim();

        if (string.IsNullOrWhiteSpace(categoria.Nombre))
        {
            ModelState.AddModelError(
                nameof(Categoria.Nombre),
                "El nombre es obligatorio.");
        }

        if (!ModelState.IsValid)
            return View(categoria);

        await _categoriaService.Alta(categoria);

        TempData["Mensaje"] =
            "Categoría creada correctamente.";

        return RedirectToAction(nameof(Index));
    }

    // GET: /Categorias/Editar/5
    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var categoria =
            await _categoriaService.BuscarUno(id);

        if (categoria == null)
            return NotFound();

        return View(categoria);
    }

    // POST: /Categorias/Editar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(
        int id,
        [Bind("Nombre")] Categoria categoria)
    {
        categoria.Nombre = categoria.Nombre.Trim();

        if (string.IsNullOrWhiteSpace(categoria.Nombre))
        {
            ModelState.AddModelError(
                nameof(Categoria.Nombre),
                "El nombre es obligatorio.");
        }

        if (!ModelState.IsValid)
        {
            categoria.ID = id;
            return View(categoria);
        }

        bool actualizado =
            await _categoriaService.ActualizarNombre(
                id, categoria.Nombre);

        if (!actualizado)
            return NotFound();

        TempData["Mensaje"] =
            "Categoría modificada correctamente.";

        return RedirectToAction(nameof(Index));
    }

    // GET: /Categorias/Eliminar/5
    [HttpGet]
    public async Task<IActionResult> Eliminar(int id)
    {
        var categoria =
            await _categoriaService.BuscarUno(id);

        if (categoria == null)
            return NotFound();

        return View(categoria);
    }

    // POST: /Categorias/Eliminar/5
    [HttpPost]
    [ActionName("Eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarConfirmado(int id)
    {
        var categoria =
            await _categoriaService.BuscarUno(id);

        if (categoria == null)
            return NotFound();

        if (await _categoriaService.TieneRecetas(id))
        {
            TempData["Error"] =
                "No se puede eliminar una categoría " +
                "que tiene recetas asociadas.";

            return RedirectToAction(nameof(Index));
        }

        bool eliminado =
            await _categoriaService.Baja(id);

        if (!eliminado)
            return NotFound();

        TempData["Mensaje"] =
            "Categoría eliminada correctamente.";

        return RedirectToAction(nameof(Index));
    }
}


