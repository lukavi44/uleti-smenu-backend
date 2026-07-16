FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish API/API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://0.0.0.0:10000
# Avoid inotify instance limit on Render during configuration file watching.
ENV DOTNET_USE_POLLING_FILE_WATCHER=1
EXPOSE 10000
ENTRYPOINT ["dotnet", "API.dll"]
