# Error handling

This API uses middleware + ASP.NET Core built-ins to produce consistent errors.

## Missing API version

If you hit `/api/*` without specifying a version, you get:

-   HTTP `400`
-   JSON payload explaining the 3 supported methods:
    -   `/api/v1/...`
    -   `?api-version=1.0`
    -   `X-Version: 1.0`

## Unsupported API version

If a version is specified but unsupported:

-   HTTP `404`
-   JSON payload like:
    -   `title: "Unsupported API Version"`
    -   `supportedVersions: ["1.0"]`

## Validation errors

Because controllers use `[ApiController]`, invalid models typically return:

-   HTTP `400`
-   A standard ASP.NET validation payload (problem details)

## Email cooldown (anti-spam)

If a sender is rate-limited by the email cooldown:

-   HTTP `400`
-   A message string (human-readable wait time)

## Request throttling / anti-abuse

-   Too many requests per IP:
    -   HTTP `429`
    -   `Retry-After: 60`
-   IP blocked due to abuse:
    -   HTTP `403`

## Unhandled exceptions

Unhandled exceptions are converted to:

-   HTTP `500`
-   JSON: `{ "error": "An unexpected error has occurred. Please try again later." }`
