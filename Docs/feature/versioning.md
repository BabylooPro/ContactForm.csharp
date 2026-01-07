# API versioning

All `/api/*` endpoints require an explicit API version (no default).

Supported versions (today): `1.0`.

## How to specify the version

You can pass the version using **one** of the methods below:

1. **URL path segment** (recommended)

```text
/api/v1/email/1
```

2. **Query string**

```text
/api/email/1?api-version=1.0
```

3. **Header**

```text
X-Version: 1.0
```

## Priority (when multiple are present)

The code resolves ambiguity using this order:

1. URL segment (highest priority)
2. Query string (`api-version`)
3. Header (`X-Version`)

If query string + header are both provided, the header is ignored (to avoid ambiguity).

## What happens if the version is missing

Requests under `/api/*` that are **not** using `/api/v{version}/...` and do **not** provide `api-version` or `X-Version` will receive:

-   HTTP `400` with a JSON payload explaining how to provide the version.
