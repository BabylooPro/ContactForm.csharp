### TODO LIST

**_Bugs to fix_**

- [ ] **fix:** ![MID][mid] deactivation of EnsureSmtpConnectionsAsync() in Program, caused by a conflict between IApplicationBuilder and WebApplication, using WebApplication allows EnsureSmtpConnectionsAsync() to function correctly but causes an issue in LambdaEntryPoint within ConfigureApp
- [ ] **fix:** ![LOW][low] test job running workflow make error (specify a project or solution file. The current working directory does not contain a project or solution file.)

**_New features to add_**

- [ ] **add:** system of catch all email service

---

#### IN PROGRESS

-

#### DONE

-

[high]: https://img.shields.io/badge/-HIGH-red
[mid]: https://img.shields.io/badge/-MID-yellow
[low]: https://img.shields.io/badge/-LOW-green
