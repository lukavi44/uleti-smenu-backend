# UletiSmenu — Backend

ASP.NET Core 8 API for **UletiSmenu** (restaurant shift hiring).

Frontend (separate repo): [uleti-smenu_react](https://github.com/lukavi44/uleti-smenu_react)

## Project structure

| Path | Description |
|------|-------------|
| `UletiSmenu/API` | Web API entry point |
| `UletiSmenu/docs/` | Deployment and configuration guides |

---

## Local development setup

### Prerequisites

- .NET SDK 8+
- SQL Server (Express, Developer, or LocalDB)
- Node.js 20+ (for the frontend — see frontend repo)

```powershell
dotnet dev-certs https --trust
```

### SQL Server

The API runs EF Core migrations on startup. No manual schema setup required.

Example connection strings:

```text
Server=(localdb)\mssqllocaldb;Database=UletiSmenuDb;Trusted_Connection=True;TrustServerCertificate=True;
Server=localhost\SQLEXPRESS;Database=UletiSmenuDb;Trusted_Connection=True;TrustServerCertificate=True;
Server=localhost;Database=UletiSmenuDb;Trusted_Connection=True;TrustServerCertificate=True;
```

### Configure and run

```powershell
cd UletiSmenu\API
Copy-Item appsettings.Development.json.example appsettings.Development.json
```

Edit `appsettings.Development.json`:

- `ConnectionStrings:UletiSmenu` — your SQL instance
- `FileSettings:UploadPath` — local upload folder (created on startup)
- `AdminSeed` — optional local admin user (see frontend repo README)

```powershell
dotnet restore
dotnet run --launch-profile https
```

- API: `https://localhost:7029`
- Swagger: `https://localhost:7029/swagger`

### Frontend

Clone and run the [frontend repo](https://github.com/lukavi44/uleti-smenu_react). Set `VITE_API_BASE_URL=https://localhost:7029` in `uleti-smenu/.env`.

---

## Documentation

Guides in `UletiSmenu/docs/`:

- `PRODUCTION_CONFIG.md` — appsettings vs env vars, CORS, SMTP, uploads, secrets
- `STAGING_DEPLOY.md` — deploy staging (payments disabled)
- `PAYMENT_PROPOSAL.md` — billing model reference
- `STRIPE_TEST_SETUP.md` — Stripe test mode

**Payments:** `Stripe:Enabled=false` by default. Billing gates and free registration credits work; online checkout is off until CorvusPay or Stripe is configured.
