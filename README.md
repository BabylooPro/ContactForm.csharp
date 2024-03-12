
# MINIMAL REST API FOR EMAIL SENDING

This project is a Minimal REST API under .NET 8 for sending e-mails via SMTP from a `Contact Form`.
It uses MailKit and MimeKit for constructing and sending emails and Xunit for integration tests.

https://github.com/BabylooPro/ContactForm.csharp/assets/35376790/cf8e36a2-6eb6-45fd-bbfc-dbf9eb498b4f

[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=BabylooPro_ContactForm.csharp&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=BabylooPro_ContactForm.csharp)
[![Bugs](https://sonarcloud.io/api/project_badges/measure?project=BabylooPro_ContactForm.csharp&metric=bugs)](https://sonarcloud.io/summary/new_code?id=BabylooPro_ContactForm.csharp)
[![Vulnerabilities](https://sonarcloud.io/api/project_badges/measure?project=BabylooPro_ContactForm.csharp&metric=vulnerabilities)](https://sonarcloud.io/summary/new_code?id=BabylooPro_ContactForm.csharp)
[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=BabylooPro_ContactForm.csharp&metric=sqale_rating)](https://sonarcloud.io/summary/new_code?id=BabylooPro_ContactForm.csharp)
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=BabylooPro_ContactForm.csharp&metric=security_rating)](https://sonarcloud.io/summary/new_code?id=BabylooPro_ContactForm.csharp)
[![Lines of Code](https://sonarcloud.io/api/project_badges/measure?project=BabylooPro_ContactForm.csharp&metric=ncloc)](https://sonarcloud.io/summary/new_code?id=BabylooPro_ContactForm.csharp)

## Prerequisites

To run this project, you will need:

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- An SMTP server (environment variables must be set to refer to it)

## Installation

Clone the repository to your local machine using the following command:

```bash
git clone https://github.com/BabylooPro/ContactForm.csharp.git
```

Navigate to the cloned project's folder:

```bash
For the API solution
cd ContactForm.csharp/ContactForm.MinimalAPI

For the Testing solution
cd ContactForm.csharp/ContactForm.Tests
```

## Project Configuration

Before launching the application, it is necessary to configure the environment variables. You have two options for creating the `.env` file that will contain these variables:

### Environment Variable Configuration

#### *Option 1: Using Command Line*
Run the following command in your terminal from `ContactForm.csharp/ContactForm.MinimalAPI` to automatically generate the `.env` file at the root of the `ContactForm.MinimalAPI` project. This command creates the file with the necessary configuration keys, but you will still need to fill them in with the appropriate values.

On macOS and Linux:
```bash
echo -e "SMTP_HOST=
SMTP_PORT=
SMTP_EMAIL=
SMTP_PASSWORD=
RECEPTION_EMAIL=" > .env
```

On Windows (cmd):
```cmd
(echo SMTP_HOST=& echo SMTP_PORT=& echo SMTP_EMAIL=& echo SMTP_PASSWORD=& echo RECEPTION_EMAIL=) > .env
```

On Windows (PowerShell):
```powershell
"SMTP_HOST=`nSMTP_PORT=`nSMTP_EMAIL=`nSMTP_PASSWORD=`nRECEPTION_EMAIL=" | Out-File -FilePath .env -Encoding UTF8
```

*After running the appropriate command for your operating system, open the `.env` file and enter the values corresponding to your SMTP configuration next to each key.*

#### *Option 2: Manual Creation*
Manually create a `.env` file at the root of the ContactForm.MinimalAPI solution and add the following lines, replacing the bracketed values with your own SMTP configuration information:

```env
SMTP_HOST=[your_smtp_server]
SMTP_PORT=[smtp_port]
SMTP_EMAIL=[your_smtp_email]
SMTP_PASSWORD=[your_smtp_password]
RECEPTION_EMAIL=[reception_email]
```

### IDE Configuration

To start working on this project, open it in an IDE compatible with C# and .NET, like Visual Studio, Visual Studio Code, or JetBrains Rider. The IDE should automatically restore NuGet packages and prepare the development environment.

If the packages are not automatically restored, run the following command at the project root:

```bash
dotnet build
```

*This command compiles the project, downloads package dependencies specified in the `.csproj` file, and prepares everything needed to run the application.*

## Starting the Application

To start the application, run the following command from the `ContactForm.MinimalAPI` folder:

```bash
dotnet run
```

The API will be accessible by default on `http://localhost:5108` for http and `https://localhost:7129` for https.

## Using the API

To send an email, use the `/api/email/send-email` endpoint with a POST request containing the following information:

```json
{
  "Email": "sender's_email_address",
  "Username": "sender's_name",
  "Message": "email_message"
}
```

You can use `curl` or a tool like Postman to make this request.

## Running Tests

To run the tests, use the following command in folder ContactForm.MinimalAPI from terminal:

```bash
dotnet test
```

This will run all the integration and validation tests defined in the test project.

## Contributing

If you wish to contribute to this project, please fork the repository, make your changes, and submit a pull request for review.

## License

This project is under the MIT License. See the [LICENSE](LICENSE) file for more details.

---

[![SonarCloud](https://sonarcloud.io/images/project_badges/sonarcloud-orange.svg)](https://sonarcloud.io/summary/new_code?id=BabylooPro_ContactForm.csharp)
