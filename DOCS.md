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

### Configuration

The API uses configuration from [appsettings.json](appsettings.json) and environment variables :

```json
{
  "SmtpSettings": {
    "Configurations": [
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
    ],
    "ReceptionEmail": "reception@example.com",
    "CatchAllEmail": "catchallemail@example.com"
  }
}
```

Each SMTP configuration requires corresponding environment variables:

- `SMTP_1_PASSWORD` for the password of the SMTP configuration with Index 1
- `SMTP_1_PASSWORD_TEST` for the password of the SMTP configuration with Index 1 for test email
- `SMTP_2_PASSWORD` for the password of the SMTP configuration with Index 2
- `SMTP_2_PASSWORD_TEST` for the password of the SMTP configuration with Index 2 for test email

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
| Username        | string  | Yes      | Sender's name                                         |
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

| Status Code               | Description          |
| ------------------------- | -------------------- |
| 400 Bad Request           | Invalid request data |
| 500 Internal Server Error | Failed to send email |

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
- SMTP connection failures initiate graceful shutdown
- Unexpected errors return 500 Internal Server Error

## Security Considerations

- The API uses CORS with appropriate headers for cross-domain requests
- SMTP passwords are stored as environment variables, not in configuration files
- SMTP connections are tested at startup to ensure availability
- Separate test email configurations allow for safe testing without affecting production settings
