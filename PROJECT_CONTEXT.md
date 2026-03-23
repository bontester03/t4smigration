# Time4Wellbeing Project Context

This file is a short working reference for coding this project on a new PC.
It reflects the current repo state after secrets were removed from source control.

## What This Project Is

Time4Wellbeing is a child health and wellbeing platform with:

- an ASP.NET Core 8 backend
- an Angular 19 frontend SPA for guest and self-registration flows
- PostgreSQL via EF Core
- ASP.NET Identity plus JWT for auth
- email sending with MailKit
- background services for reminders and recurring tasks
- admin, parent, child-health, measurement, notification, and reporting features

## Repo Layout

The repo root is:

`c:\Users\SIDDHARTHA\Downloads\time4wellbeingWebApp-Sub-Master`

Main folders:

- `frontend-angular`
  Angular 19 app
- `time4wellbeingWebApp-Sub-Master\WebApit4s`
  Main ASP.NET Core app
- `time4wellbeingWebApp-Sub-Master\WebApit4s.sln`
  Visual Studio solution

Important files:

- `frontend-angular\src\app\app.routes.ts`
- `frontend-angular\src\environments\environment.ts`
- `frontend-angular\angular.json`
- `time4wellbeingWebApp-Sub-Master\WebApit4s\Program.cs`
- `time4wellbeingWebApp-Sub-Master\WebApit4s\appsettings.json`
- `time4wellbeingWebApp-Sub-Master\WebApit4s\DAL\TimeContext.cs`
- `time4wellbeingWebApp-Sub-Master\WebApit4s\WebApit4s.csproj`

## Frontend Summary

Location:

- `frontend-angular`

Stack:

- Angular 19 standalone app
- TypeScript
- Angular Router

Main frontend purpose:

- self-registration flow under `/register`
- guest-registration flow under `/guest-registration/:code/...`

Current route shape:

- `/register/step-1` through `/register/step-5`
- `/register/review`
- `/register/success`
- `/guest-registration/invalid`
- `/guest-registration/:code/step-1` through `/guest-registration/:code/step-5`
- `/guest-registration/:code/thank-you`

Frontend API base settings:

- `apiBaseUrl = /api/guest-registration`
- `registrationApiBaseUrl = /api/registration`

Important build detail:

- the Angular app is not fully independent
- `angular.json` pulls image and CSS assets from backend `wwwroot`
- the backend build copies Angular output into `WebApit4s\wwwroot\guest-registration`

## Backend Summary

Location:

- `time4wellbeingWebApp-Sub-Master\WebApit4s`

Stack:

- ASP.NET Core 8 MVC + Web API
- Razor views for the main site
- EF Core with Npgsql/PostgreSQL
- ASP.NET Identity
- JWT bearer authentication
- Swagger in development
- MailKit email sender
- hosted background services

Main startup file:

- `time4wellbeingWebApp-Sub-Master\WebApit4s\Program.cs`

Important backend behavior:

- default local URL is `http://localhost:5206`
- if `ASPNETCORE_URLS` is set, the app uses that instead
- data protection keys are stored in `.keys` under the app content root
- database startup uses `EnsureCreated()` for Npgsql
- startup catches migration or seed failures and logs a warning instead of crashing

Hosted services:

- `RegistrationReminderService`
- `RecurringTaskService`

Main backend areas:

- MVC controllers under `Controllers`
- API controllers under `API`
- EF Core context under `DAL`
- services under `Services`
- static files and SPA output under `wwwroot`

## Deployment Summary

This project can run on Azure Linux hosting, but treat it as a config and deployment-hardening task, not a rewrite.

Current deployment shape:

- the backend is the real host application
- the Angular guest-registration app is built and then served by the backend from `wwwroot\guest-registration`

Important deployment notes:

- use environment variables or Azure App Settings for all secrets
- do not put real secrets back into `appsettings.json`
- the app currently allows broad CORS and has `UseHttpsRedirection()` commented out
- the backend/frontend coupling means CI or container builds are cleaner than manual server builds
- Data Protection keys should be planned properly for production

## Required Runtime Config

`appsettings.json` now contains placeholders only.

You must provide real values for:

- `ConnectionStrings__DefaultConnection`
- `Jwt__Issuer`
- `Jwt__Audience`
- `Jwt__Key`
- `EmailSettings__SenderEmail`
- `EmailSettings__Password`
- `EmailSettings__SmtpServer`
- `EmailSettings__Port`
- `OpenAI__ApiKey`

Optional but commonly used:

- `ASPNETCORE_ENVIRONMENT`
- `ASPNETCORE_URLS`

Example PowerShell session for local testing:

```powershell
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=YOUR_DB;Username=YOUR_USER;Password=YOUR_PASSWORD"
$env:Jwt__Issuer = "http://localhost:5206"
$env:Jwt__Audience = "time4wellbeing-local"
$env:Jwt__Key = "replace-with-a-real-32-plus-char-secret"
$env:EmailSettings__SenderEmail = "noreply@example.com"
$env:EmailSettings__Password = "your-password"
$env:EmailSettings__SmtpServer = "smtp.example.com"
$env:EmailSettings__Port = "587"
$env:OpenAI__ApiKey = "your-openai-key"
$env:ASPNETCORE_ENVIRONMENT = "Development"
```

## New PC Setup

Install:

- .NET 8 SDK
- Node.js and npm
- PostgreSQL
- Visual Studio 2022 or VS Code

Suggested setup flow:

1. Clone the repo.
2. Restore backend packages.
3. Install frontend packages.
4. Set the required environment variables.
5. Make sure PostgreSQL is running and the target database exists.
6. Start the backend.
7. If working on the Angular app directly, run the frontend dev server separately.

Useful commands:

```powershell
cd frontend-angular
npm install
npm run build
```

```powershell
cd time4wellbeingWebApp-Sub-Master\WebApit4s
dotnet restore
dotnet run
```

Angular dev server:

```powershell
cd frontend-angular
npm start
```

## Coding Notes

- The backend is the primary application. Treat the Angular app as a mounted SPA inside the backend host.
- The repo has a nested folder structure. Be careful about working in the correct `WebApit4s` folder.
- `README.md` inside `time4wellbeingWebApp-Sub-Master` contains useful background but some parts are outdated.
- The current backend uses PostgreSQL, not SQL Server.
- If you build the backend normally, the `.csproj` may trigger the Angular build automatically.
- The Angular app reuses backend assets from `wwwroot`, so frontend changes may depend on backend files.

## Verified Current State

As of the latest local check:

- the backend compiles
- a clean isolated backend start returned HTTP 200
- the app successfully talked to the local PostgreSQL instance when real runtime values were supplied
- the repo was sanitized before pushing, so source control now contains placeholders instead of live secrets

## Best File To Read First

If you open only three things first on a new PC, use:

1. `PROJECT_CONTEXT.md`
2. `time4wellbeingWebApp-Sub-Master\WebApit4s\Program.cs`
3. `frontend-angular\src\app\app.routes.ts`
