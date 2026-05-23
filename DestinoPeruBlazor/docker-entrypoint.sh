#!/bin/sh
set -e

API_BASE="${ApiBaseUrl:-https://destinoperu-production.up.railway.app/}"

cat > /usr/share/nginx/html/appsettings.Production.json << EOF
{
  "ApiBaseUrl": "${API_BASE}"
}
EOF

echo "Blazor ApiBaseUrl configurado: ${API_BASE}"
exec nginx -g 'daemon off;'
