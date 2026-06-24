# .NET License Client

A small .NET client for [Licensetorun.com](https://licensetorun.com). Activate,
validate, swap and check updates for your licensed product — against
licensetorun.com or your own self-hosted on-prem server. Targets
`netstandard2.0`, so it runs on .NET Framework 4.6.1+, .NET Core and .NET 5+.

## Install

```bash
dotnet add package Licensetorun.LicenseClient
```

## Usage

```csharp
using Licensetorun.Licensing;

var license = new LicenseClient(
    apiBase:    "https://licensetorun.com",
    productId:  "PRODUCT-PUBLIC-UUID", // from the product page in the dashboard
    licenseKey: "CUSTOMER-KEY");
    // instance defaults to Environment.MachineName

// Activate once (consumes a seat):
var activated = await license.ActivateAsync();
if (!activated.Ok)
    throw new Exception(activated.Body.GetProperty("message").GetString());

// Gate a feature:
if (await license.IsValidAsync())
{
    // ...licensed feature...
}

// Move a seat to a new machine:
await license.SwapAsync("old-host", "new-host");

// Check for updates:
var update = await license.CheckForUpdateAsync("1.2.0");
if (update.Body.TryGetProperty("update_available", out var avail) && avail.GetBoolean())
{
    Console.WriteLine($"New version: {update.Body.GetProperty("version").GetString()}");
}
```

## Result & errors

Every method returns a `LicenseResult` and never throws on a network/HTTP error:

```csharp
public sealed class LicenseResult
{
    public bool Ok { get; }            // true for a 2xx response
    public int Status { get; }         // HTTP status, or 0 on a transport error
    public JsonElement Body { get; }   // parsed JSON (or { error, message } on failure)
}
```

A transport failure (DNS, refused connection, timeout) comes back as
`Ok == false`, `Status == 0` and `Body.error == "network_error"`.

## Notes

- Reuse a single `LicenseClient` (it holds an `HttpClient`); you may also pass your
  own `HttpClient`.
- Cache `IsValidAsync()` yourself (e.g. for a few hours) if you call it often.
- Point `apiBase` at `https://licensetorun.com` or at your self-hosted on-prem server.

MIT licensed.
