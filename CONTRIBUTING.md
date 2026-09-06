# Contributing to JobTrack

Thank you for helping improve JobTrack. Bug reports, documentation updates,
tests, and focused feature contributions are welcome.

## Before you start

- Search existing issues before opening a new one.
- Open an issue before beginning a large feature or architectural change.
- Do not include credentials, personal job-search data, or generated build output.
- Keep pull requests focused on one change.

## Local development

Install the .NET 10 SDK for the API and tests. The optional Azure Functions
project also requires the .NET 8 SDK and Azure Functions Core Tools v4.

```bash
dotnet restore JobTrack.slnx
dotnet build JobTrack.slnx
dotnet test JobTrack.slnx
```

Follow `README.md` to configure SQL Server and run the projects. Store connection
strings in user secrets, environment variables, or an untracked Functions
`local.settings.json` file.

## Pull requests

Before submitting a pull request:

1. Build the affected projects without warnings.
2. Add or update tests for behavioral changes.
3. Run `dotnet test JobTrack.slnx`.
4. Update documentation when configuration, endpoints, or behavior changes.
5. Explain the motivation, implementation, and verification in the pull request.

By contributing, you agree that your contribution is licensed under the MIT
License. All participants must follow `CODE_OF_CONDUCT.md`.
