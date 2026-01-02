### TODO LIST

**_Bugs to fix_**

**_New features to add_**

- [ ] **add:** system of catch all email service

---

#### IN PROGRESS

-

#### DONE

- [x] **fixed:** clarify and handle ambiguous API version (when both query string and header are present) - now prioritizes query string over header and removes header to avoid ambiguity
- [x] **added:** unit or integration test to ensure all critical environment variables (e.g. `SMTP_2_PASSWORD`) are properly initialized before the application starts
- [x] **fixed:** security headers warnings (ASP0015) by using strongly-typed header properties instead of string indexers
- [x] **fixed:** deactivation of EnsureSmtpConnectionsAsync() in Program, caused by a conflict between IApplicationBuilder and WebApplication, using WebApplication allows EnsureSmtpConnectionsAsync() to function correctly but causes an issue in LambdaEntryPoint within ConfigureApp
- [x] **fixed:** test job running workflow make error (specify a project or solution file. The current working directory does not contain a project or solution file.)

[high]: https://img.shields.io/badge/-HIGH-red
[mid]: https://img.shields.io/badge/-MID-yellow
[low]: https://img.shields.io/badge/-LOW-green
