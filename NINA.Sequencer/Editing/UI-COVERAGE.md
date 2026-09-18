# Core advanced-sequencer input coverage

Audited on 2026-09-18. This inventory covers the 104 built-in MEF entity types, their compiled editor templates and the shared advanced-sequencer editing controls. It does not promise automatic coverage for arbitrary plugin commands.

`NINA.Test/Sequencer/Editing/CoreEditorHistoryTest.cs` discovers the entity exports rather than maintaining a separate type list. `CoreEditorTestScope` constructs real entities with mocked services and loads the exported resource dictionaries into an offscreen STA window. Each editable text, selection and checkbox binding must change the accepted model value, create one entry and restore that value through both undo and redo. Forwarding bindings are followed for assertions on the model, not just the control's displayed text. Generated numeric properties are checked by expression definition.

The baseline catalog exercises 267 input instances. The linked-template target test separately exercises all eight override fields. Counts include shared editors each time an entity uses them, including the numeric and expression forms of gain. Zero means the entity has no parameter inputs in its own template; its shared structural controls still use the command paths below. The test's `AUDIT` output lists the resolved fields for each type. This catalog is embedded by the test project and enforced as the expected export and field inventory, including repeated fields. Input commits use routed focus departure.

## Additional interaction checks

| Surface | Coverage |
| --- | --- |
| Smart Exposure, Take Exposure and flat gain/filter editors | Actual compiled templates, nested child ownership, editable dropdown typing, mouse/keyboard selection, camera defaults and expression definitions |
| Annotation, Message Box and External Script | Shared expression-string editor, deferred inner binding and toolbar undo before focus departure |
| Brightness and switch value | Text entry plus both stepper buttons; accepted adjustments replay and rejected adjustments at either limit add no entry |
| RA/Dec and Alt/Az | Whole values, signs, negative zero, extreme degrees, seconds, preserved expression definitions and repeated undo/redo |
| Time, wait-until-time and reset-variable-to-date | Provider selector, manual hours/minutes/seconds and offset; undoing a provider change restores the previous manual time |
| Programmable meridian flip | Editable fields and structural operations in both before/after action sets |
| Linked templates | Eight target override inputs, separate editing history, save/cancel lifecycle and trigger actions routed to that same temporary history |
| Plugin child collections and session boundaries | Public-only fixtures exercise a replaceable child slot, an explicitly annotated drop command, child field gestures and temporary-session disposal |
| Plugin trigger action sets | Optional action-container discovery for field edits, removal and restoration through the owning history |
| Plugin snapshot providers | Coupled nested setters with immediate/deferred bindings, runtime exclusions, ordinary-field fallback, no-ops, conflicts, replay compensation and attachment hooks that replace configuration objects |
| Standard plugin bindings | Nested configuration, expressions, text, selection, checkbox, date and single writable multi-binding input with read-only context |
| Input lifecycle | Paste, validation rejection, native text undo, focus departure, explicit commit, new edit after undo and background updates |

## Configuration commands

| Input | Recording path | Verification |
| --- | --- | --- |
| Add, delete, duplicate, move up/down and drag/drop | Existing entity commands and `DropIntoBehavior` use structural capture | Structure, history and drag/drop tests |
| Conditions, root/container triggers and custom trigger sources | Same structural capture, including trigger-source placement and coordinate-condition attachment side effects | Structure/history tests, repeated coordinate-condition moves and existing trigger view tests |
| Trigger-runner contents and programmable flip actions | Explicit editor-owned action containers in graph discovery | History tests and both programmable flip action-set cases |
| Enable/disable | `SequenceEditContext.Toggle` records enabled intent | Both directions tested independently of execution status |
| Clear sequence | Capture starts after confirmation; title and lists form one transaction | Confirm/cancel and replay-without-dialog tests |
| Target/template insertion and target drop | Structural or whole-target capture around the accepted model update | Structure/drop tests; target override tests |
| Planetarium target assignment | Whole-target capture after the async lookup | Code-inspected adapter; actual planetarium interaction is outside the automated audit |
| Script/layout/save-sequence file picker | Accepted path is applied with `SequenceEditContext.Property` | Path text bindings tested; native picker accept/cancel remains a manual integration check |
| Template edit/save/cancel | A temporary history is opened/discarded by the existing template lifecycle | Lifecycle and nested trigger-session tests |

## Deliberate exclusions

Execution buttons, reset progress, completed exposure counts, exposure-count deletion, evaluated expression outputs, current variable values and equipment status indicators are not configuration undo. Expansion, menu state, moon-chart visibility, framing navigation, library/file writes and completed equipment actions also remain outside the journal. Linked-template preview contents are read-only and cannot record in the sequence journal. Template source edits use the temporary session.

Plugin checks use test entities with the existing public contracts, optional-provider fixtures and the repository plugin-composition suite. This audit does not load installed third-party plugin DLLs.

The automated tests use routed WPF events and compiled controls with mocked equipment. They do not operate physical devices, a planetarium or native file-picker dialogs. Device-mediated actions are not undone. Opaque plugin commands, mutable custom values and converters writing independent properties require the optional integration described in [README.md](README.md).

## Maintaining coverage

When adding a core input, run the catalog test and inspect its `AUDIT` row. Check every control state that changes the editable surface. Add a focused test for command handlers, coupled setters, replacement configuration objects or custom controls beyond the standard text/selection/checkbox paths. Assert accepted model state in both directions and exclusion of unrelated runtime state. Update this inventory when those surfaces change; export or field mismatches fail the catalog test. Do not reduce the expected list simply to make a failing binding test pass.

## Catalog

<!-- Catalog rows are taken from CoreEditorHistoryTest AUDIT output. -->

| Entity | Input instances | Resolved fields |
| --- | ---: | --- |
| AboveHorizonCondition | 10 | AboveHorizonCondition.OffsetExpression, InputCoordinates.RAHours, InputCoordinates.RAMinutes, InputCoordinates.RASeconds, AboveHorizonCondition.RaExpression, InputCoordinates.DecDegrees, InputCoordinates.DecMinutes, InputCoordinates.DecSeconds, AboveHorizonCondition.DecExpression, AboveHorizonCondition.PositionAngleExpression |
| AltitudeCondition | 10 | AltitudeCondition.OffsetExpression, InputCoordinates.RAHours, InputCoordinates.RAMinutes, InputCoordinates.RASeconds, AltitudeCondition.RaExpression, InputCoordinates.DecDegrees, InputCoordinates.DecMinutes, InputCoordinates.DecSeconds, AltitudeCondition.DecExpression, AltitudeCondition.PositionAngleExpression |
| LoopCondition | 1 | LoopCondition.IterationsExpression |
| LoopWhile | 1 | LoopWhile.PredicateExpression |
| LoopWhileUnsafe | 0 | No parameter inputs |
| MoonAltitudeCondition | 2 | WaitLoopData.Comparator, MoonAltitudeCondition.OffsetExpression |
| MoonIlluminationCondition | 2 | MoonIlluminationCondition.Comparator, MoonIlluminationCondition.UserMoonIlluminationExpression |
| SafetyMonitorCondition | 0 | No parameter inputs |
| SunAltitudeCondition | 2 | WaitLoopData.Comparator, SunAltitudeCondition.OffsetExpression |
| TimeCondition | 5 | TimeCondition.SelectedProvider, TimeCondition.Hours, TimeCondition.Minutes, TimeCondition.Seconds, TimeCondition.MinutesOffset |
| TimeSpanCondition | 3 | TimeSpanCondition.Hours, TimeSpanCondition.Minutes, TimeSpanCondition.Seconds |
| ConditionalContainer | 2 | ConditionalContainer.Name, ConditionalContainer.PredicateExpression |
| DeepSkyObjectContainer | 9 | DeepSkyObjectContainer.Name, InputTarget.TargetName, InputCoordinates.RAHours, InputCoordinates.RAMinutes, InputCoordinates.RASeconds, InputCoordinates.DecDegrees, InputCoordinates.DecMinutes, InputCoordinates.DecSeconds, InputTarget.PositionAngle |
| EndAreaContainer | 0 | No parameter inputs |
| LinkedTemplateContainer | 1 | LinkedTemplateContainer.Name |
| ParallelContainer | 1 | ParallelContainer.Name |
| SequenceRootContainer | 1 | SequenceRootContainer.SequenceTitle |
| SequentialContainer | 1 | SequentialContainer.Name |
| StartAreaContainer | 0 | No parameter inputs |
| TargetAreaContainer | 0 | No parameter inputs |
| RunAutofocus | 0 | No parameter inputs |
| CoolCamera | 2 | CoolCamera.TemperatureExpression, CoolCamera.DurationExpression |
| DewHeater | 1 | DewHeater.OnOff |
| SetReadoutMode | 1 | SetReadoutMode.Mode |
| SetUSBLimit | 1 | SetUSBLimit.USBLimit |
| WarmCamera | 1 | WarmCamera.DurationExpression |
| ConnectAllEquipment | 0 | No parameter inputs |
| ConnectEquipment | 1 | ConnectEquipment.SelectedDevice |
| DisconnectAllEquipment | 0 | No parameter inputs |
| DisconnectEquipment | 1 | DisconnectEquipment.SelectedDevice |
| SwitchProfile | 2 | SwitchProfile.SelectedProfileId, SwitchProfile.Reconnect |
| CloseDomeShutter | 0 | No parameter inputs |
| DisableDomeSynchronization | 0 | No parameter inputs |
| EnableDomeSynchronization | 0 | No parameter inputs |
| FindHomeDome | 0 | No parameter inputs |
| OpenDomeShutter | 0 | No parameter inputs |
| ParkDome | 0 | No parameter inputs |
| SlewDomeAzimuth | 1 | SlewDomeAzimuth.AzimuthDegreesExpression |
| SynchronizeDome | 0 | No parameter inputs |
| GlobalConstant | 2 | GlobalConstant.Identifier, GlobalConstant.Expr |
| GlobalVariable | 2 | GlobalVariable.Identifier, GlobalVariable.OriginalExpr |
| ResetVariable | 2 | ResetVariable.Variable, ResetVariable.Expr |
| ResetVariableToDate | 6 | ResetVariableToDate.Variable, ResetVariableToDate.SelectedProvider, ResetVariableToDate.Hours, ResetVariableToDate.Minutes, ResetVariableToDate.Seconds, ResetVariableToDate.MinutesOffset |
| Variable | 2 | Variable.Identifier, Variable.OriginalExpr |
| SwitchFilter | 1 | SwitchFilter.ComboBoxText |
| AutoBrightnessFlat | 12 | AutoBrightnessFlat.MinBrightnessExpression, AutoBrightnessFlat.MaxBrightnessExpression, AutoBrightnessFlat.HistogramTargetPercentage, AutoBrightnessFlat.HistogramTolerancePercentage, TakeExposure.ExposureTime, AutoBrightnessFlat.KeepPanelClosed, LoopCondition.IterationsExpression, SwitchFilter.ComboBoxText, TakeExposure.Binning, TakeExposure.Gain, TakeExposure.GainExpression, TakeExposure.OffsetExpression |
| AutoExposureFlat | 12 | AutoExposureFlat.MinExposure, AutoExposureFlat.MaxExposure, AutoExposureFlat.HistogramTargetPercentage, AutoExposureFlat.HistogramTolerancePercentage, SetBrightness.Brightness, AutoExposureFlat.KeepPanelClosed, LoopCondition.IterationsExpression, SwitchFilter.ComboBoxText, TakeExposure.Binning, TakeExposure.Gain, TakeExposure.GainExpression, TakeExposure.OffsetExpression |
| CloseCover | 0 | No parameter inputs |
| OpenCover | 0 | No parameter inputs |
| SetBrightness | 1 | SetBrightness.BrightnessExpression |
| SkyFlat | 13 | SkyFlat.MinExposure, SkyFlat.MaxExposure, SkyFlat.HistogramTargetPercentage, SkyFlat.HistogramTolerancePercentage, SkyFlat.ShouldDither, SkyFlat.DitherPixels, SkyFlat.DitherSettleTime, LoopCondition.IterationsExpression, SwitchFilter.ComboBoxText, TakeExposure.Binning, TakeExposure.Gain, TakeExposure.GainExpression, TakeExposure.OffsetExpression |
| ToggleLight | 1 | ToggleLight.OnOff |
| TrainedDarkFlatExposure | 7 | LoopCondition.IterationsExpression, SwitchFilter.ComboBoxText, TakeExposure.Binning, TakeExposure.Gain, TakeExposure.GainExpression, TakeExposure.OffsetExpression, TrainedDarkFlatExposure.KeepPanelClosed |
| TrainedFlatExposure | 7 | LoopCondition.IterationsExpression, SwitchFilter.ComboBoxText, TakeExposure.Binning, TakeExposure.Gain, TakeExposure.GainExpression, TakeExposure.OffsetExpression, TrainedFlatExposure.KeepPanelClosed |
| MoveFocuserAbsolute | 1 | MoveFocuserAbsolute.PositionExpression |
| MoveFocuserByTemperature | 3 | MoveFocuserByTemperature.SlopeExpression, MoveFocuserByTemperature.Absolute, MoveFocuserByTemperature.InterceptExpression |
| MoveFocuserRelative | 1 | MoveFocuserRelative.RelativePositionExpression |
| Dither | 0 | No parameter inputs |
| StartGuiding | 1 | StartGuiding.ForceCalibration |
| StopGuiding | 0 | No parameter inputs |
| SmartExposure | 9 | SmartExposure.IterationsExpression, TakeExposure.ExposureTimeExpression, TakeExposure.ImageType, TakeExposure.Binning, TakeExposure.Gain, TakeExposure.GainExpression, TakeExposure.OffsetExpression, SwitchFilter.ComboBoxText, DitherAfterExposures.AfterExposuresExpression |
| TakeExposure | 6 | TakeExposure.ExposureTimeExpression, TakeExposure.ImageType, TakeExposure.Binning, TakeExposure.Gain, TakeExposure.GainExpression, TakeExposure.OffsetExpression |
| TakeManyExposures | 7 | TakeManyExposures.IterationsExpression, TakeExposure.ExposureTimeExpression, TakeExposure.ImageType, TakeExposure.Binning, TakeExposure.Gain, TakeExposure.GainExpression, TakeExposure.OffsetExpression |
| TakeSubframeExposure | 12 | TakeSubframeExposure.ExposureTimeExpression, TakeSubframeExposure.ImageType, TakeSubframeExposure.Binning, TakeSubframeExposure.Gain, TakeSubframeExposure.GainExpression, TakeSubframeExposure.OffsetExpression, TakeSubframeExposure.ROIOption, TakeSubframeExposure.ROIPctExpression, TakeSubframeExposure.LeftExpression, TakeSubframeExposure.TopExpression, TakeSubframeExposure.WidthExpression, TakeSubframeExposure.HeightExpression |
| Center | 9 | InputCoordinates.RAHours, InputCoordinates.RAMinutes, InputCoordinates.RASeconds, Center.RaExpression, InputCoordinates.DecDegrees, InputCoordinates.DecMinutes, InputCoordinates.DecSeconds, Center.DecExpression, Center.PositionAngleExpression |
| CenterAndRotate | 9 | InputCoordinates.RAHours, InputCoordinates.RAMinutes, InputCoordinates.RASeconds, CenterAndRotate.RaExpression, InputCoordinates.DecDegrees, InputCoordinates.DecMinutes, InputCoordinates.DecSeconds, CenterAndRotate.DecExpression, CenterAndRotate.PositionAngleExpression |
| SolveAndRotate | 1 | SolveAndRotate.PositionAngleExpression |
| SolveAndSync | 0 | No parameter inputs |
| MoveRotatorMechanical | 1 | MoveRotatorMechanical.MechanicalPositionExpression |
| WaitUntilSafe | 0 | No parameter inputs |
| SetSwitchValue | 2 | SetSwitchValue.SelectedSwitch, SetSwitchValue.ValueExpression |
| FindHome | 0 | No parameter inputs |
| ParkScope | 0 | No parameter inputs |
| SetTracking | 1 | SetTracking.TrackingMode |
| SlewScopeToAltAz | 9 | InputTopocentricCoordinates.AltDegrees, InputTopocentricCoordinates.AltMinutes, InputTopocentricCoordinates.AltSeconds, SlewScopeToAltAz.AltExpression, InputTopocentricCoordinates.AzDegrees, InputTopocentricCoordinates.AzMinutes, InputTopocentricCoordinates.AzSeconds, SlewScopeToAltAz.AzExpression, SlewScopeToAltAz.Tracking |
| SlewScopeToRaDec | 9 | InputCoordinates.RAHours, InputCoordinates.RAMinutes, InputCoordinates.RASeconds, SlewScopeToRaDec.RaExpression, InputCoordinates.DecDegrees, InputCoordinates.DecMinutes, InputCoordinates.DecSeconds, SlewScopeToRaDec.DecExpression, SlewScopeToRaDec.PositionAngleExpression |
| UnparkScope | 0 | No parameter inputs |
| Annotation | 1 | Annotation.Text |
| ExternalScript | 1 | ExternalScript.Script |
| LoadImagingLayout | 1 | LoadImagingLayout.FilePath |
| MessageBox | 1 | MessageBox.Text |
| SaveSequence | 1 | SaveSequence.FilePath |
| WaitForAltitude | 11 | WaitForAltitude.AboveOrBelow, WaitForAltitude.OffsetExpression, InputCoordinates.RAHours, InputCoordinates.RAMinutes, InputCoordinates.RASeconds, WaitForAltitude.RaExpression, InputCoordinates.DecDegrees, InputCoordinates.DecMinutes, InputCoordinates.DecSeconds, WaitForAltitude.DecExpression, WaitForAltitude.PositionAngleExpression |
| WaitForMoonAltitude | 2 | WaitLoopData.Comparator, WaitForMoonAltitude.OffsetExpression |
| WaitForSunAltitude | 2 | WaitLoopData.Comparator, WaitForSunAltitude.OffsetExpression |
| WaitForTime | 5 | WaitForTime.SelectedProvider, WaitForTime.Hours, WaitForTime.Minutes, WaitForTime.Seconds, WaitForTime.MinutesOffset |
| WaitForTimeSpan | 1 | WaitForTimeSpan.TimeExpression |
| WaitUntil | 1 | WaitUntil.PredicateExpression |
| WaitUntilAboveHorizon | 10 | WaitUntilAboveHorizon.OffsetExpression, InputCoordinates.RAHours, InputCoordinates.RAMinutes, InputCoordinates.RASeconds, WaitUntilAboveHorizon.RaExpression, InputCoordinates.DecDegrees, InputCoordinates.DecMinutes, InputCoordinates.DecSeconds, WaitUntilAboveHorizon.DecExpression, WaitUntilAboveHorizon.PositionAngleExpression |
| AutofocusAfterExposures | 1 | AutofocusAfterExposures.AfterExposuresExpression |
| AutofocusAfterFilterChange | 0 | No parameter inputs |
| AutofocusAfterHFRIncreaseTrigger | 3 | AutofocusAfterHFRIncreaseTrigger.SampleSizeExpression, AutofocusAfterHFRIncreaseTrigger.AmountExpression, AutofocusAfterHFRIncreaseTrigger.TrendPerFilter |
| AutofocusAfterTemperatureChangeTrigger | 1 | AutofocusAfterTemperatureChangeTrigger.AmountExpression |
| AutofocusAfterTimeTrigger | 1 | AutofocusAfterTimeTrigger.AmountExpression |
| ReconnectOnDownloadFailure | 0 | No parameter inputs |
| ReconnectTrigger | 1 | ReconnectTrigger.SelectedDevice |
| SynchronizeDomeTrigger | 0 | No parameter inputs |
| DitherAfterExposures | 1 | DitherAfterExposures.AfterExposuresExpression |
| RestoreGuiding | 0 | No parameter inputs |
| MeridianFlipTrigger | 0 | No parameter inputs |
| ProgrammableMeridianFlipTrigger | 9 | InputCoordinates.RAHours, InputCoordinates.RAMinutes, InputCoordinates.RASeconds, Center.RaExpression, InputCoordinates.DecDegrees, InputCoordinates.DecMinutes, InputCoordinates.DecSeconds, Center.DecExpression, Center.PositionAngleExpression |
| CenterAfterDriftTrigger | 2 | CenterAfterDriftTrigger.AfterExposuresExpression, CenterAfterDriftTrigger.DistanceArcMinutesExpression |
| TriggerOnUnsafe | 0 | No parameter inputs |
| CustomTrigger | 0 | No parameter inputs |
