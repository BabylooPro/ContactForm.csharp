# IAM User Policy for AWS Lambda Deployment

This directory contains the IAM policy required for the GitHub Actions workflow [aws-deploy.yml](.github/workflow/aws-deploy.yml) to function properly.

## Purpose

The file [iam-user-policy.json](.github/workflow/aws/iam-user-policy.json) contains an IAM policy that grants all necessary permissions for automated deployment without requiring AWS Console interaction after initial setup.

## Required Manual Steps

1. Create an IAM User in AWS Console
2. Attach the policy from `iam-user-policy.json` content to the user
3. Generate AWS Access Keys for GitHub Actions Environment Secrets (`AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY`)

## Permissions Granted

This policy allows:

- Managing OpenID Connect for GitHub Actions
- Creating and managing IAM roles
- Full Lambda function control
- Complete API Gateway management
- CloudWatch Logs access for monitoring

> **⚠ Important Notes**
>
> - The policy uses `"Resource": "*"` to ensure the workflow can create and manage all necessary resources.
> - This broad access is required for fully automated deployment.
> - No further AWS Console interaction is needed after initial setup.
> - All permissions are scoped to the minimum required for the deployment workflow.
