FROM mcr.microsoft.com/dotnet/sdk:6.0 as build

ARG GITHUB_PACKAGE_REGISTRY_USERNAME
ARG GITHUB_PACKAGE_REGISTRY_PASSWORD
ARG GITHUB_PACKAGE_REGISTRY_ORG_NAME

WORKDIR /src
COPY src/App/NetObsStatsGenerator.csproj .
COPY src/App/nuget.config .
RUN dotnet restore

COPY src/App .
RUN dotnet publish -c release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:6.0 as app
WORKDIR /app
COPY /src/App/docker-healthcheck.sh /app/
COPY --from=build /app .

ENV ASPNETCORE_URLS "http://*:5000"
HEALTHCHECK CMD ["/app/docker-healthcheck.sh"]
CMD ["dotnet", "App.dll"]
