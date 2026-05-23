# ============================================================
# Dockerfile para DestinoPeruAPI
# Railway construye desde dentro de la carpeta DestinoPeruAPI
# (Root Directory = DestinoPeruAPI), por eso se copia con ./
# ============================================================

# Etapa 1: compilar la aplicacion
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar el archivo de proyecto y restaurar paquetes
COPY *.csproj ./
RUN dotnet restore

# Copiar el resto del codigo y publicar
COPY . ./
RUN dotnet publish -c Release -o /app/publish --no-restore

# Etapa 2: imagen final liviana (solo runtime)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

# Puerto que Railway espera
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "DestinoPeruAPI.dll"]