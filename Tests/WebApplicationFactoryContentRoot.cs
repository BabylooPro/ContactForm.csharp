using Microsoft.AspNetCore.Mvc.Testing;

// WEBAPPLICATIONFACTORY CONTENT ROOT WHEN SOLUTION FILE LIVES UNDER /API (NOT REPO ROOT)
[assembly: WebApplicationFactoryContentRoot(
    "API, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
    "../../../../API",
    "API.csproj",
    "0"
)]
