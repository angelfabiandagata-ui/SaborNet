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

    // POST: /Ingredientes/Crear
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(
        [FromBody] IngredienteDto datos)
    {
        if (datos == null)
            return BadRequest(new { mensaje = "Datos inválidos" });

        datos.nombre = datos.nombre?.Trim() ?? "";
        datos.unidadDeMedida =
            datos.unidadDeMedida?.Trim() ?? "";

        ModelState.Clear();

        if (!TryValidateModel(datos))
            return BadRequest(ModelState);

        var ingrediente = new Ingrediente
        {
            nombre = datos.nombre,
            unidadDeMedida = datos.unidadDeMedida
        };

        await _servicio.Alta(ingrediente);

        return Ok(new
        {
            mensaje = "Ingrediente creado correctamente",
            id = ingrediente.ID
        });
    }

    // POST: /Ingredientes/Editar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(
        int id,
        [FromBody] IngredienteDto datos)
    {
        if (datos == null)
            return BadRequest(new { mensaje = "Datos inválidos" });

        datos.nombre = datos.nombre?.Trim() ?? "";
        datos.unidadDeMedida =
            datos.unidadDeMedida?.Trim() ?? "";

        ModelState.Clear();

        if (!TryValidateModel(datos))
            return BadRequest(ModelState);

        bool actualizado = await _servicio.Actualizar(
            id, datos.nombre, datos.unidadDeMedida);

        if (!actualizado)
            return NotFound(new
            {
                mensaje = "Ingrediente no encontrado"
            });

        return Ok(new
        {
            mensaje = "Ingrediente actualizado correctamente"
        });
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
}


