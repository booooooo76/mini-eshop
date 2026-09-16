FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Gateway/Gateway.csproj", "src/Gateway/"]
RUN dotnet restore "src/Gateway/Gateway.csproj"

COPY src/Gateway/ src/Gateway/
WORKDIR /src/src/Gateway
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Gateway.dll"]
