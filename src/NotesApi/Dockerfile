# syntax=docker/dockerfile:1

# --- build ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restaura primero con solo los archivos de proyecto para cachear los paquetes
# NuGet. --locked-mode falla si packages.lock.json no coincide.
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/NotesApi/NotesApi.csproj src/NotesApi/packages.lock.json src/NotesApi/
RUN dotnet restore src/NotesApi/NotesApi.csproj --locked-mode

COPY src/NotesApi/ src/NotesApi/
RUN dotnet publish src/NotesApi/NotesApi.csproj -c Release -o /app --no-restore

# --- runtime ---
# Imagen chiseled: sin shell ni gestor de paquetes. Kestrel escucha HTTP en
# 8080 por defecto (TLS se termina en el reverse proxy).
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS runtime
WORKDIR /app
EXPOSE 8080

COPY --from=build /app ./

# APP_UID viene definido en la imagen: ejecuta como usuario sin privilegios.
USER $APP_UID
ENTRYPOINT ["dotnet", "NotesApi.dll"]
