# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY CasaMulher.Api/CasaMulher.Api.csproj CasaMulher.Api/
RUN dotnet restore CasaMulher.Api/CasaMulher.Api.csproj

COPY . .
RUN dotnet publish CasaMulher.Api/CasaMulher.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Staging
EXPOSE 8080

ENTRYPOINT ["sh", "-c", "dotnet CasaMulher.Api.dll --urls http://0.0.0.0:${PORT:-8080}"]
