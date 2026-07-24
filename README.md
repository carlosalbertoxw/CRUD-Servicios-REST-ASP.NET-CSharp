# CRUD-Servicios-REST-ASP.NET-CSharp

Ejemplo de un servicio REST con ASP.NET Core (.NET 10): un CRUD de notas respaldado por MySQL, con autenticación por API key (hasheada, con revocación y expiración), notas privadas por cliente, migraciones de base de datos, paginación por keyset, búsqueda de texto completo, rate limiting, health checks, pruebas de integración y documentación OpenAPI interactiva.

## Requisitos

- [Docker](https://www.docker.com/) — suficiente para levantar todo el stack (Opción A) y para las pruebas de integración.
- [SDK de .NET 10](https://dotnet.microsoft.com/download/dotnet/10.0) — solo para desarrollo local con el SDK (Opción B).

## Puesta en marcha

### Opción A — Todo en Docker

Levanta MySQL, aplica las migraciones (con datos de ejemplo) y arranca la API con un solo comando:

```bash
docker compose up --build
```

`docker compose` orquesta el orden: espera a que MySQL esté sano, corre el servicio `migrations` (que aplica el esquema con `--seed` y termina) y solo entonces arranca `api`. La API queda en `http://localhost:8080` (ver [endpoints](#endpoints)).

### Opción B — Desarrollo local con el SDK

Útil para depurar la API con recarga y breakpoints, usando solo MySQL en Docker.

**1. Levantar la base de datos** (MySQL 8.4 en Docker):

```bash
docker compose up -d mysql
```

**2. Aplicar las migraciones** (crea las tablas; `--seed` agrega notas de ejemplo y la API key de desarrollo):

```bash
dotnet run --project src/NotesApi.Migrations -- --seed
```

**3. Ejecutar la API:**

```bash
dotnet run --project src/NotesApi --launch-profile http
```

La API queda disponible en `http://localhost:5244`.

### Endpoints

Según la opción, la base es `http://localhost:8080` (Docker) o `http://localhost:5244` (SDK local):

- **Documentación interactiva (Scalar):** `/scalar`
- **Especificación OpenAPI:** `/openapi/v1.json`
- **Health checks:** `/health/live` (proceso vivo) y `/health/ready` (BD alcanzable)

## Autenticación

Todos los endpoints exigen el encabezado `X-Api-Key` (*fail-closed*: solo la documentación y los health checks están exentos). El formato de la key es `<key_id>.<secreto>`:

```
X-Api-Key: local-dev.dev-secret        <-- key de desarrollo creada por --seed
```

Las keys viven en la tabla `api_keys` con el **secreto hasheado** (SHA-256, `BINARY(32)`), lo que permite tener varias keys (una por cliente), revocarlas individualmente (`revoked_at`), darles caducidad (`expires_at`) y detectar keys en desuso (`last_used_at`, actualizado de forma laxa, máx. una escritura por hora). **Cada cliente solo ve y modifica sus propias notas** (columna `owner_key_id`). Para administrar keys:

```sql
-- Crear: el cliente usará "X-Api-Key: cliente-1.<secreto-aleatorio-largo>"
INSERT INTO api_keys (key_id, key_hash, client_name, expires_at)
VALUES ('cliente-1', UNHEX(SHA2('<secreto-aleatorio-largo>', 256)), 'Nombre del cliente',
        UTC_TIMESTAMP() + INTERVAL 1 YEAR);

-- Revocar
UPDATE api_keys SET revoked_at = UTC_TIMESTAMP() WHERE key_id = 'cliente-1';

-- Detectar keys sin uso en 90 días
SELECT key_id, client_name, last_used_at FROM api_keys
WHERE last_used_at IS NULL OR last_used_at < UTC_TIMESTAMP() - INTERVAL 90 DAY;
```

Notas operativas:

- **Las keys se revocan, no se eliminan:** la FK de `notes.owner_key_id` impide el `DELETE` físico de una key con notas (a propósito — la revocación con `revoked_at` conserva la trazabilidad).
- **Cache de keys:** las keys validadas se cachean en memoria (`Authentication:KeyCacheSeconds`, 60 s por defecto), así el caso caliente autentica sin tocar la BD. El costo: una revocación tarda hasta ese TTL en surtir efecto.

Sin key válida (o con una revocada/expirada) la API responde `401`. Además hay **rate limiting** por IP (100 peticiones/minuto por defecto, configurable): al excederlo responde `429` con `Retry-After`.

## Endpoints

Todas las operaciones actúan **sobre las notas del cliente autenticado**; las notas de otros clientes responden `404`.

| Método | Ruta | Descripción | Respuestas |
|--------|------|-------------|------------|
| GET | `/api/notes?pageSize=&afterId=&search=` | Lista notas (id, título y fechas) | 200, 400 |
| GET | `/api/notes/{id}` | Obtiene una nota con su contenido completo | 200, 404 |
| POST | `/api/notes` | Crea una nota | 201, 400 |
| PUT | `/api/notes/{id}` | Actualiza una nota | 204, 400, 404 |
| DELETE | `/api/notes/{id}` | Elimina una nota | 204, 404 |

El cuerpo de `POST` y `PUT` es JSON con `title` (obligatorio, máx. 250 caracteres) y `text` (opcional, máx. 100.000 caracteres). Las respuestas incluyen `createdAt` y `updatedAt` (UTC).

El listado usa **paginación por keyset** (costo constante en la BD sin importar la profundidad): `pageSize` (1–100, por defecto 20) y `afterId` (el `nextAfterId` que devolvió la página anterior; `nextAfterId: null` indica que no hay más). Con `search=` se filtra por **texto completo** sobre título y contenido (índice FULLTEXT; los términos de menos de 3 caracteres y las *stopwords* de MySQL no coinciden, y los resultados se ordenan por `id`, no por relevancia):

```bash
# Listar notas (primera página)
curl -H "X-Api-Key: local-dev.dev-secret" "http://localhost:5244/api/notes?pageSize=20"

# Página siguiente (usando nextAfterId de la respuesta anterior)
curl -H "X-Api-Key: local-dev.dev-secret" "http://localhost:5244/api/notes?pageSize=20&afterId=20"

# Buscar notas
curl -H "X-Api-Key: local-dev.dev-secret" "http://localhost:5244/api/notes?search=zanahoria"

# Crear una nota
curl -X POST http://localhost:5244/api/notes \
  -H "X-Api-Key: local-dev.dev-secret" \
  -H "Content-Type: application/json" \
  -d '{"title":"Mi nota","text":"Contenido de la nota."}'

# Actualizar la nota 1
curl -X PUT http://localhost:5244/api/notes/1 \
  -H "X-Api-Key: local-dev.dev-secret" \
  -H "Content-Type: application/json" \
  -d '{"title":"Título nuevo","text":"Contenido nuevo."}'

# Eliminar la nota 1
curl -X DELETE http://localhost:5244/api/notes/1 -H "X-Api-Key: local-dev.dev-secret"
```

También puedes usar [src/NotesApi/NotesApi.http](src/NotesApi/NotesApi.http) desde Visual Studio / VS Code (extensión REST Client).

## Pruebas

```bash
dotnet test
```

Hay dos suites:

- **Pruebas unitarias** (`tests/NotesApi.UnitTests`): rápidas y sin Docker; cubren el handler de autenticación por API key, el repositorio cacheado de keys y el controlador de notas. Se pueden correr solas con `dotnet test tests/NotesApi.UnitTests`.
- **Pruebas de integración** (`tests/NotesApi.Tests`): levantan un MySQL efímero con [Testcontainers](https://dotnet.testcontainers.org/) (requiere Docker), le aplican las migraciones reales y ejercitan la API completa: autenticación, ciclo CRUD, validaciones, paginación y health checks.

En cada push, GitHub Actions ([.github/workflows/ci.yml](.github/workflows/ci.yml)) ejecuta tres jobs en paralelo: pruebas unitarias (señal rápida, sin Docker), auditoría de dependencias vulnerables (solo restore) y compilación + pruebas de integración.

## Configuración

| Clave | Variable de entorno | Descripción |
|-------|---------------------|-------------|
| `ConnectionStrings:NotesDb` | `ConnectionStrings__NotesDb` | Cadena de conexión a MySQL. El valor de [appsettings.json](src/NotesApi/appsettings.json) apunta al contenedor local de `docker-compose.yml`. Se valida al arranque: si falta, la instancia no inicia. |
| `RateLimiting:PermitLimit` | `RateLimiting__PermitLimit` | Peticiones permitidas por ventana e IP (default 100). |
| `RateLimiting:WindowSeconds` | `RateLimiting__WindowSeconds` | Tamaño de la ventana en segundos (default 60). |
| `Authentication:KeyCacheSeconds` | `Authentication__KeyCacheSeconds` | TTL del cache en memoria de API keys (default 60). Una revocación tarda hasta este tiempo en aplicar. |
| `ForwardedHeaders:KnownProxies` | `ForwardedHeaders__KnownProxies__0`, … | IPs de los reverse proxies confiables (solo fuera de Development). |
| `ForwardedHeaders:KnownNetworks` | `ForwardedHeaders__KnownNetworks__0`, … | Redes confiables en CIDR, p. ej. `10.0.0.0/8`. |

## Base de datos

- **Mínimo privilegio:** la aplicación se conecta como `notes_app` (solo `SELECT/INSERT/UPDATE/DELETE`, sin DDL — lo aprovisiona [db/init-users.sql](db/init-users.sql)); las migraciones usan `notes_user`, que sí tiene privilegios de esquema. Un bug o inyección hipotética en la app no puede alterar tablas.
- **Fechas en UTC:** el servidor MySQL corre con `--default-time-zone=+00:00` y las columnas son `DATETIME` (sin el límite de 2038 de `TIMESTAMP`); la conversión a hora local es responsabilidad del consumidor.
- **Pool de conexiones:** la cadena de conexión fija `Maximum Pool Size=20`; con varias réplicas de la API, cuida que `réplicas × pool` quede por debajo del `max_connections` de MySQL (151 por defecto).
- **Índices:** PK en `id`, índice por `owner_key_id` (implícito en la FK) para el listado por cliente, y FULLTEXT en `(title, text)` para la búsqueda.

**Producción:** la cadena de conexión debe inyectarse desde el gestor de secretos de la plataforma (variables de entorno, Key Vault, secretos de Kubernetes) — nunca commitearse. La app asume TLS terminado en un reverse proxy (respeta `X-Forwarded-Proto` y aplica HSTS y redirección a HTTPS fuera de Development); **es obligatorio declarar el proxy en `ForwardedHeaders:KnownProxies` o `KnownNetworks`** — sin eso ASP.NET Core descarta los encabezados `X-Forwarded-*`, la redirección HTTPS entra en bucle y el rate limiting agrupa a todos los clientes bajo la IP del proxy. Las migraciones se ejecutan como paso de despliegue: `dotnet run --project src/NotesApi.Migrations -- --connection "<cs>"` (sin `--seed`). Para respaldos usa un servicio administrado (RDS / Cloud SQL / Azure Database) con point-in-time recovery, o al menos `mysqldump` programado + binlog; en local: `docker exec notes-mysql mysqldump -uroot -proot_password notes > backup.sql`.

## Estructura del proyecto

```
├── docker-compose.yml            # Stack completo: MySQL 8.4 (UTC) + migrations + api
├── db/init-users.sql             # Usuario de aplicación con mínimo privilegio
├── .github/workflows/ci.yml     # CI: 3 jobs paralelos (unitarias, auditoría, build + integración)
├── src/
│   ├── NotesApi/
│   │   ├── Auth/                 # Esquema de autenticación por API key (hash + revocación)
│   │   ├── Controllers/          # NotesController (endpoints REST)
│   │   ├── Data/                 # Repositorios MySQL (Dapper) y opciones validadas
│   │   ├── Models/               # Note, NoteRequest, NoteSummary, PagedResult
│   │   ├── Dockerfile            # Imagen de la API (build SDK -> runtime aspnet)
│   │   └── Program.cs            # Pipeline: ProblemDetails, rate limiting, auth, health
│   └── NotesApi.Migrations/      # Esquema en un único script con DbUp (Scripts/0001_schema.sql y Seeds/)
│                                 #   + Dockerfile del ejecutor de migraciones
└── tests/
    ├── NotesApi.Tests/           # Pruebas de integración (Testcontainers + WebApplicationFactory)
    └── NotesApi.UnitTests/       # Pruebas unitarias (auth, cache de keys, controlador)
```

Tecnologías: ASP.NET Core (.NET 10), [Dapper](https://github.com/DapperLib/Dapper) + [MySqlConnector](https://mysqlconnector.net/), [DbUp](https://dbup.readthedocs.io/) para migraciones, [Testcontainers](https://dotnet.testcontainers.org/) para pruebas y [Scalar](https://scalar.com/) para la documentación de la API.
