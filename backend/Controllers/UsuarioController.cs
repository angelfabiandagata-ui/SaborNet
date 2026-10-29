
using System.Security.Claims;
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

        // Validaciones de los campos editables
        if (nombre.Length < 3 || nombre.Length > 20)
        {
            ModelState.AddModelError(
                nameof(Usuario.Nombre),
                "El nombre debe tener entre 3 y 20 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(email) ||
            email.Length > 30 ||
            !new System.ComponentModel.DataAnnotations
                .EmailAddressAttribute().IsValid(email))
        {
            ModelState.AddModelError(
                nameof(Usuario.Email),
                "Ingresá un email válido de hasta 30 caracteres.");
        }

        // Verificar que el correo no esté registrado
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

        // Validar imagen
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
        }

        ModelState.Remove(nameof(Usuario.Password));
        ModelState.Remove(nameof(Usuario.rol));

        if (!ModelState.IsValid)
        {
            datos.Id = usuario.Id;
            datos.AvatarUrl = usuario.AvatarUrl;

            return View(datos);
        }

        string? nuevoAvatar = null;

        try
        {
            if (avatar != null)
                nuevoAvatar = await GuardarAvatar(avatar);

            bool actualizado =
                await _servicio.ActualizarPerfil(
                    usuario.Id,
                    nombre,
                    email,
                    nuevoAvatar ?? usuario.AvatarUrl);

            if (!actualizado)
            {
                if (nuevoAvatar != null)
                    EliminarAvatar(nuevoAvatar);

                return NotFound();
            }

            if (nuevoAvatar != null)
                EliminarAvatar(usuario.AvatarUrl);

            TempData["Mensaje"] =
                "Perfil actualizado correctamente";

            return RedirectToAction(nameof(MiPerfil));
        }
        catch
        {
            if (nuevoAvatar != null)
                EliminarAvatar(nuevoAvatar);

            throw;
        }
    }

    // GET: /Usuarios
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
        if (!Enum.IsDefined(typeof(Usuario.Rol), nuevoRol))
            return BadRequest("Rol inválido.");

        var usuario = await _servicio.BuscarUno(id);

        if (usuario == null)
            return NotFound();

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

    private int? ObtenerUsuarioId()
    {
        string? valor = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        return int.TryParse(valor, out int id)
            ? id
            : null;
    }

    private async Task<string> GuardarAvatar(
        IFormFile archivo)
    {
        string carpeta = Path.Combine(
            _entorno.WebRootPath,
            "imagenes",
            "avatares");

        Directory.CreateDirectory(carpeta);

        string extension = Path.GetExtension(
            archivo.FileName).ToLowerInvariant();

        string nombre =
            $"{Guid.NewGuid():N}{extension}";

        string ruta = Path.Combine(carpeta, nombre);

        await using var stream = new FileStream(
            ruta, FileMode.CreateNew);

        await archivo.CopyToAsync(stream);

        return $"/imagenes/avatares/{nombre}";
    }

    private void EliminarAvatar(string? rutaRelativa)
    {
        if (string.IsNullOrWhiteSpace(rutaRelativa))
            return;

        const string prefijo = "/imagenes/avatares/";

        if (!rutaRelativa.StartsWith(
            prefijo, StringComparison.Ordinal))
            return;

        string nombre = rutaRelativa[prefijo.Length..];

        if (nombre != Path.GetFileName(nombre))
            return;

        string ruta = Path.Combine(
            _entorno.WebRootPath,
            "imagenes",
            "avatares",
            nombre);

        if (System.IO.File.Exists(ruta))
            System.IO.File.Delete(ruta);
    }
}
