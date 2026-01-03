# DOCUMENTATION - CONTACTFORM API

## Overview

The ContactForm API is a RESTful web service designed to handle contact form submissions, providing email delivery functionality with support for multiple SMTP configurations, templates, and attachments. This API is built using .NET 8 and follows a minimal API approach to deploy in AWS Lambda Function.

## Getting Started

### Prerequisites

- .NET 8.0 SDK or higher
- SMTP server access for sending emails

## Dependencies

- Microsoft.AspNetCore (Web framework)
- Microsoft.Extensions.Logging (Logging)
- System.Net.Mail (Email functionality)
- dotenv net (Environment variable loading)
- AWS Lambda support libraries (for Lambda deployment)
- Asp Versioning (API Versioning support)

### Configuration

The API uses configuration from [appsettings.json](appsettings.json) and environment variables.

**SMTP configurations are loaded from the `SMTP_CONFIGURATIONS` environment variable** (JSON array format) to allow dynamic configuration without modifying `appsettings.json`.

#### Environment Variables

**Required:**

- `SMTP_CONFIGURATIONS`: JSON array of SMTP configurations

  **Multiline format (recommended (readable, but risky outside the .env))** - Use quotes for multiline values in your `.env` file:

  ```
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
      \"Description\": \"Contact email for example.com website seconde\"
    }
  ]"
  ```

  **Compact format (single line):**

  ```
  SMTP_CONFIGURATIONS=[{"Index":1,"Host":"smtp.example.com","Port":465,"Email":"contact@example.com","TestEmail":"test@example.com","Description":"Contact email for example.com website","Index":1},{"Index":2,"Host":"smtp.example.com","Port":587,"Email":"contact-second@example.com","TestEmail":"test-second@example.com","Description":"Contact email for example.com website seconde","Index":2}]
  ```

- `SMTP_{INDEX}_PASSWORD`: Password for the SMTP configuration with the specified Index
- `SMTP_{INDEX}_PASSWORD_TEST`: Password for the test email of the SMTP configuration with the specified Index
- `SMTP_RECEPTION_EMAIL`: Default recipient email for testing (required)
- `SMTP_CATCHALL_EMAIL`: Fallback email for catching undeliverable messages (required)

**CORS Configuration:**

- `CORS_{INDEX}_ORIGIN`: Allowed CORS origin URL (example: `CORS_1_ORIGIN=https://example.com`, `CORS_2_ORIGIN=https://another-domain.com`)
- All localhost URLs (http://localhost:_ and https://localhost:_) are automatically allowed for development, regardless of port number
- Origins are loaded sequentially starting from index 1 until no more variables are found

## API Versioning

The API implements versioning to ensure **backward compatibility** as the API evolves. All endpoints require an explicit version.

### Versioning Methods

The API supports 3 methods of specifying the version:

1. **URL Path** (Recommended)

   ```
   /api/v1/email/1
   ```

2. **Query String**

   ```
   /api/email/1?api-version=1.0
   ```

3. **Header**
   ```
   X-Version: 1.0
   ```

### Version Enforcement

- All requests must specify a version using one of the methods above.
- Requests without a version will receive a **400 Bad Request** response.
- The error message will provide guidance on how to specify a version.

### Current API Version

- The current API version is `1.0` (represented as `v1` in the URL path).

## Rate Limiting

The API implements a progressive rate limiting system to prevent spam and abuse:

- Each sender email is tracked separately per SMTP configuration
- First-time usage has no rate limiting
- Each subsequent usage increases the timeout period by 1 hour
- For example:
  - First submission: No waiting period
  - Second submission: 1 hour waiting period
  - Third submission: 2 hour waiting period
  - Fourth submission: 3 hour waiting period
- If a user attempts to submit while rate-limited, they receive a detailed error message with:
  - The remaining wait time in a human-readable format
  - Their current usage count
- Rate limits are tracked independently for each SMTP configuration

## API Request Rate Limiting and Anti-Abuse

In addition to the email submission rate limiting, the API implements an advanced request-level rate limiting and anti-abuse system:

### Standard Rate Limiting

- Each IP address is limited to 10 requests per minute
- When exceeded, returns HTTP 429 (Too Many Requests) with a "Retry-After" header
- Independent from the email submission rate limiting system

### Anti-Abuse Detection

- Monitors traffic patterns for suspicious activity:
  - **Burst Detection**: If an IP sends 20+ requests within a 5-second window, it triggers an automatic 1-hour block
  - **Excessive Traffic**: If an IP sends 100+ requests within a 10-minute window, it triggers an automatic 6-hour block
- Blocked IPs receive HTTP 403 (Forbidden) with an explanation message

### Implementation Details

- All IP tracking is done in-memory only (no permanent storage)
- Data automatically expires and is cleaned up after 30 minutes of inactivity
- No persistent tracking of user IPs
- Memory-efficient with automatic cleanup of expired data
- Restart of the application/Lambda function clears all tracking data

### Comprehensive Security Testing

The security features are thoroughly tested with dedicated test suites:

- **Unit Tests**:

  - `IpProtectionServiceTests`: Validates IP blocking, expiration, and abuse detection
  - `RateLimitingMiddlewareTests`: Tests request throttling and appropriate status codes

- **Integration Tests**:

  - `SecurityHeadersTests`: Ensures proper security headers are set for all responses
  - `RateLimitingIntegrationTests`: End-to-end testing of rate limiting with real HTTP requests
  - `IpSpoofingTests`: Tests detection and blocking of IP spoofing attempts

- **Performance and Concurrency Tests**:
  - `RateLimitingPerformanceTests`: Measures the performance impact of rate limiting
  - `IpProtectionServiceConcurrencyTests`: Validates thread safety under high concurrent load

These tests ensure that the security features work correctly under various conditions, including:

- High traffic scenarios
- Abuse and spam detection
- Concurrent access
- Different HTTP methods and routes
- Various user agents and client types

### Error Responses

| Status Code           | Description                           |
| --------------------- | ------------------------------------- |
| 429 Too Many Requests | Rate limit exceeded                   |
| 403 Forbidden         | IP blocked due to suspicious activity |

## API Reference

### Send Email

Sends an email using the specified SMTP configuration.

#### Request

```
POST /api/email/{smtpId}
```

##### Path Parameters

| Name   | Type    | Required | Description                         |
| ------ | ------- | -------- | ----------------------------------- |
| smtpId | integer | Yes      | ID of the SMTP configuration to use |

##### Request Body

| Property        | Type    | Required | Description                                           |
| --------------- | ------- | -------- | ----------------------------------------------------- |
| Email           | string  | Yes      | Sender's email address                                |
| Username        | string  | No       | Sender's name (optional)                              |
| Message         | string  | Yes      | Message content                                       |
| CustomFields    | object  | No       | Dictionary of custom fields for template substitution |
| EmailTemplate   | string  | No       | Custom email template with placeholders               |
| Template        | enum    | No       | Predefined template (overrides EmailTemplate if set)  |
| IsHtml          | boolean | No       | Whether to send as HTML email (default: false)        |
| Attachments     | array   | No       | List of file attachments                              |
| SubjectTemplate | string  | No       | Custom subject template with placeholders             |
| Priority        | enum    | No       | Email priority (Low, Normal, High, Urgent)            |

##### Attachment Object

| Property      | Type   | Required | Description                                       |
| ------------- | ------ | -------- | ------------------------------------------------- |
| FileName      | string | Yes      | Name of the attached file                         |
| Base64Content | string | Yes      | Base64-encoded file content                       |
| ContentType   | string | No       | MIME type (guessed from filename if not provided) |

#### Response

##### Success Response (200 OK)

```json
"Email sent successfully using SMTP_1 (sender@example.com -> recipient@example.com)"
```

##### Error Responses

| Status Code               | Description                                         |
| ------------------------- | --------------------------------------------------- |
| 400 Bad Request           | Invalid request data                                |
| 429 Too Many Requests     | Rate limit exceeded with time remaining information |
| 500 Internal Server Error | Failed to send email                                |

Rate limit error example:

```json
"This email has already been used to send a message with this SMTP server. You can send another message in 1 hour (Usage: 2)"
```

### Send Test Email

Sends a test email using the specified SMTP configuration and test email settings.

#### Request

```
POST /api/email/{smtpId}/test
```

Parameters and request body are the same as the Send Email endpoint.

#### Response

Similar to the Send Email endpoint but indicates it was a test email:

```json
"Test Email sent successfully using SMTP_1 (sender@example.com -> recipient@example.com)"
```

### Get SMTP Configurations

Retrieves all available SMTP configurations.

#### Request

```
GET /api/email/configs
```

#### Response

```json
[
  {
    "Index": 1,
    "Host": "smtp.example.com",
    "Port": 465,
    "Email": "contact@example.com",
    "TestEmail": "test@example.com",
    "Description": "Contact email for example.com website"
  },
  {
    "Index": 2,
    "Host": "smtp.example.com",
    "Port": 587,
    "Email": "contact-second@example.com",
    "TestEmail": "test-second@example.com",
    "Description": "Contact email for example.com website seconde"
  }
]
```

## Email Templates

### Custom Templates

Custom email templates may be created using placeholders in the `{PropertyName}` format :

```
FROM: {Email}
NAME: {Username}
MESSAGE: {Message}
CUSTOM_FIELD: {CustomFields.field_name}
```

If Username is empty, this field can be omitted in the template rendering.

### Subject Templates

Similar to email templates, subject templates support placeholders:

```
New message from {Username} regarding {CustomFields.subject}
```

### Predefined Templates

The API provides several predefined templates that can be selected using the `Template` property:

| Template Name | Description                                        |
| ------------- | -------------------------------------------------- |
| Default       | Standard formatted email with all fields displayed |
| Modern        | Clean, modern design with responsive layout        |
| Minimal       | Simple text-based format with minimal styling      |
| Professional  | Business-oriented template with formal styling     |
| Alert         | Emphasized design for important/urgent messages    |

## Error Handling

The API includes middleware for standardized error handling:

- Validation errors return 400 Bad Request with details
- Rate limiting violations return details with waiting time information
- SMTP connection failures initiate graceful shutdown
- Unexpected errors return 500 Internal Server Error

## Security Considerations

- The API uses CORS with appropriate headers for cross-domain requests
  - CORS origins are configured via environment variables (`CORS_{INDEX}_ORIGIN`)
  - All localhost URLs are automatically allowed for development (any port)
  - Only explicitly configured origins are allowed in production
- SMTP passwords are stored as environment variables, not in configuration files
- SMTP connections are tested at startup to ensure availability
- Separate test email configurations allow for safe testing without affecting production settings
- Rate limiting helps prevent abuse and spam
