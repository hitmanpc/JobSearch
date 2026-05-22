FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/JobSearch.sln backend/
COPY backend/JobSearch.Api/JobSearch.Api.csproj backend/JobSearch.Api/
COPY backend/JobSearch.Application/JobSearch.Application.csproj backend/JobSearch.Application/
COPY backend/JobSearch.Domain/JobSearch.Domain.csproj backend/JobSearch.Domain/
COPY backend/JobSearch.Tests/JobSearch.Tests.csproj backend/JobSearch.Tests/
RUN dotnet restore backend/JobSearch.Api/JobSearch.Api.csproj

COPY backend/ backend/
RUN dotnet publish backend/JobSearch.Api/JobSearch.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "JobSearch.Api.dll"]
