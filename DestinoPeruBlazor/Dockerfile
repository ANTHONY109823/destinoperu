# DestinoPeru Blazor WASM — sitio estatico en Railway (nginx)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY DestinoPeruBlazor.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o /app/publish

FROM nginx:alpine AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
COPY docker-entrypoint.sh /docker-entrypoint.sh
RUN chmod +x /docker-entrypoint.sh

ENV ApiBaseUrl=https://destinoperu-production.up.railway.app/
EXPOSE 8080

ENTRYPOINT ["/docker-entrypoint.sh"]
