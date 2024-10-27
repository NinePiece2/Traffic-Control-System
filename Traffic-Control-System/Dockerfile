#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8443

USER root
RUN mkdir -p /app/wwwroot/hls && chmod -R 777 /app/wwwroot/hls
RUN mkdir -p /app/ffmpegBins && chmod -R 777 /app/ffmpegBins

RUN chmod -R 777 /app
RUN wget -O /app/ffmpegBins/ffmpeg https://cdn.romitsagu.com/files/FFmpeg/ffmpeg 
RUN chmod +x /app/ffmpegBins/ffmpeg


USER app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Traffic-Control-System/Traffic-Control-System.csproj", "Traffic-Control-System/"]
RUN dotnet restore "./Traffic-Control-System/Traffic-Control-System.csproj"
COPY . .
WORKDIR "/src/Traffic-Control-System"
RUN dotnet build "./Traffic-Control-System.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Traffic-Control-System.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Traffic-Control-System.dll"]