FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

RUN apt-get update \
    && apt-get install curl -y \
    && curl -fsSL https://deb.nodesource.com/setup_25.x | bash - \
    && apt-get install nodejs -y \
    && apt-get clean

COPY *.csproj ./
RUN dotnet restore

COPY . .
RUN npm install
RUN npm run build:minified:css

RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Tidawnloader.dll"]