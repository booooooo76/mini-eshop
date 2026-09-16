FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Reviews.Api/Reviews.Api.csproj", "src/Reviews.Api/"]
RUN dotnet restore "src/Reviews.Api/Reviews.Api.csproj"

COPY src/Reviews.Api/ src/Reviews.Api/
WORKDIR /src/src/Reviews.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Reviews.Api.dll"]
