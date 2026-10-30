using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaborNet.Service;

namespace backend.Controllers;

[Authorize(Roles = "Administrador")]
public class CategoriasController : Controller
{
    private readonly CategoriaService _categoriaService;

    public CategoriasController(CategoriaService categoriaService)
    {
        _categoriaService = categoriaService;
    }

    // GET: /Categorias
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var categorias = await _categoriaService.ListarOrdenadas();
        return View(categorias);
    }

    // POST: /Categorias/Crear
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(string nombre)
    {
        nombre = nombre?.Trim() ?? "";

        if (nombre.Length < 3 || nombre.Length > 50)
        {
            TempData["Error"] =
                "El nombre debe tener entre 3 y 50 caracteres.";

            return RedirectToAction(nameof(Index));
        }

        if (await _categoriaService.ExisteNombre(nombre))
        {
            TempData["Error"] =
                "Ya existe una categoría con ese nombre.";

            return RedirectToAction(nameof(Index));
        }

        await _categoriaService.Alta(new Categoria
        {
            Nombre = nombre
        });

        TempData["Mensaje"] = "Categoría creada correctamente.";

        return RedirectToAction(nameof(Index));
    }

    // POST: /Categorias/Editar
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, string nombre)
    {
        nombre = nombre?.Trim() ?? "";

        if (nombre.Length < 3 || nombre.Length > 50)
        {
            TempData["Error"] =
                "El nombre debe tener entre 3 y 50 caracteres.";

            return RedirectToAction(nameof(Index));
        }

        var categoria = await _categoriaService.BuscarUno(id);

        if (categoria == null)
            return NotFound();

        if (await _categoriaService.ExisteNombre(nombre, id))
        {
            TempData["Error"] =
                "Ya existe otra categoría con ese nombre.";

            return RedirectToAction(nameof(Index));
        }

        bool actualizado =
            await _categoriaService.ActualizarNombre(id, nombre);

        if (!actualizado)
            return NotFound();

        TempData["Mensaje"] =
            "Categoría actualizada correctamente.";

        return RedirectToAction(nameof(Index));
    }

    // POST: /Categorias/Eliminar
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        var categoria = await _categoriaService.BuscarUno(id);

        if (categoria == null)
            return NotFound();

        if (await _categoriaService.TieneRecetas(id))
        {
            TempData["Error"] =
                "No se puede eliminar esta categoría porque " +
                "tiene recetas asociadas.";

            return RedirectToAction(nameof(Index));
        }

        bool eliminado = await _categoriaService.Baja(id);

        if (!eliminado)
            return NotFound();

        TempData["Mensaje"] =
            "Categoría eliminada correctamente.";

        return RedirectToAction(nameof(Index));
    }
}