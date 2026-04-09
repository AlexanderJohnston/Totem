# KurrentDB secure mode Totem edits

This file captures the smallest Totem-side changes that would make Outermind Service and Outermind Web behave correctly against secure KurrentDB 24.10.

## Likely failure chain

1. Both apps use the same Totem EventStore/KurrentDB client builder.
2. The committed appsettings defaults still say `server.insecure: true` and `connection.password: changeit`.
3. When `server.insecure` is true, Totem builds `esdb://<host>:2113?tls=false`.
4. If the deployment is now pointing at secure KurrentDB on 2113, the client speaks plaintext to a TLS gRPC endpoint and fails during the HTTP/2 handshake.
5. Even after setting `server.insecure: false`, the current Totem builder is still brittle because:
   - it cannot express `tlsVerifyCert` or `tlsCaFile` in structured config
   - it injects the username and password directly into the URI without escaping reserved characters

## Fastest operational workaround

Before changing Totem at all, try a deployed config override like this:

```json
{
  "totem.timeline.eventStore": {
    "connectionString": "esdb://admin:<URL-ENCODED-PASSWORD>@<kurrent-host>:2113?tls=true&tlsCaFile=C%3A%5Ccerts%5Ckurrent-ca.pem"
  }
}
```

Temporary diagnostic variant only:

```json
{
  "totem.timeline.eventStore": {
    "connectionString": "esdb://admin:<URL-ENCODED-PASSWORD>@<kurrent-host>:2113?tls=true&tlsVerifyCert=false"
  }
}
```

If that works, the core issue is confirmed to be in Totem's generated connection string or its inability to express secure TLS settings cleanly.

## 1. Add structured TLS options

**File:** `src\Totem.Timeline.EventStore\Hosting\EventStoreTimelineOptions.cs`

### Current snippet

```csharp
public class ServerOptions
{
  public string Name { get; set; } = "localhost";
  public int Port { get; set; } = 2113;
  public bool Insecure { get; set; } = true;
}
```

### Replace with

```csharp
public class ServerOptions
{
  public string Name { get; set; } = "localhost";
  public int Port { get; set; } = 2113;
  public bool Insecure { get; set; } = true;

  // Only used when Insecure == false.
  // Leave null to let the client use its default behavior.
  public bool? TlsVerifyCert { get; set; }

  // Optional PEM/root CA file for self-signed or internal CA deployments.
  public string TlsCaFile { get; set; }
}
```

### Why

- This keeps the current dev/insecure defaults intact.
- It lets deployed config express certificate validation behavior without dropping down to a raw `connectionString`.
- The property names match the EventStore/Kurrent connection string options, which keeps the builder simple.

## 2. Replace the inline connection-string builder

**File:** `src\Totem.Timeline.EventStore\Hosting\EventStoreServiceExtensions.cs`

### Add this using

```csharp
using System.Collections.Generic;
```

### Current snippet

```csharp
static EventStoreClientSettings BuildClientSettings(EventStoreTimelineOptions options, IServiceProvider provider)
{
  EventStoreClientSettings s;

  if(!string.IsNullOrEmpty(options.ConnectionString))
  {
    s = EventStoreClientSettings.Create(options.ConnectionString);
  }
  else
  {
    // Omit the credentials when insecure mode is true

    var tls = options.Server.Insecure ? "tls=false" : "";
    var includeCredentials = !options.Server.Insecure && !string.IsNullOrEmpty(options.Connection.Username);

    var connStr = includeCredentials
      ? $"esdb://{options.Connection.Username}:{options.Connection.Password}@{options.Server.Name}:{options.Server.Port}?{tls}"
      : $"esdb://{options.Server.Name}:{options.Server.Port}?{tls}";

    s = EventStoreClientSettings.Create(connStr);
  }

  s.LoggerFactory = provider.GetService<ILoggerFactory>();
  s.DefaultDeadline = options.Connection.Timeout;
  return s;
}
```

### Replace with

```csharp
static EventStoreClientSettings BuildClientSettings(EventStoreTimelineOptions options, IServiceProvider provider)
{
  var connectionString = !string.IsNullOrWhiteSpace(options.ConnectionString)
    ? options.ConnectionString
    : BuildConnectionString(options);

  var settings = EventStoreClientSettings.Create(connectionString);
  settings.LoggerFactory = provider.GetService<ILoggerFactory>();
  settings.DefaultDeadline = options.Connection.Timeout;
  return settings;
}

static string BuildConnectionString(EventStoreTimelineOptions options)
{
  var server = options.Server;
  var connection = options.Connection;
  var query = new List<string>();

  if(server.Insecure)
  {
    query.Add("tls=false");
  }
  else
  {
    if(server.TlsVerifyCert.HasValue)
    {
      query.Add($"tlsVerifyCert={server.TlsVerifyCert.Value.ToString().ToLowerInvariant()}");
    }

    if(!string.IsNullOrWhiteSpace(server.TlsCaFile))
    {
      query.Add($"tlsCaFile={Escape(server.TlsCaFile)}");
    }
  }

  var credentials = !server.Insecure && !string.IsNullOrWhiteSpace(connection.Username)
    ? $"{Escape(connection.Username)}:{Escape(connection.Password)}@"
    : "";

  var queryString = query.Count == 0
    ? ""
    : "?" + string.Join("&", query);

  return $"esdb://{credentials}{server.Name}:{server.Port}{queryString}";
}

static string Escape(string value) =>
  Uri.EscapeDataString(value ?? "");
```

### Why

- It preserves `ConnectionString` as the highest-priority override.
- It stops generating a dangling `?` for secure connections with no query parameters.
- It makes stronger passwords safe by URI-encoding them before placing them in `esdb://user:password@host`.
- It exposes secure-mode settings needed for self-signed or internal CA certificates.

## 3. Deployment config after the Totem code change

Once the Totem edits are in place, the deployed configuration can stay structured instead of switching everything to a raw connection string.

### Example

```json
{
  "totem.timeline.eventStore": {
    "server": {
      "name": "<kurrent-host>",
      "port": 2113,
      "insecure": false,
      "tlsVerifyCert": true,
      "tlsCaFile": "C:\\certs\\kurrent-ca.pem"
    },
    "connection": {
      "username": "admin",
      "password": "<new admin password>",
      "timeout": "00:15:00"
    }
  }
}
```

## 4. Things not to change

- Do **not** remove `EventStoreTimelineOptions.ConnectionString`; it is still the cleanest escape hatch for unusual environments.
- Do **not** commit the real production password into repo appsettings.
- Do **not** make `tlsVerifyCert=false` the default; use it only as a temporary diagnostic or internal-dev fallback.
- Do **not** change the local dev insecure defaults unless you want the committed appsettings files to represent production instead of development.

## 5. Monday order of operations

1. Try the `connectionString` override in the deployed app first.
2. If it works, decide whether you want the Totem cleanup for maintainability.
3. If yes, make the two Totem source edits above.
4. Then move the deployment config back to structured `server` plus `connection` settings if you prefer.

## Tests

Deferred for now, per request.
