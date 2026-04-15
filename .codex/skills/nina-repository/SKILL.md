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
5. Use `references/project-map.md` only to choose the likely owner, neighboring files, and verification command shape.

## Edit Rules

- Keep changes in the owning layer. Do not put reusable domain logic in `NINA` when a lower `NINA.*` library owns it.
- Follow existing local patterns first: DI registrations, mediators, factories, MEF exports, serialization converters, settings models, and test helpers.
- Follow `.editorconfig` and surrounding style. For new or refactored MVVM code, prefer `CommunityToolkit.Mvvm` where it fits the existing class design.
- Localize user-visible strings through `NINA.Core/Locale/Locale.resx` only; leave translated `Locale.<culture>.resx` files to Crowdin.
- Treat runtime files as a three-part contract: code expectations, `NINA/NINA.csproj` output copying, and installer packaging in `NINA.Setup` when applicable.
- For plugin or sequencer surface changes, check discovery/composition metadata, clone behavior, serialization, and plugin-loader integration.
- For profile, database, astronomy, image-analysis, and native dependency changes, add focused tests and check compatibility with existing persisted/runtime data.

## Verification

Prefer targeted tests while iterating and broaden only when the changed surface justifies it. Use `references/project-map.md` for the command templates and neighboring checks.

## Resources

- `references/project-map.md`: compact owner map, common starting points, neighboring checks, and verification commands.
