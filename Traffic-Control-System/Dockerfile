
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8443

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