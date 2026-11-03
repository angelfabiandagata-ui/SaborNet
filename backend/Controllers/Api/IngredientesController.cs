
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using SaborNet.Service;

namespace backend.Controllers.Api;

[ApiController]
[Route("api/ingredientes")]
public class IngredientesController : ControllerBase
{
    private readonly IngredienteService _servicio;

    public IngredientesController(IngredienteService servicio)
    {
        _servicio = servicio;
    }

    // ==========================================
    // DTO PARA CREAR Y EDITAR INGREDIENTES
    // ==========================================

    public class IngredienteDto
    {
        public string Nombre { get; set; } = "";
        public string UnidadDeMedida { get; set; } = "";
    }

    // ==========================================
    // GET: /api/ingredientes
    // LISTADO PÚBLICO CON PAGINACIÓN
    // ==========================================

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Listar(
        int pagina = 1,
        string? buscar = null)
    {
        const int tamano = 10;

        pagina = Math.Max(1, pagina);

        var (ingredientes, total) =
            await _servicio.ListarPaginado(
                pagina, tamano, buscar);

        return Ok(new
        {
            items = ingredientes.Select(i => new
            {
                id = i.ID,
                nombre = i.nombre,
                unidadDeMedida = i.unidadDeMedida,
                imagenUrl = Url.Action(
                    nameof(Imagen),
                    "Ingredientes",
                    new { id = i.ID },
                    Request.Scheme)
            }),

            pagina,
            total,
            totalPaginas =
                (int)Math.Ceiling((double)total / tamano)
        });
    }

    // ==========================================
    // GET: /api/ingredientes/5
    // CONSULTAR UN INGREDIENTE
    // ==========================================

    [AllowAnonymous]
    [HttpGet("{id:int}")]
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
            unidadDeMedida = ingrediente.unidadDeMedida,
            imagenUrl = Url.Action(
                nameof(Imagen),
                "Ingredientes",
                new { id = ingrediente.ID },
                Request.Scheme)
        });
    }

    // ==========================================
    // GET: /api/ingredientes/5/imagen
    // CONSULTAR LA IMAGEN
    // ==========================================

    [AllowAnonymous]
    [HttpGet("{id:int}/imagen")]
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
            ingrediente.ImagenTipo ?? "application/octet-stream");
    }

    // ==========================================
    // POST: /api/ingredientes
    // SOLO ADMINISTRADORES CON JWT
    // ==========================================

    [Authorize(
        AuthenticationSchemes =
            JwtBearerDefaults.AuthenticationScheme,
        Roles = "Administrador")]
    [HttpPost]
    public async Task<IActionResult> Crear(
        [FromForm] string nombre,
        [FromForm] string unidadDeMedida,
        [FromForm] IFormFile? imagen)
    {
        string? error = ValidarDatos(
            nombre, unidadDeMedida);

        if (error != null)
        {
            return BadRequest(new { mensaje = error });
        }

        if (imagen == null || imagen.Length == 0)
        {
            return BadRequest(new
            {
                mensaje = "Seleccioná una imagen."
            });
        }

        var imagenValidada =
            await ProcesarImagen(imagen);

        if (imagenValidada.error != null)
        {
            return BadRequest(new
            {
                mensaje = imagenValidada.error
            });
        }

        var ingrediente = new Ingrediente
        {
            nombre = nombre.Trim(),
            unidadDeMedida = unidadDeMedida.Trim(),
            ImagenDeIngrediente = "",
            ImagenDatos = imagenValidada.datos,
            ImagenTipo = imagenValidada.tipo
        };

        await _servicio.Alta(ingrediente);

        return CreatedAtAction(
            nameof(Obtener),
            new { id = ingrediente.ID },
            new
            {
                mensaje = "Ingrediente creado correctamente.",
                id = ingrediente.ID,
                nombre = ingrediente.nombre,
                unidadDeMedida = ingrediente.unidadDeMedida
            });
    }

    // ==========================================
    // PUT: /api/ingredientes/5
    // SOLO ADMINISTRADORES CON JWT
    // ==========================================

    [Authorize(
        AuthenticationSchemes =
            JwtBearerDefaults.AuthenticationScheme,
        Roles = "Administrador")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Editar(
        int id,
        [FromForm] string nombre,
        [FromForm] string unidadDeMedida,
        [FromForm] IFormFile? imagenNueva)
    {
        string? error = ValidarDatos(
            nombre, unidadDeMedida);

        if (error != null)
        {
            return BadRequest(new { mensaje = error });
        }

        byte[]? imagenDatos = null;
        string? imagenTipo = null;

        if (imagenNueva != null)
        {
            var imagenValidada =
                await ProcesarImagen(imagenNueva);

            if (imagenValidada.error != null)
            {
                return BadRequest(new
                {
                    mensaje = imagenValidada.error
                });
            }

            imagenDatos = imagenValidada.datos;
            imagenTipo = imagenValidada.tipo;
        }

        bool actualizado = await _servicio.Actualizar(
            id,
            nombre.Trim(),
            unidadDeMedida.Trim(),
            imagenDatos,
            imagenTipo);

        if (!actualizado)
        {
            return NotFound(new
            {
                mensaje = "Ingrediente no encontrado."
            });
        }

        return Ok(new
        {
            mensaje = "Ingrediente actualizado correctamente."
        });
    }

    // ==========================================
    // DELETE: /api/ingredientes/5
    // SOLO ADMINISTRADORES CON JWT
    // ==========================================

    [Authorize(
        AuthenticationSchemes =
            JwtBearerDefaults.AuthenticationScheme,
        Roles = "Administrador")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var ingrediente = await _servicio.BuscarUno(id);

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
                mensaje =
                    "No se puede eliminar el ingrediente porque está asociado a una receta."
            });
        }

        await _servicio.Baja(id);

        return Ok(new
        {
            mensaje = "Ingrediente eliminado correctamente."
        });
    }

    // ==========================================
    // VALIDACIONES
    // ==========================================

    private static string? ValidarDatos(
        string? nombre,
        string? unidadDeMedida)
    {
        if (string.IsNullOrWhiteSpace(nombre) ||
            nombre.Trim().Length > 100)
        {
            return "El nombre debe tener entre 1 y 100 caracteres.";
        }

        if (string.IsNullOrWhiteSpace(unidadDeMedida) ||
            unidadDeMedida.Trim().Length > 50)
        {
            return "La unidad de medida debe tener entre 1 y 50 caracteres.";
        }

        return null;
    }

    // ==========================================
    // VALIDAR Y LEER IMÁGENES
    // ==========================================

    private static async Task<(
        byte[]? datos,
        string? tipo,
        string? error)> ProcesarImagen(IFormFile imagen)
    {
        const long maximo = 5 * 1024 * 1024;

        if (imagen.Length == 0 ||
            imagen.Length > maximo)
        {
            return (
                null,
                null,
                "La imagen debe pesar entre 1 byte y 5 MB.");
        }

        string extension = Path.GetExtension(
            imagen.FileName).ToLowerInvariant();

        if (extension != ".jpg" &&
            extension != ".jpeg" &&
            extension != ".png")
        {
            return (
                null,
                null,
                "Solo se permiten imágenes JPG y PNG.");
        }

        using var memoria = new MemoryStream();

        await imagen.CopyToAsync(memoria);

        byte[] datos = memoria.ToArray();

        // Validar la firma real de la imagen.
        bool esJpeg =
            datos.Length >= 3 &&
            datos[0] == 0xFF &&
            datos[1] == 0xD8 &&
            datos[2] == 0xFF;

        byte[] firmaPng =
        {
            0x89, 0x50, 0x4E, 0x47,
            0x0D, 0x0A, 0x1A, 0x0A
        };

        bool esPng =
            datos.Length >= 8 &&
            datos.Take(8).SequenceEqual(firmaPng);

        bool extensionCoincide =
            extension == ".png" ? esPng : esJpeg;

        if (!extensionCoincide)
        {
            return (
                null,
                null,
                "El archivo no es una imagen válida.");
        }

        string tipo = extension == ".png"
            ? "image/png"
            : "image/jpeg";

        return (datos, tipo, null);
    }
}
