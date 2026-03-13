# Claude Code Skills — Dataverse / Power Platform

A collection of Claude Code skills for Microsoft Dataverse and Power Platform development.

Skills are invoked in Claude Code using `/skill-name` (e.g. `/spkl-plugin`).

---

## Skills

### Dataverse Development with spkl

| Skill | Description |
|---|---|
| [`/spkl-plugin`](#spkl-plugin) | Scaffold a Dataverse plugin project with spkl deployment |
| [`/spkl-workflow`](#spkl-workflow) | Scaffold a custom workflow activity project with spkl deployment |
| [`/spkl-webresource`](#spkl-webresource) | Scaffold a web resource project (TypeScript/JS) with spkl deployment |

> Each skill lives at `<skill-name>/SKILL.md` within this repository.

---

## Skill Details

### `/spkl-plugin`

**File:** [`spkl-plugin/SKILL.md`](spkl-plugin/SKILL.md)

Scaffolds a complete Visual Studio **Dataverse Plugin** project with:

- SDK-style `.csproj` targeting .NET Framework 4.6.2
- Assembly signing (`.snk` strong name key)
- NuGet references: `Microsoft.CrmSdk.CoreAssemblies`, `spkl`
- `PluginBase.cs` with `LocalPluginContext` (tracing, org service, execution context)
- Plugin class with `[CrmPluginRegistration]` attributes for step registration
- `spkl.json` with default and release profiles
- Deployment batch/shell scripts

**Best practices enforced:**
- Strongly named assemblies (required by Dataverse sandbox)
- Sandbox isolation mode
- Filtering attributes on Update steps
- Pre/Post image registration patterns
- No static state in plugin classes

---

### `/spkl-workflow`

**File:** [`spkl-workflow/SKILL.md`](spkl-workflow/SKILL.md)

Scaffolds a complete Visual Studio **Custom Workflow Activity** project with:

- SDK-style `.csproj` targeting .NET Framework 4.6.2
- Assembly signing (`.snk` strong name key)
- NuGet references: `Microsoft.CrmSdk.CoreAssemblies`, `Microsoft.CrmSdk.Workflow`, `spkl`
- `CodeActivityBase.cs` with `LocalWorkflowContext` (tracing, org service, workflow context)
- Activity class with `InArgument<T>` / `OutArgument<T>` input/output parameters
- `[CrmPluginRegistration]` attributes for activity registration
- `spkl.json` with default and release profiles
- Deployment batch/shell scripts

**Best practices enforced:**
- Strongly named assemblies
- Sandbox isolation mode
- `[RequiredArgument]`, `[ReferenceTarget]`, and `[AttributeTarget]` decorators on parameters
- No static state in activity classes

---

### `/spkl-webresource`

**File:** [`spkl-webresource/SKILL.md`](spkl-webresource/SKILL.md)

Scaffolds a **Dataverse Web Resource** project with:

- TypeScript (recommended) or plain JavaScript
- Webpack bundler configuration with one entry per web resource
- `@types/xrm` for full Xrm API type safety
- Namespace-scoped form scripts (avoids global scope pollution)
- `onLoad`, `onSave`, and `onFieldChange` event handler templates
- `spkl.json` with web resource file mappings and deployment profiles
- Deployment batch/shell scripts

**Best practices enforced:**
- Publisher-prefixed web resource unique names
- `formContext` from `context.getFormContext()` — not deprecated `Xrm.Page`
- Execution context passed to all event handlers
- Source maps in dev builds, minified output in production
- `dist/` and `node_modules/` gitignored

---

## Prerequisites

All spkl skills require:

- [.NET SDK](https://dotnet.microsoft.com/download) (6.0 or later for tooling; .NET Framework 4.6.2 for plugin targets)
- [spkl NuGet package](https://www.nuget.org/packages/spkl/) or `dotnet tool install spkl`
- Visual Studio 2022 or VS Code with C# extension
- A Dataverse / Power Platform environment with appropriate permissions

For web resource projects:
- [Node.js](https://nodejs.org/) (18 LTS or later)

---

## spkl Quick Reference

| Command | Purpose |
|---|---|
| `spkl plugins` | Deploy plugin assemblies |
| `spkl workflow` | Deploy workflow activity assemblies |
| `spkl webresources` | Deploy web resources |
| `spkl earlybound` | Generate early-bound entity classes |
| `spkl instrument` | Pull existing plugin registrations from org and annotate classes |
| `spkl get-webresources /s:<prefix>_` | Generate `spkl.json` from deployed web resources |
| `spkl download-webresources` | Download web resources from org |
| `spkl whoami` | Test connection string |

Use profiles to target different environments:
```
spkl plugins /p:release
spkl webresources /p:debug
```

---

## Contributing

To add a new skill:

1. Create a subdirectory: `mkdir <skill-name>`
2. Add `<skill-name>/SKILL.md` with YAML frontmatter:

```yaml
---
name: skill-name
description: One-line description of what the skill does
---

Skill prompt content here...
```

3. Deploy it: `cp -r <skill-name> ~/.claude/skills/`
4. Add an entry to this README.
