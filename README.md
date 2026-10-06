# TabularDB

## Technology and projects

- `Desktop/TabularDB.Desktop/` - .NET 10 WPF client using MVVM and HTTP.
- `Core/TabularDB.Core/` - reusable database model, schema operations, and value validation.
- `Server/TabularDB.Server/` - ASP.NET Core REST API and JSON file persistence, shared by desktop and web clients.
- `Tests/TabularDB.Core.Tests/` - unit tests for schema operations and typed value rules.
- `Web/` - React and TypeScript browser client for the shared REST API.
- `UML/` and `Reports/` - UML PDFs and Reports.
- `TabularDB.slnx` - .NET solution.

## Run the application

Start the API in one terminal:

```powershell
dotnet run --project Server/TabularDB.Server
```

Then start the WPF client in another terminal:

```powershell
dotnet run --project Desktop/TabularDB.Desktop
```

## Run the Web client

Start the shared API in one terminal from the repository root:

```powershell
dotnet run --project Server/TabularDB.Server
```

In a second terminal, install the Web dependencies and start Vite:

```powershell
cd Web
pnpm install
pnpm dev
```

## Build and tests

```powershell
dotnet build TabularDB.slnx
dotnet test TabularDB.slnx
```
