# syntax=docker/dockerfile:1

# --- build ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restaura primero con solo el .csproj para cachear los paquetes NuGet.
COPY src/NotesApi/NotesApi.csproj src/NotesApi/
RUN dotnet restore src/NotesApi/NotesApi.csproj

COPY src/NotesApi/ src/NotesApi/
RUN dotnet publish src/NotesApi/NotesApi.csproj -c Release -o /app --no-restore

# --- runtime ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Kestrel escucha HTTP en 8080 (TLS se termina en el reverse proxy).
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app ./

# APP_UID viene definido en la imagen aspnet: ejecuta como usuario sin privilegios.
USER $APP_UID
ENTRYPOINT ["dotnet", "NotesApi.dll"]
