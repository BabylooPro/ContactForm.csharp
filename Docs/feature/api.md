# API reference

Base path: `/api`

Versioning is required - see [API versioning](versioning.md).

## Endpoints

### Send email

Preferred (URL versioning):

```http
POST /api/v1/email/{smtpId}
Content-Type: application/json
```

Alternative (query string versioning):

```http
POST /api/email/{smtpId}?api-version=1.0
Content-Type: application/json
```

Alternative (header versioning):

```http
POST /api/email/{smtpId}
X-Version: 1.0
Content-Type: application/json
```

### Send test email

```http
POST /api/v1/email/{smtpId}/test
Content-Type: application/json
```

### List SMTP configurations

```http
GET /api/v1/email/configs
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

JSON property names are **PascalCase** (server uses `PropertyNamingPolicy = null`).

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

### Send email / test email

HTTP `200`:

```json
{
    "message": "Email sent successfully using SMTP_1 (sender@example.com -> recipient@example.com)",
    "emailId": "A3F2B1C9"
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
| `500`  | Unhandled error                    | JSON: `{ "error": "An unexpected error has occurred. Please try again later." }` |
