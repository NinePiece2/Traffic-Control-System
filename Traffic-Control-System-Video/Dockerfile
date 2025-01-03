FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
RUN apt-get update && apt-get install -y ffmpeg
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
EXPOSE 1935


FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY Traffic-Control-System-Video/Traffic-Control-System-Video.csproj Traffic-Control-System-Video/
COPY Traffic-Control-System-Video/ Traffic-Control-System-Video/

RUN dotnet restore "./Traffic-Control-System-Video/Traffic-Control-System-Video.csproj"

COPY . . 

WORKDIR "/src/Traffic-Control-System-Video"
RUN dotnet build "./Traffic-Control-System-Video.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
COPY Traffic-Control-System-Video/appsettings.* /src/Traffic-Control-System-Video/
RUN dotnet publish "./Traffic-Control-System-Video.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Traffic-Control-System-Video.dll"]