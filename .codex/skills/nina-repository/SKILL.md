---
name: nina-repository
description: Repository-specific guidance for working in the N.I.N.A. codebase and NINA.sln. Use when Codex is asked to modify, review, test, debug, navigate, or explain code in this repository; touch NINA.* projects, app startup/DI, profiles/settings, localization, database migrations, equipment, astrometry, imaging, plate solving, plugins, sequencer entities, WPF UI, installers, or tests; or prepare release-note/documentation implications for a NINA change.
---

# NINA Repository

## Core Workflow

Use this skill as a routing layer, not a replacement for the repo docs.

1. Find the repo root by locating `NINA.sln`.
2. Read `AGENTS.md` before making repo changes.
3. Read `CONTRIBUTING.md` when the task touches contribution workflow, release notes, localization, docs, tests, installers, or PR expectations.
4. Read the owning project's `ARCHITECTURE.md` before non-trivial edits in that project.
5. Use `references/project-map.md` to choose the likely owner and neighboring files.
6. Use `references/testing-map.md` to choose concrete test filters and command templates.
7. If a task reveals missing reusable guidance, update the closest repository doc, route map, test, or tooling before finishing.

## Edit Rules

- Keep changes in the owning layer and follow existing local patterns first.
- Use `references/project-map.md` for recurring checks such as DI, localization, runtime files, plugin/sequencer metadata, persistence, and documentation/release-note implications.
- Follow `.editorconfig` and surrounding style; prefer `CommunityToolkit.Mvvm` for new or refactored MVVM code where it fits.
- Add or update focused tests for testable behavior changes, and update `references/testing-map.md` only when a new route, command, or constraint will help future agents.
- Keep file-writing tests isolated from shared user state unless the shared location is the behavior under test.
- Prefer making durable rules enforceable with tests, analyzers, scripts, or CI when the boundary matters repeatedly.

## Verification

Prefer targeted tests while iterating and broaden only when the changed surface justifies it. Use `references/testing-map.md` for concrete filters and command templates.

For .NET build and test commands in this sandbox, start with the stable invocation shape instead of rediscovering environment failures:

- Set `DOTNET_CLI_HOME` to the repo-local `.dotnet-cli` directory and set `DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1`, `DOTNET_ADD_GLOBAL_TOOLS_TO_PATH=0`, and `DOTNET_CLI_TELEMETRY_OPTOUT=1` in the same PowerShell command.
- Use `--no-restore` unless restore is the explicit task or a missing-assets error proves it is needed.
- Add `-m:1 -p:UseSharedCompilation=false -p:GeneratePackageOnBuild=false` to targeted `dotnet build` and `dotnet test` commands to avoid MSBuild/compiler-server sandbox failures.
- For `NINA/NINA.csproj` app builds, also add `-p:RunPostBuildEvent=Never` unless the post-build output is part of the task.
- If a previous .NET command fails with no compiler errors, first rerun it with these flags before inspecting long diagnostic logs.
- If a build or test run fails only because the sandbox denied writes under `obj`, `bin`, `TestResults`, or user-profile test paths, rerun the same command with appropriate filesystem access before treating it as a product regression.

## Resources

- `references/project-map.md`: compact owner map, common starting points, and neighboring checks.
- `references/testing-map.md`: actual `NINA.Test` routing filters, command templates, and known test constraints.


