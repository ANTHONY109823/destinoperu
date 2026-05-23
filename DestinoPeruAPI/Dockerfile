# ============================================================
# Dockerfile para DestinoPeruAPI — .NET 10
# Railway construye desde dentro de DestinoPeruAPI (Root Directory)
# ============================================================

# Etapa 1: compilar la aplicacion
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar el proyecto y restaurar paquetes
COPY *.csproj ./
RUN dotnet restore

# Copiar el resto del codigo y publicar
COPY . ./
RUN dotnet publish -c Release -o /app/publish --no-restore

# Etapa 2: imagen final liviana (solo runtime)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

# Puerto y ajustes para Railway
ENV ASPNETCORE_URLS=http://+:8080
ENV DisableHttpsRedirection=true
EXPOSE 8080

ENTRYPOINT ["dotnet", "DestinoPeruAPI.dll"]