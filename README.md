# SaborNet 🍽️

Aplicación web para compartir y descubrir recetas. Permite gestionar recetas e ingredientes, agregar comentarios y valoraciones, administrar perfiles y consultar una API protegida con JWT.

> Trabajo Práctico Integrador — Laboratorio II.

## Contenidos

- [Características](#características)
- [Tecnologías](#tecnologías)
- [Requisitos previos](#requisitos-previos)
- [Instalación y ejecución](#instalación-y-ejecución)
- [Base de datos y relaciones](#base-de-datos-y-relaciones)
- [Datos de prueba](#datos-de-prueba)
- [API REST y JWT](#api-rest-y-jwt)
- [Tabla de evidencias](#-tabla-de-evidencias)
- [Estructura del proyecto](#estructura-del-proyecto)
- [Seguridad y configuración](#seguridad-y-configuración)

## Características

- Registro, inicio y cierre de sesión con autenticación por cookies.
- Perfiles de usuario con avatar y roles: `Usuario`, `Administrador`, `Chef` y `Cocinero_Amateur`.
- Alta, consulta, edición y baja de recetas e ingredientes.
- Búsqueda de ingredientes asíncrona al armar una receta.
- Carga de fotos para recetas, ingredientes y avatares.
- Comentarios, valoraciones de recetas y votos sobre comentarios.
- Administración de usuarios, cambio de rol y baja lógica, restringidos a administradores.
- Listados paginados del lado servidor.
- API REST de ingredientes y autenticación mediante JWT Bearer.
- Actualización dinámica del listado de ingredientes con Vue 3 y `fetch` (AJAX).

## Tecnologías

- C# y ASP.NET Core MVC (`net10.0`).
- Entity Framework Core y PostgreSQL (`Npgsql`).
- Razor, HTML, CSS, Bootstrap y JavaScript.
- Vue 3 para el ABM dinámico de ingredientes.
- Tom Select para búsqueda asíncrona de ingredientes.
- JWT Bearer para proteger operaciones de la API.

## Requisitos previos

- .NET SDK 10.
- PostgreSQL en ejecución.
- Git.

## Instalación y ejecución

1. Cloná el repositorio y entrá al proyecto:

   ```bash
   git clone https://github.com/angelfabiandagata-ui/SaborNet.git
   cd SaborNet/backend
   ```

2. Creá una base PostgreSQL local llamada `sabornetdb` y configurá la conexión con un secreto de usuario. Reemplazá los valores de ejemplo por los de tu entorno:

   ```bash
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=sabornetdb;Username=TU_USUARIO;Password=TU_PASSWORD"
   ```

3. Configurá la clave de firma JWT como secreto. Debe tener al menos 32 bytes. `Issuer` y `Audience` ya están declarados en la configuración del proyecto; también podés sobreescribirlos con User Secrets si lo necesitás.

   ```bash
   dotnet user-secrets set "Jwt:Key" "CAMBIAR_POR_UN_SECRETO_LOCAL_DE_AL_MENOS_32_CARACTERES"
   ```

4. Ejecutá el script de esquema contra la base `sabornetdb`:

   ```bash
   psql -h localhost -U TU_USUARIO -d sabornetdb -f Database/DB.sql
   ```

   También podés abrir `Database/DB.sql` con DBeaver, conectado a `sabornetdb`, y ejecutarlo.

5. Restaurá dependencias y levantá la aplicación:

   ```bash
   dotnet restore
   dotnet run --launch-profile http
   ```

6. Abrí [http://localhost:5133](http://localhost:5133).

> El proyecto también incluye un perfil HTTPS en `Properties/launchSettings.json` (`https://localhost:7153`).

## Base de datos y relaciones

El esquema SQL se encuentra en [`backend/Database/DB.sql`](backend/Database/DB.sql) y el contexto EF Core en [`backend/data/AppDbContext.cs`](backend/data/AppDbContext.cs). El modelo contiene más de cuatro tablas. Entre las relaciones principales están:

- **Usuario 1:N Recetas**: un usuario puede publicar varias recetas.
- **Categoría 1:N Recetas**: una categoría puede clasificar varias recetas.
- **Receta 1:N Comentarios**: una receta puede recibir varios comentarios.
- **Receta N:M Ingredientes**, resuelta mediante `RecetaIngredientes`, que además guarda la cantidad usada.
- **Usuario 1:N Comentarios**, valoraciones de recetas y valoraciones de comentarios.

La tabla intermedia `RecetaIngredientes` tiene claves foráneas a `Recetas` e `Ingredientes`; el contexto configura estas asociaciones y evita duplicar el mismo ingrediente en una receta.

## Datos de prueba

La aplicación no documenta credenciales de demostración preconfiguradas. Creá una cuenta desde **Registrate** y usá esos datos para iniciar sesión. Por ejemplo, podés registrar localmente:

| Campo | Valor de ejemplo |
|---|---|
| Nombre | `Ana Cocina` |
| Email | `ana.cocina@example.test` |
| Contraseña | Elegí una contraseña local y guardala para las pruebas |
| Avatar | Opcional: imagen `.jpg`, `.jpeg` o `.png` de hasta 2 MB |

Al registrarse, la cuenta comienza con el rol `Usuario`. Para probar operaciones administrativas en una base **local de desarrollo**, podés promover esa cuenta después del registro:

```sql
UPDATE "Usuarios"
SET "rol" = 1
WHERE "Email" = 'ana.cocina@example.test';
```

El valor `1` corresponde a `Administrador` en el enum actual. Cerrá sesión y volvé a iniciarla para que se actualicen los claims de autenticación. No ejecutes este cambio en una base de producción.

Una vez iniciada la sesión como administrador, podés crear una categoría e ingredientes desde la interfaz. Datos de ejemplo para probar el formulario de receta:

| Dato | Ejemplo |
|---|---|
| Categoría | `Pastas` |
| Ingrediente 1 | `Harina` — unidad `g` — cantidad `250` |
| Ingrediente 2 | `Huevo` — unidad `unidad` — cantidad `2` |
| Título de receta | `Fideos caseros` |
| Descripción | `Pasta fresca para compartir` |
| Instrucciones | `Mezclar, amasar, estirar y cocinar en agua hirviendo.` |
| Tiempo | `30` minutos |
| Foto | Imagen `.jpg`, `.jpeg` o `.png` de hasta 5 MB |

Para probar roles, cambiá el rol de una cuenta desde la administración de usuarios. La aplicación también permite `Chef` y `Cocinero_Amateur`.

## API REST y JWT

### Obtener un token

La autenticación de la API está disponible en `POST /api/auth/login`. En desarrollo, con el perfil HTTP activo:

```bash
curl -X POST http://localhost:5133/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"ana.cocina@example.test","password":"TU_PASSWORD_LOCAL"}'
```

La respuesta exitosa contiene `token`, `tipo` (`Bearer`), duración y datos del usuario. La contraseña debe ser la elegida al registrar la cuenta. Las credenciales incorrectas o una cuenta inactiva reciben `401 Unauthorized`.

### Consultar ingredientes

La consulta es pública y acepta paginación y búsqueda:

```bash
curl "http://localhost:5133/api/ingredientes?pagina=1&buscar=harina"
```

También se puede consultar un ingrediente por ID:

```bash
curl "http://localhost:5133/api/ingredientes/1"
```

### Operaciones administrativas

`POST`, `PUT` y `DELETE` en `/api/ingredientes` requieren un token de un usuario con rol `Administrador`. Por ejemplo, para crear un ingrediente enviando una imagen multipart:

```bash
curl -X POST http://localhost:5133/api/ingredientes \
  -H "Authorization: Bearer TU_TOKEN_JWT" \
  -F "nombre=Tomate" \
  -F "unidadDeMedida=unidad" \
  -F "imagen=@./tomate.jpg"
```

La API acepta imágenes JPG/JPEG y PNG de hasta 5 MB y valida el formato. Las rutas implementadas y los permisos están en [`IngredientesController.cs`](backend/Controllers/Api/IngredientesController.cs) y la generación del token en [`AuthController.cs`](backend/Controllers/Api/AuthController.cs).

## 📋 Tabla de evidencias

Los enlaces llevan a los archivos del repositorio donde se implementa cada funcionalidad. **Las capturas, GIF y videos todavía deben agregarse** en `docs/evidencias/`; no se incluyen evidencias visuales que no estén actualmente en el repositorio. Reemplazá cada pendiente por un enlace al archivo real al incorporarlo.

| Requerimiento mínimo | Estado según el código | Enlace al código / controladores | Evidencia (captura / GIF / video) |
|---|---|---|---|
| **1. Cuatro tablas y relación 1:N** | ✅ Cumplido: el modelo define varias entidades y relaciones 1:N, además de la relación N:M Receta–Ingrediente con tabla intermedia. | [AppDbContext.cs](backend/data/AppDbContext.cs) · [DB.sql](backend/Database/DB.sql) · [modelos](backend/Models/) | Pendiente: adjuntar diagrama DER en `docs/der.png`. |
| **2. Login, roles y avatar** | ✅ Cumplido: login con cookies, roles y avatar opcional; el registro asigna el rol inicial `Usuario`. | [LoginController.cs](backend/Controllers/LoginController.cs) · [UsuarioController.cs](backend/Controllers/UsuarioController.cs) · [MiPerfil.cshtml](backend/Views/Usuarios/MiPerfil.cshtml) · [Usuario.cs](backend/Models/Usuario.cs) | Pendiente: captura de perfil mostrando avatar y rol en `docs/evidencias/perfil.png`. |
| **3. Manejo de archivos (extra)** | ✅ Cumplido para imágenes de recetas, ingredientes y avatares. No se encontró una carga de comprobantes en el código revisado. | [RecetasController.cs](backend/Controllers/RecetaController.cs) · [API de ingredientes](backend/Controllers/Api/IngredientesController.cs) · [ImagenesController.cs](backend/Controllers/ImagenesController.cs) | Pendiente: GIF/video mostrando la carga de una foto de receta o ingrediente en `docs/evidencias/carga-imagen.gif`. |
| **4. ABM en Vue.js + AJAX** | ✅ Cumplido para el ABM dinámico de ingredientes: Vue 3 y `fetch` para listar, crear, editar, eliminar, buscar y paginar. | [Index.cshtml de Ingredientes](backend/Views/Ingredientes/Index.cshtml) · [API de ingredientes](backend/Controllers/Api/IngredientesController.cs) | Pendiente: GIF del ABM dinámico en `docs/evidencias/abm-ingredientes.gif`. |
| **5. Paginado por servidor** | ✅ Cumplido: recetas y usuarios reciben `pagina` y consultan servicios paginados; la API de ingredientes también pagina. | [RecetasController.cs](backend/Controllers/RecetaController.cs) · [UsuarioController.cs](backend/Controllers/UsuarioController.cs) · [API de ingredientes](backend/Controllers/Api/IngredientesController.cs) | Pendiente: captura del inspector de red con una solicitud de página 2 en `docs/evidencias/paginado.png`. |
| **6. Selección asíncrona (AJAX)** | ✅ Cumplido: Tom Select carga ingredientes mediante una solicitud `fetch` al escribir al menos dos caracteres. | [Crear.cshtml de Recetas](backend/Views/Recetas/Crear.cshtml) · [RecetasController.cs](backend/Controllers/RecetaController.cs) (`BuscarIngredientes`) | Pendiente: GIF de búsqueda y selección de ingredientes en `docs/evidencias/busqueda-ingredientes.gif`. |
| **7. Web API + JWT** | ✅ Cumplido: endpoint de login que emite JWT y operaciones administrativas de ingredientes protegidas con Bearer y rol `Administrador`. | [AuthController.cs](backend/Controllers/Api/AuthController.cs) · [JwtService.cs](backend/Services/JwtService.cs) · [IngredientesController.cs](backend/Controllers/Api/IngredientesController.cs) | Pendiente: captura de Postman con respuesta de login y solicitud autorizada con Bearer Token en `docs/evidencias/api-jwt.png`. |

### Cómo completar las evidencias visuales

1. Creá el directorio `docs/evidencias/` y guardá allí capturas o GIF propios, sin contraseñas, tokens ni datos personales reales.
2. Generá el DER de la base y guardalo como `docs/der.png`.
3. Reemplazá cada texto `Pendiente` de la tabla por un enlace Markdown a la evidencia agregada.

## Estructura del proyecto

```text
SaborNet/
├── backend/
│   ├── Controllers/       # Controladores MVC y API
│   ├── Database/          # Script SQL
│   ├── data/              # AppDbContext
│   ├── Models/            # Entidades y ViewModels
│   ├── Services/          # JWT
│   ├── Service/           # Servicios de negocio
│   ├── Views/             # Vistas Razor
│   └── wwwroot/           # CSS, JavaScript e imágenes
└── README.md
```

## Seguridad y configuración

- **No publiques secretos, contraseñas ni tokens.** Configurá la conexión y la clave JWT con User Secrets o variables de entorno; no agregues credenciales reales a `appsettings.json`.
- El archivo de configuración versionado contiene actualmente una cadena de conexión con una contraseña. Si esa credencial corresponde a una base real o fue compartida, considerala expuesta: rotala y reemplazala por configuración local secreta. No la reutilices.
- Las cuentas creadas desde el registro reciben rol `Usuario`; la administración requiere un administrador existente.
- Las imágenes de avatar se limitan a 2 MB y las de recetas/ingredientes a 5 MB. Usá únicamente archivos de prueba propios.

## Licencia

No se encontró un archivo de licencia en la raíz del repositorio. Consultá con el equipo antes de reutilizar o distribuir el proyecto.
