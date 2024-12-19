ARG TARGETPLATFORM
FROM --platform=$TARGETPLATFORM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
WORKDIR /app
EXPOSE 80

FROM --platform=$TARGETPLATFORM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
ARG TARGETPLATFORM
WORKDIR /src

# Set architecture-specific variables
RUN if [ "$TARGETPLATFORM" = "linux/amd64" ]; then \
    export DOTNET_RUNTIME_IDENTIFIER=linux-x64 ; \
    elif [ "$TARGETPLATFORM" = "linux/arm64" ]; then \
    export DOTNET_RUNTIME_IDENTIFIER=linux-arm64 ; \
    fi

# Copy solution and project files first
COPY ["*.props", "./"]
COPY ["nuget.config", "./"]
COPY ["OpenWish.Web/OpenWish.Web.csproj", "OpenWish.Web/"]
COPY ["OpenWish.Common/OpenWish.Common.csproj", "OpenWish.Common/"]
COPY ["OpenWish.Data/OpenWish.Data.csproj", "OpenWish.Data/"]
RUN dotnet restore "OpenWish.Web/OpenWish.Web.csproj"

# Copy the rest of the code
COPY . .
RUN dotnet publish "OpenWish.Web/OpenWish.Web.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false \
    --runtime $DOTNET_RUNTIME_IDENTIFIER \
    --self-contained false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OpenWish.Web.dll"]