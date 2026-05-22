FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["DestinoPeruAPI/DestinoPeruAPI.csproj", "DestinoPeruAPI/"]
RUN dotnet restore "DestinoPeruAPI/DestinoPeruAPI.csproj"
COPY DestinoPeruAPI/ DestinoPeruAPI/
RUN dotnet publish "DestinoPeruAPI/DestinoPeruAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
ENV DisableHttpsRedirection=true
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "DestinoPeruAPI.dll"]
