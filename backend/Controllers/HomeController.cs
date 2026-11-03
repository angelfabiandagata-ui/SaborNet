using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using backend.Models;
using SaborNet.Service;



namespace backend.Controllers;

public class HomeController : Controller
{
    private readonly RecetaService _recetaServicio;
    private readonly ValoracionRecetaService _valoracionServicio;

    public HomeController(
        RecetaService recetaServicio,
        ValoracionRecetaService valoracionServicio)
    {
        _recetaServicio = recetaServicio;
        _valoracionServicio = valoracionServicio;
    }
    public async Task<IActionResult> Index()
    {
        var (populares, _) = await _recetaServicio.ListarPopularesPaginado(1, 4);
        var nuevas = (await _recetaServicio.MasRecientes(null)).Take(4).ToList();

        var ids = populares.Select(r => r.ID)
            .Concat(nuevas.Select(r => r.ID))
            .Distinct();

        ViewBag.Populares = populares;
        ViewBag.Nuevas = nuevas;
        ViewBag.Promedios = await _valoracionServicio.ObtenerPromedios(ids);

        return View();
    }
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
