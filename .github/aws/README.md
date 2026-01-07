# AWS deployment assets

This folder contains AWS deployment helper files (used by GitHub Actions).

-   Deployment docs: [docs/deployment/aws-lambda.md](../../docs/deployment/aws-lambda.md)
-   Workflow: [aws-deploy.yml](../workflows/aws-deploy.yml)
-   IAM policy: [iam-user-policy.json](./iam-user-policy.json)

Notes:

-   The IAM policy is intentionally broad (`"Resource": "*"`), because the workflow manages multiple AWS resources.
