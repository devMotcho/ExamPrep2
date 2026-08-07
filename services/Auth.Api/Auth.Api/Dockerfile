FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy only csproj files first, for better layer caching on rebuilds
COPY Auth.Api/Auth.Api.sln Auth.Api/
COPY Auth.Api/Auth.Api/Auth.Api.csproj Auth.Api/Auth.Api/
COPY Auth.Api/Auth.Domain/Auth.Domain.csproj Auth.Api/Auth.Domain/
COPY Auth.Api/Auth.Application/Auth.Application.csproj Auth.Api/Auth.Application/
COPY Auth.Api/Auth.Infrastructure/Auth.Infrastructure.csproj Auth.Api/Auth.Infrastructure/
COPY Auth.Api/Auth.Api.Tests/Auth.Api.Tests.csproj Auth.Api/Auth.Api.Tests/
COPY Shared/ExamPrep.Shared/ExamPrep.Shared.csproj Shared/ExamPrep.Shared/
RUN dotnet restore Auth.Api/Auth.Api.sln

COPY . .
RUN dotnet publish Auth.Api/Auth.Api/Auth.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Auth.Api.dll"]