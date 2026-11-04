FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY OficinaBrasil.Domain/OficinaBrasil.Domain.csproj OficinaBrasil.Domain/
COPY OficinaBrasil.Application/OficinaBrasil.Application.csproj OficinaBrasil.Application/
COPY OficinaBrasil.Infrastructure/OficinaBrasil.Infrastructure.csproj OficinaBrasil.Infrastructure/
COPY OficinaBrasil.API/OficinaBrasil.API.csproj OficinaBrasil.API/
RUN dotnet restore OficinaBrasil.API/OficinaBrasil.API.csproj
COPY . .
RUN dotnet publish OficinaBrasil.API/OficinaBrasil.API.csproj -c Release -o /out --no-restore
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "OficinaBrasil.API.dll"]
