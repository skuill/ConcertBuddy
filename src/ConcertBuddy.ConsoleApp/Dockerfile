#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
USER app
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ConcertBuddy.ConsoleApp/ConcertBuddy.ConsoleApp.csproj", "ConcertBuddy.ConsoleApp/"]
COPY ["MusicSearcher/MusicSearcher.csproj", "MusicSearcher/"]
RUN dotnet restore "./ConcertBuddy.ConsoleApp/ConcertBuddy.ConsoleApp.csproj"
COPY . .
WORKDIR "/src/ConcertBuddy.ConsoleApp"
RUN dotnet build "./ConcertBuddy.ConsoleApp.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ConcertBuddy.ConsoleApp.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ConcertBuddy.ConsoleApp.dll"]