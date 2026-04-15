---
name: nina-repository
description: Repository-specific guidance for working in the N.I.N.A. codebase and NINA.sln. Use when Codex is asked to modify, review, test, debug, navigate, or explain code in this repository; touch NINA.* projects, app startup/DI, profiles/settings, localization, database migrations, equipment, astrometry, imaging, plate solving, plugins, sequencer entities, WPF UI, installers, or tests; or prepare release-note/documentation implications for a NINA change.
---

# NINA Repository

## Overview

Use this skill as the repo-local operating guide for N.I.N.A. work. Locate the repository root by finding `NINA.sln`, then use the checked-in docs and project boundaries before changing code.

## Start Here

1. Read `AGENTS.md` for solution-wide boundaries, style rules, startup/composition notes, localization rules, plugin/sequencer rules, and verification hints.
2. Read `CONTRIBUTING.md` before non-trivial code, docs, release-note, localization, or installer work.
3. Read the owning project's `ARCHITECTURE.md` before making structural or behavioral changes in that project.
4. Use `references/project-map.md` for the project ownership map, common neighboring files, and verification commands.

## Change Workflow

1. Determine the owning layer before editing. Keep reusable logic out of `NINA` when a lower library already owns the concern.
2. Prefer existing patterns, dependency directions, mediator interfaces, factories, and services over new parallel mechanisms.
3. Follow `.editorconfig`: 4-space indentation, CRLF line endings, block-scoped namespaces, explicit types over `var`, braces on control-flow blocks, and same-line opening braces.
4. For new or refactored MVVM code, prefer `CommunityToolkit.Mvvm` attributes and command types where they fit the existing class shape. Avoid expanding legacy relay-command patterns unless constrained by existing APIs.
5. For user-visible strings, use localization and edit only `NINA.Core/Locale/Locale.resx`; do not manually edit translated `Locale.<culture>.resx` files.
6. If a change requires runtime files, update the executable output rules in `NINA/NINA.csproj` and check `NINA.Setup` packaging.
7. If a change affects user-facing behavior, consider `RELEASE_NOTES.md`. If it requires documentation, treat `NINA.Docs` as a separate submodule/repository.
8. Add or adjust focused tests in `NINA.Test` when behavior changes. Use documented reference values for astronomical or numerical changes.

## High-Risk Areas

- Startup and app-wide services: trace `NINA/App.xaml.cs`, `NINA/CompositionRoot.cs`, and `NINA/Utility/IoCBindings.cs`.
- Profiles and persisted settings: use `NINA.Profile` and `IProfileService`; profile changes can invalidate cached settings references.
- Database changes: coordinate `NINA.Core.Database.NINADbContext` with SQL under `NINA/Database`; add migrations instead of altering existing initial scripts for deployed schema changes.
- Plugin and sequencer changes: check MEF metadata, `NINA.Plugin/PluginLoader.cs`, clone behavior, `SequencerFactory`, and `NINA.Sequencer/Serialization/*`.
- Shared UI communication: use mediators in `NINA.WPF.Base`; do not make lower libraries reach into concrete app view models.
- Native/runtime assets: keep expected output layout synchronized across code, `NINA.csproj`, and installer authoring.

## Verification

Prefer targeted verification first, then broader checks as risk grows.

Common Windows CLI flow:

```powershell
$env:DOTNET_CLI_HOME = Join-Path (Get-Location) '.dotnet-cli'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = '0'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
dotnet test NINA.Test\NINA.Test.csproj --filter '<targeted-filter>' -v minimal
```

Broader flow from `CONTRIBUTING.md`:

```powershell
dotnet restore NINA.sln
dotnet build NINA\NINA.csproj --configuration Debug --no-restore
dotnet build NINA.Test\NINA.Test.csproj --configuration Debug --no-restore
dotnet test NINA.Test\NINA.Test.csproj --configuration Debug --no-build -p:PlatformTarget=x64
```

Use x64 tests for code paths that depend on native astronomy libraries (`SOFAlib.dll`, `NOVAS31lib.dll`) or runtime assets copied by project files.

## Resources

- `references/project-map.md`: concise ownership map, common change starting points, and neighboring checks.
