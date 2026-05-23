FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY Vittal.sln .
COPY src/Vittal.API/Vittal.API.csproj src/Vittal.API/
COPY src/Vittal.Aplicacion/Vittal.Aplicacion.csproj src/Vittal.Aplicacion/
COPY src/Vittal.BLL/Vittal.BLL.csproj src/Vittal.BLL/
COPY src/Vittal.DAL/Vittal.DAL.csproj src/Vittal.DAL/
COPY src/Vittal.DTO/Vittal.DTO.csproj src/Vittal.DTO/
COPY src/Vittal.Entity/Vittal.Entity.csproj src/Vittal.Entity/
COPY src/Vittal.IOC/Vittal.IOC.csproj src/Vittal.IOC/
COPY src/Vittal.Utility/Vittal.Utility.csproj src/Vittal.Utility/
COPY tests/Vittal.BLL.Tests/Vittal.BLL.Tests.csproj tests/Vittal.BLL.Tests/
COPY tests/Vittal.API.Tests/Vittal.API.Tests.csproj tests/Vittal.API.Tests/
RUN dotnet restore

COPY . .
RUN dotnet publish src/Vittal.API/Vittal.API.csproj -c Release -o /app/out/api
RUN dotnet publish src/Vittal.Aplicacion/Vittal.Aplicacion.csproj -c Release -o /app/out/web

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS api
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/out/api .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Vittal.API.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS web
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/out/web .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Vittal.Aplicacion.dll"]
