FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Notifications/Notifications.Worker/Notifications.Worker.csproj", "Notifications/Notifications.Worker/"]
COPY ["Notifications/Notifications.Domain/Notifications.Domain.csproj", "Notifications/Notifications.Domain/"]
COPY ["Notifications/Notifications.Application/Notifications.Application.csproj", "Notifications/Notifications.Application/"]
COPY ["Notifications/Notifications.Infrastructure/Notifications.Infrastructure.csproj", "Notifications/Notifications.Infrastructure/"]
COPY ["Shared/ExamPrep.Shared/ExamPrep.Shared.csproj", "Shared/ExamPrep.Shared/"]
RUN dotnet restore "Notifications/Notifications.Worker/Notifications.Worker.csproj"
COPY . .
WORKDIR "/src/Notifications/Notifications.Worker"
RUN dotnet build "Notifications.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Notifications.Worker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Notifications.Worker.dll"]
