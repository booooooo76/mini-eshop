FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Gateway/Gateway.csproj", "src/Gateway/"]
COPY ["src/Shared.Observability/Shared.Observability.csproj", "src/Shared.Observability/"]
RUN dotnet restore "src/Gateway/Gateway.csproj"

COPY src/Gateway/ src/Gateway/
COPY src/Shared.Observability/ src/Shared.Observability/
WORKDIR /src/src/Gateway
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ARG APP_VERSION=dev
ENV APP_VERSION=$APP_VERSION
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Gateway.dll"]
