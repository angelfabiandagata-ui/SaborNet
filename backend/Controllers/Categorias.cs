using Microsoft.AspNetCore.Mvc;
using backend.Models;
using backend.data;

namespace backend.Controllers;

public class CategoriasController : Controller
{
    private readonly AppDbContext _context;

    public CategoriasController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var categorias = _context.Categorias.ToList();
        return Ok(categorias);
    }

    [HttpGet]
    public IActionResult Crear()
    {
        
        return View();

    }

    [HttpPost]
    public IActionResult Crear(Categoria categoria)
    {
        if (ModelState.IsValid)
        {
            _context.Categorias.Add(categoria);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
        return View(categoria);
    }

    [HttpGet]
    public IActionResult Editar(int id)
    {
        var categoria = _context.Categorias.Find(id);
        if (categoria == null)
        {
            return NotFound();
        }
        return View(categoria);
    }
    [HttpPost]
    public IActionResult Editar(Categoria categoria)
    {
        if (ModelState.IsValid)
        {
            _context.Categorias.Update(categoria);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
        return View(categoria);
    }
    [HttpGet]
    public IActionResult Eliminar(int id)
    {
        var categoria = _context.Categorias.Find(id);
        if (categoria == null)
        {
            return NotFound();
        }
        return View(categoria);
    }
    
    [HttpPost, ActionName("Eliminar")]
    public IActionResult EliminarConfirmado(int id)
    {
        var categoria = _context.Categorias.Find(id);
        if (categoria == null)
        {
            return NotFound();
        }
        _context.Categorias.Remove(categoria);
        _context.SaveChanges();
        return RedirectToAction("Index");
    }
}