FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY OstrunCrawlerService.sln .
COPY src/OstrunCrawlerService.Api/*.csproj src/OstrunCrawlerService.Api/
RUN dotnet restore src/OstrunCrawlerService.Api/OstrunCrawlerService.Api.csproj

COPY src/ src/
RUN dotnet publish src/OstrunCrawlerService.Api/OstrunCrawlerService.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
RUN useradd --uid 5678 --user-group --no-create-home appuser
COPY --from=build /app/publish .
USER appuser

EXPOSE 8082
ENV ASPNETCORE_URLS=http://+:8082

ENTRYPOINT ["dotnet", "OstrunCrawlerService.Api.dll"]
