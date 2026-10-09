# AGENTS.md

Guidance for work in `NINA.sln`. Keep this entry point compact and put subsystem details in the owning architecture document.

## Read For The Task

Use the relevant route when more context is needed. Guidance already loaded for the task does not need to be reread.

| Task | Reference |
| --- | --- |
| Find ownership, trace lifecycle or change a cross-project boundary | [Solution architecture and project index](ARCHITECTURE.md), then the owning project's linked document |
| Select tests, build on Windows or diagnose verification failures | [NINA repository skill](.agents/skills/nina-repository/SKILL.md) and its task-specific references |
| Prepare an upstream contribution, release note or documentation update | [Contribution guide](CONTRIBUTING.md) |
| Fix prose or links | The affected document and its incoming links; application builds are unnecessary |

`NINA.Docs` is a separate [documentation repository](https://github.com/isbeorn/nina.docs.git) included as a submodule. It is outside `NINA.sln`; coordinate user-facing documentation there when applicable.

## Working Boundaries

- Carry authorized local work through implementation and appropriate verification without repeated confirmation. Upstream discussion requirements apply to contribution proposals and major changes; they are not a separate approval gate for an agreed local task.
- Preserve existing architecture, public interfaces and serialized compatibility unless the task requires changing them. Plugin consumers and saved profiles/sequences can outlive the current build.
- Keep reusable logic in its owning library, app-shell composition in `NINA` and plugin loading rules in `NINA.Plugin`. Use shared mediator interfaces for communication from libraries to UI handlers.
- Treat versioned repository files as the durable source of project knowledge. Record newly discovered contracts in the nearest doc, test or analyzer when they will help future work.

## Style

- Follow [`.editorconfig`](.editorconfig) for C#. Preserve touched files' line endings; use CRLF for new files unless the location dictates otherwise.
- Keep C# declarations readable: separate constructors, properties, methods and type declarations with one blank line. Keep related fields together and separate field groups from other members. Match the surrounding same-line brace style.
- Within methods, separate guard clauses and distinct logical steps with blank lines. Expand control-flow bodies and long expressions instead of compressing them onto one line; wrap long parameter and argument lists consistently. Review new and touched code for this spacing before handoff, even when an automatic formatter preserves compact code.
- Follow surrounding XAML style; there is no repository-wide XAML formatter configuration.
- Prefer modern C# supported by the project and `CommunityToolkit.Mvvm` for new or refactored MVVM code where it fits.
- Avoid new warnings. Report any intentional deferral with its reason.

## NINA-Specific Constraints

### State And Extension Contracts

- Typed settings belong in `NINA.Profile`. Account for `IProfileService.ProfileChanged` when retaining settings references and preserve defaults, notifications and persisted compatibility.
- Database changes span `NINA.Core.Database.NINADbContext` and `NINA/Database`. Follow the [migration rules](CONTRIBUTING.md#database-enhancements); changing initial SQL does not migrate existing installations.
- Sequence entities need consistent MEF metadata, factory creation, cloning, parent attachment and validation. Preserve sequence JSON and plugin contracts; consult the [sequencer architecture](NINA.Sequencer/ARCHITECTURE.md) when changing these paths.
- For expression-backed entities, use the [generator contract and diagnostics](NINA.Sequencer.Generators/ARCHITECTURE.md). Evaluate live values through generated scalar properties; attachment and watchdog ordering remain the entity's responsibility.

### Resources And Distribution

- Localize user-visible strings through `NINA.Core.Locale.Loc`. Edit only `NINA.Core/Locale/Locale.resx`; translated `Locale.<culture>.resx` files are managed by Crowdin.
- When adding, removing or replacing a dependency, synchronize `NINA/View/About/ThirdPartyLicensesView.xaml` and `NINA/3rd-party-licenses.txt`. Remove stale entries and record any deliberate choice among multiple licenses consistently.
- Runtime files must be copied by `NINA/NINA.csproj` and packaged by `NINA.Setup` when needed. Check test output copying for assets used by tests. Common asset roots are `NINA/External`, `NINA/Utility`, `NINA/Database` and `NINA/Sequencer/Examples`.

### Science And WPF

- Base astronomical and other sensitive numerical changes on published papers, standards or official model documentation. Use documented reference values, boundary cases and regression tests; SOFA/NOVAS examples already exist in `NINA.Test/AstrometryTest`.
- WPF tests that construct views or use `Application.Current.Resources` need STA and generally `[NonParallelizable]`. Compile affected XAML and instantiate the relevant view/template when practical; compilation alone misses runtime resource and binding failures.
- Isolate file-writing tests in temporary paths or injected storage unless the real user-storage location is the behavior under test.

## Verification And Completion

- Select checks for the changed behavior using the [testing map](.agents/skills/nina-repository/references/testing-map.md#routing-table). Broaden when shared contracts, persistence, numerical behavior or UI integration are affected.
- For bug fixes, reproduce the failure at the closest meaningful layer when practical. Cover paired operations and relevant boundaries, including clone/reset/attachment paths for sequencer changes.
- Run relevant checks after the final source edit and inspect the final diff. Once those checks pass, repeat or broaden only for new changes, failures or unresolved concerns.
- For prose-only changes, check links, anchors, moved references and formatting. For skill changes, also validate metadata and discovery; for test-command changes, verify filters against discovery.
- Report substantive changes, checks performed and any verification gap. Do not report unverified behavior as complete. Include a proposed PR title with a code handoff.
- Focused local checks do not replace required upstream CI. Submission requirements live in [CONTRIBUTING.md](CONTRIBUTING.md#pull-requests).
