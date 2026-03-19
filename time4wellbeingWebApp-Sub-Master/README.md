# Time4Wellbeing Web Application

A comprehensive ASP.NET Core 8 web application for managing children's health and wellbeing tracking, designed for schools, healthcare providers, and parents.

## 📋 Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Technology Stack](#technology-stack)
- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Configuration](#configuration)
- [Database Setup](#database-setup)
- [Running the Application](#running-the-application)
- [Project Structure](#project-structure)
- [API Documentation](#api-documentation)
- [User Roles](#user-roles)
- [Key Features](#key-features)
- [Security](#security)
- [Contributing](#contributing)
- [License](#license)

## 🎯 Overview

Time4Wellbeing is a health tracking platform that enables parents, schools, and healthcare providers to monitor and improve children's health outcomes through:

- Health score assessments
- Physical measurements tracking
- Medical records management
- Gamification with rewards and tasks
- Feedback and consent management
- Administrative dashboards and reporting

## ✨ Features

### For Parents/Guardians
- 📊 Track child health scores and measurements
- 🏥 Manage medical records and allergies
- 🎯 View personalized health goals
- 🎮 Engage children with gamified tasks and rewards
- 📱 Receive notifications and reminders
- 📈 View progress reports and analytics

### For Administrators
- 👥 User and child profile management
- 📊 Comprehensive reporting and KPI exports
- 🔔 Send targeted notifications
- 📝 Manage consent forms and questionnaires
- 🎥 Video reward system management
- 📈 View system-wide analytics

### For Healthcare Providers
- 📋 Access child health records (with consent)
- 📊 Track health score trends
- 🏥 Manage medical information
- 📈 Generate health reports

## 🛠 Technology Stack

### Backend
- **Framework**: ASP.NET Core 8.0 (MVC)
- **Language**: C# 12.0
- **ORM**: Entity Framework Core
- **Database**: SQL Server
- **Authentication**: ASP.NET Core Identity + JWT
- **Email**: MailKit

### Frontend
- **View Engine**: Razor Pages
- **CSS Framework**: Bootstrap 5
- **Icons**: Feather Icons, Font Awesome
- **JavaScript**: Vanilla JS, jQuery

### API
- **REST API**: ASP.NET Core Web API
- **Documentation**: Swagger/OpenAPI
- **Authentication**: JWT Bearer Tokens

### Additional Services
- **Background Tasks**: IHostedService (Registration reminders, recurring tasks)
- **Notifications**: Custom NotificationService
- **AI Integration**: GenAI Goal Service
- **Export**: KPI Export Service (Excel, CSV)

## 📦 Prerequisites

Before you begin, ensure you have the following installed:

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (version 8.0 or higher)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (2019 or higher) or SQL Server Express
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (recommended) or VS Code
- [Git](https://git-scm.com/) for version control

## 🚀 Installation

### 1. Clone the Repository

```bash
git clone https://github.com/bontester03/time4wellbeingWebApp.git
cd time4wellbeingWebApp
```

### 2. Restore NuGet Packages

```bash
cd WebApit4s
dotnet restore
```

### 3. Update Database Connection String

Edit `appsettings.json` and update the connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=Time4WellbeingDB;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

### 4. Apply Database Migrations

```bash
dotnet ef database update
```

Or the application will automatically apply migrations on startup.

## ⚙️ Configuration

### appsettings.json

Key configuration sections:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=__DB_HOST__;Port=5432;Database=__DB_NAME__;Username=__DB_USER__;Password=__DB_PASSWORD__"
  },
  "Jwt": {
    "Key": "__SET_JWT_KEY__",
    "Issuer": "__SET_JWT_ISSUER__",
    "Audience": "__SET_JWT_AUDIENCE__",
    "AccessTokenMinutes": 15
  },
  "EmailSettings": {
    "SmtpServer": "__SET_SMTP_SERVER__",
    "Port": "587",
    "SenderEmail": "__SET_EMAIL_SENDER__",
    "Password": "__SET_EMAIL_PASSWORD__"
  },
  "OpenAI": {
    "ApiKey": "__SET_OPENAI_API_KEY__"
  }
}
```

### Environment-Specific Settings

For production, use `appsettings.Production.json` with:
- Secure JWT keys (minimum 32 characters)
- Production database connection strings
- Production email credentials
- HTTPS enforcement

## 🗄️ Database Setup

### Automatic Migration (Recommended)

The application automatically applies pending migrations on startup via `Program.cs`:

```csharp
context.Database.Migrate();
await IdentitySeeder.SeedAdminAsync(services);
```

### Manual Migration

```bash
# Create a new migration
dotnet ef migrations add MigrationName

# Apply migrations
dotnet ef database update

# Rollback to specific migration
dotnet ef database update PreviousMigrationName
```

### Default Admin Account

After first run, a default admin account is seeded:

- **Email**: `admin@time4wellbeing.com`
- **Password**: `Admin@123`

⚠️ **IMPORTANT**: Change this password immediately in production!

## 🏃 Running the Application

### Using Visual Studio
1. Open `WebApit4s.sln`
2. Set `WebApit4s` as the startup project
3. Press `F5` or click "Run"

### Using .NET CLI

```bash
cd WebApit4s
dotnet run
```

The application will start on:
- HTTP: `http://localhost:5206`
- Swagger UI: `http://localhost:5206/swagger` (Development only)

### Using Docker (Optional)

```bash
# Build image
docker build -t time4wellbeing .

# Run container
docker run -p 5206:5206 time4wellbeing
```

## 📁 Project Structure

```
WebApit4s/
├── API/                          # REST API Controllers
│   ├── ApiDashboardController.cs
│   ├── ApiHealthScoreController.cs
│   ├── ApiMeasurementController.cs
│   └── ApiProfileController.cs
├── Controllers/                  # MVC Controllers
│   ├── AccountController.cs
│   ├── DashboardController.cs
│   ├── AdminDashboardController.cs
│   ├── GuestRegistrationController.cs
│   └── ...
├── DAL/                         # Data Access Layer
│   └── TimeContext.cs
├── Identity/                    # Identity Models
│   ├── ApplicationUser.cs
│   ├── ApplicationRole.cs
│   └── IdentitySeeder.cs
├── Models/                      # Domain Models
│   ├── Child.cs
│   ├── HealthScore.cs
│   ├── Measurement.cs
│   ├── MedicalRecord.cs
│   ├── Notification.cs
│   └── ...
├── Services/                    # Business Logic
│   ├── DashboardService.cs
│   ├── HealthScoreService.cs
│   ├── NotificationService.cs
│   ├── EmailSender.cs
│   ├── RecurringTaskService.cs
│   └── ...
├── Views/                       # Razor Views
│   ├── Account/
│   ├── Dashboard/
│   ├── AdminDashboard/
│   └── Shared/
│       ├── _LayoutDashboard.cshtml
│       └── _LayoutAdmin.cshtml
├── wwwroot/                     # Static Files
│   ├── css/
│   ├── js/
│   ├── images/
│   └── Theme/
├── appsettings.json
├── appsettings.Development.json
└── Program.cs
```

## 📚 API Documentation

### Authentication

All API endpoints require JWT authentication except `/api/auth/login`.

**Login Endpoint:**

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "password123"
}
```

**Response:**

```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "expiresIn": 3600
}
```

**Using the Token:**

```http
GET /api/dashboard/summary
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

### Available API Endpoints

#### Dashboard API
- `GET /api/dashboard/summary` - Get dashboard summary
- `GET /api/dashboard/children` - Get user's children

#### Health Score API
- `GET /api/healthscore/{childId}` - Get health scores for child
- `POST /api/healthscore` - Create new health score
- `PUT /api/healthscore/{id}` - Update health score
- `DELETE /api/healthscore/{id}` - Delete health score

#### Measurement API
- `GET /api/measurement/{childId}` - Get measurements for child
- `POST /api/measurement` - Create new measurement
- `PUT /api/measurement/{id}` - Update measurement
- `DELETE /api/measurement/{id}` - Delete measurement

#### Profile API
- `GET /api/profile` - Get user profile
- `PUT /api/profile` - Update user profile
- `GET /api/profile/children/{childId}` - Get child profile

### Swagger Documentation

Access interactive API documentation at: `http://localhost:5206/swagger` (Development mode only)

## 👥 User Roles

### Admin
- Full system access
- User and child management
- System configuration
- KPI reporting and exports
- Video and reward management
- Notification broadcasting

### Parent/Guardian
- Manage own children profiles
- View and submit health data
- Access medical records
- View rewards and tasks
- Submit feedback
- Manage consent forms

### Healthcare Provider
- View consented child records
- Submit professional assessments
- Generate health reports

### Guest
- Limited registration access
- View public resources

## 🔑 Key Features

### Health Tracking
- **Health Score Questionnaire**: 5 metrics (physical activity, breakfast, fruit/veg, snacks, fatty foods)
- **Measurements**: Height, weight, BMI, centile scores
- **Medical Records**: GP details, conditions, allergies, medications
- **Progress Tracking**: Historical data with charts and trends

### Gamification
- **Rewards System**: Coin-based rewards for health activities
- **Video Rewards**: Educational videos with coin rewards upon completion
- **Tasks**: Daily/weekly health tasks with point rewards
- **Progress Badges**: Achievement system

### Communication
- **Notifications**: In-app notification system with read/unread status
- **Email Reminders**: Automated registration and task reminders
- **Feedback System**: Parent feedback with star ratings
- **Consent Management**: Digital consent forms with timestamping

### Administrative Features
- **User Management**: CRUD operations for users and children
- **Reporting**: Comprehensive KPI exports (Excel, CSV)
- **Analytics**: Dashboard with engagement metrics
- **Bulk Operations**: Batch notifications and updates
- **Video Management**: Upload and manage educational content

## 🔒 Security

### Authentication & Authorization
- ✅ ASP.NET Core Identity for user management
- ✅ JWT tokens for API authentication
- ✅ Role-based authorization (Admin, Parent, etc.)
- ✅ Session management with 30-minute timeout

### Data Protection
- ✅ CSRF protection via anti-forgery tokens
- ✅ Secure password hashing (PBKDF2)
- ✅ SQL injection prevention via EF Core parameterized queries
- ✅ XSS protection via Razor encoding
- ✅ Sensitive data marking for medical records

### Best Practices
- ✅ HTTPS enforcement (production)
- ✅ Input validation on all forms
- ✅ Output encoding
- ✅ Secure cookie settings (HttpOnly, SameSite)
- ✅ Connection string encryption

### Recommended Actions Before Production
- [ ] Change default admin password
- [ ] Update JWT secret key (minimum 32 secure characters)
- [ ] Configure SSL/TLS certificates
- [ ] Review and update CORS policy
- [ ] Enable logging and monitoring
- [ ] Set up database backups
- [ ] Configure email credentials securely
- [ ] Remove duplicate code (ViewChildDetails.cshtml)

## 🧪 Testing

### Manual Testing Checklist
- [ ] User registration and login
- [ ] Child profile creation
- [ ] Health score submission
- [ ] Measurement tracking
- [ ] Medical record management
- [ ] Notification system
- [ ] Feedback submission
- [ ] Admin dashboard access
- [ ] API endpoints with Swagger

### Test User Accounts

After seeding:
- **Admin**: admin@time4wellbeing.com / Admin@123
- Create additional test users via registration

## 🚨 Known Issues & Fixes Required

1. **Duplicate Tab in ViewChildDetails.cshtml**: 
   - Issue: Child Info tab appears twice (lines 357-492)
   - Fix: Remove one of the duplicate sections before deployment

2. **Email Configuration**: 
   - Requires valid SMTP credentials for email features to work
   - Update `EmailSettings` in appsettings.json

3. **JWT Key**: 
   - Development key is insecure
   - Must be changed in production (minimum 32 characters)
   - Use environment variables or Azure Key Vault

## 🔧 Troubleshooting

### Database Connection Issues

```bash
# Test connection
dotnet ef database update --verbose

# Reset database
dotnet ef database drop
dotnet ef database update
```

### Migration Issues

```bash
# Remove last migration
dotnet ef migrations remove

# Create fresh migration
dotnet ef migrations add InitialCreate
```

### Port Already in Use

Edit `Program.cs` and change:

```csharp
builder.WebHost.UseUrls("http://0.0.0.0:5207"); // Different port
```

### CORS Issues

If API calls fail from external clients, verify CORS policy in `Program.cs`:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});
```

## 📝 Development Workflow

### Adding New Features

1. Create feature branch: `git checkout -b feature/feature-name`
2. Implement changes
3. Test locally
4. Commit: `git commit -m "Add feature description"`
5. Push: `git push origin feature/feature-name`
6. Create Pull Request

### Database Changes

1. Modify model classes in `Models/`
2. Create migration: `dotnet ef migrations add MigrationName`
3. Review generated migration in `Migrations/`
4. Apply: `dotnet ef database update`

## 🚀 Deployment

### Prerequisites
- SQL Server database
- Web server (IIS, Azure App Service, etc.)
- SSL certificate

### Steps
1. Update `appsettings.Production.json`
2. Build: `dotnet publish -c Release`
3. Deploy files from `bin/Release/net8.0/publish/`
4. Configure connection strings
5. Apply migrations to production database
6. Test deployment

### Azure Deployment

```bash
# Login to Azure
az login

# Create resource group
az group create --name time4wellbeing-rg --location eastus

# Create app service
az webapp create --resource-group time4wellbeing-rg --plan myAppServicePlan --name time4wellbeing

# Deploy
dotnet publish -c Release
cd bin/Release/net8.0/publish
zip -r ../app.zip .
az webapp deployment source config-zip --resource-group time4wellbeing-rg --name time4wellbeing --src app.zip
```

## 📖 Documentation

### Code Documentation
- XML comments on public APIs
- README files in major folders
- Inline comments for complex logic

### User Guides
- Admin guide (TODO)
- Parent user guide (TODO)
- API integration guide (TODO)

## 👨‍💻 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

### Coding Standards
- Follow C# naming conventions
- Use meaningful variable names
- Add XML documentation to public methods
- Write unit tests for new features
- Keep methods focused (single responsibility)

## 📧 Support & Contact

- **Issues**: [GitHub Issues](https://github.com/bontester03/time4wellbeingWebApp/issues)
- **Project Link**: [https://github.com/bontester03/time4wellbeingWebApp](https://github.com/bontester03/time4wellbeingWebApp)

## 🙏 Acknowledgments

- Bootstrap team for the UI framework
- Feather Icons for beautiful icons
- Font Awesome for additional icons
- Microsoft for ASP.NET Core
- Entity Framework Core team
- MailKit for email functionality
- All contributors and testers

## 📄 License

This project is proprietary software. All rights reserved.

---

**Version**: 1.0.0  
**Last Updated**: December 2024  
**Status**: Active Development  
**Branch**: Sub-Master  

⭐ Star this repo if you find it helpful!
