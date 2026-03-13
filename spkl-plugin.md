---
name: spkl-plugin
description: Scaffold a Visual Studio Dataverse plugin project with spkl deployment, assembly signing, and best-practice structure
---

Scaffold a complete Visual Studio **Dataverse Plugin** project using spkl for deployment.

## Gather Requirements

Ask the user for the following if not already provided:

1. **Solution/project name** — e.g. `Contoso.Plugins`
2. **Publisher prefix** — e.g. `con` (used for solution prefix)
3. **Target entity and message** — e.g. `account`, `Create`
4. **Stage** — PreValidation (10), PreOperation (20), or PostOperation (40)
5. **Execution mode** — Synchronous or Asynchronous
6. **Filtering attributes** — comma-separated logical names, or none
7. **Pre/Post images needed?** — yes/no and which attributes
8. **Additional plugin classes?** — list any extra entity/message combos

---

## Generate the Project Structure

Create the following folder and file structure:

```
<ProjectName>/
├── <ProjectName>.sln
├── spkl/
│   ├── spkl.csproj
│   ├── packages.config
│   ├── deploy-plugins.bat
│   └── deploy-plugins.sh
└── <ProjectName>/
    ├── <ProjectName>.csproj
    ├── <ProjectName>.snk          ← strong name key placeholder
    ├── spkl.json
    ├── Properties/
    │   └── AssemblyInfo.cs
    ├── PluginBase.cs
    └── <PluginClassName>.cs
```

---

## File Contents

### `<ProjectName>/<ProjectName>.csproj`

SDK-style project targeting .NET Framework 4.6.2 with assembly signing:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net462</TargetFramework>
    <AssemblyName>$(MSBuildProjectName)</AssemblyName>
    <RootNamespace>$(MSBuildProjectName)</RootNamespace>
    <SignAssembly>true</SignAssembly>
    <AssemblyOriginatorKeyFile>$(MSBuildProjectName).snk</AssemblyOriginatorKeyFile>
    <Optimize>true</Optimize>
    <Nullable>enable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.CrmSdk.CoreAssemblies" Version="9.0.2.49" />
    <PackageReference Include="spkl" Version="1.2.8">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

> **Note:** Tell the user to run `sn -k <ProjectName>.snk` in the project directory to generate the strong name key, or use Visual Studio's project properties to create it. Commit the `.snk` file to source control.

---

### `<ProjectName>/PluginBase.cs`

```csharp
using System;
using Microsoft.Xrm.Sdk;

namespace <Namespace>
{
    /// <summary>
    /// Base class for all Dataverse plugins. Provides a strongly-typed
    /// local context with tracing, service factory, and execution context.
    /// </summary>
    public abstract class PluginBase : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
                throw new ArgumentNullException(nameof(serviceProvider));

            var context = new LocalPluginContext(serviceProvider);
            context.Trace($"Entered {GetType().FullName}");

            try
            {
                ExecuteDataversePlugin(context);
            }
            catch (InvalidPluginExecutionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                context.Trace($"Unhandled exception: {ex}");
                throw new InvalidPluginExecutionException(
                    $"Unhandled exception in {GetType().FullName}: {ex.Message}", ex);
            }
            finally
            {
                context.Trace($"Exiting {GetType().FullName}");
            }
        }

        protected abstract void ExecuteDataversePlugin(LocalPluginContext context);
    }

    public class LocalPluginContext
    {
        public IServiceProvider ServiceProvider { get; }
        public IOrganizationServiceFactory ServiceFactory { get; }
        public IOrganizationService OrganizationService { get; }
        public IOrganizationService InitiatingUserService { get; }
        public IPluginExecutionContext ExecutionContext { get; }
        public ITracingService TracingService { get; }

        public LocalPluginContext(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            TracingService = serviceProvider.GetService<ITracingService>();
            ExecutionContext = serviceProvider.GetService<IPluginExecutionContext>();
            ServiceFactory = serviceProvider.GetService<IOrganizationServiceFactory>();
            OrganizationService = ServiceFactory.CreateOrganizationService(ExecutionContext.UserId);
            InitiatingUserService = ServiceFactory.CreateOrganizationService(ExecutionContext.InitiatingUserId);
        }

        public void Trace(string message) => TracingService.Trace(message);
    }
}
```

---

### `<ProjectName>/<PluginClassName>.cs`

Generate one class per plugin step. Use `CrmPluginRegistration` attributes to register steps — spkl reads these at deploy time.

```csharp
using Microsoft.Xrm.Sdk;
using Dataverse.Xrm;  // CrmPluginRegistration lives here (included via spkl NuGet)

namespace <Namespace>
{
    [CrmPluginRegistration(
        "<Message>",                     // e.g. "Create", "Update", "Delete", or custom action name
        "<EntityLogicalName>",           // e.g. "account", "contact", or "none" for global
        StageEnum.<Stage>,               // PreValidation=10, PreOperation=20, PostOperation=40
        ExecutionModeEnum.<Mode>,        // Synchronous or Asynchronous
        "<FilteringAttributes>",         // comma-separated or null for all attributes
        "<StepName>",                    // friendly name shown in Plugin Registration Tool
        1,                               // execution order
        IsolationModeEnum.Sandbox,
        Description = "<Description>"
        // Uncomment to add images:
        // Image1Name = "PreImage", Image1Type = ImageTypeEnum.PreImage, Image1Attributes = "name,statecode"
        // Image2Name = "PostImage", Image2Type = ImageTypeEnum.PostImage, Image2Attributes = ""
    )]
    public class <PluginClassName> : PluginBase
    {
        protected override void ExecuteDataversePlugin(LocalPluginContext context)
        {
            var target = context.ExecutionContext.InputParameters["Target"] as Entity;
            if (target == null) return;

            context.Trace($"Processing {target.LogicalName} Id={target.Id}");

            // TODO: implement plugin logic here
        }
    }
}
```

> **Multiple steps:** `[CrmPluginRegistration]` supports `AllowMultiple = true` — stack multiple attributes on one class for multiple step registrations.

---

### `<ProjectName>/spkl.json`

```json
{
  "$schema": "https://raw.githubusercontent.com/scottdurow/SparkleXrm/master/spkl/SparkleXrm.Tasks/spkl.schema.json",
  "plugins": [
    {
      "assemblypath": "bin\\Debug\\net462\\<ProjectName>.dll",
      "profile": "default",
      "classRegex": ".*",
      "connectionstring": "[[YOUR_CONNECTION_STRING]]"
    },
    {
      "assemblypath": "bin\\Release\\net462\\<ProjectName>.dll",
      "profile": "release",
      "classRegex": ".*",
      "connectionstring": "[[YOUR_CONNECTION_STRING]]"
    }
  ]
}
```

> **Connection string formats:**
> - Interactive login: `AuthType=OAuth;Url=https://org.crm.dynamics.com;AppId=51f81489-12ee-4a9e-aaae-a2591f45987d;RedirectUri=app://58145B91-0C36-4500-8554-080854F2AC97;LoginPrompt=Auto`
> - Service principal: `AuthType=ClientSecret;Url=https://org.crm.dynamics.com;ClientId=<AppId>;ClientSecret=<Secret>`
> - Username/Password: `AuthType=AD;Url=https://org.crm.dynamics.com;Username=user@org.onmicrosoft.com;Password=<Password>`

---

### `spkl/deploy-plugins.bat`

```bat
@echo off
cd /d "%~dp0"
dotnet build ..\<ProjectName>\<ProjectName>.csproj -c Debug
"%~dp0..\packages\spkl.*\tools\spkl.exe" plugins %* || (
  for /f "delims=" %%i in ('dir /b /s "%~dp0..\packages\spkl.*\tools\spkl.exe" 2^>nul') do set SPKL=%%i
  "%SPKL%" plugins %*
)
```

Or use the spkl NuGet tools path for SDK-style projects:

```bat
@echo off
dotnet build ..\<ProjectName>\<ProjectName>.csproj -c Debug
dotnet tool run spkl plugins ..\<ProjectName>\spkl.json
```

### `spkl/deploy-plugins.sh` (cross-platform)

```bash
#!/bin/bash
set -e
dotnet build ../<ProjectName>/<ProjectName>.csproj -c Debug
dotnet tool run spkl plugins ../<ProjectName>/spkl.json "$@"
```

---

## Post-Scaffold Instructions

Tell the user:

1. **Generate strong name key:**
   ```
   sn -k <ProjectName>.snk
   ```
   Or use Visual Studio → Project Properties → Signing → New key file.

2. **Update connection string** in `spkl.json` (never commit secrets — use environment variables or a local `spkl.json` override outside source control).

3. **Build and deploy:**
   ```
   dotnet build
   spkl plugins /p:default
   ```
   Or run `deploy-plugins.bat` / `deploy-plugins.sh`.

4. **Instrument existing org** (if connecting to an existing deployment):
   ```
   spkl instrument
   ```
   This downloads existing plugin step registrations and adds `[CrmPluginRegistration]` attributes to your classes.

5. **Add `.snk` to source control** but add connection strings to `.gitignore`.

---

## Best Practices Checklist

- [ ] Assembly is strongly named (`.snk` present and referenced in `.csproj`)
- [ ] Target framework is `net462` (required by Dataverse sandbox)
- [ ] Isolation mode is `Sandbox` (required for online environments)
- [ ] Plugin inherits from `PluginBase`, not directly from `IPlugin`
- [ ] No static state or static fields in plugin classes
- [ ] `InvalidPluginExecutionException` used for user-facing errors
- [ ] Tracing used throughout for diagnostics
- [ ] Filtering attributes specified on Update steps to avoid unnecessary executions
- [ ] Pre/Post images defined only for attributes actually needed
- [ ] Connection strings not committed to source control
- [ ] Each plugin assembly has its own `.snk` file
