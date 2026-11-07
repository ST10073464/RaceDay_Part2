
# ==========================================
# RaceDay API - Dockerfile
# .NET 8
# ==========================================

# Stage 1: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

WORKDIR /app

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080


# Stage 2: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

ARG BUILD_CONFIGURATION=Release

WORKDIR /src

COPY ["RaceDay.API/RaceDay.API.csproj", "RaceDay.API/"]
COPY ["RaceDay.Contracts/RaceDay.Contracts.csproj", "RaceDay.Contracts/"]

RUN dotnet restore "RaceDay.API/RaceDay.API.csproj"

COPY . .

WORKDIR /src/RaceDay.API

RUN dotnet build "RaceDay.API.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/build


# Stage 3: Publish
FROM build AS publish

ARG BUILD_CONFIGURATION=Release

RUN dotnet publish "RaceDay.API.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false


# Stage 4: Final image
FROM base AS final

WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "RaceDay.API.dll"]
