FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG TARGETARCH
WORKDIR /app

# Copie uniquement le fichier .csproj pour restaurer les dépendances
COPY ./src/WPTelegramBot.csproj ./src/
RUN dotnet restore ./src/WPTelegramBot.csproj

# Copie tout le projet
COPY ./src ./src

# Compile et publie l'application
RUN dotnet publish ./src/WPTelegramBot.csproj -c Release -o /app/out 

# Image finale d'exécution
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

COPY --from=build /app/out ./

ENTRYPOINT ["dotnet", "WPTelegramBot.dll"]
