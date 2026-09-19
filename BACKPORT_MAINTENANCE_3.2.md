# 3.2 Hotfix 1 CI and dependency maintenance

This follow-up implements the additional maintenance scope approved after the 96 application backport topics. Target: `release/3.2.x`, release-candidate seed `3.2.1.3000-rc`. Development remains on `develop`.

## Checklist

- [x] Audit the omitted CI changes and adapt them to maintenance branches.
- [x] Keep runtime, CI and NuGet maintenance in separate topic commits.
- [x] Remain on .NET 8 and preserve the 3.2 plugin contracts.
- [x] Inventory tracked package references and select only narrowly scoped stable patches.
- [x] Compare published 3.2 public APIs and load an unchanged plugin compiled against 3.2.
- [x] Compare updated dependency APIs, assembly identities and the resolved package graph.
- [x] Run regression tests, isolated WPF tests, release-policy tests and installer checks.
- [x] Update the `3.2 Hotfix 1` release notes and document verification limits.
- [ ] Exercise hosted release automation and signed publication through the repository's protected environments.

## CI backports

The original application-backport list reviewed CI but deferred it as separate release engineering work. It is now included in `Backport protected release workflows with 3.2 plugin compatibility gates` (`01eab64b6`). Its source commits are:

| Source | Retained change |
| --- | --- |
| `b1e0d2302`, `0a981a35f`, `14a2d6023` | Protected version pull requests, validated merges and publication after successful CI |
| `df851e6ac` | Cancel superseded builds |
| `778e9e72f` | Pin MkDocs tooling |
| `998bb6537` | Set up Pandoc through its action |
| `3262dd6ea` | Produce and upload NuGet symbol packages |

The cached LFS checkout was already present. .NET 10, 3.3 package versions, the new generator package and unrelated 3.3 payload changes remain excluded.

Maintenance adaptations:

- CI runs for `release/3.2.x`, `develop`, `master` and pull requests. Test results upload with read-only repository permissions, including fork pull requests.
- Release preparation supports a build increment or a patch increment. For example, `3.2.1.9002` advances to `3.2.2.9001` for a patch release. Channel changes remain manual; build counters cannot roll into another channel.
- Only the 13 existing assembly/package version files may change in the automated version pull request. The merge job checks its exact tested head, owner-started preparation run, release branch and version-only diff.
- Publication uses `workflow_dispatch` on the selected branch. The existing **Build and Release Version** entry point can invoke the new preparation helper before that helper is registered on the default branch.
- Beta/RC and stable builds from `release/3.2.x` update the shared beta feed through the protected `beta-release` environment. Stable builds also update the stable feed. The nightly feed remains restricted to `develop`. Existing signing, upload and publication environments remain in place.
- Plugin compatibility was checked locally against exact published `3.2.0.9001` packages and an unchanged compiled plugin. The added CI runner and fixture projects were subsequently removed at the user's request; compatibility review remains manual. The local results are retained below as verification history.

See [CONTRIBUTING.md](CONTRIBUTING.md#versioning-in-nina) for the release procedure. The branch starts at assembly/file version `3.2.1.3000` and informational/package version `3.2.1.3000-rc`. Run preparation with `increment=build` to produce the first candidate, `3.2.1.3001-rc`. Release notes remain under `3.2 Hotfix 1`. RC publication uses the existing Betas storage channel and shared beta update feed, leaving the stable and nightly feeds unchanged.

## .NET servicing

`Pin .NET 8 servicing runtime and derive installer DAC filename` (`fe3a6c710`) adapts `23144353a` and `5bad81494`:

- `global.json` selects SDK **8.0.425**, permits patch roll-forward within its feature band and rejects preview SDKs.
- The self-contained application pins **8.0.31** for both the .NET and Windows Desktop runtimes. Target frameworks remain unchanged.
- WiX derives the versioned DAC alias from the packaged `mscordaccore.dll`. It no longer depends on the old 8.0.21 filename or a local build override.

These versions were checked against Microsoft's [.NET 8 downloads](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) on 2026-09-19.

## NuGet decisions

All tracked project package references were inventoried against the stable versions exposed by NuGet. Only these patches were selected:

The changes are isolated in `Apply compatibility-checked NuGet patches for 3.2` (`80e88eb10`). These are fresh servicing updates selected for 3.2, rather than a cherry-pick of develop's broad package upgrade.

| Package | Previous version | Hotfix version |
| --- | --- | --- |
| System.Drawing.Common | 8.0.7 / 8.0.10 | 8.0.31 |
| System.Data.SqlClient | 4.9.0 | 4.9.1 |
| Fastenshtein | 1.0.10 | 1.0.12 |
| Trinet.Core.IO.Ntfs | 4.1.2 | 4.1.3 |
| ZstdSharp.Port | 0.8.7 | 0.8.8 |

All five updated DLLs pass API comparison against their pre-maintenance binaries, including parameter names. Assembly names and public key tokens are retained, and no assembly version decreases. The resolved test/application dependency graph changes exactly these five packages with no additional transitive package upgrades. ZstdSharp remains the dependency introduced by the approved XISF feature.

Other available updates within existing major versions were deliberately deferred:

| Package or group | Retained version | Available version reviewed | Reason to retain the 3.2 baseline |
| --- | --- | --- | --- |
| ASCOM.Alpaca.Components, ASCOM.Alpaca.Device, ASCOM.Com.Components, ASCOM.Tools | 2.1.0 | 2.2.1 | Shared driver and plugin contracts; hardware compatibility needs separate coverage |
| CommunityToolkit.Mvvm | 8.4.0 | 8.4.2 | Preserve generated code and existing binding behavior |
| Dirkster.AvalonDock | 4.72.1 | 4.74.1 | Preserve docking and plugin view behavior |
| DotNetProjects.Extended.Wpf.Toolkit | 5.0.124 | 5.0.129 | Preserve shared WPF control behavior |
| Microsoft.Xaml.Behaviors.Wpf | 1.1.135 | 1.1.161 | Preserve shared XAML behavior contracts |
| Microsoft.Web.WebView2 | 1.0.3296.44 | 1.0.4191.47 | Browser SDK update requires separate view and runtime coverage |
| Newtonsoft.Json | 13.0.3 | 13.0.4 | Preserve profile, sequence and plugin serialization behavior |
| NJsonSchema | 11.3.2 | 11.6.1 | Preserve plugin schema generation and validation |
| Google.Protobuf / Google.Protobuf.Tools | 3.31.1 | 3.36.2 | Keep runtime and generated protocol code aligned with the existing baseline |
| Grpc.Core.Api / Grpc.Tools | 2.71.0 / 2.72.0 | 2.83.0 / 2.84.0 | Keep the existing transport and code-generation combination |
| Serilog.Sinks.Console | 6.0.0 | 6.1.1 | No selected hotfix requires changing logging behavior |
| NUnit / NUnit.Analyzers / NUnit3TestAdapter | 4.3.2 / 4.8.1 / 5.0.0 | 4.6.1 / 4.15.0 / 5.2.0 | Preserve the established regression test runner and analyzer behavior |
| FluentAssertions | [7.0.0] | 7.2.2 | Retain the repository's explicit test-library pin |
| Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers | 0.4.346202 | 0.4.421302 | Migration tooling is unnecessary for .NET 8 servicing |
| System.Data.SQLite in StarDataImport | 1.0.116 | 1.0.119 | Separate catalogue utility; shipped application already uses 1.0.119 |

Deferral does not assert that these versions are breaking. It avoids expanding the hotfix's compatibility surface without a specific need and corresponding verification. Major upgrades and prerelease transitions were excluded. Packages with no eligible newer stable version remain unchanged.

## Verification

Local Windows x64 validation after the selected dependency updates, before the release-candidate seed was selected. The installer/package versions below identify those original validation artifacts:

- Full regression suite: **3,668 passed**, three existing skips and no failures.
- Nine isolated WPF/integration runs: **56 passed**, including actual view construction and direct-IP Alpaca bindings.
- Compiled 3.2 plugin: loaded through the production assembly-load context, composed with MEF, initialized, executed, cloned, canceled and torn down without recompiling it against the hotfix. Its DLL hash remained unchanged.
- Published API comparison: all **12 contract assemblies passed** against `3.2.0.9001`; all **five updated dependency DLLs passed** against the pre-maintenance baseline. The local check also rejected a deliberately incompatible assembly.
- Release-policy tests: **25 passed**, including patch resets, all four channels, lower/upper boundaries, overflow rejection, exact version-only diffs and dispatching the selected branch. Four new cases failed before the maintenance adaptations.
- Actionlint and PowerShell parsing passed for the three changed workflows.
- Release managed/WPF build and x64 MSI build passed without a runtime override. Runtime configuration includes .NET and Windows Desktop **8.0.31**.
- MSI table inspection: product `3.2.1.9001`, **978 files**, the correct `mscordaccore_amd64_amd64_8.0.3126.42015.dll` alias and `REINSTALLMODE=amus` before `CostInitialize` in both UI and execute sequences.
- All **12 NuGet package/symbol pairs** were generated using the release workflow's explicit `--include-symbols -p:SymbolPackageFormat=snupkg` options. Archive inspection confirmed version `3.2.1.9001` and portable PDBs.

After selecting the `3.2.1.3000-rc` seed, the 25 release-policy tests and all 12 published API comparisons passed again. The unchanged 3.2 plugin fixture also passed against the rebuilt RC assemblies. A dry run on copies of the actual 13 version files produced `3.2.1.3001-rc` and passed the version-only merge gate. No hosted workflow was triggered during that local preparation.

Reproduce the principal checks:

```powershell
node --test .github/scripts/merge-version-pull-request.test.js .github/scripts/release-version.test.js
dotnet test NINA.Test/NINA.Test.csproj -c Debug
dotnet msbuild NINA.sln -restore -t:NINA_Setup -p:Configuration=Release -p:Platform=x64 -p:RestoreForce=true -m
```

Detailed package inventories, API output, TRX results and MSI inspection are retained locally under `.tmp/hotfix-maintenance-3.2`. Existing package compatibility, obsolete API, analyzer and WiX warnings remain.

Public API comparison and a representative compiled plugin provide evidence of compatibility, not exhaustive coverage of every third-party plugin's behavior. Real plugin acceptance, physical equipment checks and installer upgrade/repair acceptance remain as described in [the application backport record](BACKPORT_IMPLEMENTATION_3.2.md#remaining-release-acceptance-checks). Hosted GitHub Actions, environment approvals, signing and publication have not been executed locally. No release was published.

Proposed pull request title: **Backport 3.2 release automation and compatible .NET 8 dependency patches**.
