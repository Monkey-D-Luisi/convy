FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Convy.Domain/*.csproj src/Convy.Domain/
COPY src/Convy.Application/*.csproj src/Convy.Application/
COPY src/Convy.Infrastructure/*.csproj src/Convy.Infrastructure/
COPY src/Convy.Worker/*.csproj src/Convy.Worker/
RUN dotnet restore src/Convy.Worker/Convy.Worker.csproj

COPY . .
RUN dotnet publish src/Convy.Worker/Convy.Worker.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "Convy.Worker.dll"]
