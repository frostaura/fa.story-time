# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY src/backend/StoryTime.Api/StoryTime.Api.csproj src/backend/StoryTime.Api/
RUN dotnet restore src/backend/StoryTime.Api/StoryTime.Api.csproj

# Copy everything else and build
COPY src/backend/StoryTime.Api/ src/backend/StoryTime.Api/
WORKDIR /src/src/backend/StoryTime.Api
RUN dotnet build StoryTime.Api.csproj -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish StoryTime.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 5000

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

# Copy published files
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "StoryTime.Api.dll"]
