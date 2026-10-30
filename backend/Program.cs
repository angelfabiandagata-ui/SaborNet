using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using backend.data;
using backend.Hubs;
using backend.Models;
using SaborNet.Service;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme) //Permitir/rechazar autenticación basada en cookies 
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
// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder =>
    {
        builder.WithOrigins("http://localhost:5133") 
               .AllowAnyHeader()
               .AllowAnyMethod()
               .AllowCredentials();
    });
});
builder.Services.AddScoped<RecetaService>();
builder.Services.AddScoped<IngredienteService>();
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<CategoriaService>();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.ExecuteSqlRawAsync("""
        ALTER TABLE IF EXISTS "Ingredientes"
            ADD COLUMN IF NOT EXISTS "ImagenDeIngrediente" text NOT NULL DEFAULT '';
        """);
    await db.Database.ExecuteSqlRawAsync("""
        ALTER TABLE IF EXISTS "Ingredientes"
            ALTER COLUMN "ImagenDeIngrediente" DROP DEFAULT;
        """);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();


app.UseRouting();


app.UseCors("CorsPolicy");

app.UseStaticFiles();


app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.UseCors();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapHub<ComentariosHub>("/comentariosHub");

app.Run();
