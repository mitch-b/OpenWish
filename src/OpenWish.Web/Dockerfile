FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/aspnet:9.0-noble-chiseled-extra AS base
WORKDIR /app

ENV \    
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \    
    LC_ALL=en_US.UTF-8 \    
    LANG=en_US.UTF-8

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0-noble AS build

ARG TARGETPLATFORM
ARG TARGETARCH
ARG BUILD_CONFIGURATION=Release
ARG BUILD_VERSION=UNKNOWN
ARG GIT_SHA=UNKNOWN

WORKDIR /src

# Copy solution and project files first
COPY ["*.props", "./"]
COPY ["nuget.config", "./"]
COPY ["OpenWish.Web/OpenWish.Web.csproj", "OpenWish.Web/"]
COPY ["OpenWish.Web.Client/OpenWish.Web.Client.csproj", "OpenWish.Web.Client/"]
COPY ["OpenWish.Shared/OpenWish.Shared.csproj", "OpenWish.Shared/"]
COPY ["OpenWish.Application/OpenWish.Application.csproj", "OpenWish.Application/"]
COPY ["OpenWish.Data/OpenWish.Data.csproj", "OpenWish.Data/"]
RUN dotnet restore "OpenWish.Web/OpenWish.Web.csproj"

# Copy the rest of the code and publish
COPY . .
RUN dotnet publish "OpenWish.Web/OpenWish.Web.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false \
    --arch $TARGETARCH 
    # --self-contained false

RUN echo "${BUILD_VERSION}.${GIT_SHA}" > /app/version.txt

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=build /app/version.txt .

# Entrypoint script to set APP_VERSION, then start the app
RUN echo '#!/bin/sh' > /entrypoint.sh && \
    echo 'export APP_VERSION=$(cat /app/version.txt)' >> /entrypoint.sh && \
    echo 'echo "App version: $APP_VERSION"' >> /entrypoint.sh && \
    echo 'exec dotnet OpenWish.Web.dll' >> /entrypoint.sh && \
    chmod +x /entrypoint.sh

ENTRYPOINT ["/entrypoint.sh"]
