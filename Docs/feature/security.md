# Security

## CORS

CORS is configured via env vars (see [Configuration](../configuration.md)):

-   `CORS_{INDEX}_ORIGIN`

Behavior:

-   All localhost/127.0.0.1 origins are allowed (any port)
-   Only configured origins are allowed otherwise

## Security headers

The API sets basic security headers on all responses:

-   `X-Content-Type-Options: nosniff`
-   `X-Frame-Options: DENY`
-   `X-XSS-Protection: 1; mode=block`
-   `Referrer-Policy: strict-origin-when-cross-origin`
-   `Content-Security-Policy: default-src 'self'`

## IP tracking / privacy

-   Request-level tracking is in-memory only
-   Old tracking data is pruned (see [Rate limiting & anti-abuse](rate-limiting.md))
