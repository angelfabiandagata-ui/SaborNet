
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using backend.data;
using backend.Hubs;
using backend.Models;
using SaborNet.Service;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// BASE DE DATOS
// ==========================================

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<
    IPasswordHasher<Usuario>,
    PasswordHasher<Usuario>>();

// ==========================================
// AUTENTICACIÓN: COOKIES + JWT
// ==========================================

// Las cookies siguen siendo el mecanismo
// predeterminado para la página MVC.
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login/Login";
        options.AccessDeniedPath = "/Acceso/AccesoDenegado";

        options.Events.OnValidatePrincipal = async context =>
        {
            var idTexto = context.Principal?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(idTexto, out int usuarioId))
            {
                context.RejectPrincipal();

                await context.HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);

                return;
            }

            var db = context.HttpContext.RequestServices
                .GetRequiredService<AppDbContext>();

            var usuario = await db.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == usuarioId);

            if (usuario == null || !usuario.Activo)
            {
                context.RejectPrincipal();

                await context.HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);

                return;
            }

            var rolActual = context.Principal?
                .FindFirst(ClaimTypes.Role)?.Value;

            if (rolActual != usuario.rol.ToString())
            {
                var claims = new[]
                {
                    new Claim(
                        ClaimTypes.NameIdentifier,
                        usuario.Id.ToString()),

                    new Claim(
                        ClaimTypes.Name,
                        usuario.Nombre),

                    new Claim(
                        ClaimTypes.Email,
                        usuario.Email),

                    new Claim(
                        ClaimTypes.Role,
                        usuario.rol.ToString())
                };

                var identidad = new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults.AuthenticationScheme);

                context.ReplacePrincipal(
                    new ClaimsPrincipal(identidad));

                context.ShouldRenew = true;
            }
        };
    });

// ==========================================
// CONFIGURACIÓN DE JWT
// ==========================================

string claveJwt = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "No se configuró Jwt:Key. Revisá los User Secrets.");

string emisorJwt = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException(
        "No se configuró Jwt:Issuer.");

string audienciaJwt = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException(
        "No se configuró Jwt:Audience.");

if (Encoding.UTF8.GetByteCount(claveJwt) < 32)
{
    throw new InvalidOperationException(
        "La clave JWT debe tener al menos 32 bytes.");
}

builder.Services
    .AddAuthentication()
    .AddJwtBearer(
        JwtBearerDefaults.AuthenticationScheme,
        options =>
        {
            options.MapInboundClaims = true;

            options.TokenValidationParameters =
                new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = emisorJwt,

                    ValidateAudience = true,
                    ValidAudience = audienciaJwt,

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey =
                        new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(claveJwt)),

                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,

                    ClockSkew = TimeSpan.Zero
                };

            // Verificar que el usuario siga activo
            // y que su rol no haya cambiado.
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    var idTexto = context.Principal?
                        .FindFirst(
                            ClaimTypes.NameIdentifier)?.Value;

                    if (!int.TryParse(
                        idTexto, out int usuarioId))
                    {
                        context.Fail("Token sin usuario válido.");
                        return;
                    }

                    var db = context.HttpContext
                        .RequestServices
                        .GetRequiredService<AppDbContext>();

                    var usuario = await db.Usuarios
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            u => u.Id == usuarioId);

                    if (usuario == null || !usuario.Activo)
                    {
                        context.Fail(
                            "El usuario no existe o está inactivo.");
                        return;
                    }

                    var rolToken = context.Principal?
                        .FindFirst(ClaimTypes.Role)?.Value;

                    if (rolToken != usuario.rol.ToString())
                    {
                        context.Fail(
                            "El rol del usuario cambió. Iniciá sesión nuevamente.");
                        return;
                    }
                }
            };
        });

// ==========================================
// AUTORIZACIÓN
// ==========================================

builder.Services.AddAuthorization();

// ==========================================
// MVC Y SIGNALR
// ==========================================

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

// ==========================================
// CORS
// ==========================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:5133")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ==========================================
// SERVICIOS DE SABORNET
// ==========================================

builder.Services.AddScoped<RecetaService>();
builder.Services.AddScoped<IngredienteService>();
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<CategoriaService>();
builder.Services.AddScoped<ComentarioService>();
builder.Services.AddScoped<ValoracionComentarioService>();
builder.Services.AddScoped<ValoracionRecetaService>();

var app = builder.Build();

// ==========================================
// COMPATIBILIDAD CON BASE DE DATOS EXISTENTE
// ==========================================

// Conservamos estas instrucciones porque
// ya estaban en tu proyecto.

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<AppDbContext>();

    await db.Database.ExecuteSqlRawAsync("""
        ALTER TABLE IF EXISTS "Ingredientes"
            ADD COLUMN IF NOT EXISTS "ImagenDeIngrediente"
            text NOT NULL DEFAULT '';
        """);

    await db.Database.ExecuteSqlRawAsync("""
        ALTER TABLE IF EXISTS "Ingredientes"
            ALTER COLUMN "ImagenDeIngrediente"
            DROP DEFAULT;
        """);
}

// ==========================================
// CONFIGURACIÓN HTTP
// ==========================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseCors("CorsPolicy");

// Primero autenticación, después autorización.
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// ==========================================
// RUTAS MVC
// ==========================================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// ==========================================
// RUTAS DE LA API REST
// ==========================================

app.MapControllers();

// ==========================================
// SIGNALR
// ==========================================

app.MapHub<ComentariosHub>("/comentariosHub");

app.Run();
