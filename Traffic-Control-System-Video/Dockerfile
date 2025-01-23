FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
RUN apt-get update && apt-get install -y ffmpeg

USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
EXPOSE 8446
EXPOSE 1935

USER root
#RUN apt-get update && apt-get install -y wget unzip && apt-get clean
#
#RUN mkdir -p /app/ffmpegBins && chmod -R 777 /app/ffmpegBins
#
#RUN chmod -R 777 /app
#RUN wget -q -O /app/ffmpegBins/ffmpeg-6.1-linux-64.zip https://cdn.romitsagu.com/files/FFmpeg/ffmpeg-6.1-linux-64.zip
#RUN wget -q -O /app/ffmpegBins/ffprobe-6.1-linux-64.zip https://cdn.romitsagu.com/files/FFmpeg/ffprobe-6.1-linux-64.zip
#RUN unzip /app/ffmpegBins/ffmpeg-6.1-linux-64.zip -d  /app/ffmpegBins
#RUN unzip /app/ffmpegBins/ffprobe-6.1-linux-64.zip -d  /app/ffmpegBins
#RUN rm -rf /app/ffmpegBins/ffmpeg-6.1-linux-64.zip
#RUN rm -rf /app/ffmpegBins/ffprobe-6.1-linux-64.zip
#RUN chmod +x /app/ffmpegBins/ffmpeg
#RUN chmod +x /app/ffmpegBins/ffprobe

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY Traffic-Control-System-Video/Traffic-Control-System-Video.csproj Traffic-Control-System-Video/
COPY Traffic-Control-System-Video/ Traffic-Control-System-Video/

RUN dotnet restore "./Traffic-Control-System-Video/Traffic-Control-System-Video.csproj"

COPY . . 

RUN rm /src/Traffic-Control-System/appsettings.Development.json \
    /src/Traffic-Control-System/appsettings.json \
    /src/Traffic-Control-System/appsettings.Production.json

WORKDIR "/src/Traffic-Control-System-Video"
RUN dotnet build "./Traffic-Control-System-Video.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release

COPY Traffic-Control-System-Video/appsettings.json /src/Traffic-Control-System-Video/
COPY Traffic-Control-System-Video/appsettings.Development.json /src/Traffic-Control-System-Video/
COPY Traffic-Control-System-Video/appsettings.Production.json /src/Traffic-Control-System-Video/

RUN dotnet publish "./Traffic-Control-System-Video.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Traffic-Control-System-Video.dll"]