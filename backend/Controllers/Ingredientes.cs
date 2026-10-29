using Microsoft.AspNetCore.Mvc;
using backend.Models;
using backend.data;

namespace backend.Controllers;

public class IngredientesController : Controller
{
    private readonly AppDbContext _context;

    public IngredientesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var ingredientes = _context.Ingredientes.ToList();
        return View(ingredientes);
    }

    [HttpGet]
    public IActionResult Crear()
    {
        
        return View();

    }

    [HttpPost]
    public IActionResult Crear(Ingrediente ingrediente)
    {
        if (ModelState.IsValid)
        {
            _context.Ingredientes.Add(ingrediente);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
        return View(ingrediente);
    }

    [HttpGet]
    public IActionResult Editar(int id)
    {
        var ingrediente = _context.Ingredientes.Find(id);
        if (ingrediente == null)
        {
            return NotFound();
        }
        return View(ingrediente);
    }
    [HttpPost]
    public IActionResult Editar(Ingrediente ingrediente)
    {
        if (ModelState.IsValid)
        {
            _context.Ingredientes.Update(ingrediente);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
        return View(ingrediente);
    }
    [HttpGet]
    public IActionResult Eliminar(int id)
    {
        var ingrediente = _context.Ingredientes.Find(id);
        if (ingrediente == null)
        {
            return NotFound();
        }
        return View(ingrediente);
    }
    
    [HttpPost, ActionName("Eliminar")]
    public IActionResult EliminarConfirmado(int id)
    {
        var ingrediente = _context.Ingredientes.Find(id);
        if (ingrediente == null)
        {
            return NotFound();
        }
        _context.Ingredientes.Remove(ingrediente);
        _context.SaveChanges();
        return RedirectToAction("Index");
    }
}