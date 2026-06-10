# Base image for production
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["TeknikServisAPI/TeknikServisAPI.csproj", "TeknikServisAPI/"]
RUN dotnet restore "TeknikServisAPI/TeknikServisAPI.csproj"
COPY . .
WORKDIR "/src/TeknikServisAPI"
RUN dotnet build "TeknikServisAPI.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "TeknikServisAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TeknikServisAPI.dll"]
