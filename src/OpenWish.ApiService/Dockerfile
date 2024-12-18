FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src
# Copy solution and project files first
COPY ["OpenWish.ApiService/OpenWish.ApiService.csproj", "OpenWish.ApiService/"]
COPY ["OpenWish.Common/OpenWish.Common.csproj", "OpenWish.Common/"]
COPY ["OpenWish.Data/OpenWish.Data.csproj", "OpenWish.Data/"]
RUN dotnet restore "OpenWish.ApiService/OpenWish.ApiService.csproj"
# Copy the rest of the code
COPY . .
RUN dotnet publish "OpenWish.ApiService/OpenWish.ApiService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OpenWish.ApiService.dll"]