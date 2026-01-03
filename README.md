# CONTACT FORM MINIMAL API DOTNET 8.0 / AWS LAMBDA - API GATEWAY

[VERSION_BADGE]: https://img.shields.io/badge/dotnet-8.0.+-purple.svg
[LICENSE_BADGE]: https://img.shields.io/badge/license-MIT-blue.svg
[LICENSE_URL]: LICENSE
[TODO_BADGE]: https://img.shields.io/badge/TODO-gren.svg
[TODO_URL]: TODO.md
[DOCS_BADGE]: https://img.shields.io/badge/DOCS-blue.svg
[DOCS_URL]: DOCS.md

![Version][VERSION_BADGE] [![License][LICENSE_BADGE]][LICENSE_URL] [![TODO][TODO_BADGE]][TODO_URL] [![DOCS][DOCS_BADGE]][DOCS_URL]

A flexible and customizable contact form backend API built with .NET 8 Minimal API. This service enables easy integration of contact forms on websites by providing a robust email sending service with multiple SMTP configurations, customizable templates, and attachment support.

## Features

- Multiple SMTP configurations with failover support
- Customizable email templates (Default, Modern, Minimal, Professional, Alert)
- HTML email support with rich formatting
- Attachment handling with base64 encoding
- Email priority levels (Low, Normal, High, Urgent)
- AWS Lambda deployment support
- Environment variable configuration for secure credential management
- Advanced email tracking with rate limiting and progressive timeout
- Error handling middleware
- CORS configuration for cross-domain integration
- Optional username field for anonymous submissions

## Architecture

- **Services Layer**: Core services for email sending, SMTP testing, and template management
- **Interfaces**: Clean separation of concerns using dependency injection
- **Models**: Data models for email requests and SMTP configuration
- **Middleware**: Error handling and request processing
- **Controllers**: RESTful API endpoints
- **AWS Lambda Integration**: Support for serverless deployment

## Testing

The project includes a comprehensive test suite in the `ContactForm.Tests` project, covering all aspects of the application:

- **Unit Tests**:

  - `ModelsTests`: Validates data models and their validation rules
  - `ServicesTests`: Tests individual services in isolation with mocked dependencies
    - `EmailServiceTests`: Email generation and sending functionality
    - `EmailTrackingServiceTests`: Rate limiting and usage tracking
    - `IpProtectionServiceTests`: IP blocking, expiration, and abuse detection
    - `RateLimitingMiddlewareTests`: Request throttling, IP blocking, and rate limit checks
  - `ControllersTests`: Ensures API endpoints function correctly with mocked services

- **Integration Tests**:

  - `IntegrationTests`: End-to-end tests using `ApplicationFactory` to simulate real API interactions
  - `SecurityHeadersTests`: Validates security headers and CORS configurations across requests
  - `RateLimitingIntegrationTests`: Tests rate limiting functionality with real HTTP requests
  - `IpSpoofingTests`: Detects and blocks suspicious IP spoofing attempts

- **Performance and Concurrency Tests**:
  - `RateLimitingPerformanceTests`: Measures overhead of rate limiting middleware
  - `IpProtectionServiceConcurrencyTests`: Validates thread safety under concurrent traffic

The test project uses xUnit and ASPNET Core testing framework for thorough testing coverage.

To execute all tests in the `ContactForm.MinimalAPI` directory, run the following command:

```bash
dotnet test
```

## API Endpoints

- `POST /api/v1/email/{smtpId}` - Send an email using specified SMTP configuration
- `POST /api/v1/email/{smtpId}/test` - Send a test email using test email address
- `GET /api/v1/email/configs` - Get all available SMTP configurations
- `GET /api/v{version}/versiontest` - Test v1 and v2 versioning
- `GET /test` - Test if the API is running

## Api Versioning

The API supports 3 methods of versioning (URL Path, Query String, Header). See [DOCS.md](DOCS.md) for detailed information.

## Rate Limiting

The API implements a dual-layer rate limiting system (email submission + API request level). See [DOCS.md](DOCS.md) for detailed information.

## Documentation

The API documentation is available in two formats:

- **Detailed Documentation**: See the [DOCS.md](DOCS.md) file for comprehensive API documentation including request/response formats, templates, and configuration details.
- **Interactive Swagger UI**: When running the API locally, access the Swagger documentation at the root URL `http://localhost:5108/` on navigator. Swagger provides an interactive interface to explore and test all API endpoints.

## Prerequisites

Required for this project:

- .NET SDK 8.0+
- SMTP server access for sending emails
- Environment variables for SMTP configurations

## Configuration

SMTP configurations and CORS origins are loaded from environment variables. See [DOCS.md](DOCS.md) for detailed configuration instructions and examples.

### CORS Configuration

CORS origins are configured using indexed environment variables:

- `CORS_1_ORIGIN` - First allowed origin (example:, `https://example.com`)
- `CORS_2_ORIGIN` - Second allowed origin (example:, `https://another-domain.com`)
- `CORS_{INDEX}_ORIGIN` - Additional origins as needed

**Note:** All localhost URLs (http://localhost:_ and https://localhost:_) are automatically allowed for development purposes, regardless of port number.

## Installation

```bash
# CLONE THE REPOSITORY
git clone https://github.com/BabylooPro/ContactForm.csharp.git

# NAVIGATE TO PROJECT DIRECTORY
cd ContactForm.csharp/ContactForm.MinimalAPI

# CREATE .ENV FILE (see DOCS.md for detailed configuration examples)
# Minimum required variables:
# - SMTP_CONFIGURATIONS (JSON array)
# - SMTP_1_PASSWORD, SMTP_1_PASSWORD_TEST
# - SMTP_RECEPTION_EMAIL, SMTP_CATCHALL_EMAIL
# - CORS_1_ORIGIN, CORS_2_ORIGIN (allowed CORS origins)

# RESTORE DEPENDENCIES
dotnet restore

# CLEAN SOLUTION
dotnet clean

# BUILD SOLUTION
dotnet build

# RUN TESTS
dotnet test

# RUN PROJECT
dotnet run
```

## AWS Lambda Deployment

The project includes AWS Lambda integration via the `LambdaEntryPoint.cs` class and is deployed automatically through GitHub Actions.

### Automated Deployment (GitHub Actions)

Deployment is fully automated using the GitHub Actions workflow defined in [.github/workflows/aws-deploy.yml](.github/workflows/aws-deploy.yml).

**This workflow includes:**

- Builds and packages the application
- Creates necessary IAM roles and permissions for Lambda execution
- Deploys to AWS Lambda with proper environment variables
- Configures API Gateway with REST endpoints and proxy resources
- Sets up usage plans, throttling limits, and API keys
- Configures CORS for cross-domain access
- Preserves API Gateway ID across deployments for endpoint stability

**Workflow Structure:**

1. First creates OIDC role for GitHub Actions with necessary permissions
2. Runs tests for the application (optional)
3. Builds and packages the .NET application for Lambda
4. Creates IAM execution roles for Lambda function
5. Deploys Lambda function with environment configuration
6. Creates or reuses API Gateway with proper resources and methods
7. Sets up Lambda permissions for API Gateway invocation
8. Configures CORS headers for all API resources
9. Creates or reuses API keys and usage plans

_For detailed documentation of this deployment workflow, see this [README](https://github.com/BabylooPro/TEMPLATE-DEPLOY-WORKFLOW-DOTNET-AWS-LAMBDA-APIGATEWAY/blob/main/README.md)._

**To trigger deployment:**

1. Manually trigger the "Deploy to AWS Lambda" workflow from the GitHub Actions tab `https://github.com/[OWNER]/[REPO]/actions/workflows/aws-deploy.yml` and click `Run workflow` button.

**Required GitHub secrets:**

- `AWS_ACCESS_KEY_ID` - AWS access key with deployment permissions
- `AWS_SECRET_ACCESS_KEY` - AWS secret key
- `SMTP_1_PASSWORD` - SMTP password for configuration 1 for regular email
- `SMTP_1_PASSWORD_TEST` - SMTP password for configuration 1 for test email
- `SMTP_2_PASSWORD` - SMTP password for configuration 2 for regular email
- `SMTP_2_PASSWORD_TEST` - SMTP password for configuration 2 for test email

### Manual Deployment (UNTESTED)

For manual deployment:

1. Configure AWS credentials locally
2. Build the project with `dotnet publish`
3. Deploy using AWS SAM or AWS CDK commands

## Example Usage

Send an email via the API:

```http
POST /api/v1/email/1
Content-Type: application/json

{
  "Email": "sender@example.com",                // SENDER EMAIL ADDRESS (REQUIRED)
  "Username": "John Doe",                       // SENDER NAME (OPTIONAL)
  "Message": "Hello, this is a test message",   // MESSAGE CONTENT (REQUIRED)
  "IsHtml": false,                              // SET TO TRUE FOR HTML-FORMATTED EMAILS
  "Priority": "Normal"                          // EMAIL PRIORITY (LOW, NORMAL, HIGH, URGENT)
}
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
