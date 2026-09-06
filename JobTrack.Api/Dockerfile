FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY JobTrack.Api/JobTrack.Api.csproj JobTrack.Api/
RUN dotnet restore JobTrack.Api/JobTrack.Api.csproj

COPY JobTrack.Api/ JobTrack.Api/
RUN dotnet publish JobTrack.Api/JobTrack.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

USER $APP_UID
ENTRYPOINT ["dotnet", "JobTrack.Api.dll"]
