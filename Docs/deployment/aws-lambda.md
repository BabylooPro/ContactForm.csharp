# Deployment: AWS Lambda + GitHub Actions

This project is designed to run as an AWS Lambda function (ASP NET Core + AWS Lambda hosting).

## Where deployment is defined

-   GitHub Actions workflow: [`.github/workflows/aws-deploy.yml`](../../.github/workflows/aws-deploy.yml)
-   IAM policy example (for manual setup): [`.github/aws/iam-user-policy.json`](../../.github/aws/iam-user-policy.json)

## What the workflow does (high level)

-   Builds + packages the .NET app for Lambda
-   Creates/updates IAM roles
-   Deploys the Lambda function
-   Creates/updates API Gateway resources + methods
-   Configures CORS headers for API Gateway
-   Manages API keys / usage plans (if configured in the workflow)

## Required GitHub secrets (typical)

At minimum (depends on your workflow config):

-   `AWS_ACCESS_KEY_ID`
-   `AWS_SECRET_ACCESS_KEY`
-   SMTP secrets:
    -   `SMTP_1_PASSWORD`, `SMTP_1_PASSWORD_TEST`
    -   `SMTP_2_PASSWORD`, `SMTP_2_PASSWORD_TEST`
    -   ...

## IAM policy file

The file `.github/aws/iam-user-policy.json` is intentionally broad (`"Resource": "*"`), because the workflow needs to create/manage multiple AWS resources.

Related: [`.github/aws/README.md`](../../.github/aws/README.md) (minimal pointer doc inside that folder).
