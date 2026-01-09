# Configuration

Configuration is loaded from:

-   `API/appsettings.json` (baseline config)
-   Environment variables (required for SMTP + CORS)
-   `API/.env` (loaded via `dotenv.net`; also used as a fallback reader for multiline values)

## SMTP configuration

SMTP servers are provided via **one env var**:

-   `SMTP_CONFIGURATIONS`: JSON array of SMTP configs

Schema (see `API/Models/SmtpConfig.cs`):

-   `Index` (int): unique ID used in routes (`/email/{smtpId}`)
-   `Host` (string)
-   `Port` (int)
-   `Email` (string): sender address for real emails
-   `TestEmail` (string): sender address for test emails
-   `Description` (string)

> **Note:** The order of SMTP configurations in the array matters. If no `smtpId` is specified in the API request, the first SMTP configuration in the list (first entry in the array) will be used by default.

### Required environment variables

-   `SMTP_CONFIGURATIONS`
-   `SMTP_{INDEX}_PASSWORD` (one per config)
-   `SMTP_RECEPTION_EMAIL` (recipient, required)
-   `SMTP_CATCHALL_EMAIL` (fallback recipient, required)

If you use the test endpoint, you also need:

-   `SMTP_{INDEX}_PASSWORD_TEST` (one per config, used only by `/test`)

### `SMTP_CONFIGURATIONS` examples

**Multiline format (readable, put it in `API/.env`):**

```dotenv
SMTP_CONFIGURATIONS="[
  {
    \"Index\": 1,
    \"Host\": \"smtp.example.com\",
    \"Port\": 465,
    \"Email\": \"contact@example.com\",
    \"TestEmail\": \"test@example.com\",
    \"Description\": \"Contact email for example.com website\"
  },
  {
    \"Index\": 2,
    \"Host\": \"smtp.example.com\",
    \"Port\": 587,
    \"Email\": \"contact-second@example.com\",
    \"TestEmail\": \"test-second@example.com\",
    \"Description\": \"Contact email for example.com website second\"
  }
]"
```

**Compact format (single line):**

```dotenv
SMTP_CONFIGURATIONS=[{"Index":1,"Host":"smtp.example.com","Port":465,"Email":"contact@example.com","TestEmail":"test@example.com","Description":"Contact email for example.com website"},{"Index":2,"Host":"smtp.example.com","Port":587,"Email":"contact-second@example.com","TestEmail":"test-second@example.com","Description":"Contact email for example.com website second"}]
```

## CORS configuration

Allowed origins are loaded from indexed variables:

-   `CORS_1_ORIGIN`
-   `CORS_2_ORIGIN`
-   `CORS_{INDEX}_ORIGIN` (…)

Notes:

-   All localhost origins are automatically allowed in code (`http://localhost:*`, `https://localhost:*`, `127.0.0.1`).
-   Origins are read sequentially starting at index 1 until a variable is missing.
