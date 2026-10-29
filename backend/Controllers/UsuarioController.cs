
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaborNet.Service;

namespace backend.Controllers;

public class UsuariosController : Controller
{
    private readonly UsuarioService _servicio;
    private readonly IWebHostEnvironment _entorno;

    public UsuariosController(
        UsuarioService servicio,
        IWebHostEnvironment entorno)
    {
        _servicio = servicio;
        _entorno = entorno;
    }

    // GET: /Usuarios/MiPerfil
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> MiPerfil()
    {
        int? id = ObtenerUsuarioId();

        if (id == null)
            return Challenge();

        var usuario = await _servicio.BuscarUno(id.Value);

        if (usuario == null)
            return NotFound();

        return View(usuario);
    }

    // GET: /Usuarios/Perfil/5
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Perfil(int id)
    {
        var usuario = await _servicio.BuscarUno(id);

        if (usuario == null)
            return NotFound();

        return View(usuario);
    }

    // GET: /Usuarios/EditarPerfil
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> EditarPerfil()
    {
        int? id = ObtenerUsuarioId();

        if (id == null)
            return Challenge();

        var usuario = await _servicio.BuscarUno(id.Value);

        if (usuario == null)
            return NotFound();

        return View(usuario);
    }

    // POST: /Usuarios/EditarPerfil
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarPerfil(
        [Bind("Nombre,Email")] Usuario datos,
        IFormFile? avatar)
    {
        int? id = ObtenerUsuarioId();

        if (id == null)
            return Challenge();

        var usuario = await _servicio.BuscarUno(id.Value);

        if (usuario == null)
            return NotFound();

        string nombre = datos.Nombre?.Trim() ?? "";

        string email = datos.Email?.Trim()
            .ToLowerInvariant() ?? "";

        // VALIDAR NOMBRE
        if (nombre.Length < 3 || nombre.Length > 20)
        {
            ModelState.AddModelError(
                nameof(Usuario.Nombre),
                "El nombre debe tener entre 3 y 20 caracteres.");
        }

        // VALIDAR EMAIL
        if (string.IsNullOrWhiteSpace(email) ||
            email.Length > 30 ||
            !new EmailAddressAttribute().IsValid(email))
        {
            ModelState.AddModelError(
                nameof(Usuario.Email),
                "Ingresá un email válido de hasta 30 caracteres.");
        }

        // COMPROBAR EMAIL DUPLICADO
        if (!string.IsNullOrWhiteSpace(email))
        {
            var otroUsuario =
                await _servicio.BuscarPorEmail(email);

            if (otroUsuario != null &&
                otroUsuario.Id != usuario.Id)
            {
                ModelState.AddModelError(
                    nameof(Usuario.Email),
                    "Ese correo ya está registrado.");
            }
        }

        // VALIDAR AVATAR
        if (avatar != null)
        {
            if (avatar.Length == 0 ||
                avatar.Length > 2 * 1024 * 1024)
            {
                ModelState.AddModelError(
                    "avatar",
                    "El avatar debe pesar entre 1 byte y 2 MB.");
            }

            string extension = Path.GetExtension(
                avatar.FileName).ToLowerInvariant();

            if (extension != ".jpg" &&
                extension != ".jpeg" &&
                extension != ".png")
            {
                ModelState.AddModelError(
                    "avatar",
                    "Solo se permiten imágenes JPG y PNG.");
            }

            // Comprobar la firma real del archivo
            if (avatar.Length > 0 &&
                avatar.Length <= 2 * 1024 * 1024 &&
                (extension == ".jpg" ||
                 extension == ".jpeg" ||
                 extension == ".png"))
            {
                if (!await EsImagenValida(avatar, extension))
                {
                    ModelState.AddModelError(
                        "avatar",
                        "El archivo no es una imagen JPG o PNG válida.");
                }
            }
        }

        // Estas propiedades no se editan
        // desde el formulario.
        ModelState.Remove(nameof(Usuario.Password));
        ModelState.Remove(nameof(Usuario.rol));
        ModelState.Remove(nameof(Usuario.AvatarUrl));

        if (!ModelState.IsValid)
        {
            datos.Id = usuario.Id;
            datos.AvatarUrl = usuario.AvatarUrl;

            return View(datos);
        }

        // IMPORTANTE:
        // Guardamos la ruta anterior ANTES de actualizar
        // la entidad en Entity Framework.
        string? avatarAnterior = usuario.AvatarUrl;
        string? nuevoAvatar = null;

        try
        {
            // GUARDAR NUEVO AVATAR
            if (avatar != null)
            {
                nuevoAvatar = await GuardarAvatar(avatar);
            }

            // ACTUALIZAR BASE DE DATOS
            bool actualizado =
                await _servicio.ActualizarPerfil(
                    usuario.Id,
                    nombre,
                    email,
                    nuevoAvatar ?? avatarAnterior ?? "");

            if (!actualizado)
            {
                if (nuevoAvatar != null)
                    EliminarAvatar(nuevoAvatar);

                return NotFound();
            }

            // BORRAR AVATAR ANTERIOR
            if (nuevoAvatar != null &&
                !string.Equals(
                    avatarAnterior,
                    nuevoAvatar,
                    StringComparison.Ordinal))
            {
                EliminarAvatar(avatarAnterior);
            }

            TempData["Mensaje"] =
                "Perfil actualizado correctamente.";

            return RedirectToAction(nameof(MiPerfil));
        }
        catch
        {
            // Si falla la operación, intentamos
            // limpiar el archivo nuevo.
            // La limpieza no debe ocultar
            // la excepción original.
            if (nuevoAvatar != null)
            {
                try
                {
                    EliminarAvatar(nuevoAvatar);
                }
                catch
                {
                    // Registrar el error de limpieza
                    // si se agrega ILogger.
                }
            }

            throw;
        }
    }

    // GET: /Usuarios
    // SOLO ADMINISTRADORES
    [Authorize(Roles = "Administrador")]
    [HttpGet]
    public async Task<IActionResult> Index(int pagina = 1)
    {
        const int tamano = 10;

        pagina = Math.Max(1, pagina);

        var (usuarios, total) =
            await _servicio.ListarPaginado(
                pagina, tamano);

        ViewBag.PaginaActual = pagina;

        ViewBag.TotalPaginas =
            (int)Math.Ceiling((double)total / tamano);

        return View(usuarios);
    }

    // POST: /Usuarios/CambiarRol/5
    [Authorize(Roles = "Administrador")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarRol(
        int id, Usuario.Rol nuevoRol)
    {
        if (!Enum.IsDefined(
            typeof(Usuario.Rol), nuevoRol))
        {
            return BadRequest("Rol inválido.");
        }

        var usuario = await _servicio.BuscarUno(id);

        if (usuario == null)
            return NotFound();

        // Evitar modificar el propio rol.
        if (id == ObtenerUsuarioId())
        {
            return BadRequest(
                "No podés modificar tu propio rol.");
        }

        bool actualizado =
            await _servicio.CambiarRol(id, nuevoRol);

        if (!actualizado)
            return NotFound();

        TempData["Mensaje"] =
            "Rol actualizado correctamente.";

        return RedirectToAction(nameof(Index));
    }

    // OBTENER ID DEL USUARIO AUTENTICADO
    private int? ObtenerUsuarioId()
    {
        string? valor = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        return int.TryParse(valor, out int id)
            ? id
            : null;
    }

    // VALIDAR LA FIRMA DE LA IMAGEN
    private async Task<bool> EsImagenValida(
        IFormFile archivo,
        string extension)
    {
        byte[] firma = new byte[8];

        await using var stream = archivo.OpenReadStream();

        int leidos = await stream.ReadAsync(
            firma.AsMemory(0, firma.Length));

        if (extension == ".jpg" ||
            extension == ".jpeg")
        {
            return leidos >= 3 &&
                   firma[0] == 0xFF &&
                   firma[1] == 0xD8 &&
                   firma[2] == 0xFF;
        }

        if (extension == ".png")
        {
            byte[] firmaPng =
            {
                0x89, 0x50, 0x4E, 0x47,
                0x0D, 0x0A, 0x1A, 0x0A
            };

            return leidos == 8 &&
                   firma.SequenceEqual(firmaPng);
        }

        return false;
    }

    // GUARDAR AVATAR EN WWWROOT
    private async Task<string> GuardarAvatar(
        IFormFile archivo)
    {
        string webRoot = _entorno.WebRootPath
            ?? Path.Combine(
                _entorno.ContentRootPath,
                "wwwroot");

        string carpeta = Path.Combine(
            webRoot,
            "imagenes",
            "avatares");

        Directory.CreateDirectory(carpeta);

        string extension = Path.GetExtension(
            archivo.FileName).ToLowerInvariant();

        string nombre =
            $"{Guid.NewGuid():N}{extension}";

        string ruta = Path.Combine(carpeta, nombre);

        await using (var stream = new FileStream(
            ruta, FileMode.CreateNew))
        {
            await archivo.CopyToAsync(stream);
        }

        // Verificar que el archivo se creó.
        if (!System.IO.File.Exists(ruta))
        {
            throw new IOException(
                "No se pudo guardar el avatar.");
        }

        Console.WriteLine(
            $"Avatar guardado: {ruta}");

        return $"/imagenes/avatares/{nombre}";
    }

    // ELIMINAR AVATAR ANTERIOR
    private void EliminarAvatar(string? rutaRelativa)
    {
        if (string.IsNullOrWhiteSpace(rutaRelativa))
            return;

        const string prefijo =
            "/imagenes/avatares/";

        if (!rutaRelativa.StartsWith(
            prefijo,
            StringComparison.Ordinal))
        {
            return;
        }

        string nombre =
            rutaRelativa[prefijo.Length..];

        // Evitar rutas fuera de la carpeta.
        if (nombre != Path.GetFileName(nombre))
            return;

        string webRoot = _entorno.WebRootPath
            ?? Path.Combine(
                _entorno.ContentRootPath,
                "wwwroot");

        string ruta = Path.Combine(
            webRoot,
            "imagenes",
            "avatares",
            nombre);

        if (System.IO.File.Exists(ruta))
        {
            System.IO.File.Delete(ruta);
        }
    }
}
