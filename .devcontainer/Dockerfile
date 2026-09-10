FROM mcr.microsoft.com/devcontainers/dotnet:1-10.0-noble

# The base image's Yarn signing key expired. Feature installation runs apt-get update,
# so remove the unused source rather than allowing it to break container creation.
RUN rm -f /etc/apt/sources.list.d/yarn.list
