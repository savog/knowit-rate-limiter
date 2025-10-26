# RateLimiter for ASP.NET Core

A lightweight and flexible **rate-limiting middleware** for ASP.NET Core (.NET 9+).  
It helps you control how many requests a client (by IP) can send to your API in a given time — either globally or per endpoint.

---

## Features

- Per-IP and per-endpoint rate limiting
- Simple configuration (default + per-endpoint rules)
- Zero external dependencies — minimal and fast
- Plug-and-play middleware (no need for service registration)
- Works with any `ILogger` provider out of the box

---

## Installation

Install from NuGet:

```bash
dotnet add package RateLimiter
```

or via the NuGet Package Manager:

```powershell
Install-Package RateLimiter
```

---

## Quick Start

Add it to your pipeline:

```csharp
using RateLimiter;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseRateLimiter(new RateLimiterOptions
{
    RequestLimiterEnabled = true,
    DefaultRequestLimitMs = 1000,  // 1-second window
    DefaultRequestLimitCount = 5   // max 5 requests per IP in that window
});

app.MapGet("/", () => "Hello, world!");
app.Run();
```

If a client exceeds the limit, the middleware responds with:

```
HTTP 429 Too Many Requests
```

---

## Inline Configuration

You can also configure everything inline:

```csharp
app.UseRateLimiter(options =>
{
    options.RequestLimiterEnabled = true;
    options.DefaultRequestLimitMs = 2000;
    options.DefaultRequestLimitCount = 10;

    options.EndpointLimits.Add(new EndpointLimitOptions
    {
        Endpoint = "/api/fast",
        RequestLimitMs = 1000,
        RequestLimitCount = 3
    });
});
```

---

## Configuration via *appsettings.json*

```json
{
  "RateLimiter": {
    "RequestLimiterEnabled": true,
    "DefaultRequestLimitMs": 1000,
    "DefaultRequestLimitCount": 10,
    "EndpointLimits": [
      { "Endpoint": "/api/fast", "RequestLimitMs": 1000, "RequestLimitCount": 5 },
      { "Endpoint": "/api/slow", "RequestLimitMs": 10000, "RequestLimitCount": 20 }
    ]
  }
}
```

and bind it easily:

```csharp
var configSection = builder.Configuration.GetSection("RateLimiter");
app.UseRateLimiter(configSection);
```

---

## Logging

If you have `ILogger` configured, each request can log debug info like:

```
GET /api/data @ 18:44:05.123
```

Blocked requests (when limit is hit) are logged as `Debug` entries —  
you can enable or disable them depending on your environment.

---

## Architecture Overview

| Component | Description |
|------------|-------------|
| `RateLimiterMiddleware` | Checks limits before each request. |
| `IRateLimiter` | Defines the rate-limiting contract. |
| `DefaultRateLimiter` | Default in-memory implementation. |
| `RateLimiterOptions` | Holds default and per-endpoint settings. |
| `RateLimiterApplicationBuilderExtensions` | Provides `UseRateLimiter` helpers. |

---

## Recommended Pipeline Order

```csharp
app.UseHttpsRedirection();
app.UseRateLimiter(configSection); // before auth
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

---

## Response Example

```http
HTTP/1.1 429 Too Many Requests
Content-Type: text/plain; charset=utf-8
```

---

## Notes

- The limiter is **in-memory**, designed for single-instance apps.
- When the limit is exceeded, the client receives `HTTP 429 Too Many Requests`.

---

Developed by Savo Garović
