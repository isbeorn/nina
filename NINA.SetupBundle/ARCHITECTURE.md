# NINA.SetupBundle Architecture

## Purpose

`NINA.SetupBundle` is the WiX Burn bootstrapper project. It wraps the MSI from `NINA.Setup` in a branded installer executable and controls bundle-level install UX.

Build shape from `NINA.SetupBundle.wixproj`:

- Project type: WiX v4 bundle
- Output type: `Bundle`
- Output name: `NINASetupBundle`
- Depends on `NINA.Setup`

## Bundle Definition

`Bundle.wxs` is the main entry point.

The bundle:

- uses `WixStandardBootstrapperApplication`
- applies a custom theme file (`RtfTheme.xml`) and logo (`ninasplash-small.png`)
- uses generated release notes as the displayed license file
- resolves a previous install folder from the registry and reuses it when available
- chains a single `MsiPackage` for `NINASetup.msi`

The bundle is therefore a thin outer installer shell around the MSI, not a second independent package definition.

Burn controls the MSI command line for install and repair, including `REINSTALLMODE`. The chained MSI deliberately reasserts its required file-replacement mode before file costing. Do not rely on an MSI Property-table default for a value that Burn passes on the command line.

## Build-Time Responsibilities

`NINA.SetupBundle.wixproj` also performs build-pipeline work:

- `PreBuild`
  Runs Pandoc to convert `RELEASE_NOTES.md` into `RELEASE_NOTES.rtf` and `RELEASE_NOTES.html`.
- `PostBuild`
  Deletes the temporary RTF file and copies PDBs into the bundle output directory.

Those steps are part of the installer pipeline, not runtime behavior.

## Dependency Position

This project depends on:

- `NINA.Setup`

No runtime code depends on it. All application file layout still comes from the MSI project.

## Contribution Notes

- Change installer shell behavior here when the bundle UI, chained package behavior, or release-note presentation needs to change.
- Keep actual application file packaging in `NINA.Setup`; this project should stay focused on the outer bootstrapper.
- If you change release-note generation or branding assets, update both the WiX markup and the prebuild/postbuild steps.
