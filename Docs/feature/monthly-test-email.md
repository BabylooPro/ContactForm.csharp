# Monthly test email service

A background service that automatically sends a test email once per month in production to verify the email API is functioning correctly.

## Behavior

-   Runs only in **Production** environment
-   Sends a test email every **30 days**
-   Uses the first SMTP configuration
-   Tracks last sent date in `.monthly-test-email-last-sent` file

## Testing in development

To enable in non-production environments, set:

```dotenv
MONTHLY_TEST_EMAIL_FORCE_ENABLE=true
```

## Rate limiting

If a test email attempt is blocked due to rate limiting:

-   The attempt is skipped (logged as debug)
-   The last sent date is **not** updated
-   The service retries on the next check cycle (30 days later)

## Test email details

-   **From**: Regular sender email from first SMTP configuration
-   **To**: `SMTP_RECEPTION_EMAIL`
-   **Subject**: `Monthly API Health Check - Test Email - [ID]`
-   **Sender address**: `monthly-test@system.local` (for rate limiting tracking)
