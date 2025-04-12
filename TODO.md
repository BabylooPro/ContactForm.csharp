### TODO LIST

**_Bugs to fix_**

- [ ] **fix:** clarify and handle ambiguous API version (when both query string and header are present)
    - Goal: avoid `Requested API version is ambiguous` warning
    - Idea: either prioritize one version source (e.g. query > header), or explicitly reject multiple version definitions with a custom response

**_New features to add_**

- [ ] **add:** unit or integration test to ensure all critical environment variables (e.g. `SMTP_2_PASSWORD`) are properly initialized before the application starts
    - Goal: avoid runtime errors like `System.InvalidOperationException: The following environment variables are missing or empty`
    - Idea: mock the environment or use `Environment.SetEnvironmentVariable` in tests to simulate missing/valid scenarios
- [ ] **add:** system of catch all email service

---

#### IN PROGRESS

-

#### DONE

- [x] **fixed:** security headers warnings (ASP0015) by using strongly-typed header properties instead of string indexers
- [x] **fixed:** deactivation of EnsureSmtpConnectionsAsync() in Program, caused by a conflict between IApplicationBuilder and WebApplication, using WebApplication allows EnsureSmtpConnectionsAsync() to function correctly but causes an issue in LambdaEntryPoint within ConfigureApp
- [x] **fixed:** test job running workflow make error (specify a project or solution file. The current working directory does not contain a project or solution file.)

[high]: https://img.shields.io/badge/-HIGH-red
[mid]: https://img.shields.io/badge/-MID-yellow
[low]: https://img.shields.io/badge/-LOW-green
