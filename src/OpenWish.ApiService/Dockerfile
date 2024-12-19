FROM --platform=$TARGETPLATFORM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
WORKDIR /app
EXPOSE 80

FROM --platform=$TARGETPLATFORM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
ARG TARGETPLATFORM
WORKDIR /src

# Set architecture-specific variables
ENV DOTNET_RUNTIME_IDENTIFIER=${TARGETPLATFORM:+${TARGETPLATFORM/linux\//linux-}} \
    DOTNET_RUNTIME_IDENTIFIER=${DOTNET_RUNTIME_IDENTIFIER/arm64/arm64} \
    DOTNET_RUNTIME_IDENTIFIER=${DOTNET_RUNTIME_IDENTIFIER/amd64/x64}

# Copy solution and project files first
COPY ["*.props", "./"]
COPY ["nuget.config", "./"]
COPY ["OpenWish.ApiService/OpenWish.ApiService.csproj", "OpenWish.ApiService/"]
COPY ["OpenWish.Common/OpenWish.Common.csproj", "OpenWish.Common/"]
COPY ["OpenWish.Data/OpenWish.Data.csproj", "OpenWish.Data/"]
RUN dotnet restore "OpenWish.ApiService/OpenWish.ApiService.csproj"

# Copy the rest of the code and publish
COPY . .
RUN dotnet publish "OpenWish.ApiService/OpenWish.ApiService.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false \
    --runtime $DOTNET_RUNTIME_IDENTIFIER \
    --self-contained false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OpenWish.ApiService.dll"]
