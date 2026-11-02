
using System.ComponentModel.DataAnnotations;
using backend.data;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaborNet.Service;

namespace backend.Controllers.Api;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<Usuario> _passwordHasher;
    private readonly JwtService _jwtService;

    public AuthController(
        AppDbContext db,
        IPasswordHasher<Usuario> passwordHasher,
        JwtService jwtService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
    }

    public class LoginApiDto
    {
        [Required(ErrorMessage = "Ingresá tu email.")]
        [EmailAddress(ErrorMessage = "Ingresá un email válido.")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Ingresá tu contraseña.")]
        public string Password { get; set; } = "";
    }

    // POST: /api/auth/login
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginApiDto datos)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        string email = datos.Email.Trim().ToLowerInvariant();

        var usuario = await _db.Usuarios
            .SingleOrDefaultAsync(
                u => u.Email.ToLower() == email);

        if (usuario == null || !usuario.Activo)
        {
            return Unauthorized(new
            {
                mensaje = "Credenciales inválidas."
            });
        }

        var resultado = _passwordHasher.VerifyHashedPassword(
            usuario,
            usuario.Password,
            datos.Password);

        if (resultado == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new
            {
                mensaje = "Credenciales inválidas."
            });
        }

        string token = _jwtService.GenerarToken(usuario);

        return Ok(new
        {
            mensaje = "Autenticación correcta.",
            token,
            tipo = "Bearer",
            expiraEnSegundos = 3600,
            usuario = new
            {
                id = usuario.Id,
                nombre = usuario.Nombre,
                email = usuario.Email,
                rol = usuario.rol.ToString()
            }
        });
    }
}
