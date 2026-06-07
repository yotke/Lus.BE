###############################################################################
# Root Dockerfile for Railway (build context = repo root).
# The .NET solution lives under src/. This lets Railway auto-detect the build
# at the repository root with NO "Root Directory" setting required.
# (src/Dockerfile still exists for docker-compose, which uses context: ./src)
###############################################################################
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# project files first (better layer-cache for restore)
COPY src/Lus.Api/Lus.Api.csproj                             src/Lus.Api/
COPY src/Lus.Application/Lus.Application.csproj             src/Lus.Application/
COPY src/Lus.Contracts/Lus.Contracts.csproj                src/Lus.Contracts/
COPY src/Lus.Infrastructure/Lus.Infrastructure.csproj      src/Lus.Infrastructure/
COPY src/Lus.Authorization/Lus.Authorization.csproj        src/Lus.Authorization/
COPY src/Lus.NotificationCenter/Lus.NotificationCenter.csproj src/Lus.NotificationCenter/
COPY src/tools/Lus.FilterEngine/Lus.FilterEngine.csproj    src/tools/Lus.FilterEngine/

RUN dotnet restore src/Lus.Api/Lus.Api.csproj

# copy the rest of the sources & publish
COPY . .
RUN dotnet publish src/Lus.Api/Lus.Api.csproj \
      -c Release \
      -o /publish \
      /p:UseAppHost=false \
      -v minimal

###############################################################################
# Runtime – ASP.NET Core 9 + native libs needed for PDF generation
###############################################################################
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

RUN set -eux; \
    for i in 1 2 3; do apt-get update && break || (echo "apt-get update retry ($i)" && sleep 2); done; \
    apt-get install -y --no-install-recommends \
        libgdiplus \
        libc6-dev \
        ghostscript \
        fontconfig \
        libfontconfig1 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /publish .

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

# Railway provides $PORT; bind Kestrel to it.
ENTRYPOINT ["sh", "-c", "dotnet Lus.Api.dll --urls http://+:${PORT:-8080}"]
