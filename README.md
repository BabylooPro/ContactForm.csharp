# CONTACT FORM MINIMAL API DOTNET 8.0 / AWS LAMBDA - API GATEWAY

[VERSION_BADGE]: https://img.shields.io/badge/dotnet-8.0.+-purple.svg
[LICENSE_BADGE]: https://img.shields.io/badge/license-MIT-blue.svg
[LICENSE_URL]: LICENSE
[TODO_BADGE]: https://img.shields.io/badge/TODO-gren.svg
[TODO_URL]: TODO.md

![Version][VERSION_BADGE] [![License][LICENSE_BADGE]][LICENSE_URL] [![TODO][TODO_BADGE]][TODO_URL]

A flexible and customizable contact form backend API built with .NET 8 Minimal API. This service enables easy integration of contact forms on websites by providing a robust email sending service with multiple SMTP configurations, customizable templates, and attachment support.

## Features

- Multiple SMTP configurations with failover support
- Customizable email templates (Default, Modern, Minimal, Professional, Alert)
- HTML email support with rich formatting
- Attachment handling with base64 encoding
- Email priority levels (Low, Normal, High, Urgent)
- AWS Lambda deployment support
- Environment variable configuration for secure credential management
- Custom email tracking service
- Error handling middleware
- CORS configuration for cross-domain integration

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
  - `ControllersTests`: Ensures API endpoints function correctly with mocked services

- **Integration Tests**:
  - `IntegrationTests`: End-to-end tests using `ApplicationFactory` to simulate real API interactions
  - Tests SMTP configuration, email sending with various templates and configurations

The test project uses xUnit and ASPNET Core testing framework for thorough testing coverage.

To execute all tests in the `ContactForm.MinimalAPI` directory, run the following command:

```bash
dotnet test
```

## API Endpoints

- `POST /api/email/{smtpId}` - Send an email using specified SMTP configuration
- `POST /api/email/{smtpId}/test` - Send a test email using test email address
- `GET /api/email/configs` - Get all available SMTP configurations
- `GET /test` - Test if the API is running

## Prerequisites

Required for this project:

- .NET SDK 8.0+
- SMTP server access for sending emails
- Environment variables for SMTP configurations

## Configuration

### SMTP Settings (appsettings.json)

```json
"SmtpSettings": {
  "Configurations": [
    {
      "Host": "smtp.example.com",               // SMTP SERVER HOSTNAME
      "Port": 465,                              // SMTP SERVER PORT (TYPICALLY 465 FOR SSL, 587 FOR TLS)
      "Email": "contact@main-example.com",      // PRIMARY EMAIL ADDRESS FOR SENDING EMAILS
      "TestEmail": "test@main-example.com",     // TEST EMAIL ADDRESS FOR TESTING FUNCTIONALITY
      "Description": "Main contact email",      // DESCRIPTION FOR IDENTIFYING THIS CONFIGURATION
      "Index": 1                                // UNIQUE INDEX FOR REFERENCING THIS CONFIGURATION
    },
    {
      "Host": "smtp.example.com",               // SECOND SMTP SERVER (CAN BE SAME HOST)
      "Port": 465,                              // SECOND SMTP PORT
      "Email": "contact@second-example.com",    // SECONDARY EMAIL FOR SENDING
      "TestEmail": "test@second-example.com",   // SECONDARY TEST EMAIL
      "Description": "Second contact email",    // DESCRIPTION FOR SECOND CONFIGURATION
      "Index": 2                                // UNIQUE INDEX FOR SECOND CONFIGURATION (MUST BE DIFFERENT)
    }
  ],
  "ReceptionEmail": "reception@example.com",    // DEFAULT RECIPIENT EMAIL FOR TESTING
  "CatchAllEmail": "catchall@example.com"       // FALLBACK EMAIL FOR CATCHING UNDELIVERABLE MESSAGES
}
```

### Environment Variables (.env)

```
# MAIN REGULAR EMAIL PASSWORD
SMTP_1_PASSWORD=password_value_here

# MAIN TEST EMAIL PASSWORD
SMTP_1_PASSWORD_TEST=test_password_value_here

# SECOND REGULAR EMAIL PASSWORD
SMTP_2_PASSWORD=password_value_here

# SECOND TEST EMAIL PASSWORD
SMTP_2_PASSWORD_TEST=test_password_value_here
```

## Installation

```bash
# CLONE THE REPOSITORY
git clone https://github.com/BabylooPro/ContactForm.csharp.git

# NAVIGATE TO PROJECT DIRECTORY
cd ContactForm.csharp/ContactForm.MinimalAPI

# CREATE .ENV FILE ON MACOS/LINUX
cat > .env << EOF
# MAIN REGULAR EMAIL PASSWORD
SMTP_1_PASSWORD=password_value_here

# MAIN TEST EMAIL PASSWORD
SMTP_1_PASSWORD_TEST=test_password_value_here

# SECOND REGULAR EMAIL PASSWORD
SMTP_2_PASSWORD=password_value_here

# SECOND TEST EMAIL PASSWORD
SMTP_2_PASSWORD_TEST=test_password_value_here
EOF

# OR CREATE .ENV FILE ON WINDOWS (POWERSHELL)
@"
# MAIN REGULAR EMAIL PASSWORD
SMTP_1_PASSWORD=password_value_here

# MAIN TEST EMAIL PASSWORD
SMTP_1_PASSWORD_TEST=test_password_value_here

# SECOND REGULAR EMAIL PASSWORD
SMTP_2_PASSWORD=password_value_here

# SECOND TEST EMAIL PASSWORD
SMTP_2_PASSWORD_TEST=test_password_value_here
"@ | Out-File -FilePath .env -Encoding utf8

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
POST /api/email/1
Content-Type: application/json

{
  "Email": "sender@example.com",                // SENDER EMAIL ADDRESS
  "Username": "John Doe",                       // SENDER NAME
  "Message": "Hello, this is a test message",   // MESSAGE CONTENT
  "IsHtml": false,                              // SET TO TRUE FOR HTML-FORMATTED EMAILS
  "Priority": "Normal"                          // EMAIL PRIORITY (LOW, NORMAL, HIGH, URGENT)
}
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
