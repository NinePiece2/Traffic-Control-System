# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY Traffic-Control-System-API/Traffic-Control-System-API.csproj Traffic-Control-System-API/
COPY Traffic-Control-System-API/ Traffic-Control-System-API/

RUN dotnet restore "./Traffic-Control-System-API/Traffic-Control-System-API.csproj"

COPY . . 

WORKDIR "/src/Traffic-Control-System-API"
RUN dotnet build "./Traffic-Control-System-API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Traffic-Control-System-API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Traffic-Control-System-API.dll"]
