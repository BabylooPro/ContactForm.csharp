# Email templates

The API supports:

-   A built-in default body (plain text or HTML)
-   Predefined templates via `Template`
-   Fully custom templates via `EmailTemplate` / `SubjectTemplate`

## Placeholders

Both subject and body templates support the following placeholders:

-   `{Email}`
-   `{Username}` (empty string if not provided)
-   `{Message}`

### Custom fields

For each entry in `CustomFields`, the key can be used as a placeholder:

-   If `CustomFields = { "subject": "Pricing", "orderId": "123" }`
-   You can use `{subject}` and `{orderId}` in templates

## Subject templates

If `SubjectTemplate` is omitted, the subject defaults to:

-   `Message from {Username}`

The API always appends the email ID:

-   `... - [A3F2B1C9]`

## Body templates

### Default body (when `EmailTemplate` is empty)

If `EmailTemplate` is not provided, the API renders a default template (HTML if `IsHtml=true`, otherwise plain text).

### Custom body (`EmailTemplate`)

Provide any string and use placeholders described above.

## Predefined templates (`Template`)

Set `Template` to one of:

-   `Default`
-   `Modern`
-   `Minimal`
-   `Professional`
-   `Alert`

Behavior:

-   The template body is applied to `EmailTemplate`
-   The template `IsHtml` overwrites `IsHtml`
-   If `SubjectTemplate` is empty, the template subject is used
