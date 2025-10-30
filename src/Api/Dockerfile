FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY DataSenseAPI.sln ./
COPY src/Api/DataSense.Api.csproj src/Api/
COPY src/Application/DataSense.Application.csproj src/Application/
COPY src/Domain/DataSense.Domain.csproj src/Domain/
COPY src/Infrastructure/DataSense.Infrastructure.csproj src/Infrastructure/

RUN dotnet restore DataSenseAPI.sln

COPY . .

RUN dotnet publish src/Api/DataSense.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

ENTRYPOINT ["dotnet", "DataSenseAPI.dll"]
