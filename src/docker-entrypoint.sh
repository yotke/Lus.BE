#!/bin/sh
set -e

# Railway injects $PORT. Fall back to 8080 locally.
PORT=${PORT:-8080}

echo "Lus.Api starting → binding Kestrel to http://+:${PORT} (env=${ASPNETCORE_ENVIRONMENT})"

exec dotnet Lus.Api.dll --urls "http://+:${PORT}"
