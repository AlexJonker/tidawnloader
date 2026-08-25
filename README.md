<div align="center">
  <img src="./wwwroot/favicon.svg" alt="logo" width="150" />
  <br>
  <strong>Tidawnloader</strong>
  <br><br>
  Tidal - Downloader
  <br>
  Self hosted tidal music downloader/manager
</div>

# Quick start

Download [docker-compose.yaml](./docker-compose.yaml) and configure it to your liking, then run:
```bash
docker compose up -d
```
Tidawnloader will be available at `http://localhost:4675`.

# Local development

## Prerequisites
- .NET SDK (10.0)
- Node.js
- A running MySQL instance

## Setup

Install node modules:
```bash
npm i
```

`Db:host`, `Db:port`, `Db:name`, and `Db:user` already default to sensible local values in `appsettings.Development.json`. You only need to set your database password:
```bash
dotnet user-secrets set "Db:password" "Abc123"
```

If your local MySQL setup differs from the defaults (host `localhost`, port `3306`, db `tidawnloader`, user `root`), override those too:
```bash
dotnet user-secrets set "Db:host" "localhost"
dotnet user-secrets set "Db:port" 3306
dotnet user-secrets set "Db:name" "tidawnloader"
dotnet user-secrets set "Db:user" "root"
```

> [!WARNING]
> User secrets are stored unencrypted in `~/.microsoft/usersecrets/09d59150-7919-456b-9d77-07ded5ba5acd/secrets.json` (Linux/macOS) or `%APPDATA%\Microsoft\UserSecrets\09d59150-7919-456b-9d77-07ded5ba5acd\secrets.json` (Windows).

Run tidawnloader:
```bash
npm run dev
```
Tidawnloader will be available at `http://localhost:8000`.