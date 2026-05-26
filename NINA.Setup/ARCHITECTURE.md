# NINA.Setup Architecture

## Purpose

`NINA.Setup` is the WiX MSI packaging project for the application. It turns the built application and its runtime assets into an installable Windows package.

Build shape from `NINA.Setup.wixproj`:

- Project type: WiX v4 MSI package
- Output name: `NINASetup`
- References the built outputs of the main `NINA.*` runtime projects

## Packaging Model

The package definition is centered in `Product.wxs`.

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

## Dependency Position

This project sits at the packaging edge of the solution:

- it depends on nearly all runtime projects
- no runtime project depends on it

It should contain installer authoring and packaging rules, not application logic.

## Contribution Notes

- If a runtime feature requires a new shipped file or directory, verify both the executable project output and this WiX authoring.
- Keep the install layout aligned with the paths the runtime code expects, especially under `External`, `Database`, `Utility`, and `Sequencer`.
- Package behavior such as shortcuts, registry entries, and custom actions belongs here, not in the main application project.
