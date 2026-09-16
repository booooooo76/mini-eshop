FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Notifications.Api/Notifications.Api.csproj", "src/Notifications.Api/"]
COPY ["src/Shared.Contracts/Shared.Contracts.csproj", "src/Shared.Contracts/"]
RUN dotnet restore "src/Notifications.Api/Notifications.Api.csproj"

COPY src/Notifications.Api/ src/Notifications.Api/
COPY src/Shared.Contracts/ src/Shared.Contracts/
WORKDIR /src/src/Notifications.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Notifications.Api.dll"]
