FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY *.slnx .
COPY src/Convy.Domain/*.csproj src/Convy.Domain/
COPY src/Convy.Application/*.csproj src/Convy.Application/
COPY src/Convy.Infrastructure/*.csproj src/Convy.Infrastructure/
COPY src/Convy.API/*.csproj src/Convy.API/
RUN dotnet restore

COPY . .
RUN dotnet publish src/Convy.API/Convy.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Convy.API.dll"]
