FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/OficinaBrasil.Domain/OficinaBrasil.Domain.csproj src/OficinaBrasil.Domain/
COPY src/OficinaBrasil.Application/OficinaBrasil.Application.csproj src/OficinaBrasil.Application/
COPY src/OficinaBrasil.Infrastructure/OficinaBrasil.Infrastructure.csproj src/OficinaBrasil.Infrastructure/
COPY src/OficinaBrasil.API/OficinaBrasil.API.csproj src/OficinaBrasil.API/
RUN dotnet restore src/OficinaBrasil.API/OficinaBrasil.API.csproj
COPY . .
RUN dotnet publish src/OficinaBrasil.API/OficinaBrasil.API.csproj -c Release -o /out --no-restore
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "OficinaBrasil.API.dll"]
