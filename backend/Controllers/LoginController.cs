using System.Security.Claims;
using backend.data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using backend.Models;
using Microsoft.AspNetCore.Authorization;

namespace backend.Controllers;

public class LoginController : Controller
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<Usuario> _passwordHasher;

    public LoginController(AppDbContext db, IPasswordHasher<Usuario> passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    [AllowAnonymous]
    public IActionResult Login()
    {
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var email = model.Email.Trim().ToLowerInvariant();

        var usuario = await _db.Usuarios
            .SingleOrDefaultAsync(u => u.Email.ToLower() == email);

        if (usuario is null ||
            !usuario.Activo ||
            _passwordHasher.VerifyHashedPassword(
                usuario,
                usuario.Password,
                model.Password) == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(
                string.Empty,
                "Email o contraseña incorrectos, o cuenta deshabilitada.");

            return View(model);
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.Nombre),
            new Claim(ClaimTypes.Email, usuario.Email),
            new Claim(ClaimTypes.Role, usuario.rol.ToString())
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return RedirectToAction("Index", "Home");
    }



    public IActionResult Registrarse()
    {
        return View(new RegistroViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registrarse(
    RegistroViewModel model,
    IFormFile? avatar)
    {
        if (!ModelState.IsValid)
            return View(model);

        if (await _db.Usuarios.AnyAsync(u => u.Email == model.Email))
        {
            ModelState.AddModelError(
                nameof(model.Email),
                "Ya existe una cuenta con ese email.");

            return View(model);
        }

        string avatarUrl = "";

        // Validar el archivo si el usuario seleccionó uno.
        if (avatar != null && avatar.Length > 0)
        {
            const long tamanoMaximo = 2 * 1024 * 1024;

            if (avatar.Length > tamanoMaximo)
            {
                ModelState.AddModelError(
                    "avatar",
                    "La imagen no puede superar los 2 MB.");

                return View(model);
            }

            var extension = Path.GetExtension(
                avatar.FileName).ToLowerInvariant();

            var extensionesPermitidas = new[]
            {
            ".jpg", ".jpeg", ".png"
        };

            if (!extensionesPermitidas.Contains(extension))
            {
                ModelState.AddModelError(
                    "avatar",
                    "Solo se permiten imágenes JPG o PNG.");

                return View(model);
            }

            // Guardar el archivo dentro de wwwroot.
            var carpeta = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "imagenes",
                "avatares");

            Directory.CreateDirectory(carpeta);

            var nombreArchivo =
                $"{Guid.NewGuid():N}{extension}";

            var rutaCompleta = Path.Combine(
                carpeta,
                nombreArchivo);

            await using (var stream = new FileStream(
                rutaCompleta,
                FileMode.CreateNew))
            {
                await avatar.CopyToAsync(stream);
            }

            avatarUrl = $"/imagenes/avatares/{nombreArchivo}";
        }

        var usuario = new Usuario
        {
            Nombre = model.Nombre,
            Email = model.Email,
            AvatarUrl = avatarUrl,
            rol = Usuario.Rol.Usuario
        };

        usuario.Password = _passwordHasher.HashPassword(
            usuario,
            model.Password);

        _db.Usuarios.Add(usuario);

        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Login));
    }

    // POST: /Login/Logout
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction("Index", "Home");
    }

    // GET: /Cuenta/AccesoDenegado
    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccesoDenegado()
    {
        return View();
    }

}