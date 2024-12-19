FROM --platform=$TARGETPLATFORM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
WORKDIR /app
EXPOSE 80

FROM --platform=$TARGETPLATFORM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
ARG TARGETPLATFORM
ARG TARGETOS
ARG TARGETARCH
ARG TARGETVARIANT
ARG BUILDPLATFORM
ARG BUILDOS
ARG BUILDARCH
ARG BUILDVARIANT

RUN echo "Building on $BUILDPLATFORM, targeting $TARGETPLATFORM"
RUN echo "Building on ${BUILDOS} and ${BUILDARCH} with optional variant ${BUILDVARIANT}"
RUN echo "Targeting ${TARGETOS} and ${TARGETARCH} with optional variant ${TARGETVARIANT}"

WORKDIR /src

# Copy solution and project files first
COPY ["*.props", "./"]
COPY ["nuget.config", "./"]
COPY ["OpenWish.Web/OpenWish.Web.csproj", "OpenWish.Web/"]
COPY ["OpenWish.Common/OpenWish.Common.csproj", "OpenWish.Common/"]
COPY ["OpenWish.Data/OpenWish.Data.csproj", "OpenWish.Data/"]
RUN dotnet restore "OpenWish.Web/OpenWish.Web.csproj"

# Copy the rest of the code and publish
COPY . .
RUN dotnet publish "OpenWish.Web/OpenWish.Web.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false \
    --runtime $TARGETPLATFORM \
    --self-contained false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OpenWish.Web.dll"]
