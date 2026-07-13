# syntax=docker/dockerfile:1

# --- build ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restaura primero con solo el .csproj para cachear los paquetes NuGet.
COPY src/NotesApi.Migrations/NotesApi.Migrations.csproj src/NotesApi.Migrations/
RUN dotnet restore src/NotesApi.Migrations/NotesApi.Migrations.csproj

COPY src/NotesApi.Migrations/ src/NotesApi.Migrations/
RUN dotnet publish src/NotesApi.Migrations/NotesApi.Migrations.csproj -c Release -o /app --no-restore

# --- runtime ---
# App de consola que aplica las migraciones y termina; basta con el runtime base.
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app

COPY --from=build /app ./

USER $APP_UID
ENTRYPOINT ["dotnet", "NotesApi.Migrations.dll"]
