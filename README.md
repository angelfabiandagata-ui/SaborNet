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

La aplicación estará disponible en `http://localhost:5133` o `https://localhost:7153`.

## API con JWT

- `POST /api/auth/login`: recibe email y contraseña y devuelve un token Bearer.
- `/api/ingredientes`: permite consultar ingredientes; las operaciones de escritura requieren un token JWT de un usuario Administrador.

La web utiliza sus propias pantallas MVC y no consume esta API como cliente. No se encontró una colección de Postman, Bruno o Insomnia en el repositorio.

## Matriz de evidencias

| Requerimiento | Estado | Código / evidencia |
|---|---|---|
| Modelo con al menos 6 tablas y relaciones 1:N y N:M | ✅ Cumplido | [AppDbContext.cs](backend/data/AppDbContext.cs), [DB.sql](backend/Database/DB.sql) y [diagrama DER](docs/der.png). `RecetaIngredientes` relaciona recetas e ingredientes. |
| Login, roles y avatar | ✅ Cumplido | [LoginController.cs](backend/Controllers/LoginController.cs), [UsuarioController.cs](backend/Controllers/UsuarioController.cs) y [Usuario.cs](backend/Models/Usuario.cs). |
| Carga y almacenamiento de archivos, además del avatar | ✅ Cumplido | [RecetaController.cs](backend/Controllers/RecetaController.cs), [Ingredientes.cs](backend/Controllers/Ingredientes.cs) e [ImagenesController.cs](backend/Controllers/ImagenesController.cs). Se guardan imágenes de recetas e ingredientes. |
| ABM completo con Vue y AJAX | ✅ Cumplido | [Ingredientes/Index.cshtml](backend/Views/Ingredientes/Index.cshtml) y [Ingredientes.cs](backend/Controllers/Ingredientes.cs). |
| Paginado del lado del servidor en los listados principales | ⚠️ Parcial | [RecetaController.cs](backend/Controllers/RecetaController.cs), [Ingredientes.cs](backend/Controllers/Ingredientes.cs) y [UsuarioController.cs](backend/Controllers/UsuarioController.cs) implementan paginado. Falta completar o revisar el listado de recetas recientes. |
| Búsqueda asíncrona de entidades relacionadas | ✅ Cumplido | [RecetaController.cs](backend/Controllers/RecetaController.cs), [Recetas/Crear.cshtml](backend/Views/Recetas/Crear.cshtml) y [Recetas/Editar.cshtml](backend/Views/Recetas/Editar.cshtml). |
| Web API autenticada con JWT | ⚠️ Parcial | [AuthController.cs](backend/Controllers/Api/AuthController.cs), [IngredientesController.cs](backend/Controllers/Api/IngredientesController.cs) y [JwtService.cs](backend/Services/JwtService.cs). Falta agregar la colección de pruebas de la API. |

### Pendiente para la entrega

- El script SQL crea el esquema, pero no carga datos de prueba; tampoco se incluyen credenciales de prueba por rol.
- Agregar una colección de Postman, Bruno o Insomnia para probar la API con JWT.
- Completar y verificar el paginado de recetas recientes.
- Incorporar capturas, GIF o video de las funcionalidades; por ahora, el repositorio incluye el diagrama DER como evidencia visual.
