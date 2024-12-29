FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
WORKDIR /app

ENV \    
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \    
    LC_ALL=en_US.UTF-8 \    
    LANG=en_US.UTF-8
    RUN apk add --no-cache \    
    icu-data-full \    
    icu-libs

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build

ARG TARGETPLATFORM
ARG TARGETARCH
ARG BUILD_CONFIGURATION=Release

WORKDIR /src

# Copy solution and project files first
COPY ["*.props", "./"]
COPY ["nuget.config", "./"]
COPY ["OpenWish.Web/OpenWish.Web.csproj", "OpenWish.Web/"]
COPY ["OpenWish.Web.Client/OpenWish.Web.Client.csproj", "OpenWish.Web.Client/"]
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

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OpenWish.Web.dll"]
