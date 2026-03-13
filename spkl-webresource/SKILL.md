---
name: spkl-webresource
description: Scaffold a Dataverse web resource project with spkl deployment, TypeScript support, and best-practice structure
---

Scaffold a complete **Dataverse Web Resource** project using spkl for deployment, with TypeScript support and best-practice folder structure.

## Gather Requirements

Ask the user for the following if not already provided:

1. **Project name** — e.g. `Contoso.WebResources`
2. **Publisher prefix** — e.g. `con` (used as the web resource name prefix, e.g. `con_/js/account.form.js`)
3. **Language preference** — TypeScript (recommended) or plain JavaScript
4. **Web resource types needed** — JS form scripts, HTML pages, CSS, images, other
5. **Target entity and form** — e.g. `account` main form — to name the initial script appropriately
6. **Use a bundler?** — Webpack (recommended for TypeScript) or none (single-file scripts only)
7. **Additional web resources?** — list any other scripts, pages, or assets to scaffold

---

## Generate the Project Structure

### TypeScript + Webpack (recommended)

```
<ProjectName>/
├── <ProjectName>.sln            ← optional, include if part of a larger solution
├── spkl/
│   ├── deploy-webresources.bat
│   └── deploy-webresources.sh
└── <ProjectName>/
    ├── spkl.json
    ├── package.json
    ├── tsconfig.json
    ├── webpack.config.js
    ├── .gitignore
    ├── src/
    │   ├── <prefix>_/<entity>.form.ts    ← TypeScript source
    │   └── types/
    │       └── Xrm.d.ts                  ← type declarations (from @types/xrm)
    └── dist/
        └── <prefix>_/<entity>.form.js    ← compiled output (gitignored)
```

### Plain JavaScript (no bundler)

```
<ProjectName>/
├── spkl/
│   ├── deploy-webresources.bat
│   └── deploy-webresources.sh
└── <ProjectName>/
    ├── spkl.json
    └── webresources/
        ├── js/
        │   └── <prefix>_<entity>.form.js
        ├── html/
        └── css/
```

---

## File Contents

### `<ProjectName>/package.json` (TypeScript projects)

```json
{
  "name": "<projectname-lowercase>",
  "version": "1.0.0",
  "private": true,
  "scripts": {
    "build": "webpack --mode production",
    "build:dev": "webpack --mode development",
    "watch": "webpack --watch --mode development",
    "lint": "eslint src/**/*.ts"
  },
  "devDependencies": {
    "@types/xrm": "^9.0.0",
    "typescript": "^5.0.0",
    "webpack": "^5.0.0",
    "webpack-cli": "^5.0.0",
    "ts-loader": "^9.0.0",
    "@typescript-eslint/eslint-plugin": "^6.0.0",
    "@typescript-eslint/parser": "^6.0.0",
    "eslint": "^8.0.0"
  }
}
```

---

### `<ProjectName>/tsconfig.json`

```json
{
  "compilerOptions": {
    "target": "ES6",
    "module": "ES6",
    "moduleResolution": "bundler",
    "lib": ["ES6", "DOM"],
    "strict": true,
    "noImplicitAny": true,
    "sourceMap": true,
    "outDir": "./dist",
    "rootDir": "./src",
    "types": ["xrm"]
  },
  "include": ["src/**/*"],
  "exclude": ["node_modules", "dist"]
}
```

---

### `<ProjectName>/webpack.config.js`

```js
const path = require("path");

module.exports = (env, argv) => {
  const isDev = argv.mode === "development";

  return {
    entry: {
      // Add one entry per web resource file:
      "<prefix>_/<entity>.form": "./src/<prefix>_/<entity>.form.ts",
    },
    output: {
      filename: "[name].js",
      path: path.resolve(__dirname, "dist"),
      library: {
        type: "assign-properties",  // exposes exports as global namespace properties
      },
    },
    resolve: {
      extensions: [".ts", ".js"],
    },
    module: {
      rules: [
        {
          test: /\.ts$/,
          use: "ts-loader",
          exclude: /node_modules/,
        },
      ],
    },
    devtool: isDev ? "source-map" : false,
    optimization: {
      minimize: !isDev,
    },
  };
};
```

---

### `<ProjectName>/src/<prefix>_/<entity>.form.ts`

Scaffold the initial form script with typed Xrm API usage:

```typescript
/**
 * Form scripts for the <EntityDisplayName> main form.
 * Web resource name: <prefix>_/js/<entity>.form.js
 */

namespace <Namespace>.<Entity>Form {

  /**
   * Called on form OnLoad.
   * Register in: Form Properties → Events → On Load
   * Function: <Namespace>.<Entity>Form.onLoad
   */
  export function onLoad(context: Xrm.Events.EventContext): void {
    const formContext = context.getFormContext();
    console.log(`[<Namespace>] <Entity> form loaded. Form type: ${formContext.ui.getFormType()}`);

    // Example: hide a field on new records
    if (formContext.ui.getFormType() === XrmEnum.FormType.Create) {
      // formContext.getControl("fieldname")?.setVisible(false);
    }
  }

  /**
   * Called on form OnSave.
   * Register in: Form Properties → Events → On Save
   * Function: <Namespace>.<Entity>Form.onSave
   */
  export function onSave(context: Xrm.Events.SaveEventContext): void {
    const formContext = context.getFormContext();
    const saveMode = context.getEventArgs().getSaveMode();
    console.log(`[<Namespace>] <Entity> form saving. Save mode: ${saveMode}`);
  }

  /**
   * Called when a field value changes.
   * Register on specific field: Field → Events → On Change
   * Function: <Namespace>.<Entity>Form.onFieldChange
   */
  export function onFieldChange(context: Xrm.Events.EventContext): void {
    const formContext = context.getFormContext();
    const attribute = context.getEventSource() as Xrm.Attributes.Attribute;
    console.log(`[<Namespace>] Field changed: ${attribute.getName()} = ${attribute.getValue()}`);
  }
}
```

> **Event handler registration:** In the form designer, set the library to the web resource name (e.g. `con_/js/account.form.js`) and the function to the fully-qualified namespace path (e.g. `Contoso.AccountForm.onLoad`). Always check **"Pass execution context as first parameter"**.

---

### `<ProjectName>/spkl.json`

```json
{
  "$schema": "https://raw.githubusercontent.com/scottdurow/SparkleXrm/master/spkl/SparkleXrm.Tasks/spkl.schema.json",
  "webresources": [
    {
      "profile": "default",
      "root": "dist/",
      "files": [
        {
          "file": "<prefix>_/<entity>.form.js",
          "uniquename": "<prefix>_/<entity>.form.js",
          "description": "<EntityDisplayName> form script"
        }
      ],
      "connectionstring": "[[YOUR_CONNECTION_STRING]]"
    },
    {
      "profile": "release",
      "root": "dist/",
      "files": [
        {
          "file": "<prefix>_/<entity>.form.js",
          "uniquename": "<prefix>_/<entity>.form.js",
          "description": "<EntityDisplayName> form script"
        }
      ],
      "connectionstring": "[[YOUR_CONNECTION_STRING]]"
    }
  ]
}
```

> **Naming convention:** Web resource unique names must start with the publisher prefix followed by `_`. The recommended pattern is `<prefix>_/<subfolder>/<name>.<ext>` which creates a virtual folder structure in the CRM web resource manager.

> **For plain JS projects:** Change `root` to `webresources/` and update `file` paths accordingly.

---

### `<ProjectName>/.gitignore`

```
node_modules/
dist/
*.js.map
```

---

### `spkl/deploy-webresources.bat`

```bat
@echo off
cd /d "%~dp0..\<ProjectName>"
call npm run build
dotnet tool run spkl webresources spkl.json %*
```

### `spkl/deploy-webresources.sh`

```bash
#!/bin/bash
set -e
cd "$(dirname "$0")/../<ProjectName>"
npm run build
dotnet tool run spkl webresources spkl.json "$@"
```

---

## Post-Scaffold Instructions

Tell the user:

1. **Install Node dependencies:**
   ```
   cd <ProjectName>
   npm install
   ```

2. **Build TypeScript:**
   ```
   npm run build          # production (minified)
   npm run build:dev      # development (with source maps)
   npm run watch          # watch mode for development
   ```

3. **Deploy to Dataverse:**
   ```
   spkl webresources spkl.json /p:default
   ```
   Or run `deploy-webresources.bat` / `.sh`.

4. **First-time setup — discover existing web resources:**
   If connecting to an org that already has web resources:
   ```
   spkl get-webresources /s:<prefix>_
   ```
   This generates `spkl.json` entries by matching deployed web resources to local files.

5. **Download existing web resources from org:**
   ```
   spkl download-webresources
   ```

6. **Register event handlers** in the form designer pointing to your web resource and namespace-qualified function names.

---

## Best Practices Checklist

- [ ] Web resource unique names start with publisher prefix followed by `_`
- [ ] TypeScript used with `@types/xrm` for full Xrm API type safety
- [ ] Namespace used to avoid global scope pollution (e.g. `Contoso.AccountForm`)
- [ ] Execution context passed to all event handlers
- [ ] `formContext` obtained from `context.getFormContext()`, not from global `Xrm.Page`
- [ ] `console.log` / tracing added for key operations during development
- [ ] `dist/` and `node_modules/` gitignored — only source committed
- [ ] One webpack entry per deployed web resource file
- [ ] Connection strings not committed to source control
- [ ] Web resource description populated in `spkl.json` for discoverability
- [ ] Source maps enabled in development builds, disabled in production
