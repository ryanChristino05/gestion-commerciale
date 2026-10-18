FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY GestionCommerciale.Api/GestionCommerciale.Api.csproj GestionCommerciale.Api/
COPY GestionCommerciale.Tests/GestionCommerciale.Tests.csproj GestionCommerciale.Tests/

RUN dotnet restore GestionCommerciale.Api/GestionCommerciale.Api.csproj

COPY GestionCommerciale.Api/ GestionCommerciale.Api/
COPY GestionCommerciale.Tests/ GestionCommerciale.Tests/

RUN dotnet publish GestionCommerciale.Api/GestionCommerciale.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "GestionCommerciale.Api.dll"]