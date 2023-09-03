#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["ConcertBuddy.ConsoleApp/ConcertBuddy.ConsoleApp.csproj", "ConcertBuddy.ConsoleApp/"]
COPY ["SetlistFmAPI/SetlistFmAPI.csproj", "SetlistFmAPI/"]
COPY ["MusicSearcher/MusicSearcher.csproj", "MusicSearcher/"]
RUN dotnet restore "ConcertBuddy.ConsoleApp/ConcertBuddy.ConsoleApp.csproj"
COPY . .
WORKDIR "/src/ConcertBuddy.ConsoleApp"
RUN dotnet build "ConcertBuddy.ConsoleApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ConcertBuddy.ConsoleApp.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ConcertBuddy.ConsoleApp.dll"]