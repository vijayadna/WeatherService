# 🌤️ Weather Microservice

A production-grade Weather REST API built with **ASP.NET Core 8**, featuring JWT authentication, resilient external API integration, alert subscriptions, CSV export, Swagger UI, background jobs, and a full CI/CD pipeline.

----

## 📐 Architecture Overview

```
┌─────────────────────────────────────────────────┐
│              Client / Swagger UI                │
└────────────────────────┬────────────────────────┘
                         │ HTTPS + JWT Bearer
┌────────────────────────▼────────────────────────┐
│          ASP.NET Core 8 Web API                 │
│  ┌──────────┐ ┌──────────┐ ┌─────────────────┐  │
│  │ Weather  │ │  Alerts  │ │   Auth          │  │
│  │Controller│ │Controller│ │   Controller    │  │
│  └────┬─────┘ └────┬─────┘ └────────┬────────┘  │
│       │            │                │            │
│  ┌────▼────────────▼────────────────▼────────┐  │
│  │           Service Layer                   │  │
│  │  WeatherService │ AlertService │ AuthSvc  │  │
│  └────────┬───────────────┬──────────────────┘  │
│           │               │                     │
│  ┌────────▼──────┐  ┌─────▼──────────────────┐ │
│  │  Repository   │  │ OpenWeatherMap Client  │ │
│  │  (EF Core)    │  │ (Polly + Retry/CB)     │ │
│  └────────┬──────┘  └─────────────────────────┘ │
│           │                                     │
│  ┌────────▼──────┐                              │
│  │  SQLite DB    │                              │
│  │  (EF Core)    │                              │
│  └───────────────┘                              │
└─────────────────────────────────────────────────┘
```

---

## 🚀 Features

| Feature | Details |
|---|---|
| **Current Weather** | Live data via OpenWeatherMap, cached 10 min in SQLite |
| **Forecasts** | Hourly (48h) or 5-day daily forecasts || 
| **CSV Export** | Download historical data as a `.csv` file |
| **Alert Subscriptions** | Subscribe to Temperature / Rain / AQI / UV / Wind alerts ||
| **JWT Auth** | Secure all endpoints; issue tokens via `/api/v1/auth/login` |
| **Rate Limiting** | 60 req/min globally, 10 CSV exports/hour per IP |
| **Polly Resilience** | Retry (3×), circuit breaker, timeout on external HTTP calls |
| **Swagger UI** | Fully annotated OpenAPI spec served at `/` ||
| **Health Checks** | `/health` endpoint — DB connectivity |
| **ILogger** | Logging to console |
| **Docker** | Multi-stage Dockerfile with volume mounts |
| **CI/CD** | Build the Docker image and Push it to Docker Hub |

---

## ⚡ Quick Start

### Option A — Docker Compose (recommended)

```bash
# 1. Clone
git clone https://github.com/your-org/weather-microservice
cd weather-microservice

# 2. Set your OpenWeatherMap API key (get one free at openweathermap.org)
export OWM_API_KEY=your_api_key_here
export JWT_KEY=your-32-char-secret-key-here!!

# 3. Run
docker compose up -d

# 4. Open Swagger UI
open http://localhost:8080
```

### Option B — Local .NET CLI

```bash
cd src/WeatherService.API

# Set secrets (dev)
dotnet user-secrets set "ExternalApis:OpenWeatherMap:ApiKey" "YOUR_KEY"
dotnet user-secrets set "Jwt:Key" "CHANGE-ME-TO-A-32-CHAR-SECRET-KEY!!"

dotnet run
# Swagger UI → http://localhost:5000
```

---

## 🔐 Authentication

All endpoints except `/api/v1/auth/login` and `/health` require a **JWT Bearer token**.

```http
POST /api/v1/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "Admin@Weather1!"
}
```

**Demo accounts**

| Username | Password | Role |
|---|---|---|
| `admin` | `Admin@Weather1!` | admin |
| `readonly` | `ReadOnly@Weather1!` | reader |

Copy the returned `accessToken` and use it as:
```
Authorization: Bearer <token>
```

---

## 📡 API Reference

### Weather

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/v1/weather/current?location=Singapore` | Current conditions |
| `GET` | `/api/v1/weather/forecast?location=London&period=daily` | 5-day / hourly forecast |
| `GET` | `/api/v1/weather/export?location=Singapore` | Download CSV |

### Alerts

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/v1/alerts/subscriptions` | Create alert subscription |
| `GET` | `/api/v1/alerts/subscriptions?email=user@example.com` | List subscriptions |
| `PATCH` | `/api/v1/alerts/subscriptions/{id}` | Update subscription |
| `DELETE` | `/api/v1/alerts/subscriptions/{id}` | Delete subscription |

### Create Alert Subscription — Example

```json
POST /api/v1/alerts/subscriptions
{
  "subscriberEmail": "user@example.com",
  "location": "Singapore",
  "alertType": "Temperature",   // Temperature | Rain | AQI | UV | Wind
  "operator": "gt",             // gt | lt | eq
  "threshold": 35
}
```
---

## ☁️ CI/CD Pipeline

```
Push to main
    ↓
┌─────────────┐   ┌──────────────┐   ┌─────────────────┐   
│ Build &     │──▶│ Security     │──▶│ Docker Build    │
│ Unit Tests  │   │ Scan         │   │ & GHCR Push     │   
└─────────────┘   └──────────────┘   └─────────────────┘   └──────────────────┘
```

Configure the following **GitHub Secrets** for full deployment:
- `OWM_API_KEY` — OpenWeatherMap API key
- `JWT_KEY` — 32+ char JWT signing key

---

## 🔒 Security Practices

- **JWT authentication** on all data endpoints
- **Rate limiting** prevents API abuse (60 req/min, 10 exports/hour)
- **Polly circuit breaker** protects the service when OWM is degraded
- **Input validation** via Data Annotations and explicit guards
- **Global exception middleware** — never leaks stack traces
- **Secrets via environment variables** — no credentials in source

---

## 📦 Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 8 |
| ORM | Entity Framework Core 8 |
| Database | SQLite (swap for PostgreSQL/MSSQL in prod) |
| Auth | JWT Bearer (Microsoft.AspNetCore.Authentication.JwtBearer) |
| Resilience | Polly / Microsoft.Extensions.Http.Resilience |
| Rate Limiting | AspNetCoreRateLimit |
| CSV | CsvHelper |
| Docs | Swashbuckle / Swagger |
| Logging | ILogger |
| CI/CD | GitHub Actions |
| Container | Docker / Docker Compose |
