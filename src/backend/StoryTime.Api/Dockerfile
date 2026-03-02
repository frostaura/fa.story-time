FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/backend/StoryTime.Api/StoryTime.Api.csproj StoryTime.Api/
RUN dotnet restore StoryTime.Api/StoryTime.Api.csproj
COPY src/backend/StoryTime.Api/. StoryTime.Api/
WORKDIR /src/StoryTime.Api
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "StoryTime.Api.dll"]
