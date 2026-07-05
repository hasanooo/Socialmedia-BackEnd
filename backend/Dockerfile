FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY backend/ApplifyLab.sln .
COPY backend/src/ApplifyLab.Api/ApplifyLab.Api.csproj src/ApplifyLab.Api/
COPY backend/src/ApplifyLab.Application/ApplifyLab.Application.csproj src/ApplifyLab.Application/
COPY backend/src/ApplifyLab.Domain/ApplifyLab.Domain.csproj src/ApplifyLab.Domain/
COPY backend/src/ApplifyLab.Infrastructure/ApplifyLab.Infrastructure.csproj src/ApplifyLab.Infrastructure/
COPY backend/tests/ApplifyLab.Tests/ApplifyLab.Tests.csproj tests/ApplifyLab.Tests/
RUN dotnet restore src/ApplifyLab.Api/ApplifyLab.Api.csproj

COPY backend/. .
RUN dotnet publish src/ApplifyLab.Api/ApplifyLab.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "ApplifyLab.Api.dll"]
