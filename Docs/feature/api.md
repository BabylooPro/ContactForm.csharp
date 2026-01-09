# API reference

Base path: `/api`

Versioning is required - see [API versioning](versioning.md).

## API type

This project exposes a **RESTful HTTP API** (resource-oriented).

Notes:

-   It is **RESTful** in practice (resources + HTTP verbs/status codes).
-   It is **not “strict REST”** (no HATEOAS links, no ETag/conditional requests, and the `Email` resource is currently stored in-memory).

## Endpoints

### Create email (send)

Preferred (URL versioning):

```http
POST /api/v1/emails?smtpId={smtpId}
Content-Type: application/json
```

Without `smtpId` (uses first SMTP configuration):

```http
POST /api/v1/emails
Content-Type: application/json
```

Optional test mode:

```http
POST /api/v1/emails?smtpId={smtpId}&test=true
Content-Type: application/json
```

Alternative (query string versioning):

```http
POST /api/emails?smtpId={smtpId}&api-version=1.0
Content-Type: application/json
```

Alternative (header versioning):

```http
POST /api/emails?smtpId={smtpId}
X-Version: 1.0
Content-Type: application/json
```

> **Note:** The `smtpId` query parameter is optional. If not specified, the request will be sent using the first SMTP configuration in the list (the first entry in `SMTP_CONFIGURATIONS`).

### Get email (by id)

```http
GET /api/v1/emails/{emailId}
```

### List SMTP configurations

```http
GET /api/v1/smtp-configurations
GET /api/v1/smtp-configurations/{smtpId}
```

### Version test endpoint

```http
GET /api/v1/versiontest
GET /api/v2/versiontest
```

### Health check

```http
GET /test
```

## Request body: `EmailRequest`

JSON property names are **case-insensitive** (examples use PascalCase).

| Property          | Type                  | Required | Notes                                                             |
| ----------------- | --------------------- | -------- | ----------------------------------------------------------------- |
| `Email`           | string                | yes      | Sender email (validated)                                          |
| `Username`        | string                | no       | Optional sender name                                              |
| `Message`         | string                | yes      | Message text                                                      |
| `CustomFields`    | object<string,string> | no       | Key/value pairs used in templates as `{key}`                      |
| `EmailTemplate`   | string                | no       | Custom body template (see [Email templates](templates.md))        |
| `Template`        | string enum           | no       | One of: `Default`, `Modern`, `Minimal`, `Professional`, `Alert`   |
| `IsHtml`          | bool                  | no       | Default: `false`                                                  |
| `Attachments`     | array                 | no       | List of attachments (see below)                                   |
| `SubjectTemplate` | string                | no       | Custom subject template (see [Email templates](templates.md))     |
| `Priority`        | string enum           | no       | One of: `Low`, `Normal`, `High`, `Urgent`                         |
| `EmailId`         | string                | no       | **Read-only**: generated server-side; client value is overwritten |

### Attachment object

| Property        | Type   | Required | Notes                             |
| --------------- | ------ | -------- | --------------------------------- |
| `FileName`      | string | yes      | Attachment name                   |
| `Base64Content` | string | yes      | Base64 encoded bytes              |
| `ContentType`   | string | no       | If missing, guessed from filename |

## Success responses

### Create email (send)

HTTP `201` + `Location: /api/v1/emails/{emailId}`:

```json
{
    "id": "A3F2B1C9",
    "status": "Sent",
    "requestedSmtpId": 1,
    "isTest": false,
    "createdAt": "2026-01-07T12:34:56.789Z",
    "receptionEmail": "recipient@example.com"
}
```

The `emailId` is also appended to the subject: `... - [A3F2B1C9]`.

## Error responses (most common)

| Status | When                               | Payload                                                                          |
| ------ | ---------------------------------- | -------------------------------------------------------------------------------- |
| `400`  | Missing API version                | JSON with examples (`/api/v1/...`, `?api-version=1.0`, `X-Version`)              |
| `400`  | Validation errors / email cooldown | Validation problem details or a message string                                   |
| `403`  | IP blocked (anti-abuse)            | Plain text: `Your IP address has been blocked due to suspicious activity.`       |
| `404`  | Unsupported API version            | JSON with supported versions                                                     |
| `429`  | Too many requests per IP           | Plain text + `Retry-After: 60`                                                   |
| `502`  | SMTP delivery failed               | `application/problem+json` (ProblemDetails)                                      |
| `500`  | Unhandled error                    | JSON: `{ "error": "An unexpected error has occurred. Please try again later." }` |
