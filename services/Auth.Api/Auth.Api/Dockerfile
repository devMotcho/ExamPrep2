FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy only csproj files first, for better layer caching on rebuilds
COPY Auth.Api.sln .
COPY Auth.Api/Auth.Api.csproj Auth.Api/
COPY Auth.Domain/Auth.Domain.csproj Auth.Domain/
COPY Auth.Application/Auth.Application.csproj Auth.Application/
COPY Auth.Infrastructure/Auth.Infrastructure.csproj Auth.Infrastructure/
COPY Auth.Api.Tests/Auth.Api.Tests.csproj Auth.Api.Tests/
RUN dotnet restore Auth.Api.sln

COPY . .
RUN dotnet publish Auth.Api/Auth.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Auth.Api.dll"]