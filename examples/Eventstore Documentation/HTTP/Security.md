# EventStoreDB 22.10 — Security

EventStoreDB supports basic HTTP authentication for API calls and per-stream Access Control Lists (ACLs). This document explains how to create users, configure ACLs, and set default ACLs for system and user streams.

## Authentication

EventStoreDB uses basic HTTP authentication for the HTTP API. The initial default administrative user is:

- username: `admin`
- password: `changeit`

Use the admin credentials when creating additional users or performing administrative operations.

### Creating users

Create internal users with the HTTP API (or via the Admin UI). The request must be authenticated with an existing admin user's credentials.

Example JSON payload (save as `new-user.json`):

```json
{
  "LoginName": "adminuser",
  "FullName": "EventStore Admin",
  "Groups": [
    "$admins",
    "DataScience"
  ],
  "Password": "aVerySecurePassword"
}
```

Create the user via HTTP POST:

```bash
curl -i -d "@new-user.json" "http://127.0.0.1:2113/users" \
  -H "Content-Type:application/json" -u admin:changeit
```

If a request requires authentication and you provide no or incorrect credentials, the server returns HTTP 401 Unauthorized:

```bash
curl -i 'http://127.0.0.1:2113/streams/$all' -u admin:password
# -> 401 Unauthorized
```

Tip: When sending credentials over HTTP, enable TLS/SSL to protect usernames and passwords in transit.

---

## Access control lists (ACLs)

EventStoreDB supports per-stream ACLs stored in the stream's metadata. Use the metadata resource for a stream to read or write ACLs. Always follow the metadata link provided in the stream’s feed rather than hardcoding metadata URIs.

You can set ACLs via a POST to the stream's metadata resource (or via the Admin UI). ACL entries use reserved metadata keys such as:

- `$r` — read permission (list or single user/group)
- `$w` — write permission
- `$d` — delete permission
- `$mr` — read permission for metadata
- `$mw` — write permission for metadata

### ACL example

The example below grants `writer` read+write on the stream, `reader` read-only, and gives `$admins` the exclusive right to delete or modify metadata.

Example event body to post as stream metadata (save as `metadata.json`):

```json
[
  {
    "eventId": "7c314750-05e1-439f-b2eb-f5b0e019be72",
    "eventType": "$user-updated",
    "data": {
      "readRole": "$all",
      "metaReadRole": "$all"
    }
  }
]
```

Post the metadata (authenticated as admin):

```bash
curl -i -d @metadata.json http://127.0.0.1:2113/streams/newstream/metadata \
  --user admin:changeit \
  -H "Content-Type: application/vnd.eventstore.events+json"
```

Successful response (example):

```
HTTP/1.1 201 Created
Location: http://127.0.0.1:2113/streams/%24%24newstream/0
...
```

You can also set a stream-specific ACL like:

```json
{
  "$acl": {
    "$r": ["reader", "also-reader"]
  }
}
```

When merged with default ACLs, a stream-specific `$acl` overrides the effective permissions for that stream.

Warning: Follow the metadata link in the stream feed instead of constructing metadata URLs manually—this makes clients resilient to future URI changes.

---

## Default ACL

You can configure default ACLs (for user streams and system streams) using the `$settings` stream. This sets the default permissions applied to streams that don't have explicit `$acl`.

Example default ACL that gives user `ouro` read/write/delete and configures system stream access:

```json
{
  "$userStreamAcl": {
    "$r": "$all",
    "$w": "ouro",
    "$d": "ouro",
    "$mr": "ouro",
    "$mw": "ouro"
  },
  "$systemStreamAcl": {
    "$r": ["$admins", "ouro"],
    "$w": "$admins",
    "$d": "$admins",
    "$mr": "$admins",
    "$mw": "$admins"
  }
}
```

After applying such defaults, user `ouro` can read system streams (for example, `$settings`):

```bash
curl -i http://127.0.0.1:2113/streams/%24settings -u ouro:ouroboros
```

If a stream has its own `$acl`, it is evaluated and merged with defaults to produce the effective permissions. For example, a stream with:

```json
{
  "$acl": {
    "$r": ["reader", "also-reader"]
  }
}
```

will produce an effective ACL similar to:

```json
{
  "$acl": {
    "$r": ["reader", "also-reader"],
    "$w": "ouro",
    "$d": "ouro",
    "$mr": "ouro",
    "$mw": "ouro"
  }
}
```

---

## Notes and warnings

- Caching and visibility: streams made visible to `$all` may be cached by intermediaries. If you change permissions from `$all` to more restrictive values, previously cached intermediaries might still serve the old content. Avoid relying on intermediaries to enforce access control—use HTTPS and plan ACL changes accordingly.
- Always protect credentials in transit with TLS/SSL.
- Use the Admin UI for easier management of users and ACLs when possible.
- If authentication or authorization fails, check logs and confirm user membership in the required groups (e.g., `$admins`).

---

This file captures the Security documentation (authentication, user creation, ACL examples, and default ACL handling) for EventStoreDB 22.10.