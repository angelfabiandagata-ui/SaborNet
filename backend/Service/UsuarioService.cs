
using backend.data;
using backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace SaborNet.Service;

public class UsuarioService : ServicioBase<Usuario>
{
    private readonly IPasswordHasher<Usuario> _passwordHasher;

    public UsuarioService(
        AppDbContext db,
        IPasswordHasher<Usuario> passwordHasher)
        : base(db)
    {
        _passwordHasher = passwordHasher;
    }

    // BUSCAR USUARIO POR EMAIL
    public Task<Usuario?> BuscarPorEmail(string email)
    {
        string emailNormalizado =
            email.Trim().ToLowerInvariant();

        return Db.Usuarios.FirstOrDefaultAsync(u =>
            u.Email.ToLower() == emailNormalizado);
    }

    // REGISTRAR USUARIO
    public async Task<Usuario?> Registrar(
        string nombre,
        string email,
        string password)
    {
        email = email.Trim().ToLowerInvariant();

        var existente = await BuscarPorEmail(email);

        if (existente != null)
            return null;

        var usuario = new Usuario
        {
            Nombre = nombre.Trim(),
            Email = email,
            AvatarUrl = "",
            rol = Usuario.Rol.Usuario
        };

        // Guardamos el hash
        usuario.Password =
            _passwordHasher.HashPassword(usuario, password);

        Db.Usuarios.Add(usuario);
        await Db.SaveChangesAsync();

        return usuario;
    }

    // VALIDAR CREDENCIALES PARA LOGIN
    public async Task<Usuario?> ValidarCredenciales(
        string email,
        string password)
    {
        var usuario = await BuscarPorEmail(email);

        if (usuario == null)
            return null;

        var resultado = _passwordHasher.VerifyHashedPassword(
            usuario,
            usuario.Password,
            password);

        if (resultado == PasswordVerificationResult.Failed)
            return null;

        // Actualizar el hash si el algoritmo lo requiere
        if (resultado ==
            PasswordVerificationResult.SuccessRehashNeeded)
        {
            usuario.Password =
                _passwordHasher.HashPassword(usuario, password);

            await Db.SaveChangesAsync();
        }

        return usuario;
    }

    // LISTAR USUARIOS CON PAGINACIÓN
    public async Task<(List<Usuario> Items, int Total)>
        ListarPaginado(int pagina, int tamano)
    {
        pagina = Math.Max(1, pagina);
        tamano = Math.Clamp(tamano, 1, 100);

        var consulta = Db.Usuarios.AsNoTracking();

        int total = await consulta.CountAsync();

        var usuarios = await consulta
            .OrderBy(u => u.Nombre)
            .ThenBy(u => u.Id)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .ToListAsync();

        return (usuarios, total);
    }

    // ACTUALIZAR PERFIL
    public async Task<bool> ActualizarPerfil(
        int id,
        string nombre,
        string email,
        string avatarUrl)
    {
        var usuario = await Db.Usuarios.FindAsync(id);

        if (usuario == null)
            return false;

        usuario.Nombre = nombre.Trim();
        usuario.Email = email.Trim().ToLowerInvariant();
        usuario.AvatarUrl = avatarUrl;

        await Db.SaveChangesAsync();

        return true;
    }

    // CAMBIAR ROL DE UN USUARIO
    public async Task<bool> CambiarRol(
        int id,
        Usuario.Rol nuevoRol)
    {
        var usuario = await Db.Usuarios.FindAsync(id);

        if (usuario == null)
            return false;

        usuario.rol = nuevoRol;

        await Db.SaveChangesAsync();

        return true;
    }
}
