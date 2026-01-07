# ContactForm API (.NET 8 + AWS Lambda)

[VERSION_BADGE]: https://img.shields.io/badge/dotnet-8.0.+-purple.svg
[LICENSE_BADGE]: https://img.shields.io/badge/license-MIT-blue.svg
[LICENSE_URL]: LICENSE
[TODO_BADGE]: https://img.shields.io/badge/TODO-gren.svg
[TODO_URL]: TODO.md
[DOCS_BADGE]: https://img.shields.io/badge/DOCS-blue.svg
[DOCS_URL]: docs/README.md

![Version][VERSION_BADGE] [![License][LICENSE_BADGE]][LICENSE_URL] [![TODO][TODO_BADGE]][TODO_URL] [![DOCS][DOCS_BADGE]][DOCS_URL]

Backend RESTful API for contact form submissions: multi-SMTP failover, templates, attachments, API versioning, rate limiting. Runs locally or on AWS Lambda.

## Documentation

##### Setup

-   [Getting started](docs/getting-started.md)
-   [Configuration (env vars, SMTP, CORS)](docs/configuration.md)
-   [Deployment (AWS Lambda + GitHub Actions)](docs/deployment/aws-lambda.md)

##### Feature

-   [API reference (endpoints, payloads, responses)](docs/feature/api.md)
-   [API versioning](docs/feature/versioning.md)
-   [Rate limiting & anti-abuse](docs/feature/rate-limiting.md)
-   [Email templates](docs/feature/templates.md)
-   [Error handling](docs/feature/error-handling.md)
-   [Security](docs/feature/security.md)

## Quick start

```bash
cd API
dotnet restore
dotnet run
```

-   Swagger UI: `http://localhost:5108/`
-   Health check: `GET http://localhost:5108/test`

## Configuration

Minimum required env vars:

-   `SMTP_CONFIGURATIONS`
-   `SMTP_{INDEX}_PASSWORD`
-   `SMTP_RECEPTION_EMAIL`
-   `SMTP_CATCHALL_EMAIL`

Optional (prod CORS allow-list):

-   `CORS_{INDEX}_ORIGIN`

See [docs/configuration.md](docs/configuration.md).

## API

Primary endpoints (v1):

-   `POST /api/v1/emails?smtpId={smtpId}`
-   `POST /api/v1/emails?smtpId={smtpId}&test=true`
-   `GET /api/v1/emails/{emailId}`
-   `GET /api/v1/smtp-configurations`
-   `GET /api/v1/versiontest`

Minimal local call example:

```bash
curl -X POST "http://localhost:5108/api/v1/emails?smtpId=1" \
  -H "Content-Type: application/json" \
  -d '{"Email":"sender@example.com","Username":"John Doe","Message":"Hello"}'
```

Details: [docs/api.md](docs/feature/api.md) and [docs/versioning.md](docs/feature/versioning.md).

## Deployment

-   AWS Lambda + GitHub Actions: [docs/deployment/aws-lambda.md](docs/deployment/aws-lambda.md)

## License

MIT - see [LICENSE](LICENSE).
