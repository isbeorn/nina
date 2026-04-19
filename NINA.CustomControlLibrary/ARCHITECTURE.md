# NINA.CustomControlLibrary Architecture

## Purpose

`NINA.CustomControlLibrary` is the reusable WPF control library for N.I.N.A.-specific controls and their default themes.

Build shape from `NINA.CustomControlLibrary.csproj`:

- Target framework: `net10.0-windows`
- Output type: `Library`
- WPF enabled

## What Lives Here

The project is intentionally small and split between control classes and theme dictionaries:

- Control classes in the project root
  `AsyncProcessButton`, `CancellableButton`, `AutoCompleteBox`, `HintTextBox`, `UnitTextBox`, `StepperControl`, `LoadingControl`, `Arc`, `OutlinedTextBlock`, `IntStepperControl`, `DetachingExpander`, `GridHelpers`, `HitTestGroupBox`, `CardinalSplineShape`
- Value converters
  `Converters/*`
- Theme dictionaries
  `Themes/*.Generic.xaml`

`Themes/Generic.xaml` is the merge point for the per-control resource dictionaries:

- `StepperControl.Generic.xaml`
- `CancellableButton.Generic.xaml`
- `HintTextBox.Generic.xaml`
- `UnitTextBox.Generic.xaml`
- `LoadingControl.Generic.xaml`
- `AsyncProcessButton.Generic.xaml`
- `Arc.Generic.xaml`
- `AutoCompleteBox.Generic.xaml`

## Control Model

The controls are normal WPF custom controls with `DefaultStyleKeyProperty` overrides and dependency properties.

Examples:

- `AsyncProcessButton`
  Extends `CancellableButton` with pause/resume commands and icon properties.
- `AutoCompleteBox`
  Extends `HintTextBox` and coordinates popup/list behavior around `IAutoCompleteItem` collections from `NINA.Core.Interfaces`.

There is no app startup logic, no DI container, and no project-specific view model layer here.

## Dependency Position

Internal dependency surface is minimal:

- `NINA.Core`

That matches the code. The library uses shared types like `IAutoCompleteItem`, but it does not depend on app-shell code, mediators, equipment implementations, or profiles.

## Contribution Notes

- Add new reusable controls here only when they are presentation primitives, not feature-specific views.
- For a new control, add both the C# control class and its default theme XAML, then merge the theme in `Themes/Generic.xaml`.
- Keep feature logic out of the control library; app/view-model orchestration belongs in higher projects.
