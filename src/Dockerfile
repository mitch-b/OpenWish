# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY OpenWish/*.csproj OpenWish/
COPY OpenWish.Client/*.csproj OpenWish.Client/
RUN dotnet restore OpenWish/OpenWish.csproj

# copy and build app and libraries
COPY OpenWish/ OpenWish/
COPY OpenWish.Client/ OpenWish.Client/

# test stage -- exposes optional entrypoint
# target entrypoint with: docker build --target test
##################
# FROM build AS test
# 
# COPY OpenWish.Tests/*.csproj OpenWish.Tests/
# WORKDIR /source/OpenWish.Tests
# RUN dotnet restore
# 
# COPY OpenWish.Tests/ .
# RUN dotnet build --no-restore
# 
# ENTRYPOINT ["dotnet", "test", "--logger:trx", "--no-build"]
##################

FROM build AS publish
WORKDIR /source/OpenWish
RUN dotnet publish --no-restore -o /app

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "OpenWish.dll"]
