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
7. **Additional activity classes?** — list any extra activities to scaffold

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

### `<ProjectName>/<ProjectName>.csproj`

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
    <PackageReference Include="Microsoft.CrmSdk.Workflow" Version="9.0.2.49" />
    <PackageReference Include="spkl" Version="1.2.8">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

> **Note:** `Microsoft.CrmSdk.Workflow` provides `CodeActivity`, `InArgument<T>`, `OutArgument<T>`, and other workflow-specific types.

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

Generate one class per workflow activity. Use `CrmPluginRegistration` to register with spkl. Map each user-provided input/output parameter to `InArgument<T>` / `OutArgument<T>` properties.

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

### `<ProjectName>/spkl.json`

```json
{
  "$schema": "https://raw.githubusercontent.com/scottdurow/SparkleXrm/master/spkl/SparkleXrm.Tasks/spkl.schema.json",
  "workflow": [
    {
      "assemblypath": "bin\\Debug\\net462\\<ProjectName>.dll",
      "profile": "default",
      "connectionstring": "[[YOUR_CONNECTION_STRING]]"
    },
    {
      "assemblypath": "bin\\Release\\net462\\<ProjectName>.dll",
      "profile": "release",
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

2. **Update connection string** in `spkl.json`. Never commit credentials — use environment variables or a gitignored local override file.

3. **Build and deploy:**
   ```
   dotnet build
   spkl workflow /p:default
   ```

4. **Using in Power Automate / Classic Workflow:**
   - The activity will appear under the group name specified in `[CrmPluginRegistration]`
   - Input/output parameters map to the step's input/output properties in the designer

5. **Debugging tips:**
   - Enable tracing in the Plugin Registration Tool for sandbox assembly
   - Tracing output appears in the `PluginTraceLog` table

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
- [ ] Group name is consistent across all activities in the same assembly
