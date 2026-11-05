# SaborNet

SaborNet es una plataforma web de recetas desarrollada con arquitectura MVC. Busca reunir en un solo espacio recetas organizadas por categoría para que las personas puedan descubrir preparaciones, compartir sus conocimientos culinarios y participar en una comunidad gastronómica.

Los usuarios registrados pueden publicar recetas con fotografías, ingredientes e instrucciones, y participar mediante comentarios. Las personas visitantes pueden explorar el contenido público e iniciar el registro para acceder a las funciones de participación.

## Funcionalidades

- Consulta y búsqueda de recetas y categorías.
- Publicación de recetas con imágenes, instrucciones y sus ingredientes.
- Perfiles con avatar y autenticación con roles.
- Comentarios y valoraciones en recetas y comentarios.
- Gestión de ingredientes con un ABM reactivo en Vue.

La aplicación cuenta con los roles `Usuario`, `Cocinero_Amateur`, `Chef` y `Administrador`. Las secciones de gestión administrativa están restringidas al rol Administrador. La versión actual incluye valoraciones, además de comentarios.

## Tecnologías

- ASP.NET Core MVC con .NET 10
- Entity Framework Core
- PostgreSQL
- Vue 3 y `fetch` para el ABM de ingredientes
- SignalR para funciones en tiempo real

## Modelo de datos

El modelo incluye usuarios, recetas, categorías, ingredientes, comentarios y valoraciones. Un usuario puede publicar varias recetas y cada receta pertenece a una categoría. `RecetaIngredientes` resuelve la relación muchos a muchos entre recetas e ingredientes y almacena la cantidad utilizada. Los comentarios y las valoraciones también relacionan usuarios con recetas o comentarios.

![Diagrama entidad-relación](docs/der.png)

El script de creación del esquema está en [`backend/Database/DB.sql`](backend/Database/DB.sql).

## Requisitos

- .NET SDK 10
- PostgreSQL

## Ejecución local

1. Crear la base de datos `sabornetdb` en PostgreSQL.
2. Ejecutar [`backend/Database/DB.sql`](backend/Database/DB.sql) conectado a esa base.
3. Configurar la conexión y la clave JWT desde la carpeta `backend`:

   ```bash
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=sabornetdb;Username=postgres;Password=TU_PASSWORD"
   dotnet user-secrets set "Jwt:Key" "REEMPLAZAR_POR_UNA_CLAVE_DE_AL_MENOS_32_CARACTERES"
   ```

4. Iniciar la aplicación:

   ```bash
   dotnet run
   ```

La aplicación estará disponible en `http://localhost:5133` .

## API con JWT

- `POST /api/auth/login`: recibe email y contraseña y devuelve un token Bearer.
- `/api/ingredientes`: permite consultar ingredientes; las operaciones de escritura requieren un token JWT de un usuario Administrador.

## Tabla de evidencias

| Requerimiento mínimo | Estado | Enlace al código (método exacto) | Evidencia |
| --- | --- | --- | --- |
| 1. 4 tablas y relación 1:N | ✅ Cumplido | `DbSet` de las 8 tablas: [`AppDbContext.cs#L10`](backend/Data/AppDbContext.cs#L10) · Relaciones: [`OnModelCreating`](backend/Data/AppDbContext.cs#L23) | Diagrama DER: [`docs/der.png`](docs/der.png) (se muestra arriba en "Modelo de datos") |
| 2. Login, roles y avatar | ✅ Cumplido | POST `/Login/Login`: [`LoginController.cs#L32`](backend/Controllers/LoginController.cs#L32) · Claim de rol: [`LoginController.cs#L61`](backend/Controllers/LoginController.cs#L61) · Subida avatar en registro: [`LoginController.cs#L80`](backend/Controllers/LoginController.cs#L80) · Cambio de rol: [`UsuarioController.cs#L234`](backend/Controllers/UsuarioController.cs#L234) | Captura pendiente: perfil con avatar |
| 3. Manejo de archivos (extra) | ✅ Cumplido | Servir archivos guardados: [`ImagenesController.cs#L17`](backend/Controllers/ImagenesController.cs#L17) · POST de receta con imagen: [`RecetaController.cs#L211`](backend/Controllers/RecetaController.cs#L211) | Video: [Subir receta con imagen](docs/SubirReceta.mp4) |
| 4. ABM en Vue.js + AJAX | ✅ Cumplido | App Vue (`createApp`): [`Views/Ingredientes/Index.cshtml#L218`](backend/Views/Ingredientes/Index.cshtml#L218) · Acciones AJAX del backend: [`Ingredientes.cs#L182`](backend/Controllers/Ingredientes.cs#L182), [`Ingredientes.cs#L323`](backend/Controllers/Ingredientes.cs#L323) | Video: [ABM de ingredientes en Vue](docs/ABM%20Ingrediente%20Vue.mp4) |
| 5. Paginado por servidor | ✅ Cumplido | Paginado server-side: [`Api/IngredientesController.cs#L38`](backend/Controllers/Api/IngredientesController.cs#L38) (petición `GET /api/Ingredientes/?pagina=2`) | Video: [Paginado](docs/Paginado.mp4) |
| 6. Selección asíncrona (AJAX) | ✅ Cumplido | Búsqueda de ingredientes por AJAX: [`RecetaController.cs#L181`](backend/Controllers/RecetaController.cs#L181) · Listado AJAX de ingredientes: [`Ingredientes.cs#L45`](backend/Controllers/Ingredientes.cs#L45) | Captura pendiente: búsqueda asíncrona en desplegable |
| 7. Web API + JWT | ✅ Cumplido | Login API que emite el token: [`Api/AuthController.cs#L44`](backend/Controllers/Api/AuthController.cs#L44) · `[Authorize]` con rol sobre la API: [`Api/IngredientesController.cs#L131`](backend/Controllers/Api/IngredientesController.cs#L131) | Documento: [Pruebas desde PowerShell (PDF)](docs/Pruebas%20desde%20Powershell.pdf) · Captura pendiente: token Bearer en PowerShell/Postman |

