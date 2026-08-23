###############################################################################
# 1. BUILD STAGE – compile & publish the Lus API
###############################################################################
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# project files first (better layer-cache for restore)
COPY Lus.Api/Lus.Api.csproj                         Lus.Api/
COPY Lus.Application/Lus.Application.csproj          Lus.Application/
COPY Lus.Contracts/Lus.Contracts.csproj             Lus.Contracts/
COPY Lus.Infrastructure/Lus.Infrastructure.csproj   Lus.Infrastructure/
COPY Lus.Authorization/Lus.Authorization.csproj     Lus.Authorization/
COPY Lus.NotificationCenter/Lus.NotificationCenter.csproj Lus.NotificationCenter/
COPY tools/Lus.FilterEngine/Lus.FilterEngine.csproj   tools/Lus.FilterEngine/

# restore only the API project (pulls in transitive project deps)
RUN dotnet restore Lus.Api/Lus.Api.csproj

# copy the rest of the sources & publish
COPY . .
RUN dotnet publish \
      Lus.Api/Lus.Api.csproj \
      -c Release \
      -o /app/publish \
      /p:UseAppHost=false \
      -v minimal

###############################################################################
# 2. RUNTIME STAGE – ASP.NET Core 9 + native libs needed for PDF generation
###############################################################################
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# itext7 / GhostScript.NetCore / Select.HtmlToPdf need these native libraries.
# python3 is required for the Document Builder agent runtime (P0). Prefer the
# root Dockerfile (compose context: ..) which also COPY PythonScripts/.
RUN set -eux; \
    for i in 1 2 3; do apt-get update && break || (echo "apt-get update retry ($i)" && sleep 2); done; \
    apt-get install -y --no-install-recommends \
        libgdiplus \
        libc6-dev \
        ghostscript \
        fontconfig \
        libfontconfig1 \
        python3 \
        python3-pip \
    && ln -sf /usr/bin/python3 /usr/bin/python \
    && rm -rf /var/lib/apt/lists/*

# copy published .NET app
COPY --from=build /app/publish .

###############################################################################
# 3. ENVIRONMENT (override with Railway service variables)
###############################################################################
ENV ASPNETCORE_ENVIRONMENT=Production

# Default DB connection – OVERRIDE in Railway with ConnectionStrings__DefaultConnection
# (Railway MySQL internal URL: mysql.railway.internal:3306)
ENV ConnectionStrings__DefaultConnection="Server=mysql.railway.internal;Port=3306;Database=railway;User=root;Password=change_me;SslMode=None;AllowPublicKeyRetrieval=true;"

# Cookie / CORS domain
ENV Auth__Cookie__Domain=".shiftiz.com"
ENV Cors__Origins="https://shiftiz.com,https://www.shiftiz.com,https://app.shiftiz.com,https://api.shiftiz.com"

# Railway provides $PORT; the entrypoint binds Kestrel to it.
EXPOSE 8080

COPY docker-entrypoint.sh /docker-entrypoint.sh
RUN chmod +x /docker-entrypoint.sh
ENTRYPOINT ["/docker-entrypoint.sh"]
