# Rate limiting & anti-abuse

There are **two independent layers**:

1. **Email submission cooldown** (per sender email + SMTP config)
2. **Request-level throttling + anti-abuse** (per IP)

## 1) Email submission cooldown (anti-spam)

Scope:

-   Key = `{senderEmail}:{smtpIndex}`

Rules (progressive cooldown):

-   First submission: allowed
-   After that, cooldown increases by **1 hour per usage count**
    -   Usage 1 → wait 1 hour
    -   Usage 2 → wait 2 hours
    -   Usage 3 → wait 3 hours
    -   …

Response when blocked:

-   HTTP `400` with a human-readable message like:
    -   `This email has already been used to send a message recently. You can send another message in 1 hour (Usage: 2)`

Notes:

-   In-memory only (resets when the process/Lambda container is restarted)
-   Tracked per SMTP configuration index

## 2) Request throttling (per IP)

Standard throttling:

-   10 requests per minute per IP
-   When exceeded:
    -   HTTP `429 Too Many Requests`
    -   `Retry-After: 60`

## Anti-abuse auto-blocking (per IP)

Burst detection:

-   If an IP sends **≥ 20 requests in 5 seconds** → blocked for **1 hour**

Excessive traffic:

-   If an IP sends **> 100 requests in 10 minutes** → blocked for **6 hours**

When blocked:

-   HTTP `403 Forbidden`
-   Body: `Your IP address has been blocked due to suspicious activity.`

Data retention:

-   Tracking data is in-memory and pruned (keeps ~30 minutes of request timestamps).
