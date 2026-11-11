# syntax=docker/dockerfile:1

# --- build ---
FROM mcr.microsoft.com/dotnet/sdk:11.0 AS build
WORKDIR /src

# Restaura primero con solo los archivos de proyecto para cachear los paquetes
# NuGet. --locked-mode falla si packages.lock.json no coincide.
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/NotesApi.Migrations/NotesApi.Migrations.csproj src/NotesApi.Migrations/packages.lock.json src/NotesApi.Migrations/
RUN dotnet restore src/NotesApi.Migrations/NotesApi.Migrations.csproj --locked-mode

COPY src/NotesApi.Migrations/ src/NotesApi.Migrations/
RUN dotnet publish src/NotesApi.Migrations/NotesApi.Migrations.csproj -c Release -o /app --no-restore

# --- runtime ---
# App de consola que aplica las migraciones y termina: basta con el runtime
# base, en su variante chiseled (sin shell ni gestor de paquetes).
FROM mcr.microsoft.com/dotnet/runtime:10.0-noble-chiseled AS runtime
WORKDIR /app

COPY --from=build /app ./

USER $APP_UID
ENTRYPOINT ["dotnet", "NotesApi.Migrations.dll"]
