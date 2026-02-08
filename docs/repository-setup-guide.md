# Repository Setup Guide

**Last Updated**: 2026-02-08
**Applies To**: medium-to-pdf v1.0+
**.NET SDK**: 8.0

---

## Table of Contents

1. [Overview](#1-overview)
2. [Repository Structure](#2-repository-structure)
3. [SDK and Build Configuration](#3-sdk-and-build-configuration)
4. [Project Configuration](#4-project-configuration)
5. [Code Style and Editor Configuration](#5-code-style-and-editor-configuration)
6. [Git and Source Control Configuration](#6-git-and-source-control-configuration)
7. [NuGet Configuration](#7-nuget-configuration)
8. [Building, Testing, and Packing](#8-building-testing-and-packing)
9. [Reference Links](#9-reference-links)

---

## 1. Overview

### 1.1 What Is medium-to-pdf

**medium-to-pdf** is a .NET 8 global tool that converts Medium articles to PDF. Install it once with `dotnet tool install` and invoke it from any terminal session using the `medium-to-pdf` command. The tool fetches a Medium article by URL, renders its content, and produces a clean PDF suitable for offline reading or archival.

### 1.2 Purpose of This Document

This document serves as the primary reference for the medium-to-pdf repository structure and project configuration. It covers:

- The directory layout and the role of each top-level folder and file.
- Configuration files that control SDK versioning, build behavior, package management, and code style.
- The `.csproj` properties that make this project a packable .NET global tool.
- Commands to clone, build, test, pack, and install the tool locally.

Refer to this guide when onboarding to the project, reviewing configuration decisions, or troubleshooting build issues.

### 1.3 Prerequisites

| Requirement | Minimum Version | Verify With |
|---|---|---|
| .NET SDK | 8.0 | `dotnet --version` |
| Git | 2.x | `git --version` |
| Text editor or IDE | Any (Visual Studio, Rider, VS Code) | -- |

---

## 2. Repository Structure

### 2.1 Directory Tree

```
medium-to-pdf/
├── .github/                    # GitHub configuration (CI workflows, issue templates)
├── docs/                       # Project documentation
├── src/
│   └── MediumToPdf/            # Main application project
│       └── MediumToPdf.csproj
├── tests/
│   └── MediumToPdf.Tests/      # Test project (xUnit)
│       └── MediumToPdf.Tests.csproj
├── .editorconfig               # Code style rules
├── .gitattributes              # Git line-ending normalization
├── .gitignore                  # Git ignore patterns
├── Directory.Build.props       # Shared MSBuild properties (all projects)
├── Directory.Packages.props    # Central NuGet package version management
├── global.json                 # .NET SDK version pin
├── LICENSE                     # MIT license
├── MediumToPdf.sln             # Solution file
├── nuget.config                # NuGet feed configuration
└── README.md                   # Project overview and quick-start
```

### 2.2 Directory Purposes

| Directory | Purpose |
|---|---|
| `src/` | Production source code. Each subdirectory is a single .NET project. Currently contains only `MediumToPdf`, the main application. |
| `tests/` | Test projects. Each subdirectory mirrors a `src/` project and contains its tests. Currently contains `MediumToPdf.Tests`. |
| `docs/` | Supplementary documentation beyond the README. Guides, architecture notes, and design decisions live here. |
| `.github/` | GitHub-specific configuration: CI/CD workflow definitions, issue templates, pull request templates, and Dependabot settings. |

### 2.3 Conventions

This layout follows the structure used by established .NET global tool repositories such as [dotnet-outdated](https://github.com/dotnet-outdated/dotnet-outdated) and [dotnet-serve](https://github.com/natemcmaster/dotnet-serve). The key conventions are:

- **Separate `src/` and `tests/` trees.** Production code and test code never share a parent directory. This makes it straightforward to apply different build properties or package settings to each category.
- **One project per subdirectory.** Each `.csproj` lives in its own folder named after the project assembly. This scales cleanly as the solution grows.
- **Root-level configuration files.** Files like `Directory.Build.props` and `Directory.Packages.props` sit at the repository root so MSBuild auto-imports them for every project in the tree.

---

## 3. SDK and Build Configuration

### 3.1 global.json -- SDK Version Pinning

`global.json` pins the .NET SDK version used to build the project. When a developer runs any `dotnet` command in this repository, the CLI checks `global.json` and selects a matching SDK version installed on the machine.

```json
{
  "sdk": {
    "version": "8.0.100",
    "rollForward": "latestFeature"
  }
}
```

**Key properties:**

| Property | Value | Effect |
|---|---|---|
| `version` | `8.0.100` | Baseline SDK version. The minimum acceptable version. |
| `rollForward` | `latestFeature` | Allows the SDK to roll forward to the latest installed feature band (e.g., `8.0.200`, `8.0.300`) but stays within the `8.0.x` major/minor range. |

The `latestFeature` roll-forward policy balances reproducibility with practicality. Developers do not need the exact `8.0.100` release -- any .NET 8 feature band works. However, a .NET 9 SDK will not be selected, preventing accidental framework upgrades.

If no matching SDK is installed, the `dotnet` CLI prints an error message indicating the required version. Install the correct SDK from [https://dot.net/download](https://dot.net/download).

### 3.2 Directory.Build.props -- Shared Build Properties

`Directory.Build.props` is an MSBuild file that the build engine automatically imports into every project file (`.csproj`) found beneath the directory where it resides. By placing it at the repository root, all projects in both `src/` and `tests/` inherit its properties. This eliminates duplication across `.csproj` files and provides a single point of change for common settings.

```xml
<Project>

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>

</Project>
```

**Property reference:**

| Property | Value | Purpose |
|---|---|---|
| `TargetFramework` | `net8.0` | All projects target .NET 8. |
| `LangVersion` | `latest` | Use the latest stable C# language version supported by the SDK. |
| `Nullable` | `enable` | Enable nullable reference type annotations and warnings project-wide. |
| `ImplicitUsings` | `enable` | Automatically import common namespaces (`System`, `System.Collections.Generic`, `System.Linq`, etc.). |
| `TreatWarningsAsErrors` | `true` | Promote all build warnings to errors. Prevents warning accumulation and enforces clean builds. |

Because these properties are inherited, individual `.csproj` files do not need to declare them. A project can override any property by redeclaring it in its own `.csproj` if necessary.

### 3.3 Directory.Packages.props -- Central Package Management

Central Package Management (CPM) is a .NET feature that centralizes NuGet package version declarations in a single file. Instead of specifying versions in each `.csproj`, all versions are declared in `Directory.Packages.props`. Individual projects reference packages by name only -- the version resolves from this central file.

```xml
<Project>

  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>

  <ItemGroup>
    <!-- Test framework -->
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageVersion Include="xunit" Version="2.9.2" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageVersion Include="coverlet.collector" Version="6.0.2" />
  </ItemGroup>

</Project>
```

**How it works:**

1. Set `ManagePackageVersionsCentrally` to `true` in `Directory.Packages.props`.
2. Declare each package and its version using `<PackageVersion>` elements.
3. In `.csproj` files, use `<PackageReference Include="xunit" />` without a `Version` attribute. MSBuild resolves the version from the central file.

**Benefits:**

- **No version drift.** Every project uses the same version of a given package.
- **Easier upgrades.** Update a package version in one place, and all projects pick up the change.
- **Cleaner project files.** `.csproj` files contain only package names, not version numbers.

---

## 4. Project Configuration

### 4.1 Main Project: src/MediumToPdf/MediumToPdf.csproj

This is the main application project. It produces the executable that runs as a .NET global tool. The `.csproj` includes properties that configure packaging behavior and NuGet package metadata.

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <PackAsTool>true</PackAsTool>
    <ToolCommandName>medium-to-pdf</ToolCommandName>
    <PackageOutputPath>./nupkg</PackageOutputPath>
  </PropertyGroup>

  <PropertyGroup>
    <PackageId>MediumToPdf</PackageId>
    <Version>1.0.0</Version>
    <Authors>Marcin</Authors>
    <Description>A .NET global tool that converts Medium articles to PDF.</Description>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <RepositoryUrl>https://github.com/user/medium-to-pdf</RepositoryUrl>
    <PackageReadmeFile>README.md</PackageReadmeFile>
  </PropertyGroup>

  <ItemGroup>
    <None Include="../../README.md" Pack="true" PackagePath="/" />
  </ItemGroup>

</Project>
```

**Global tool properties:**

| Property | Value | Purpose |
|---|---|---|
| `OutputType` | `Exe` | Produces an executable assembly. Required for global tools. |
| `PackAsTool` | `true` | Marks this project as a .NET tool. When packed, the resulting `.nupkg` contains the metadata NuGet needs to install it as a `dotnet tool`. Without this property, `dotnet pack` produces a standard library package. |
| `ToolCommandName` | `medium-to-pdf` | The CLI command name users type after installing the tool. This is independent of the assembly name or package ID. |
| `PackageOutputPath` | `./nupkg` | Directs `dotnet pack` to place the generated `.nupkg` file in a `nupkg/` subdirectory within the project folder, keeping pack output separate from build output. |

**Package metadata properties:**

| Property | Purpose |
|---|---|
| `PackageId` | The NuGet package identifier. Used in `dotnet tool install MediumToPdf`. |
| `Version` | SemVer version string. Controls the package version published to NuGet feeds. |
| `Authors` | Package author(s) displayed on NuGet.org. |
| `Description` | Short description shown in NuGet search results and package listings. |
| `PackageLicenseExpression` | SPDX license identifier. `MIT` matches the repository LICENSE file. |
| `RepositoryUrl` | Link to the source repository. Displayed on the NuGet package page. |
| `PackageReadmeFile` | Path to the README bundled inside the `.nupkg` for display on NuGet.org. |

Note that `TargetFramework`, `LangVersion`, `Nullable`, `ImplicitUsings`, and `TreatWarningsAsErrors` are absent from this file. They are inherited from `Directory.Build.props` at the repository root.

### 4.2 Test Project: tests/MediumToPdf.Tests/MediumToPdf.Tests.csproj

The test project uses xUnit as its test framework. It references the main project and is configured as non-packable.

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <IsPublishable>false</IsPublishable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="coverlet.collector" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../src/MediumToPdf/MediumToPdf.csproj" />
  </ItemGroup>

</Project>
```

**Key points:**

- `IsPackable` is `false` because the test project should never be packed into a NuGet package.
- `IsPublishable` is `false` to prevent accidental `dotnet publish` of the test assembly.
- `<PackageReference>` elements have no `Version` attribute. Versions resolve from `Directory.Packages.props` via Central Package Management.
- `<ProjectReference>` points to the main project using a relative path. This creates a build dependency and allows tests to reference the main project's public API.

### 4.3 Solution File: MediumToPdf.sln

The solution file groups both projects so that IDEs and the `dotnet` CLI can operate on the entire codebase with a single command. Running `dotnet build` at the repository root builds both projects. Running `dotnet test` discovers and executes all test projects in the solution.

The solution file is generated by the .NET CLI. If it is lost or needs regeneration, recreate it with:

```bash
dotnet new sln -n MediumToPdf
dotnet sln MediumToPdf.sln add src/MediumToPdf/MediumToPdf.csproj
dotnet sln MediumToPdf.sln add tests/MediumToPdf.Tests/MediumToPdf.Tests.csproj
```

---

## 5. Code Style and Editor Configuration

### 5.1 .editorconfig -- C# Coding Conventions

The `.editorconfig` file defines coding conventions enforced by IDEs and the compiler. Visual Studio, JetBrains Rider, and VS Code (with OmniSharp or the C# Dev Kit) all read this file and apply its rules during editing and build.

```editorconfig
root = true

# All files
[*]
indent_style = space
indent_size = 4
end_of_line = lf
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true

# C# files
[*.cs]
# Using directives
dotnet_sort_system_directives_first = true
dotnet_separate_import_directive_groups = false

# this. preferences
dotnet_style_qualification_for_field = false:warning
dotnet_style_qualification_for_property = false:warning
dotnet_style_qualification_for_method = false:warning
dotnet_style_qualification_for_event = false:warning

# var preferences
csharp_style_var_for_built_in_types = true:suggestion
csharp_style_var_when_type_is_apparent = true:suggestion
csharp_style_var_elsewhere = true:suggestion

# Expression-bodied members
csharp_style_expression_bodied_methods = when_on_single_line:suggestion
csharp_style_expression_bodied_constructors = false:suggestion
csharp_style_expression_bodied_properties = true:suggestion
csharp_style_expression_bodied_accessors = true:suggestion

# Naming conventions
dotnet_naming_rule.private_fields_should_be_camel_case.severity = warning
dotnet_naming_rule.private_fields_should_be_camel_case.symbols = private_fields
dotnet_naming_rule.private_fields_should_be_camel_case.style = camel_case_underscore_prefix

dotnet_naming_symbols.private_fields.applicable_kinds = field
dotnet_naming_symbols.private_fields.applicable_accessibilities = private
dotnet_naming_symbols.private_fields.required_modifiers =

dotnet_naming_style.camel_case_underscore_prefix.required_prefix = _
dotnet_naming_style.camel_case_underscore_prefix.capitalization = camel_case

# New line preferences
csharp_new_line_before_open_brace = all
csharp_new_line_before_else = true
csharp_new_line_before_catch = true
csharp_new_line_before_finally = true
```

**Key rules summary:**

| Category | Convention | Severity |
|---|---|---|
| Indentation | 4 spaces, no tabs | Applied automatically |
| Line endings | LF (Unix-style) | Applied automatically |
| `this.` qualifier | Omit unless necessary | Warning |
| `var` usage | Prefer `var` when type is apparent | Suggestion |
| Private field naming | Prefix with underscore, camelCase (`_myField`) | Warning |
| Brace placement | Allman style (new line before opening brace) | Applied automatically |
| Using directives | `System` namespaces first, no blank line separation | Applied automatically |

### 5.2 Relationship to TreatWarningsAsErrors

Rules configured with `warning` severity interact with the `TreatWarningsAsErrors` property from `Directory.Build.props`. When a style rule produces a warning and `TreatWarningsAsErrors` is `true`, the build fails. This means:

- Naming convention violations (e.g., a private field without the `_` prefix) break the build.
- Unnecessary `this.` qualifiers break the build.

This enforcement is intentional. It ensures consistent code style across all contributions without relying on manual code review to catch formatting issues.

Rules at `suggestion` severity (such as `var` preferences) appear in the IDE but do not affect the build.

---

## 6. Git and Source Control Configuration

### 6.1 .gitignore -- Ignored Files and Directories

The `.gitignore` file is based on the standard .NET template and excludes build artifacts, IDE-specific files, and generated content from version control.

**Key patterns:**

```gitignore
# Build output
[Bb]in/
[Oo]bj/

# NuGet packages
*.nupkg
nupkg/

# IDE and editor files
.vs/
*.user
*.suo
*.DotSettings.user
.idea/

# OS files
.DS_Store
Thumbs.db

# Test results
TestResults/
```

These patterns prevent build output, NuGet packages, IDE workspace files, and OS metadata from polluting the repository. The `.nupkg` exclusion is particularly important -- packed tool binaries should never be committed.

### 6.2 .gitattributes -- Line Ending Normalization

The `.gitattributes` file normalizes line endings across platforms. Without it, contributors on Windows (CRLF) and macOS/Linux (LF) can produce noisy diffs containing only whitespace changes.

```gitattributes
# Auto-detect text files and normalize line endings to LF
* text=auto eol=lf

# Explicitly mark file types
*.cs text eol=lf
*.csproj text eol=lf
*.sln text eol=lf
*.xml text eol=lf
*.json text eol=lf
*.md text eol=lf
*.yml text eol=lf

# Binary files
*.png binary
*.jpg binary
*.pdf binary
```

Setting `eol=lf` ensures that all text files in the repository use Unix-style line endings regardless of the contributor's operating system. This aligns with the `.editorconfig` setting `end_of_line = lf`.

### 6.3 LICENSE

The repository uses the **MIT License**. This is a permissive open-source license that allows commercial use, modification, distribution, and private use with minimal restrictions. The only requirement is that the license and copyright notice be included in copies of the software.

The `PackageLicenseExpression` property in the main `.csproj` is set to `MIT` to match. NuGet.org displays this license identifier on the package page.

---

## 7. NuGet Configuration

### 7.1 nuget.config -- NuGet Feed Configuration

`nuget.config` defines the package sources used during `dotnet restore`. The default configuration points to the public NuGet.org feed.

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
```

**Key details:**

- `<clear />` removes any package sources inherited from the global NuGet configuration (`~/.nuget/NuGet/NuGet.Config`). This ensures builds are deterministic and do not depend on machine-specific feed configuration.
- The `nuget.org` source is the standard public feed. All packages referenced in `Directory.Packages.props` resolve from this feed.

**When to add additional feeds:** If the project takes a dependency on a private NuGet feed (e.g., a company-internal package, a GitHub Packages feed, or a local development feed), add it as a second `<add>` element in the `<packageSources>` section. Authenticate private feeds using `<packageSourceCredentials>` in this file or via environment variables in CI.

---

## 8. Building, Testing, and Packing

### 8.1 Prerequisites Checklist

Before building the project, verify the environment.

**Check the .NET SDK version:**

```bash
dotnet --version
```

The output should show a version in the `8.0.x` range (e.g., `8.0.100`, `8.0.204`, `8.0.306`). If a different major version is displayed, install .NET 8 from [https://dot.net/download](https://dot.net/download).

**Clone the repository:**

```bash
git clone https://github.com/user/medium-to-pdf.git
cd medium-to-pdf
```

### 8.2 Restore and Build

Restore NuGet packages and compile all projects in the solution:

```bash
dotnet restore
dotnet build
```

Alternatively, `dotnet build` implicitly runs `dotnet restore`. Running them separately is useful for diagnosing package resolution issues.

**Expected output:**

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

Because `TreatWarningsAsErrors` is enabled, a successful build guarantees zero warnings. If warnings appear, they are promoted to errors and the build fails until resolved.

### 8.3 Run Tests

Execute all tests in the solution:

```bash
dotnet test
```

**Expected output:**

```
Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1
```

The initial test project contains a placeholder test to verify the xUnit infrastructure works. As the application develops, add tests alongside new functionality.

For verbose output during test development:

```bash
dotnet test --verbosity normal
```

### 8.4 Pack as a Global Tool

Generate the `.nupkg` file:

```bash
dotnet pack
```

This produces the NuGet package at:

```
src/MediumToPdf/nupkg/MediumToPdf.1.0.0.nupkg
```

The `PackageOutputPath` property in the `.csproj` directs the output to the `nupkg/` subdirectory. The filename includes the `PackageId` and `Version`.

### 8.5 Install and Test Locally

Install the tool globally from the local `.nupkg` file:

```bash
dotnet tool install --global --add-source ./src/MediumToPdf/nupkg MediumToPdf
```

**Verify the installation:**

```bash
medium-to-pdf --help
```

This should display the tool's help text. If the command is not found, verify that the .NET global tools directory is on the system PATH. The typical location is:

- **Windows:** `%USERPROFILE%\.dotnet\tools`
- **macOS/Linux:** `$HOME/.dotnet/tools`

**Uninstall the tool when finished testing:**

```bash
dotnet tool uninstall --global MediumToPdf
```

### 8.6 Common Issues and Troubleshooting

**SDK version mismatch**

```
The command could not be loaded, possibly because:
  * You intended to execute a .NET application:
      The application '--version' does not exist.
  * You intended to execute a .NET SDK command:
      A compatible .NET SDK was not found.

Global.json file:
  /path/to/medium-to-pdf/global.json

Requested SDK version: 8.0.100
```

**Cause:** No .NET 8 SDK is installed, or the installed version does not satisfy the `rollForward` policy.
**Fix:** Install .NET 8 SDK from [https://dot.net/download](https://dot.net/download). Any 8.0.x feature band works because `rollForward` is set to `latestFeature`.

---

**Build warnings treated as errors**

```
error CS8600: Converting null literal or possible null value to non-nullable type.
```

**Cause:** `TreatWarningsAsErrors` is `true` in `Directory.Build.props`. Compiler warnings and analyzer diagnostics are promoted to errors.
**Fix:** Address the warning in the source code. Do not suppress warnings without team discussion. If a specific warning is genuinely inapplicable, suppress it with a `#pragma` directive or a `<NoWarn>` property in the `.csproj` and document the reason.

---

**Package version conflicts**

```
error NU1605: Detected package downgrade: xunit from 2.9.2 to 2.8.0.
```

**Cause:** A `<PackageReference>` in a `.csproj` file specifies a `Version` attribute that conflicts with the version in `Directory.Packages.props`.
**Fix:** Remove the `Version` attribute from the `<PackageReference>` in the `.csproj`. With Central Package Management enabled, versions must be declared only in `Directory.Packages.props`.

---

**Tool command not found after install**

```
medium-to-pdf: command not found
```

**Cause:** The .NET global tools directory is not on the system PATH.
**Fix:** Add the tools directory to PATH.

On macOS/Linux, add to `~/.bashrc`, `~/.zshrc`, or equivalent:

```bash
export PATH="$HOME/.dotnet/tools:$PATH"
```

On Windows, add `%USERPROFILE%\.dotnet\tools` to the system PATH via Environment Variables settings.

---

## 9. Reference Links

| Resource | URL |
|---|---|
| .NET Global Tools Overview | [learn.microsoft.com/dotnet/core/tools/global-tools](https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools) |
| Central Package Management | [learn.microsoft.com/nuget/consume-packages/central-package-management](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management) |
| Directory.Build.props | [learn.microsoft.com/visualstudio/msbuild/customize-by-directory](https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-by-directory) |
| .editorconfig Reference | [learn.microsoft.com/dotnet/fundamentals/code-analysis/code-style-rule-options](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/code-style-rule-options) |
| global.json Overview | [learn.microsoft.com/dotnet/core/tools/global-json](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json) |
| Reference: dotnet-outdated | [github.com/dotnet-outdated/dotnet-outdated](https://github.com/dotnet-outdated/dotnet-outdated) |
| Reference: dotnet-serve | [github.com/natemcmaster/dotnet-serve](https://github.com/natemcmaster/dotnet-serve) |
