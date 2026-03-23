# Azure Linux Deployment Guide

This file is a practical deployment guide for running this project on Azure Linux App Service.

It is written for the current repo shape:

- ASP.NET Core 8 backend in `time4wellbeingWebApp-Sub-Master\WebApit4s`
- Angular SPA in `frontend-angular`
- Angular build output copied into backend `wwwroot\guest-registration`
- PostgreSQL database

## Recommended Azure Shape

Use this setup:

- Azure App Service Plan on Linux
- Azure Web App on Linux, code-based deployment
- Azure Database for PostgreSQL Flexible Server
- App Settings for secrets and runtime config

Recommended for this repo:

- use `B1` or higher, not `F1`
- publish the app first, then deploy the publish output as a ZIP
- do not rely on Azure to build this mixed backend plus Angular repo directly

Reason:

- this project has hosted background services
- this project has a backend build that also triggers an Angular build
- artifact deployment is simpler and more repeatable than raw repo deployment

## Important Project-Specific Notes

1. The backend `WebApit4s.csproj` builds the Angular app during normal build or publish.
2. The Angular app depends on assets inside backend `wwwroot`.
3. `appsettings.json` in source control contains placeholders only.
4. Real secrets must be supplied through Azure App Settings.
5. This app contains background work (`RegistrationReminderService`, `RecurringTaskService`), so do not use a plan where the app can idle too aggressively.

## Pre-Deployment Checklist

Before Azure deployment, make sure you have:

- Azure subscription
- Azure CLI installed
- .NET 8 SDK
- Node.js and npm
- PostgreSQL client tools if you want to test DB connectivity manually

Login:

```powershell
az login
```

Optional: select the right subscription:

```powershell
az account set --subscription "<your-subscription-id>"
```

## Step 1: Choose Names

Set working variables in PowerShell:

```powershell
$rg = "t4s-rg"
$location = "uksouth"
$plan = "t4s-linux-plan"
$app = "t4s-webapp-unique-name"
$pg = "t4s-pg-unique-name"
$pgAdmin = "t4sadmin"
$pgPassword = "<strong-password>"
$dbName = "time4wellbeing"
```

The web app name and PostgreSQL server name must be globally unique.

## Step 2: Create Resource Group

```powershell
az group create --name $rg --location $location
```

## Step 3: Create Linux App Service Plan

Use `B1` or higher.

```powershell
az appservice plan create `
  --name $plan `
  --resource-group $rg `
  --is-linux `
  --sku B1
```

## Step 4: Create Linux Web App

Create the web app, then explicitly set the Linux runtime to .NET 8.

```powershell
az webapp create `
  --resource-group $rg `
  --plan $plan `
  --name $app
```

```powershell
az webapp config set `
  --name $app `
  --resource-group $rg `
  --linux-fx-version "DOTNETCORE|8.0"
```

Optional: list supported Linux runtimes first:

```powershell
az webapp list-runtimes --os linux | findstr DOTNET
```

## Step 5: Configure App Service General Settings

In the Azure portal, open the Web App and set:

- `HTTPS Only = On`
- `Always On = On`
- `HTTP version = 2.0`
- `Health check` if you later add a dedicated health endpoint

Why `Always On` matters here:

- this app has continuous hosted background work
- App Service docs say Always On is required for continuous or scheduled background work such as WebJobs
- my inference is that this repo should also run on a non-Free plan with Always On enabled for reliable background processing

## Step 6: Create Azure Database for PostgreSQL Flexible Server

Quick public-access setup:

```powershell
az postgres flexible-server create `
  --resource-group $rg `
  --name $pg `
  --location $location `
  --admin-user $pgAdmin `
  --admin-password $pgPassword `
  --sku-name Standard_B1ms `
  --tier Burstable `
  --public-access 0.0.0.0 `
  --storage-size 32 `
  --tags "Environment=Production"
```

Create the application database:

```powershell
az postgres flexible-server db create `
  --resource-group $rg `
  --server-name $pg `
  --database-name $dbName
```

Get the server endpoint:

```powershell
az postgres flexible-server show `
  --resource-group $rg `
  --name $pg `
  --query "{serverName:fullyQualifiedDomainName, adminUser:administratorLogin}" `
  --output table
```

Use a PostgreSQL connection string with `sslmode=require`.

Example:

```text
Host=<server-name>.postgres.database.azure.com;Port=5432;Database=time4wellbeing;Username=<admin-user>;Password=<password>;Ssl Mode=Require;Trust Server Certificate=true
```

## Step 7: Add Azure App Settings

Do not put real values back into `appsettings.json`.

Set them in the Web App under:

- `Settings`
- `Environment variables`
- `App settings`

Use these keys:

- `ConnectionStrings__DefaultConnection`
- `Jwt__Issuer`
- `Jwt__Audience`
- `Jwt__Key`
- `EmailSettings__SenderEmail`
- `EmailSettings__Password`
- `EmailSettings__SmtpServer`
- `EmailSettings__Port`
- `OpenAI__ApiKey`
- `ASPNETCORE_ENVIRONMENT`

Recommended values:

- `ASPNETCORE_ENVIRONMENT = Production`
- `Jwt__Issuer = https://<your-app-name>.azurewebsites.net`
- `Jwt__Audience = time4wellbeing-api`

Example CLI:

```powershell
az webapp config appsettings set `
  --resource-group $rg `
  --name $app `
  --settings `
    ASPNETCORE_ENVIRONMENT="Production" `
    ConnectionStrings__DefaultConnection="Host=$($pg).postgres.database.azure.com;Port=5432;Database=$dbName;Username=$pgAdmin;Password=$pgPassword;Ssl Mode=Require;Trust Server Certificate=true" `
    Jwt__Issuer="https://$app.azurewebsites.net" `
    Jwt__Audience="time4wellbeing-api" `
    Jwt__Key="<minimum-32-char-secret>" `
    EmailSettings__SenderEmail="<email>" `
    EmailSettings__Password="<password>" `
    EmailSettings__SmtpServer="<smtp-host>" `
    EmailSettings__Port="587" `
    OpenAI__ApiKey="<openai-key>"
```

Notes:

- Linux App Service uses `__` for nested config keys
- App Service values override `appsettings.json`
- App settings are encrypted at rest by Azure

## Step 8: Build Frontend and Publish Backend Locally

From repo root:

```powershell
cd frontend-angular
npm install
npm run build
```

Then publish the backend:

```powershell
cd ..\time4wellbeingWebApp-Sub-Master\WebApit4s
dotnet publish -c Release -o ..\..\publish\WebApit4s
```

Expected result:

- Angular assets are built
- Angular output is copied into backend `wwwroot\guest-registration`
- the publish output lands in `publish\WebApit4s`

## Step 9: Package the Publish Output

Zip the contents of the publish folder, not the publish folder itself.

```powershell
cd ..\..\publish\WebApit4s
Compress-Archive -Path * -DestinationPath ..\WebApit4s.zip -Force
```

You should end up with:

- `publish\WebApit4s.zip`

## Step 10: Deploy the ZIP to Azure

```powershell
az webapp deploy `
  --resource-group $rg `
  --name $app `
  --src-path "c:\Users\SIDDHARTHA\Downloads\time4wellbeingWebApp-Sub-Master\publish\WebApit4s.zip"
```

This restarts the app after deployment.

## Step 11: Port Binding Check

This project currently defaults to local port `5206` unless `ASPNETCORE_URLS` is set.

Azure App Service on Linux built-in containers use the `PORT` environment variable for the app port.

Because of that, if the app fails warmup or startup on Azure, do this first:

- add a startup script that maps Azure `PORT` to `ASPNETCORE_URLS`

Example `startup.sh`:

```sh
#!/bin/sh
export ASPNETCORE_URLS="http://0.0.0.0:${PORT:-8080}"
dotnet WebApit4s.dll
```

Then deploy it as a startup script:

```powershell
az webapp deploy `
  --resource-group $rg `
  --name $app `
  --src-path ".\startup.sh" `
  --type startup
```

If you prefer code-level compatibility instead, update `Program.cs` to honor the `PORT` environment variable before falling back to `5206`.

## Step 12: Verify the Deployment

Open:

```text
https://<your-app-name>.azurewebsites.net
```

Check logs:

```powershell
az webapp log config `
  --resource-group $rg `
  --name $app `
  --docker-container-logging filesystem `
  --level Verbose
```

```powershell
az webapp log tail `
  --resource-group $rg `
  --name $app
```

Things to verify:

- app returns a page instead of startup failure
- database connection works
- registration flows load
- `/guest-registration/...` serves the Angular SPA
- email features work with production SMTP values
- JWT login and protected APIs work

## Step 13: Production Hardening After First Successful Deploy

After the first deployment works, tighten the setup:

- restrict CORS in `Program.cs`
- re-enable or review HTTPS redirection behavior
- move secrets to Key Vault references if possible
- use PostgreSQL private access or VNet integration instead of broad public access
- add Application Insights
- add deployment slots
- add a dedicated health endpoint
- consider moving long-running background work to WebJobs or Functions if needed later

## Fast Troubleshooting

If the app fails on Azure, check these first:

1. Wrong or missing app settings
2. Bad PostgreSQL connection string
3. Missing `sslmode=require`
4. Port binding issue on Linux App Service
5. SMTP settings invalid
6. App running on Free plan without Always On
7. ZIP created incorrectly with the parent folder included

## Official References

Microsoft docs used for this guide:

- App Service app settings and general config:
  https://learn.microsoft.com/en-us/azure/app-service/configure-common
- Configure ASP.NET Core for App Service:
  https://learn.microsoft.com/en-gb/azure/app-service/configure-language-dotnetcore
- ZIP deploy:
  https://learn.microsoft.com/en-us/azure/app-service/deploy-zip
- Linux App Service FAQ:
  https://learn.microsoft.com/en-gb/troubleshoot/azure/app-service/faqs-app-service-linux-new
- Azure Database for PostgreSQL Flexible Server quickstart:
  https://learn.microsoft.com/en-us/azure/postgresql/configure-maintain/quickstart-create-server

## Summary

For this repo, the safest Linux App Service path is:

1. Create Linux App Service and PostgreSQL.
2. Put secrets in Azure App Settings.
3. Build Angular locally.
4. Publish backend locally.
5. ZIP the publish output.
6. Deploy the ZIP.
7. Fix port binding first if Linux startup fails.
