---
name: spkl-workflow
description: Scaffold a Visual Studio Dataverse custom workflow activity project with spkl deployment and best-practice structure
---

Scaffold a complete Visual Studio **Dataverse Custom Workflow Activity** project using spkl for deployment.

## Gather Requirements

Ask the user for the following if not already provided:

1. **Project name** — e.g. `Contoso.WorkflowActivities`
2. **Publisher prefix** — e.g. `con`
3. **Activity name and purpose** — e.g. `SendApprovalEmail` — generates a formatted approval email
4. **Input parameters** — name, type, required/optional, label for each
5. **Output parameters** — name, type, label for each
6. **Group name** — logical grouping shown in Power Automate / classic workflow designer
7. **Does the activity need Newtonsoft.Json (Json.NET) or other third-party NuGet dependencies?**
   - If **yes**: the project must be structured as a **Plugin Package** (NuGet package deployed to the `PluginPackage` table). Ask the user to confirm this approach before proceeding.
   - If **no**: standard single-assembly workflow activity (no bundling needed).
8. **Additional activity classes?** — list any extra activities to scaffold

---

## Project Type Decision

### Standard Workflow Activity (no third-party dependencies)

Use when the activity only references `Microsoft.CrmSdk.*` packages already present in the Dataverse sandbox.

### Plugin Package (has third-party dependencies, e.g. Newtonsoft.Json)

Use when the activity needs NuGet packages not available in the Dataverse sandbox. The project is packaged as a `.nupkg` and deployed to Dataverse as a `PluginPackage` record.

> **ILMerge is NOT supported** by Microsoft for Dataverse plugins or workflow activities. Plugin Packages are the official replacement.

---

## Generate the Project Structure

```
<ProjectName>/
├── <ProjectName>.sln
├── spkl/
│   ├── spkl.csproj
│   ├── deploy-workflow.bat
│   └── deploy-workflow.sh
└── <ProjectName>/
    ├── <ProjectName>.csproj
    ├── <ProjectName>.snk          ← strong name key placeholder
    ├── spkl.json
    ├── Properties/
    │   └── AssemblyInfo.cs
    ├── CodeActivityBase.cs
    └── <ActivityClassName>.cs
```

---

## File Contents

### `<ProjectName>/<ProjectName>.csproj` — Standard Workflow Activity

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
    <!-- PrivateAssets=All prevents the CRM SDK from being bundled — it's already present in the sandbox -->
    <PackageReference Include="Microsoft.CrmSdk.CoreAssemblies" Version="9.0.2.60">
      <PrivateAssets>All</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.CrmSdk.Workflow" Version="9.0.2.60">
      <PrivateAssets>All</PrivateAssets>
    </PackageReference>
    <PackageReference Include="spkl" Version="1.0.640">
      <PrivateAssets>All</PrivateAssets>
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
    <!-- REQUIRED: PrivateAssets=All on CRM SDK — Dataverse will reject packages that bundle these assemblies -->
    <PackageReference Include="Microsoft.CrmSdk.CoreAssemblies" Version="9.0.2.60">
      <PrivateAssets>All</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.CrmSdk.Workflow" Version="9.0.2.60">
      <PrivateAssets>All</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
    <!-- Add other dependencies here -->
    <PackageReference Include="spkl" Version="1.0.640">
      <PrivateAssets>All</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

> **Note:** `Microsoft.CrmSdk.Workflow` provides `CodeActivity`, `InArgument<T>`, `OutArgument<T>`, and other workflow-specific types.

> **Note:** Run `sn -k <ProjectName>.snk` to generate the strong name key. Commit the `.snk` to source control.

---

### `<ProjectName>/CodeActivityBase.cs`

```csharp
using System;
using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;

namespace <Namespace>
{
    /// <summary>
    /// Base class for all custom workflow activities.
    /// Provides a strongly-typed context with tracing, org service, and workflow context.
    /// </summary>
    public abstract class CodeActivityBase : CodeActivity
    {
        protected override void Execute(CodeActivityContext activityContext)
        {
            if (activityContext == null)
                throw new ArgumentNullException(nameof(activityContext));

            var context = new LocalWorkflowContext(activityContext);
            context.Trace($"Entered {GetType().FullName}");

            try
            {
                ExecuteWorkflowActivity(context);
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

        protected abstract void ExecuteWorkflowActivity(LocalWorkflowContext context);
    }

    public class LocalWorkflowContext
    {
        public CodeActivityContext ActivityContext { get; }
        public IWorkflowContext WorkflowContext { get; }
        public IOrganizationServiceFactory ServiceFactory { get; }
        public IOrganizationService OrganizationService { get; }
        public ITracingService TracingService { get; }

        public LocalWorkflowContext(CodeActivityContext activityContext)
        {
            ActivityContext = activityContext;
            TracingService = activityContext.GetExtension<ITracingService>();
            WorkflowContext = activityContext.GetExtension<IWorkflowContext>();
            ServiceFactory = activityContext.GetExtension<IOrganizationServiceFactory>();
            OrganizationService = ServiceFactory.CreateOrganizationService(WorkflowContext.UserId);
        }

        public void Trace(string message) => TracingService?.Trace(message);
    }
}
```

---

### `<ProjectName>/<ActivityClassName>.cs`

Generate one class per workflow activity. Use `CrmPluginRegistration` to register with spkl.

```csharp
using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using Dataverse.Xrm;  // CrmPluginRegistration from spkl NuGet

namespace <Namespace>
{
    [CrmPluginRegistration(
        "<ActivityClassName>",        // internal name
        "<FriendlyName>",             // label shown in workflow designer
        "<Description>",
        "<GroupName>",                // logical grouping, e.g. "Contoso Utilities"
        IsolationModeEnum.Sandbox
    )]
    public class <ActivityClassName> : CodeActivityBase
    {
        // --- Input Parameters ---
        [Input("<InputParamLabel>")]
        [RequiredArgument]
        public InArgument<string> <InputParamName> { get; set; }

        // Example with EntityReference input:
        // [Input("Target Record")]
        // [ReferenceTarget("account")]
        // [RequiredArgument]
        // public InArgument<EntityReference> TargetRecord { get; set; }

        // Example with optional OptionSet input:
        // [Input("Status")]
        // [AttributeTarget("account", "statuscode")]
        // public InArgument<OptionSetValue> Status { get; set; }

        // --- Output Parameters ---
        [Output("<OutputParamLabel>")]
        public OutArgument<string> <OutputParamName> { get; set; }

        protected override void ExecuteWorkflowActivity(LocalWorkflowContext context)
        {
            var input = <InputParamName>.Get(context.ActivityContext);
            context.Trace($"Input received: {input}");

            // TODO: implement activity logic here

            var result = $"Processed: {input}";
            <OutputParamName>.Set(context.ActivityContext, result);
        }
    }
}
```

### Supported parameter types

| C# Type | Workflow designer type |
|---|---|
| `string` | Single Line of Text |
| `int` | Whole Number |
| `decimal` | Decimal Number |
| `double` | Floating Point Number |
| `bool` | Two Options |
| `DateTime` | Date and Time |
| `EntityReference` | Record reference |
| `OptionSetValue` | Option Set |
| `Money` | Currency |
| `Entity` | Dynamic Entity |

---

### `<ProjectName>/spkl.json` — Standard Workflow Activity

> **Note:** Workflow activities are deployed using the `plugins` section in `spkl.json`, just like regular plugins. Use `spkl workflow` command to deploy.

```json
{
  "$schema": "https://raw.githubusercontent.com/scottdurow/SparkleXrm/master/spkl/SparkleXrm.Tasks/spkl.schema.json",
  "plugins": [
    {
      "assemblypath": "bin\\Debug\\net462\\<ProjectName>.dll",
      "profile": "default,debug",
      "solution": "<SolutionUniqueName>",
      "classRegex": ".*",
      "connectionstring": "[[YOUR_CONNECTION_STRING]]"
    },
    {
      "assemblypath": "bin\\Release\\net462\\<ProjectName>.dll",
      "profile": "release",
      "solution": "<SolutionUniqueName>",
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
      "profile": "default,debug",
      "solution": "<SolutionUniqueName>",
      "classRegex": ".*",
      "connectionstring": "[[YOUR_CONNECTION_STRING]]"
    },
    {
      "assemblypath": "bin\\Release\\<ProjectName>.1.0.0.nupkg",
      "profile": "release",
      "solution": "<SolutionUniqueName>",
      "classRegex": ".*",
      "connectionstring": "[[YOUR_CONNECTION_STRING]]"
    }
  ]
}
```

> **Connection string formats:**
> - Interactive: `AuthType=OAuth;Url=https://org.crm.dynamics.com;AppId=51f81489-12ee-4a9e-aaae-a2591f45987d;RedirectUri=app://58145B91-0C36-4500-8554-080854F2AC97;LoginPrompt=Auto`
> - Service principal: `AuthType=ClientSecret;Url=https://org.crm.dynamics.com;ClientId=<AppId>;ClientSecret=<Secret>`

---

### `spkl/deploy-workflow.bat`

```bat
@echo off
dotnet build ..\<ProjectName>\<ProjectName>.csproj -c Debug
dotnet tool run spkl workflow ..\<ProjectName>\spkl.json %*
```

### `spkl/deploy-workflow.sh`

```bash
#!/bin/bash
set -e
dotnet build ../<ProjectName>/<ProjectName>.csproj -c Debug
dotnet tool run spkl workflow ../<ProjectName>/spkl.json "$@"
```

---

## Post-Scaffold Instructions

Tell the user:

1. **Generate strong name key:**
   ```
   sn -k <ProjectName>.snk
   ```

2. **Update connection string** in `spkl.json`. Never commit credentials.

3. **Build and deploy:**
   ```
   dotnet build
   spkl workflow /p:default
   ```

4. **Using in Power Automate / Classic Workflow:**
   - The activity appears under the group name specified in `[CrmPluginRegistration]`
   - Input/output parameters map to step properties in the designer

5. **Plugin Package only — versioning:** bump `<Version>` in the `.csproj` before each deploy.

6. **Debugging:** Enable tracing in the Plugin Registration Tool. Output appears in the `PluginTraceLog` table.

---

## Best Practices Checklist

- [ ] Assembly is strongly named (`.snk` present and referenced in `.csproj`)
- [ ] Target framework is `net462`
- [ ] Isolation mode is `Sandbox`
- [ ] Activity inherits from `CodeActivityBase`, not directly from `CodeActivity`
- [ ] All required inputs decorated with `[RequiredArgument]`
- [ ] `EntityReference` inputs decorated with `[ReferenceTarget("<logicalname>")]`
- [ ] `OptionSetValue` inputs decorated with `[AttributeTarget("<entity>", "<attribute>")]`
- [ ] No static state or static fields in activity classes
- [ ] `InvalidPluginExecutionException` used for user-facing errors
- [ ] Tracing used throughout for diagnostics
- [ ] Connection strings not committed to source control
- [ ] Third-party dependencies (e.g. Newtonsoft.Json) handled via Plugin Package, NOT ILMerge
- [ ] `PrivateAssets=All` set on `Microsoft.CrmSdk.CoreAssemblies` and `Microsoft.CrmSdk.Workflow` (prevents SDK from being bundled — Dataverse will reject packages that include them)
- [ ] Plugin Package version bumped on each deployment when using NuGet package approach
- [ ] `solution` field populated in `spkl.json` so the assembly is automatically added to the correct solution
- [ ] Group name is consistent across all activities in the same assembly
