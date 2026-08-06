FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Notifications.Worker/Notifications.Worker.csproj", "Notifications.Worker/"]
COPY ["Notifications.Domain/Notifications.Domain.csproj", "Notifications.Domain/"]
COPY ["Notifications.Application/Notifications.Application.csproj", "Notifications.Application/"]
COPY ["Notifications.Infrastructure/Notifications.Infrastructure.csproj", "Notifications.Infrastructure/"]
RUN dotnet restore "Notifications.Worker/Notifications.Worker.csproj"
COPY . .
WORKDIR "/src/Notifications.Worker"
RUN dotnet build "Notifications.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Notifications.Worker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Notifications.Worker.dll"]
