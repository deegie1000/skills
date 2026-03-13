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
8. **Does the plugin need Newtonsoft.Json (Json.NET) or other third-party NuGet dependencies?**
   - If **yes**: the project must be structured as a **Plugin Package** (NuGet package deployed to the `PluginPackage` table). Ask the user to confirm this approach before proceeding.
   - If **no**: standard single-assembly plugin (no bundling needed).
9. **Additional plugin classes?** — list any extra entity/message combos

---

## Project Type Decision

### Standard Plugin (no third-party dependencies)

Use when the plugin only references `Microsoft.CrmSdk.*` / `Microsoft.PowerPlatform.Dataverse.Client` packages that are already present in the Dataverse sandbox. This is the simpler setup.

### Plugin Package (has third-party dependencies, e.g. Newtonsoft.Json)

Use when the plugin needs NuGet packages not already available in the Dataverse sandbox (e.g. `Newtonsoft.Json`, `Polly`, `FluentValidation`). The project is packaged as a `.nupkg` and deployed to Dataverse as a `PluginPackage` record. Dataverse then loads the assembly and its dependencies from the package.

> **ILMerge is NOT supported** by Microsoft for Dataverse plugins. Plugin Packages are the official replacement.

---

## Generate the Project Structure

### Standard Plugin

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

### Plugin Package (with dependencies)

```
<ProjectName>/
├── <ProjectName>.sln
├── spkl/
│   ├── spkl.csproj
│   ├── packages.config
│   ├── deploy-plugins.bat
│   └── deploy-plugins.sh
└── <ProjectName>/
    ├── <ProjectName>.csproj       ← configured to produce a .nupkg
    ├── <ProjectName>.snk
    ├── spkl.json                  ← references the .nupkg
    ├── PluginBase.cs
    └── <PluginClassName>.cs
```

---

## File Contents

### `<ProjectName>/<ProjectName>.csproj` — Standard Plugin

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
    <PackageReference Include="Microsoft.CrmSdk.CoreAssemblies" Version="9.0.2.60" />
    <PackageReference Include="spkl" Version="1.0.640">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

---

### `<ProjectName>/<ProjectName>.csproj` — Plugin Package (with dependencies)

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

    <!-- Plugin Package settings -->
    <IsPackable>true</IsPackable>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
    <PackageId>$(MSBuildProjectName)</PackageId>
    <Version>1.0.0</Version>
    <Authors>YourName</Authors>
    <Description>Dataverse Plugin Package for $(MSBuildProjectName)</Description>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.CrmSdk.CoreAssemblies" Version="9.0.2.60" />
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
    <!-- Add other dependencies here -->
    <PackageReference Include="spkl" Version="1.0.640">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

> **Note:** Tell the user to run `sn -k <ProjectName>.snk` to generate the strong name key, or use Visual Studio → Project Properties → Signing. Commit the `.snk` to source control.

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

Generate one class per plugin step. Use `CrmPluginRegistration` attributes — spkl reads these at deploy time.

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

> **Pre/Post image availability:**
> | Message | Stage | PreImage | PostImage |
> |---------|-------|----------|-----------|
> | Create  | PRE   | No       | No        |
> | Create  | POST  | No       | Yes       |
> | Update  | PRE   | Yes      | No        |
> | Update  | POST  | Yes      | Yes       |
> | Delete  | PRE   | Yes      | No        |
> | Delete  | POST  | Yes      | No        |

> **Multiple steps:** `[CrmPluginRegistration]` supports `AllowMultiple = true` — stack multiple attributes on one class for multiple step registrations.

---

### `<ProjectName>/spkl.json` — Standard Plugin

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

### `<ProjectName>/spkl.json` — Plugin Package (with dependencies)

```json
{
  "$schema": "https://raw.githubusercontent.com/scottdurow/SparkleXrm/master/spkl/SparkleXrm.Tasks/spkl.schema.json",
  "plugins": [
    {
      "assemblypath": "bin\\Debug\\<ProjectName>.1.0.0.nupkg",
      "profile": "default",
      "classRegex": ".*",
      "connectionstring": "[[YOUR_CONNECTION_STRING]]"
    },
    {
      "assemblypath": "bin\\Release\\<ProjectName>.1.0.0.nupkg",
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
dotnet build ..\<ProjectName>\<ProjectName>.csproj -c Debug
dotnet tool run spkl plugins ..\<ProjectName>\spkl.json %*
```

### `spkl/deploy-plugins.sh`

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

2. **Update connection string** in `spkl.json`. Never commit secrets — use environment variables or a gitignored local override file.

3. **Build and deploy:**
   ```
   dotnet build
   spkl plugins /p:default
   ```
   Or run `deploy-plugins.bat` / `deploy-plugins.sh`.

4. **Instrument existing org** (pull step registrations from an already-deployed assembly):
   ```
   spkl instrument
   ```

5. **Plugin Package only — versioning:** bump `<Version>` in the `.csproj` before each deploy so Dataverse registers a new package version.

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
- [ ] Third-party dependencies (e.g. Newtonsoft.Json) handled via Plugin Package, NOT ILMerge
- [ ] Plugin Package version bumped on each deployment when using NuGet package approach
