# NINA.Setup Architecture

## Purpose

`NINA.Setup` is the WiX MSI packaging project for the application. It turns the built application and its runtime assets into an installable Windows package.

Build shape from `NINA.Setup.wixproj`:

- Project type: WiX v4 MSI package
- Output name: `NINASetup`
- References the built outputs of the main `NINA.*` runtime projects

## Packaging Model

The package definition is centered in `Product.wxs`.

`Directory.Build.props` enables optimization for `SignedRelease` on every platform. The release workflow builds the installer project directly with `Platform=x64`, so its C# project references inherit `x64` rather than the solution's `Any CPU` mappings. Optimization must not depend on those mappings.

From the code, the MSI is responsible for:

- installing the application under Program Files
- registering install location in the registry
- configuring major upgrades
- creating program-menu and desktop shortcuts
- creating `%LOCALAPPDATA%\\NINA` support folders
- registering Windows Error Reporting crash-dump settings for `NINA.exe`
- adding custom actions related to API firewall and URL ACL setup

## What Gets Packaged

`Product.wxs` does not just package `NINA.exe`. It explicitly includes:

- core project outputs through project references
- native SDK/runtime folders under `External/x64/*`
- utility files such as `Utility/ExifTool`
- database initialization and migration scripts
- localization folders
- sequencer example templates
- harvested documentation under `docs`

The file layout in the MSI mirrors the runtime layout expected by the executable and libraries.

## Upgrade File Replacement

`Product.wxs` schedules `REINSTALLMODE=amus` before `CostInitialize` in both MSI sequences. This must be a scheduled property assignment rather than a Property-table default because Burn supplies its own `REINSTALLMODE` value on the MSI command line. The scheduled assignment makes file costing replace every packaged file during upgrade and repair, including files that are missing, locally changed or report a higher version than the packaged file.

`Tests/InstallerUpgradeFileBehavior.ps1` verifies this through two isolated full-payload Burn bundles. It covers missing, lower-version, equal-version mismatch, higher-version and modified unversioned files. It also covers `Newtonsoft.Json.dll` as a third-party dependency and verifies that an unrelated sentinel is preserved. The harness uses test-only product, bundle, component, registry and install-directory identities and confirms that existing N.I.N.A. registrations and selected file hashes are unchanged after cleanup.

## Project References And Harvesting

`NINA.Setup.wixproj` references the built outputs of:

- `NINA`
- `NINA.Astrometry`
- `NINA.Core`
- `NINA.CustomControlLibrary`
- `NINA.Equipment`
- `NINA.Image`
- `NINA.MGEN`
- `NINA.PlateSolving`
- `NINA.Plugin`
- `NINA.Profile`
- `NINA.Sequencer`
- `NINA.WPF.Base`
- `nikoncswrapper`

Those references use `DoNotHarvest=True`, so the WiX authoring stays explicit. Documentation is the notable exception: the project uses `HarvestDirectory` to package `NINA/bin/<configuration>/net10.0-windows/win-x64/docs`.

## Published Payloads And ReadyToRun

The release workflow runs `.github/scripts/publish-application.ps1` in `ReadyToRun` mode, attempts to sign the final rewritten NINA assemblies and copies the generated documentation into the publish directory. It passes that absolute directory as `NinaPublishDir` to the bundle build. The MSI then skips its application project references and takes every runtime file from the published payload. This prevents an installer rebuild from substituting ordinary IL assemblies for the prepared native images. Without `NinaPublishDir`, the existing project-output packaging path remains available.

The publisher requires an empty destination and accepts `Baseline`, `ReadyToRun` or `ReadyToRunComposite`. It preserves the private Canon and Nikon SDK assets. `ReadyToRunComposite` remains an experimental publishing option; the installer includes `NINA.r2r.dll` whenever that file exists in the selected payload. Its component assemblies and native image must always come from the same publish operation.

The MSI records the selected publish path and preprocessor values in an incremental-build input. WiX otherwise checks file timestamps without noticing a changed publish directory, which can reuse a previous mode's MSI. The input also invalidates documentation harvesting when the selected payload changes.

For a local unsigned package, publish with `-Configuration Release`, copy the MkDocs site into `<publish>/docs` and build `NINA.Setup/NINA.Setup.wixproj` with `-p:Configuration=Release -p:Platform=x64 -p:NinaPublishDir=<absolute-publish-directory>`. For release signing, use `-Configuration SignedRelease -Sign` with the configured Sectigo certificate and `signtool.exe` available. Like the existing project signing steps, `-Sign` is best effort: a missing tool or failed signing/verification produces a warning while publishing succeeds. Publishing failures remain fatal. Signing before publishing alone is insufficient because ReadyToRun rewrites the DLLs.

## Dependency Position

This project sits at the packaging edge of the solution:

- it depends on nearly all runtime projects
- no runtime project depends on it

It should contain installer authoring and packaging rules, not application logic.

## Contribution Notes

- If a runtime feature requires a new shipped file or directory, verify both the executable project output and this WiX authoring.
- Keep the install layout aligned with the paths the runtime code expects, especially under `External`, `Database`, `Utility`, and `Sequencer`.
- Package behavior such as shortcuts, registry entries, and custom actions belongs here, not in the main application project.
- Keep the scheduled `REINSTALLMODE=amus` assignment before file costing. A Property-table default can be overwritten by the Burn command line and can cause a skipped higher-version file to be removed during a major upgrade.
