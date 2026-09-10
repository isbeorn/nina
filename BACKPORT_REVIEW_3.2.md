# NINA 3.2 backport candidate review

Reviewed on 2026-09-10. This is a selection report for a maintained 3.2 release line. No application changes or cherry-picks were made.

The complete range contains **447 commits: 413 non-merge commits and 34 merges**. The list contains **61 recommended fix groups, 15 conditional ports, 19 optional small improvements and 1 approved larger feature**. These are independently selectable behaviors, not a commit-by-commit cherry-pick queue. Some groups share a source commit and some require several commits together.

**Selection decision:** The user accepted the list and explicitly added direct-IP Alpaca as an exception to the small-feature limit. A01 includes the original feature and its relevant follow-up fixes. Existing compatibility constraints and required validation still apply.

**Start with the recommended fixes, applying each as a focused 3.2 change.** Keep the conditional ports separate until their compatibility or behavior questions are resolved. Optional features can follow once the core fixes have passed the release checks.

## Scope and baseline

| Reference | Audited commit |
| --- | --- |
| `release/3.2.x`, `HEAD` and `master` | `2393eae581145ed5b8114bf07c48ca2580540fd5` |
| `develop` | `6a83379822e0e015d13591a5ede54574ecc3d0e3` |
| Merge base | `2393eae581145ed5b8114bf07c48ca2580540fd5` |

The local master and develop tips match their remote heads, including a final read-only remote check. release/3.2.x is the local review branch; no matching remote release/3.2.x head was returned. The release baseline is 3.2.0.9001 and targets .NET 8; develop has moved to .NET 10. The analysis is pinned to these commits, so later development is outside this report.

I reviewed the complete commit inventory, changed-file manifests, source diffs, relevant 3.2 implementations and tests, merge conflict resolutions and submodule history. Broad feature commits were also checked for independently useful smaller changes. The result is recorded commit by commit in [the complete ledger](BACKPORT_REVIEW_3.2_COMMITS.md).

The eligibility rule is existing-behavior fixes or bounded improvements that can retain the 3.2 runtime, sequence format and plugin contract. Interface additions, public signature changes and constructor changes count as compatibility concerns even when the feature itself looks small. New optional concrete properties or enum values require preserving existing values/defaults and testing old data.

## Important findings for selection

- **33 commits already have equivalent behavior in 3.2.** The develop-only ancestry is not a list of missing fixes: release-side commits have different identities or were combined. Examples include the HFR percentage formula, Nikon Z8/Z9 live-view header, original dome cancellation guard, daily-rollover display, initial framing rotation and native filter selection.
- **Do not cherry-pick the coverage commits wholesale.** In particular, `82b42b6cb` contains many real production fixes across unrelated areas. R19-R60 identify the applicable pieces; the ledger links the exact groups for each mixed commit.
- **Do not cherry-pick either modal-window change alone.** `10398d812` needs the correction in `100cfcb1b`. Likewise, the DEBUG indicator needs its later overlap fix.
- **Several upstream patches need correction or adaptation.** The color-schema patch checks the wrong field for the alternate scheme; ASCOM exposure metadata changes public types and has invalid-value failure paths; the HFR option needs its value copied by Clone; the rotator logging patch also removes a public serialized property.
- **New SDK locking syntax is not .NET 8-compatible.** Adapt it and audit native callback/close interactions. Do not bring the QHY interface cleanup or all vendor binaries along with a small camera fix.

A useful first batch would prioritize capture/cancellation and shutdown (R02-R06, R11-R18), data correctness and persistence (R28, R31-R37, R44-R45), plate-solver error handling (R52-R54) and the installer fix (R01). These still need the targeted checks below; the grouping is a priority suggestion, not a claim that they have already been validated on 3.2.

## Recommended fixes

These address existing 3.2 paths and have a bounded scope. "Recommended" means worth preparing and testing for backport, not certified safe to cherry-pick.

### R01. Make installer upgrades and repairs restore the packaged files

Source: [56d8f144a](https://github.com/isbeorn/nina/commit/56d8f144a469c01639dd8df5eefdd32d5cc6a62f).

Missing, modified or higher-version files can be skipped during file costing and then disappear during an upgrade. Particularly relevant to repeated 3.2 patch releases.

**Port scope:** Port the scheduled REINSTALLMODE=amus SetProperty before CostInitialize in both MSI sequences. The older Property-table setting from 96e89ddfc is already in 3.2 and is insufficient. Keep the 3.2 payload and runtime.

**Required validation:** Run the isolated installer harness adapted to 3.2: fresh install, upgrade and repair; missing/lower/equal/higher-version files, modified unversioned files and unrelated sentinel preservation.

### R02. Propagate Abort Exposure cancellation into the active capture

Source: [30eaa1cd6](https://github.com/isbeorn/nina/commit/30eaa1cd630b17653885ee28e2d5e81577912e2a).

An abort can leave the capture task waiting because its cancellation token was not canceled.

**Port scope:** Port CameraMediator linked cancellation and lifecycle handling. Existing mediator signatures stay intact.

**Required validation:** Abort during exposure and download, caller cancellation, repeated abort, capture after abort and disposal races.

### R03. Fix Nikon automatic exposure cancellation

Source: [635b514bd](https://github.com/isbeorn/nina/commit/635b514bd91a7db9cd2c9c191123a93b0fe5aefe).

Canceling a non-bulb automatic exposure can issue an inappropriate bulb-stop operation and fail fatally.

**Port scope:** Port the automatic/bulb distinction, exactly-once stop action and cancellation handling without the unrelated Nikon SDK enum refresh.

**Required validation:** Automatic exposures below and at 30 seconds, bulb above 30 seconds, external shutter modes, cancel/disconnect/reconnect and exactly one stop per active bulb exposure. Hardware confirmation required.

### R04. Apply readout mode before gain and offset

Source: [0cf9ddd13](https://github.com/isbeorn/nina/commit/0cf9ddd130b5ee59f41844b7bd440158ac116677).

Changing readout mode can reset gain and offset after the requested values have already been applied.

**Port scope:** Port both snapshot and normal capture paths in CameraVM. Pair with R05 for QHY-specific state handling.

**Required validation:** Verify call order and actual gain/offset in both capture paths, changed/unchanged modes and consecutive exposures.

### R05. Keep QHY gain and offset correct across read mode changes

Source: [f360a7bab](https://github.com/isbeorn/nina/commit/f360a7bab50bae776af928fe0e5dbde4c996506e).

QHY mode changes and initialization can reset settings or expose inflated values on affected modes.

**Port scope:** Adapt to the existing 3.2 IQhySdk signatures. Include the mode-specific gain/offset probe and restoration logic, then pair with R04. Do not import the StringBuilder API removal.

**Required validation:** Switch modes in both directions, minimum/maximum gain and offset, fixed-offset/HDR modes, initial connection and actual image metadata. Hardware required.

### R06. Shut down QHY cooling and sensor workers during disconnect

Source: [77c403c1e](https://github.com/isbeorn/nina/commit/77c403c1eb48074d8e81044def97d8e998f53bb4).

Setting Connected=false too early can cause teardown to skip cooler shutdown.

**Port scope:** Port the teardown ordering and review the accompanying removal of CoolerOn=false on connection as one lifecycle change.

**Required validation:** Cooling on/off, disconnect while exposing, connection failure and reconnect. Confirm no SDK calls after close and the intended cooler state on hardware.

### R07. Close QHY filter-wheel handles when discovery fails

Source: [a7f5a2f3c](https://github.com/isbeorn/nina/commit/a7f5a2f3c78737b765ca4c96e96272a69a4c4dec).

An exception while probing a QHY wheel can escape and leave the camera handle open.

**Port scope:** Port guarded probing and finally-based cleanup using the old StringBuilder-based SDK API.

**Required validation:** Probe success, failures at each SDK call, no wheel, repeated discovery and exactly-once close.

### R08. Bound ASCOM binning enumeration on camera connection

Source: [f747a569b](https://github.com/isbeorn/nina/commit/f747a569b1ae637d70bc0fdebe9315b09f6f2d42).

Large or invalid driver maxima can cause excessive work or overflow in binning list construction.

**Port scope:** Port capability caching, integer iteration and the 1..16 UI enumeration bound. Driver capabilities themselves are not changed.

**Required validation:** Zero/negative/one/16/greater-than-16/short.MaxValue maxima, symmetric and asymmetric X/Y modes and driver capability exceptions.

### R09. Connect to ASCOM V1 telescope drivers with unsupported tracking properties

Source: [0ef041c3e](https://github.com/isbeorn/nina/commit/0ef041c3e97684d9aede0478e86cea02941d493b).

Unguarded TrackingRates access can make otherwise usable older telescope drivers fail to connect.

**Port scope:** Port capability fallbacks and guarded tracking access while retaining the existing telescope interface.

**Required validation:** V1 unsupported properties, modern drivers, tracking on/off and successful/failed mode changes.

### R10. Connect to ASCOM V1 cameras without SensorType support

Source: [d00af6c37](https://github.com/isbeorn/nina/commit/d00af6c372fe1606018597ed92e981d1df28b2e8).

An optional SensorType check can throw and prevent connection.

**Port scope:** Port the targeted ASCOM.NotImplementedException guard.

**Required validation:** Unsupported SensorType, supported monochrome/color types and real connection failure propagation.

### R11. Wait for Cool Camera targets from either temperature direction

Source: [0f8db1ff5](https://github.com/isbeorn/nina/commit/0f8db1ff5b143fe2a54ef1dc2b4d0bbe17fd711e).

Cool Camera can complete immediately when the camera starts colder than the requested target.

**Port scope:** Port absolute target tolerance and final setpoint handling into the existing non-expression 3.2 instructions and temperature regulator.

**Required validation:** Start above/below/at target and both tolerance boundaries; ramp enabled/disabled, cancellation and the paired Warm Camera operation.

### R12. Complete dome parking reliably and enforce its waiting timeout

Source: [6a8337982](https://github.com/isbeorn/nina/commit/6a83379822e0e015d13591a5ede54574ecc3d0e3).

Cached park state can report completion incorrectly or leave a sequence waiting indefinitely.

**Port scope:** Port direct driver state checks, rejected-park handling, disconnect handling and the linked 10-minute waiting timeout. A synchronous driver call that never returns is still not forcibly terminated.

**Required validation:** Already parked, slow successful park, rejected command, stopped-but-not-parked, disconnect, user cancellation, timeout and AbortSlew exceptions.

### R13. Serialize NOVAS calls and reject corrupt Sun/Moon results

Source: [27625373a](https://github.com/isbeorn/nina/commit/27625373ab6335ab62248aa3d7833ee18bb9a734).

Concurrent calls into shared native NOVAS state can corrupt Sun/Moon altitude calculations used by sequencing.

**Port scope:** Adapt the common native-call gate and invalid-result handling to the 3.2 astronomy methods. Keep public signatures, including the managed NOVAS_geo_posvel entry point. The broad SOFA/time-scale rewrite is not a prerequisite for this isolated fix.

**Required validation:** Concurrent Sun/Moon/coordinate calculations against serial references, every native entry point, native errors and NaN/Infinity in both above/below altitude conditions and wait instructions.

### R14. Stop PHD2 guiding from every non-stopped state

Source: [34cec02a9](https://github.com/isbeorn/nina/commit/34cec02a90e108b8736b968ea4030714cd066fa1).

Stop Guiding currently skips some active states instead of ensuring STOPPED.

**Port scope:** Port the state predicate independently of the persistent-socket refactor.

**Required validation:** STOPPED, guiding, calibrating, lost lock, looping and paused states; cancellation and disconnected behavior.

### R15. Read complete PHD2 event messages across TCP packet boundaries

Source: [12b7cec15](https://github.com/isbeorn/nina/commit/12b7cec158140e5c7fc352e8a3cba45599ea98df).

The old chunk-based listener can parse partial messages or mishandle message boundaries. A line reader also allows clean cancellation and end-of-stream handling.

**Port scope:** Port the UTF-8 line listener onto the 3.2 two-connection architecture. Do not take the ReadExactlyAsync(1024) change from d2ff4d5eb or require the later single-socket rewrite.

**Required validation:** Split and coalesced JSON lines, Unicode, blank/malformed lines, remote close, cancellation and reconnect with the real PHD2 protocol.

### R16. Stop the OpenMeteo worker cleanly on cancellation

Source: [56a7424b1](https://github.com/isbeorn/nina/commit/56a7424b148e03049e0e4b7a61039a96f8eaf475).

Cancellation is caught as a general failure and can produce repeated logging/work after disconnect.

**Port scope:** Port the dedicated cancellation exit. The earlier model-parameter removal is already present.

**Required validation:** Disconnect while waiting and during a request, genuine request errors and reconnect without a surviving old worker.

### R17. Prevent overlapping Connect All and Disconnect All commands

Source: [e4d1cbd9f](https://github.com/isbeorn/nina/commit/e4d1cbd9f815aa0f67ac145ef5a44fbe93b348a2).

The UI can launch both bulk operations at once and leave equipment state inconsistent.

**Port scope:** Port mutual command CanExecute/IsRunning guards. This guards these commands rather than providing a global device-operation lock.

**Required validation:** Both launch orders, mid-operation exceptions, individual-device failures and command re-enablement.

### R18. Respect the active sequence scope in Center After Drift

Source: [fe5ca093d](https://github.com/isbeorn/nina/commit/fe5ca093d91262e083429fbb83b971cdda7afc16).

A drift trigger can react to images from another container or a late solve result after its scope has ended.

**Port scope:** Adapt the scope and completion checks to 3.2 trigger code, retaining the existing public PlatesolvingImageFollower constructor. Do not take expression or custom-trigger infrastructure.

**Required validation:** Root/nested/sibling containers, inactive/disabled triggers, target changes and delayed plate-solve completion after leaving the scope.

### R19. Guard missing trigger runners and null validation issue lists

Source: [08540191e](https://github.com/isbeorn/nina/commit/08540191edd4d19c69231e29b2cd808b976c36cc), [f3650a722](https://github.com/isbeorn/nina/commit/f3650a722863a35466e4fddc8bd3abeaabebd9cc).

Reset and validation can throw on incomplete or plugin-provided entities instead of reporting their state.

**Port scope:** Port guards across items, conditions and triggers. Both defects have counterparts in 3.2.

**Required validation:** Null/non-null runners, null/empty/populated issue lists, false Validate results and plugin entities in a constructed sequence.

### R20. Preserve Annotation metadata when cloning

Source: [c2fa869e3](https://github.com/isbeorn/nina/commit/c2fa869e3078738fb889daf37fddd8368249f14e).

Cloned annotations omit common instruction metadata.

**Port scope:** Port CopyMetaData in the clone path.

**Required validation:** Clone name/category/description and other base metadata together with annotation text.

### R21. Make Sky Flat validation return failure when issues exist

Source: [89cbc60df](https://github.com/isbeorn/nina/commit/89cbc60df4c26f392c0209a42c9f2d6e603df46a).

Sky Flat can collect validation issues but still report successful validation.

**Port scope:** Extract only the SkyFlat validation result correction. The rest of this coverage commit includes expression-dependent clone fixes.

**Required validation:** Each missing equipment/precondition case, valid configuration and Flat Wizard execution through the real instruction.

### R22. Use the correct altitude start-time field in Sky Atlas

Source: [8c58bfc34](https://github.com/isbeorn/nina/commit/8c58bfc343ef16067891831439262b17142e67e4).

SelectedAltitudeTimeFrom compares against the through-time backing field, so some start-time edits are ignored.

**Port scope:** Extract the one-line setter correction independently of the transit filter feature.

**Required validation:** Change From to the current Through value, both edit orders and windows spanning midnight.

### R23. Refresh horizon and altitude bindings after profile/date/site changes

Source: [d7469606b](https://github.com/isbeorn/nina/commit/d7469606b1ede44b2bc4e6b48df2c98ee87138f2), [438240369](https://github.com/isbeorn/nina/commit/4382403692b968833c4538f96fed7f17c26003a2).

A changed profile horizon is not announced and SetDateAndPosition clears altitude data without notifying bindings.

**Port scope:** Port HorizonChanged after a profile switch plus only the SkyObjectBase Altitudes=null setter call from the large framing commit.

**Required validation:** Profile A/B/A, custom/default horizons and date/site changes in already constructed charts, including unchanged values.

### R24. Raise the mechanical rotator event for mechanical moves

Source: [f0fc8a38d](https://github.com/isbeorn/nina/commit/f0fc8a38db4249e82a7bdb37fca2edb8e4a0bc89).

Mechanical movement raises the logical Moved event instead of MovedMechanical.

**Port scope:** Port the event correction while retaining both event contracts.

**Required validation:** Logical and mechanical moves, event payloads and subscribers to each event.

### R25. Track filter-wheel changes made by another client

Source: [624e09b26](https://github.com/isbeorn/nina/commit/624e09b26c19cb2107baa4b7e0c24516d1045820).

A wheel moved outside NINA can leave the selected filter and subsequent metadata stale.

**Port scope:** Port the background position poll with its start/stop lifecycle and filter synchronization.

**Required validation:** External/internal moves, moving position -1, disconnect/reconnect, changed filter lists and polling errors. Confirm acceptable driver load.

### R26. Harden ToupTek-alike camera initialization and setting application

Source: [903643091](https://github.com/isbeorn/nina/commit/9036430918fc63b8df5213e761897867a9cc147d).

Failed opens can report success; initial temperature units, clamped fan writes and binning can be wrong.

**Port scope:** Port the camera-class fixes, readout-list initialization and failed-connect cleanup. The title mentions enum conversion, but the production diff here is the camera class; do not assume it updates all wrapper enum mappings.

**Required validation:** Null/throwing Open, partial initialization, temperature tenths, fan limits, binning below/at/above limits and all supported camera brands with representative hardware.

### R27. Read ZWO camera properties by camera ID

Source: [9ac95100b](https://github.com/isbeorn/nina/commit/9ac95100b8b5f77a2713fc0749d59acfac9d4ed5).

A persistent camera ID is passed to an SDK function that expects an enumeration index.

**Port scope:** Port the ID-based P/Invoke and call site. Verify that the shipped 3.2 native DLL exports ASIGetCameraPropertyByID. Replace System.Threading.Lock syntax with the existing .NET 8 lock pattern.

**Required validation:** Multiple cameras with ID different from index, disconnect/re-enumeration and the actual packaged DLL export.

### R28. Preserve FITS filenames containing brackets or parentheses

Source: [5cc5a7af9](https://github.com/isbeorn/nina/commit/5cc5a7af9e19a1792081b21f0ec04e4b2cfe48d1), [44db2a6c1](https://github.com/isbeorn/nina/commit/44db2a6c176565a656e358af3859d108bbac2cbf).

CFITSIO extended filename syntax can reinterpret ordinary user filenames; the writer currently rewrites these characters.

**Port scope:** Port create_diskfile and open_diskfile together, including temporary-file paths. Verify ffdkinit/ffdkopn exports in the 3.2 DLL.

**Required validation:** Create/read round trips with parentheses/brackets/spaces, compressed/uncompressed FITS, legacy/new writer paths and nested directories.

### R29. Handle XISF short reads explicitly

Source: [d2ff4d5eb](https://github.com/isbeorn/nina/commit/d2ff4d5ebe33a89778ce57863d458cfd9fbaf45b).

FileStream.Read can return fewer bytes than requested, leaving signature, header or image data incomplete.

**Port scope:** Extract only the four XISF ReadExactly changes. Keep existing public exception constructors and omit the unrelated PHD2 fixed-size read change.

**Required validation:** Valid files, truncated signature/header/payload and compressed/uncompressed data using the 3.2 reader.

### R30. Release image-analysis bitmaps and graphics resources

Source: [53fe7284f](https://github.com/isbeorn/nina/commit/53fe7284f9ed856ec0dabbe85d55389f64523661).

Repeated image analysis can retain disposable bitmap/Graphics resources, especially when processing fails.

**Port scope:** Port the ownership fixes in Bahtinov/contrast analysis, ImageUtility and StarAnnotator. Adapt only relevant paths to the old debayer implementation.

**Required validation:** Repeated ROI/no-ROI analysis, noise-reduction choices, color/mono paths and exceptions during conversion; monitor GDI/resource growth.

### R31. Fix Gaussian blur buffer selection and bitmap stride handling

Source: [9bfefbe97](https://github.com/isbeorn/nina/commit/9bfefbe97dfcba2fef721515641c0cf817d3b31f), [82b42b6cb](https://github.com/isbeorn/nina/commit/82b42b6cbdaf0c229c465ec751b15fc092cd690d).

The horizontal blur reads a destination buffer where it should read the source, while bitmap copying assumes a contiguous layout.

**Port scope:** Combine the one-line horizontal fix with the FastGaussianBlur row/stride import/export and finally-based unlock hunks from the coverage commit.

**Required validation:** Reference blur comparisons, small/odd dimensions, row padding, positive/negative stride where supported and failure cleanup.

### R32. Write SBIG electrons-per-ADU metadata

Source: [e12d71811](https://github.com/isbeorn/nina/commit/e12d71811118927e1690979d74caca62474865c7).

EGAIN lacks a valid conversion gain even when the camera reports one.

**Port scope:** Extract the unbinned readout-mode gain lookup and NaN fallback. Omit unrelated stylistic rewrites.

**Required validation:** Valid/unavailable gain, binned/unbinned frames and resulting FITS/XISF metadata.

### R33. Correct FITS site metadata guards

Source: [82b42b6cb](https://github.com/isbeorn/nina/commit/82b42b6cbdaf0c229c465ec751b15fc092cd690d).

Latitude/longitude checks accidentally depend on elevation and site-name output depends on the wrong name field.

**Port scope:** Extract FITSHeader guard corrections.

**Required validation:** Independently missing/present elevation, latitude, longitude, site and observer names; inspect written headers.

### R34. Correct XISF metadata values and Bayer offset keyword reading

Source: [82b42b6cb](https://github.com/isbeorn/nina/commit/82b42b6cbdaf0c229c465ec751b15fc092cd690d).

Focal ratio, target coordinates and wind-speed units can be wrong; correctly spelled Bayer offset keywords were not handled.

**Port scope:** Extract XISFHeader fixes, retaining support for the former misspelled keywords as well as XBAYROFF/YBAYROFF.

**Required validation:** Known focal length/aperture, differing target/telescope coordinates, wind km/h-to-m/s conversion and old/new Bayer-key round trips.

### R35. Correct guider RMS removal calculations

Source: [82b42b6cb](https://github.com/isbeorn/nina/commit/82b42b6cbdaf0c229c465ec751b15fc092cd690d).

Removing samples uses the wrong remaining-count statistics and can leave bad RMS values.

**Port scope:** Extract RMS.RemoveDataPoint fixes including empty/single-sample and tiny-negative-roundoff handling.

**Required validation:** Add/remove symmetry against a batch reference, empty/one/many points, repeated values and sliding windows.

### R36. Fix PHD2 guide-distance display and lock-position hashing

Source: [82b42b6cb](https://github.com/isbeorn/nina/commit/82b42b6cbdaf0c229c465ec751b15fc092cd690d).

DEC guide distance displays the raw distance and LockPosition hashing disagrees with equality.

**Port scope:** Extract PhdEventGuideStep display assignment and LockPosition.GetHashCode. No socket refactor required.

**Required validation:** Raw and guide values deliberately different, RA/DEC comparison and equal positions with different event timestamps in hash sets.

### R37. Stop successful Retry actions being retried and fix async overload forwarding

Source: [82b42b6cb](https://github.com/isbeorn/nina/commit/82b42b6cbdaf0c229c465ec751b15fc092cd690d).

The Action wrapper returns null on success and the Func<Task> wrapper can resolve to the wrong overload.

**Port scope:** Extract both non-result Retry overload corrections together.

**Required validation:** Exactly one call on synchronous/asynchronous success, specified retry counts on failures and propagated final exceptions.

### R38. Return correct image-pattern insertion results

Source: [82b42b6cb](https://github.com/isbeorn/nina/commit/82b42b6cbdaf0c229c465ec751b15fc092cd690d).

AddImagePattern reports failure even when it inserts a pattern.

**Port scope:** Extract the successful return-value correction.

**Required validation:** New and duplicate patterns, stored value and caller-visible return value.

### R39. Reject malformed binning strings

Source: [82b42b6cb](https://github.com/isbeorn/nina/commit/82b42b6cbdaf0c229c465ec751b15fc092cd690d).

Binning parsing accepts extra separators instead of requiring exactly two dimensions.

**Port scope:** Extract the TryParse part-count guard.

**Required validation:** Valid XxY, missing/extra parts, invalid numbers and unchanged valid serialization.

### R40. Fix byte-size formatting

Source: [82b42b6cb](https://github.com/isbeorn/nina/commit/82b42b6cbdaf0c229c465ec751b15fc092cd690d).

The decimal format string is invalid for the value being formatted.

**Port scope:** Extract CoreUtil.FormatBytes invariant numeric formatting.

**Required validation:** Zero, each unit transition, large values and cultures with comma decimal separators.

### R41. Make manual rotator completion and cancellation consistent

Source: [82b42b6cb](https://github.com/isbeorn/nina/commit/82b42b6cbdaf0c229c465ec751b15fc092cd690d).

Canceled dialogs can leave IsMoving set or leave completion waiting; final angles need normalization.

**Port scope:** Extract ManualRotator finally cleanup, canceled completion and target normalization.

**Required validation:** Confirm/cancel/close, repeated moves, angle wrap in both directions and IsMoving reset on failure.

### R42. Use invariant scale values and correct mirroring metadata with TheSkyX

Source: [82b42b6cb](https://github.com/isbeorn/nina/commit/82b42b6cbdaf0c229c465ec751b15fc092cd690d).

Locale-dependent scale strings can break scripts and the mirrored-image response maps to the wrong JSON name.

**Port scope:** Extract both scale read/write paths and the imageIsMirrored mapping.

**Required validation:** Comma/dot cultures, mirrored/non-mirrored results and real script request/response parsing.

### R43. Keep filter-wheel settings changes observable after reset

Source: [82b42b6cb](https://github.com/isbeorn/nina/commit/82b42b6cbdaf0c229c465ec751b15fc092cd690d).

SetDefault bypasses the collection setter that installs change observation.

**Port scope:** Extract use of FilterWheelFilters setter when resetting defaults.

**Required validation:** Reset then add/remove/edit filters, profile dirty state and persistence; old collection must stop affecting the profile.

### R44. Preserve distinct trained flat-calibration records

Source: [82b42b6cb](https://github.com/isbeorn/nina/commit/82b42b6cbdaf0c229c465ec751b15fc092cd690d).

Adding training data can use nearest-match lookup and overwrite a different calibration entry.

**Port scope:** Extract exact-match lookup for updates while keeping nearest-match behavior for reads.

**Required validation:** Exact match, nearby but distinct gain/bin/filter/brightness combinations, add/update and retrieval fallback.

### R45. Save new profiles without replacing nonexistent files or good backups

Source: [82b42b6cb](https://github.com/isbeorn/nina/commit/82b42b6cbdaf0c229c465ec751b15fc092cd690d).

Initial or empty profile files need different handling from replacing an existing populated profile.

**Port scope:** Extract initial-file Move versus existing-file Replace handling and backup protection.

**Required validation:** Missing/empty/populated destination, existing good backup, normal second save and failed writes.

### R46. Publish equipment state before connection events

Source: [82b42b6cb](https://github.com/isbeorn/nina/commit/82b42b6cbdaf0c229c465ec751b15fc092cd690d).

Connected-event subscribers can receive an event while mediator state still says disconnected.

**Port scope:** Extract ordering fixes consistently for dome, flat device, focuser, guider, rotator, safety monitor, switch, telescope and weather.

**Required validation:** Read mediator state from event handlers for every affected device, failed connection and disconnect/reconnect.

### R47. Guard disconnected camera binning and guider operations

Source: [82b42b6cb](https://github.com/isbeorn/nina/commit/82b42b6cbdaf0c229c465ec751b15fc092cd690d).

Commands forwarded without an active device can throw null-reference exceptions.

**Port scope:** Extract CameraVM.SetBinning plus GuiderVM shift-rate/stop-shift/get-lock-position guards.

**Required validation:** Connected and disconnected calls, null binning and lock-position return behavior.

### R48. Handle invalid plugin behavior/provider types without secondary null failures

Source: [82b42b6cb](https://github.com/isbeorn/nina/commit/82b42b6cbdaf0c229c465ec751b15fc092cd690d).

Incorrect plugin types can cause the error-reporting path itself to throw or accept a null cast.

**Port scope:** Extract PluggableBehaviorSelector cast checking and PluginEquipmentProvider logging. Preserve interfaces.

**Required validation:** Valid/wrong generic implementations, accurate diagnostics and unrelated valid plugins still loading.

### R49. Rebind Flat Wizard filter settings when the settings object changes

Source: [82b42b6cb](https://github.com/isbeorn/nina/commit/82b42b6cbdaf0c229c465ec751b15fc092cd690d).

The wrapper can keep listening to the old settings object and display stale filter/histogram values.

**Port scope:** Extract old-handler removal, new-handler registration and dependent-property notifications.

**Required validation:** Replace A with B and back, edits to old/new objects, filter identity and histogram mean/tolerance updates.

### R50. Allow null plate-solving progress reporters

Source: [82b42b6cb](https://github.com/isbeorn/nina/commit/82b42b6cbdaf0c229c465ec751b15fc092cd690d).

CenteringSolver dereferences an optional progress reporter.

**Port scope:** Extract null-conditional progress reporting without the rest of the test-coverage changes.

**Required validation:** Centering with/without progress and both success/failure paths.

### R51. Accept comma-decimal sexagesimal coordinates

Source: [950d1b67c](https://github.com/isbeorn/nina/commit/950d1b67c42b0a477ceb696f25434d9efc7d268c).

Coordinate text with comma decimal fractions fails invariant parsing.

**Port scope:** Extract ParseSexagesimal normalization and relevant tests independently of the astronomy overhaul.

**Required validation:** Comma/dot fractional seconds, signs, RA/Dec bounds and malformed inputs.

### R52. Make command-line plate-solver timeout and cancellation effective

Source: [5cbaa72e5](https://github.com/isbeorn/nina/commit/5cbaa72e50e1e12fc0f8de389ef7910b2b63a819).

A timeout token is created but StartCLI receives the original token, so the timeout is ineffective.

**Port scope:** Extract linked-token forwarding, process disposal and cancellation cleanup. Keep the existing timeout constant if a new protected SolverTimeout extension point is unnecessary.

**Required validation:** Successful subprocess, hang, user cancellation and timeout; verify child-process cleanup and no false success.

### R53. Restore the original filter after failed or canceled capture solves

Source: [5cbaa72e5](https://github.com/isbeorn/nina/commit/5cbaa72e50e1e12fc0f8de389ef7910b2b63a819).

Filter restoration does not run on all capture/solve failure paths.

**Port scope:** Extract CaptureSolver finally restoration and bounded cleanup token. Ensure a restoration failure is reported without losing the original error context.

**Required validation:** Success, null image, capture/solve/thumbnail exception, cancellation, retries and restoration failure.

### R54. Propagate failed centering slews and honor plate-solve gain

Source: [5cbaa72e5](https://github.com/isbeorn/nina/commit/5cbaa72e50e1e12fc0f8de389ef7910b2b63a819).

Center/CenterAndRotate can continue after a failed slew and several rotate/sync solve captures omit the configured gain.

**Port scope:** Extract failed-slew checks in Center, CenterAndRotate and CenteringSolver plus gain assignment in CenterAndRotate, SolveAndRotate and SolveAndSync. Keep existing coordinate math.

**Required validation:** False/throwing/successful slews, no later capture after failure and requested gain on all affected solve instructions.

### R55. Honor the requested coordinate projection type

Source: [5cbaa72e5](https://github.com/isbeorn/nina/commit/5cbaa72e50e1e12fc0f8de389ef7910b2b63a819).

The ViewPort projection overload does not forward its type argument.

**Port scope:** Extract the Coordinates projection overload forwarding fix only.

**Required validation:** Compare both overloads for every supported projection, nonzero rotation and edge coordinates.

### R56. Log malformed plugin manifests and assembly metadata clearly

Source: [c681a498a](https://github.com/isbeorn/nina/commit/c681a498a7915057b6fa4753ff9fffcc2f63032b).

Metadata-reading failures can hide the reason a plugin was not loaded or generate misleading diagnostics for helper DLLs.

**Port scope:** Port guarded metadata reading and manifest-aware diagnostics into the old loader. Preserve the 3.2 loader constructor and composition contracts.

**Required validation:** Valid plugin, malformed assembly metadata, invalid manifest, helper DLL without manifest and a mixed plugin directory.

### R57. Report invalid profile IDs during startup

Source: [4025777ef](https://github.com/isbeorn/nina/commit/4025777ef6c4d3e92ba7208ec430cff2f6a5d533).

An invalid requested profile can end startup with little useful information.

**Port scope:** Port logging, clear failure exit handling and avoiding JumpList work on the failed-start path.

**Required validation:** Valid/missing/malformed profile IDs and startup exit/log behavior, including normal profile selection.

### R58. Hide driver controls for unsupported focuser/rotator capabilities

Source: [011f8f28d](https://github.com/isbeorn/nina/commit/011f8f28d4d4a8e8c01316e7abd52b717a2450d4).

The UI offers controls which the connected driver cannot perform.

**Port scope:** Port the capability-based visibility bindings in the existing views.

**Required validation:** Instantiate views for capability true/false, disconnected state and reconnect with a different driver.

### R59. Show installed-plugin indicators reliably

Source: [39a9ac5e0](https://github.com/isbeorn/nina/commit/39a9ac5e03a87bd8d4947765dcc6b4e1621de35c).

Automatic column sizing can hide installed-state indicators in Available Plugins.

**Port scope:** Port the targeted column width change.

**Required validation:** Installed/uninstalled rows, sorting/filtering and narrow/high-DPI plugin views.

### R60. Avoid parsing an empty PopupButton focus path

Source: [68940aa0d](https://github.com/isbeorn/nina/commit/68940aa0d0185bd8d4cefea5947e86f62b73410b).

Opening a popup without a usable FocusIndex can throw during string splitting/parsing.

**Port scope:** Extract only PopupButton blank/parse guards from the expression-system commit. Validate visual-child bounds during the port rather than importing the expression UI.

**Required validation:** Null/empty/valid/malformed paths through a constructed popup and its actual button interaction.

### R61. Fix initial popup rendering without prematurely closing modal dialogs

Source: [10398d812](https://github.com/isbeorn/nina/commit/10398d8122899d7e1e45e8253f6bb3fc06e595fe), [100cfcb1b](https://github.com/isbeorn/nina/commit/100cfcb1bd5dee714e1714d8c78c663f74d7b72b).

New windows/message boxes can briefly render incorrectly. The initial attempted fix then caused modal dialogs to close immediately.

**Port scope:** Port the final combined window behavior using temporary opacity rather than Visibility.Hidden, including restoration of the original opacity. Never take 10398d812 alone.

**Required validation:** Modal/modeless/custom/plugin windows, input/message boxes, owner/centering, original opacity, repeated opening and high DPI. Compile and instantiate affected WPF views.

## Approved larger feature

The user explicitly requested this addition despite its greater implementation scope. It is included in the accepted backport list.

### A01. Direct-IP Alpaca connections

Source: [adbcf77e5](https://github.com/isbeorn/nina/commit/adbcf77e554271b045697b96a460d6a890514f44), [cfa3c7897](https://github.com/isbeorn/nina/commit/cfa3c789727504512b5ee8b1aeb55e4ffeb4b936), [8783c7b37](https://github.com/isbeorn/nina/commit/8783c7b374b2b15b5d76bc89e74e699bb7317b68).

User-approved addition to the accepted backport list. Allows equipment to connect to a configured Alpaca IP address and port without relying on automatic discovery. This larger integration is explicitly in scope for 3.2.

**Port scope:** Port the direct connection settings, chooser registration, setup template and adapters for camera, telescope, dome, filter wheel, cover calibrator, focuser, rotator, safety monitor, switch and observing conditions. Include the safety-monitor ID correction from cfa3c7897 only: its original ID collided with the rotator ID and their settings share the ID-based options store. Include the disconnected-device metadata/null guards from 8783c7b37 across all ten adapters. Preserve existing discovered-device behavior, .NET 8 and plugin interfaces; the original feature does not require the expression system, new RAW stack or new native drivers. Review endpoint validation and setup/disconnect/reconnect event lifecycles as part of the port.

**Required validation:** Direct entries remain usable with discovery unavailable or returning no devices; valid/invalid IP, port boundaries, device number and HTTP/HTTPS selection; profile save/reload and switching with rotator/safety-monitor settings isolated; first use before connection, connection failure, cancellation, disconnect/reconnect and handler cleanup for all ten adapters. Exercise true/false safety reports and failures through the existing monitor path. Compile and instantiate the setup UI, test an Alpaca simulator and verify representative real endpoints.

## Conditional ports

These behaviors are worth considering, but the upstream patch must not be taken unchanged. Each item explains the compatibility, dependency or behavior decision.

### C01. Correct ASCOM Bayer offsets during automatic debayering

Source: [cdb0dc4db](https://github.com/isbeorn/nina/commit/cdb0dc4dbbe6ae964ba60636ead542c069155f65).

Automatic debayering can select the wrong pattern for images with nonzero Bayer offsets.

**Port scope:** Adapt the pattern-offset utility and old ImageControlVM path without libraw, new image interfaces or the new debayer algorithm. Preserve manual override semantics.

**Required validation:** Every supported Bayer pattern, X/Y parity combinations including negative/even offsets, automatic/manual selection and FITS/XISF round trips.

### C02. Use actual ASCOM exposure start and duration safely

Source: [2f971307d](https://github.com/isbeorn/nina/commit/2f971307dc73973102ecc759b36a5bb0186163bf).

Driver-provided timing can be more accurate than requested duration and application timestamps.

**Port scope:** Do not cherry-pick: it changes public LastExposureDuration/LastExposureStartTime types and can cast a null date or construct a TimeSpan from NaN. Implement private parsing while preserving the old public properties, explicit UTC and requested-value fallback.

**Required validation:** Unsupported/throwing/empty/malformed dates, invalid duration, drivers gaining values only after capture, correct UTC and no downstream overwrite in CameraVM/ImagingVM.

### C03. Serialize native camera SDK calls and retain native callbacks

Source: [dd95ed6fb](https://github.com/isbeorn/nina/commit/dd95ed6fb0fdd0baec9c43ba5e0e4567d39dc12b), [d1374aa90](https://github.com/isbeorn/nina/commit/d1374aa90b07816dab25bde23eeba8fbc5f6d505), [061529e59](https://github.com/isbeorn/nina/commit/061529e5929c98f5ebf66f2003489272cead6dac).

Concurrent native access and callback lifetime/teardown are plausible sources of intermittent camera failures.

**Port scope:** Port separately per vendor. Preserve QhySdk public handle and IQhySdk signatures; replace .NET 9+ System.Threading.Lock with .NET 8-compatible locking. Include ToupTek callback retention and disconnect-event handling, auditing all seven wrappers.

**Required validation:** Concurrent capture/poll/control/close, callback reentrancy, reconnect and no lock held while waiting for a callback that needs the same lock. Vendor hardware soak tests required.

### C04. Wait for a telescope sync position update

Source: [cc4ce8c96](https://github.com/isbeorn/nina/commit/cc4ce8c969199a8037443670e777e2a543153567), [c054f438b](https://github.com/isbeorn/nina/commit/c054f438bfa67b45211c8e27c37e83c6c4bb3e43), [f422c9e67](https://github.com/isbeorn/nina/commit/f422c9e67d99cb382aa20a840bd55edf392f575e), [422364674](https://github.com/isbeorn/nina/commit/422364674ff3570873a6ad3dc9fec8e7881a7c3a), [954d54eb8](https://github.com/isbeorn/nina/commit/954d54eb8b00a2cd482bfec639b0f2d8dc10404a), [959e73a2a](https://github.com/isbeorn/nina/commit/959e73a2a71ae073ac5b316fc58aea3c754c4e23), [d6a3497a1](https://github.com/isbeorn/nina/commit/d6a3497a19ef17e7319689eb0bcf529fadda417f), [a88a6d632](https://github.com/isbeorn/nina/commit/a88a6d632c80804d8948e80f58e3d503e6329242), [11c5898c3](https://github.com/isbeorn/nina/commit/11c5898c391913016ae1fda327370906d083ccdc).

A fixed delay can be unnecessary or insufficient when a driver updates coordinates asynchronously.

**Port scope:** Port the final cumulative behavior, not intermediate string/plate-threshold/delta comparisons. Review removal of the minimum settle delay, the one-arcsecond threshold and uncanceled WaitForNextUpdate calls; the loop timeout alone cannot bound a stuck update wait.

**Required validation:** Immediate/delayed/failed/no-op sync, paused update timer, zero/positive settle times, both epochs and centering with real mount drivers.

### C05. Improve no-sync centering correction geometry

Source: [5cbaa72e5](https://github.com/isbeorn/nina/commit/5cbaa72e50e1e12fc0f8de389ef7910b2b63a819).

Scalar coordinate offsets are fragile near poles and RA wrap; the change uses a measured spherical rotation.

**Port scope:** Keep this separate from R52-R55. It is a bounded algorithmic bug fix but higher risk for a patch release. Port only compatible vector helpers and centering math without the general astronomy rewrite.

**Required validation:** RA 0/24 wrap both directions, both poles, no-sync/failed/silent/successful sync, nearly identical/opposite coordinates and actual convergence.

### C06. Add padding to pier-side projection near the meridian

Source: [8e53588ec](https://github.com/isbeorn/nina/commit/8e53588ec0d0996e6e41de20d2a116a7036d7ec7).

Very tight meridian-flip timing can make pier-side projection unstable.

**Port scope:** Review the deliberate 0.2 sidereal-hour projection offset against the 3.2 flip implementation. Keep this as a separately selectable mount-behavior change.

**Required validation:** East/west pier sides, before/after meridian, both hemispheres, near-zero and nonzero flip delays and drivers with unusual DestinationSideOfPier behavior.

### C07. Keep tracking disabled after unpark

Source: [c85bda65a](https://github.com/isbeorn/nina/commit/c85bda65adc3539b0e77ba5d5791dc1d068fb0d6).

Some drivers begin sidereal tracking automatically when unparked, which may be unwanted for the next instruction.

**Port scope:** This is a user-visible behavior change, not a universally correct fix. Backport only with an explicit 3.2 policy for unpark/tracking and clear release notes.

**Required validation:** Direct and sequenced unpark, already unparked, unsupported tracking control, following slew/start-tracking instructions and current user workflows.

### C08. Use native Alt/Az slews and expose tracking choice in the sequence instruction

Source: [971d504d0](https://github.com/isbeorn/nina/commit/971d504d03d27fa157eae1919317d6b92e761a40).

SlewScopeToAltAz currently routes through equatorial movement even when native Alt/Az slewing is available.

**Port scope:** Adapt dispatch and the small Tracking option to the old instruction, retaining its public constructor and coordinate types. Review default true and restarting guiding when tracking is false.

**Required validation:** Native/fallback routes, tracking true/false, cloning and old JSON default, guiding restart and failed slews.

### C09. Refresh built-in color schemes without losing custom colors

Source: [f95bc053e](https://github.com/isbeorn/nina/commit/f95bc053ef103d73c55c6cb2ecee29e12106e15b).

Application upgrades do not reliably refresh built-in color definitions while preserving custom schemes.

**Port scope:** Correct the proposed port: the alternate-schema branch checks ColorSchema.Name where it needs AltColorSchema.Name. Do not copy this asymmetry.

**Required validation:** All primary/alternate built-in/custom combinations, scheme toggling, upgrade and persistence of edited colors.

### C10. Make PlayerOne filter-wheel connection wait for homing

Source: [c3f66da21](https://github.com/isbeorn/nina/commit/c3f66da213b7410a00ea67f03a68c76eb83bab72), [9cf804c5d](https://github.com/isbeorn/nina/commit/9cf804c5da761c3b3aa6d713279fe519d8fc9cff).

The wheel may still be homing when connection is reported ready.

**Port scope:** Port the homing wait with the later two-minute timeout. Unidirectional control is optional; omit the global default change from false to true for every wheel. If updating PlayerOnePW, select its exact SDK change rather than the whole External tree.

**Required validation:** Slow/fast homing, timeout/cancel/disconnect, movement after connect and existing unidirectional preferences. Hardware required.

### C11. Fill missing capture filter metadata from the active wheel

Source: [0c1033a47](https://github.com/isbeorn/nina/commit/0c1033a475584cc845e9849cabe93f6706817032).

A null sequence filter can produce missing/wrong filter metadata despite an active wheel filter.

**Port scope:** The upstream CameraVM constructor changes. Preserve its 3.2 public construction contract and adapt only the fallback behavior, using existing dependencies or a compatible overload if needed.

**Required validation:** Explicit filter takes precedence, null filter with/without wheel, moving wheel and manual/sequence captures.

### C12. Improve Sky Atlas search responsiveness

Source: [a95941407](https://github.com/isbeorn/nina/commit/a95941407b9ab16a169dfd5a110946777a871779).

Search performs repeated database work, per-object renderer allocation and forced GC, plus unnecessary altitude-chart materialization.

**Port scope:** Port in small units: read-only/no-tracking queries, one renderer per search and grid virtualization first. Adapt joins and horizon sampling to the 3.2 EF/SQLite stack, excluding HiPS/transit-only code. Verify behavior equivalence before including the full query rewrite.

**Required validation:** Same result set, all sort fields/directions, aliases/missing designations, custom horizon and duration windows, cancellation and repeated searches with a representative database.

### C13. Correct cached sky-image orientation across zoom levels

Source: [105c71ee3](https://github.com/isbeorn/nina/commit/105c71ee3fcbaa14338493e684d86b4c0aa175d5).

Cached images can acquire inconsistent orientation when transformed between fields of view.

**Port scope:** The underlying old cache renderer exists in 3.2, but this patch uses SkyMapViewportProjection from the excluded new renderer. Consider a math-only adaptation to the existing renderer; do not import the new framing architecture.

**Required validation:** Cached versus source image orientation at zoom in/out, arbitrary rotation, RA wrap, high declination and mosaic alignment.

### C14. Surface asynchronous image-save timeouts clearly

Source: [e4a31b366](https://github.com/isbeorn/nina/commit/e4a31b366d0afd87975910b2bfbd078217a53eaa).

Timeout cancellation in the save worker can be silent, even though ordinary save exceptions already show an error.

**Port scope:** Consider only a local error/timeout notification and logging using the 3.2 notification service. Exclude new IImageSaveMediator/IImageSaveController events, sequence failure-stream integration and the new toast system.

**Required validation:** Write timeout, disk full, preparation failure, user shutdown and notification throttling. Verify queue continuation and existing success events.

### C15. Match plugin compatibility entries by identifier

Source: [68940aa0d](https://github.com/isbeorn/nina/commit/68940aa0d0185bd8d4cefea5947e86f62b73410b).

The 3.2 map is keyed by display name while lookups use plugin.Identifier, so known minimum-version rules are missed.

**Port scope:** Extract key corrections only and audit each rule for 3.2. Do not import the new Sequencer Powerups minimum or 3.3 plugin policy. Enabling previously ineffective rules may newly block installed plugins.

**Required validation:** Below/at/above each intended minimum, identifier casing, renamed plugins, offline loading and unchanged supported 3.2 plugin set.

## Optional small improvements

These fit the intended direction more closely than the excluded major features, but have lower priority than correctness and reliability fixes.

### O01. Offer per-filter or shared HFR autofocus trends

Source: [e4227e317](https://github.com/isbeorn/nina/commit/e4227e3175c7e3dd5eae7371f85090e07386851b), [8a437357d](https://github.com/isbeorn/nina/commit/8a437357d9b6324bf3c27ac3295567c975110cfa).

A small opt-in control for whether HFR trend history is separated by filter.

**Port scope:** Port the option and its UI separator without unsafe-monitor constructor changes or expressions. Preserve default true. Add the missing TrendPerFilter clone copy when adapting it. The HFR percentage formula fix is already in 3.2.

**Required validation:** On/off, filter switches, cloning, old/new JSON and trigger threshold behavior.

### O02. Add Zstandard compression for XISF

Source: [75c05a0da](https://github.com/isbeorn/nina/commit/75c05a0da368b8da20c35569924abf62b99dcb0e).

A contained file-format option with no replacement of the imaging architecture.

**Port scope:** Port the appended compression option, ZstdSharp.Port dependency and installer payload only. Retain existing enum values and the .NET 8 package set.

**Required validation:** Read/write with and without byte shuffle, all supported pixel types, old codecs, malformed data and interoperability with an independent XISF reader.

### O03. Add more bright stars for manual focusing

Source: [0aaccdf2c](https://github.com/isbeorn/nina/commit/0aaccdf2c615e39a25c62a900ea2f9319a96eb0e), [d744e8848](https://github.com/isbeorn/nina/commit/d744e8848498820aa8b8bc843304e55aa4b77aa8).

The migration extends the existing focus-star catalogue.

**Port scope:** Include migration 15.sql in output and installer. Review duplicate names/coordinates rather than claiming every INSERT is a distinct new star. Do not take migration 16 for the new HiPS feature.

**Required validation:** Upgrade a copy of a 3.2 database, fresh database, rerun/idempotence and star selection/coordinates.

### O04. Filter and sort Sky Atlas by transit time

Source: [8c58bfc34](https://github.com/isbeorn/nina/commit/8c58bfc343ef16067891831439262b17142e67e4).

Extends an existing search workflow without a new subsystem.

**Port scope:** Optional full transit-filter delta after extracting R22. Check sidereal/UTC/local-time handling and preserve existing sort enum values.

**Required validation:** Both sort directions, midnight/rollover windows, time zones, circumpolar objects and integration with altitude filters.

### O05. Show meridian crossing time on altitude charts

Source: [3156bcee2](https://github.com/isbeorn/nina/commit/3156bcee2fb7f32ea6830c23ff19c2edb64abd85).

Adds useful timing information to an existing chart annotation.

**Port scope:** Port the converters/resources and chart layout; omit unrelated package version changes.

**Required validation:** Construct the affected charts, local time/date rollover, varying altitude and narrow/high-DPI layouts.

### O06. Show the selected readout mode in simple-sequence details

Source: [5d30f50ab](https://github.com/isbeorn/nina/commit/5d30f50abfdbd04ff06beab9d589c91067fab0d7).

Makes capture settings easier to review without changing sequence execution.

**Port scope:** Port the converter, binding and localized label.

**Required validation:** Known/invalid/unavailable mode, disconnected camera and old simple sequences in the actual view.

### O07. Expose Reset All for Legacy Sequencer target sets

Source: [8d0ee6a4b](https://github.com/isbeorn/nina/commit/8d0ee6a4b4a3c35b855eb1636ed88f3be493fd29).

Adds a button for the existing root ResetProgressCommand.

**Port scope:** Port the XAML and label, retaining the disabled-while-running condition.

**Required validation:** Actual command binding, all target counts plus startup/end progress, disabled during run and target data preserved.

### O08. Hide empty thumbnail filter labels and freeze thumbnail drawing objects

Source: [75f19b8ff](https://github.com/isbeorn/nina/commit/75f19b8ffa2edc899cd937acaaacd0dbbeb0c2b2), [ad9ab876d](https://github.com/isbeorn/nina/commit/ad9ab876debab53949ef14a70c650615ccdeecd2).

Removes empty overlay text and improves WPF drawing-object handling.

**Port scope:** Port the functional hunks. Skip the preceding CodeMaid-only commit.

**Required validation:** Empty/populated filter, loading/flagging thumbnails and UI/background thread access.

### O09. Show distinct DEBUG and TRACE logging indicators

Source: [349b72c15](https://github.com/isbeorn/nina/commit/349b72c15a3fa2af9b87ca1f61417581d7d4dbed), [56ec4781e](https://github.com/isbeorn/nina/commit/56ec4781e9f0c5fa955e833c7e0ba66fa3b09337).

Makes diagnostic logging visible to the user.

**Port scope:** Take both commits as one result: the first alone shows overlapping labels.

**Required validation:** DEBUG/TRACE/normal levels, profile switches and exactly one visible annotation.

### O10. Reduce loading-spinner rendering overhead

Source: [992352675](https://github.com/isbeorn/nina/commit/992352675a06e74fffcf0c95a1c20f7a6626fa23).

Shares frozen geometry/brush resources and improves rendering of the existing spinner.

**Port scope:** Port without changing existing dependency properties.

**Required validation:** Construct multiple spinners, custom styling, animation start/stop and repeated view creation.

### O11. Improve plate-solving panel layout

Source: [eecf12ec6](https://github.com/isbeorn/nina/commit/eecf12ec6c7fc7b07d4f1785a10e4f791f96f158), [7071f30eb](https://github.com/isbeorn/nina/commit/7071f30eb563ae311e5895521090952ffce12b68).

Combines related error values and improves sizing/readability.

**Port scope:** Treat the cleanup and follow-up sizing fix together, limiting changes to the plate-solving views.

**Required validation:** Docked/popout views, long/localized values, narrow widths and high DPI with runtime binding checks.

### O12. Improve error messages and small display details

Source: [1975cdf64](https://github.com/isbeorn/nina/commit/1975cdf64b9eb931e10413a0f96918d9fb4f739e), [ba77ad8e0](https://github.com/isbeorn/nina/commit/ba77ad8e03e97bd5ad65d1371b64568aa9bc58b6), [6825e2ea9](https://github.com/isbeorn/nina/commit/6825e2ea91932f5fa004670a2864a117b3b871c7), [a73db3151](https://github.com/isbeorn/nina/commit/a73db315197b489717ace6fd36857b7f3c585fc4).

Removes a disused unresolved switch label, displays useful sky-brightness precision, clarifies absolute mechanical rotation and improves Canon Invalid Mode guidance.

**Port scope:** These are independently selectable UI/text changes. Bring only relevant resource keys and ensure useful connection-error reporting remains.

**Required validation:** Affected views and locale fallback, Canon error path and absolute-rotation wording.

### O13. Improve existing diagnostic messages

Source: [452df1ee1](https://github.com/isbeorn/nina/commit/452df1ee134949a95af8205318b36ad0bc3b0450), [df97ce3a1](https://github.com/isbeorn/nina/commit/df97ce3a16bd4f59468518abafa9c782079150d2), [d718b00e1](https://github.com/isbeorn/nina/commit/d718b00e1f8eb93529496da4779010e2c8383e6c), [f2f373b79](https://github.com/isbeorn/nina/commit/f2f373b79b349035273a539ea5a61127dbafdfad), [82b42b6cb](https://github.com/isbeorn/nina/commit/82b42b6cbdaf0c229c465ec751b15fc092cd690d).

Records plugin load times, disabled location sync, rotator reverse changes, QHY firmware versions and invariant GPS coordinates.

**Port scope:** Select only diagnostics. d718b00e1 also removes public RotatorSettings.Reverse: leave that removal out. From 82b42b6cb take only Gpsd formatting here.

**Required validation:** Expected log levels, QHY major-version boundary 9 and nibble mask, invariant culture and no removal of serialized settings.

### O14. Include the source filename in sequence deserialization diagnostics

Source: [11b23460a](https://github.com/isbeorn/nina/commit/11b23460a43ab69def6bcc518246dcd56e94335e).

Makes it easier to find the broken sequence/template file.

**Port scope:** Adapt filename context to the existing converter and retain the old Deserialize overload. Keep useful exception detail instead of importing broad logging cleanup.

**Required validation:** Main sequence/template/plugin deserialization failures and filenames with special characters.

### O15. Improve hyperlink handling and offer Copy URL

Source: [b1888de15](https://github.com/isbeorn/nina/commit/b1888de1592f427adbe23c86d5f20623ddfc54ff).

A small user-facing convenience implemented across many existing views.

**Port scope:** Port the behavior and migrate existing links as a separately tested UI change. Preserve external navigation behavior and supported URLs.

**Required validation:** Mouse/keyboard activation, context menu, clipboard, relative/invalid links and representative plugin/about/options views.

### O16. Use equatorial fallback for the equipment-panel Alt/Az slew

Source: [75c85ee70](https://github.com/isbeorn/nina/commit/75c85ee70b9f6250322a485bbff3e6c8c88a30e7).

Makes an existing control useful for mounts without native Alt/Az slewing.

**Port scope:** Port the capability-based fallback using 3.2 coordinate transforms.

**Required validation:** Native/fallback paths, current site/time, both hemispheres and slew cancellation.

### O17. Refresh translations for retained 3.2 functionality

Source: all Localization rows in the commit ledger.

Translation commits contain improvements to existing controls as well as strings for excluded 3.3 features.

**Port scope:** Merge by retained resource key into the 3.2 catalogues, adding keys only for selected backports. ILoc/Loc differ only in copyright headers; a translation refresh does not require changing their contracts.

**Required validation:** Placeholder preservation, XML validity, missing-key fallback and representative localized views. Every translation commit is identified in the ledger.

### O18. Make drag/drop layout lookup tolerate missing application layout

Source: [89cbc60df](https://github.com/isbeorn/nina/commit/89cbc60df4c26f392c0209a42c9f2d6e603df46a).

DragOverBehavior assumes a layout parent is always available during construction and interaction.

**Port scope:** Extract null/error guards that apply to existing 3.2 views. Avoid a new public constructor solely for testing and omit expression clone fixes.

**Required validation:** Construct/attach/detach with and without the layout parent and real sequence drag/drop behavior.

### O19. Cache valid zero Earth-rotation corrections

Source: [7db787b5b](https://github.com/isbeorn/nina/commit/7db787b5bda8e567019e002238f043c01083e768).

DeltaUT uses zero as a cache-miss marker even though zero can be a valid correction, leading to repeated lookup work.

**Port scope:** Extract only the nullable today/yesterday/tomorrow cache sentinels and dictionary cache-hit handling. Keep the existing astronomical calculation, public methods and native libraries.

**Required validation:** Zero/nonzero values, today/yesterday/tomorrow/other dates, repeated queries and cache rollover to a new UTC date.

## Changes to keep in 3.3

The ledger records all exclusions individually. The main excluded families are:

| Family | Main source commits | Reason |
| --- | --- | --- |
| Expressions and symbols | [68940aa0d](https://github.com/isbeorn/nina/commit/68940aa0d0185bd8d4cefea5947e86f62b73410b) and its many follow-ups | New sequencer model, generators, serialization and plugin contracts. Only the isolated popup/compatibility-map pieces are candidates. |
| Sequence change tracking | [c5102d8ef](https://github.com/isbeorn/nina/commit/c5102d8efc5979a503bb7d5e7c4777a12ef358fd) | Changed contract and semantics; the later restored interface name does not make the old behavior compatible. |
| New trigger/container concepts | [5206e8f56](https://github.com/isbeorn/nina/commit/5206e8f561d6ba7961f853b2ebc37cfd3abc4e50), [74814a606](https://github.com/isbeorn/nina/commit/74814a6066e829404c633f781095a9fc934c3440), [10097e438](https://github.com/isbeorn/nina/commit/10097e438ef9d975bf60bd98b158d691eb1c34fd), [2cbef4d6f](https://github.com/isbeorn/nina/commit/2cbef4d6f9c2858b7e237862037f8609967a91f0), [e474063a6](https://github.com/isbeorn/nina/commit/e474063a6a5bddb37d922a33557ebe597b196e9b) | Unsafe/custom/programmable triggers, linked templates and conditional instruction sets belong in the next feature release. |
| Trigger interruption extension | [9def91043](https://github.com/isbeorn/nina/commit/9def91043528298222fd9871ac31eb3d07ff2640) | Adds members to ISequenceRootContainer and ISequenceTrigger. |
| Runtime and package migration | [3a48bdfc4](https://github.com/isbeorn/nina/commit/3a48bdfc42c840e22a208bbb0339e42791a4add0), [d3196f566](https://github.com/isbeorn/nina/commit/d3196f566c0fe75b054105f09c37d8abdf8aca1c) | .NET 10, SQLite and broad dependency changes. The latter also changes the symbol-function API. |
| Astronomy/time-scale replacement | [7db787b5b](https://github.com/isbeorn/nina/commit/7db787b5bda8e567019e002238f043c01083e768), [3deb577d5](https://github.com/isbeorn/nina/commit/3deb577d53156f2e123557b2196a291f3be042f0), [6d29dae7f](https://github.com/isbeorn/nina/commit/6d29dae7f807c297d08b9234efd9b258b4cfd1fc) | Coordinated SOFA/time/rise-set changes, not a small drop-in fix. O19 extracts only zero-value caching; NOVAS concurrency is isolated in R13. |
| New image processing and measurements | [bffbfd569](https://github.com/isbeorn/nina/commit/bffbfd569c19803a04b1a5c04c4e2352c5b2a755), [63be6ed05](https://github.com/isbeorn/nina/commit/63be6ed056e827b4ef3729728bd00cb282589e09), [de1471e1f](https://github.com/isbeorn/nina/commit/de1471e1faeb5a609d28a54022c9d616641f4a78) | Major debayer/star-measurement changes and new plugin-facing metrics. |
| RAW replacement | [40681fe5f](https://github.com/isbeorn/nina/commit/40681fe5fd1f714f1922610f4a80bc9c48a85e1f), [967f2248d](https://github.com/isbeorn/nina/commit/967f2248d554b2faeeec697e92572648fdc93ed7) | libraw conversion/storage with changed camera/image/converter contracts. |
| Offline sky-map redesign and multiple HiPS sources | [438240369](https://github.com/isbeorn/nina/commit/4382403692b968833c4538f96fed7f17c26003a2), [26996bf14](https://github.com/isbeorn/nina/commit/26996bf14c9ac6f60fc706336cd63244bbbed2c3), [7d8be4a2e](https://github.com/isbeorn/nina/commit/7d8be4a2e7ddd74653839b8a679e014d3afe9deb) | New projection/time/horizon architecture and changed framing interfaces. Their regressions are not automatically 3.2 bugs. |
| Notification replacement | [7d2eb4865](https://github.com/isbeorn/nina/commit/7d2eb486584dac2995d439c548af8a02958b9ce9) | New toast infrastructure. Use the old service for any local save-error improvement. |
| New native equipment families | [c7545e4c7](https://github.com/isbeorn/nina/commit/c7545e4c70dd4dc9aec08291711c5a8e245e73f0), [0ea008255](https://github.com/isbeorn/nina/commit/0ea008255561b92b23bcbe8f6003b6cab4745eec), [1a2275ab8](https://github.com/isbeorn/nina/commit/1a2275ab8788c5e35192e2047a077854bf3b9717), [d2497d6c9](https://github.com/isbeorn/nina/commit/d2497d6c96d60bb65d52bc9c76381a425e50e199), [98de5505c](https://github.com/isbeorn/nina/commit/98de5505c3d3e737795d671773c54d988d946dee) | New native integrations remain excluded. Direct-IP Alpaca is separately approved as A01 and does not require these native drivers. |
| Small features with interface changes | [6ebcc1476](https://github.com/isbeorn/nina/commit/6ebcc14760a26790fa3f7a6d5ab54fb47a66a185), [9bbca46af](https://github.com/isbeorn/nina/commit/9bbca46af1ae877f247181f9254496d7c809293e), [36d8faee3](https://github.com/isbeorn/nina/commit/36d8faee333719aa39b80627964424580780b44a), [5b5c9cb27](https://github.com/isbeorn/nina/commit/5b5c9cb27cb336ea5d2ec617661ea1f32440f012), [46a7afd35](https://github.com/isbeorn/nina/commit/46a7afd35ccf1520d01cdcde510ca5f5e8a7966e) | Focuser multipliers, container colors, guide overlays, minimum dither and Load Imaging Layout extend plugin-facing settings or mediator/view-model interfaces. |
| PHD2 transport rewrite | [9d693044a](https://github.com/isbeorn/nina/commit/9d693044ad69ba4b1b8cf9ce7541c83b377ddb0a), [acc71f6cc](https://github.com/isbeorn/nina/commit/acc71f6ccd47078be70e0f6778d2a20cb7392c61) | Larger persistent-socket change. Prefer the smaller listener/state corrections first. |

## SDKs, resources and release engineering

The External submodule moves from `06a8ea463769a9024115e3a0437742bc5b759a29` to `de19c6cf6` through eight commits. Its history includes new Moravian/Oasis drivers, PlayerOnePW 1.2.3, a SOFA replacement/rename, a broad ASI/QHY/PlayerOne/ToupTek-family refresh and a later ToupTek-family/libraw update. These were examined as dependencies, not automatically accepted as hotfixes. A library pointer or opaque DLL change does not establish which vendor bugs it fixes. For a selected hardware fix, use the minimum required binary, confirm entry-point compatibility and perform hardware validation. Do not advance the whole submodule just to obtain one DLL.

The Docs submodule history was also checked. Its latest additions mainly document expressions, new triggers, linked templates and conditional containers. Corrections to existing instructions, equipment descriptions and rollover behavior can be curated for 3.2 separately.

There are **28 changed .resx files** and **467 changed or newly translated values under resource keys that already exist in the baseline English catalogue**. This is a count across languages, not distinct messages or approved translations. O17 limits the refresh to appropriate retained keys, with placeholder and semantic review. New 3.3-only keys are excluded unless a selected backport needs them.

The release automation work in [b1e0d2302](https://github.com/isbeorn/nina/commit/b1e0d2302cdedef7b635f0a9d4040a453a8e592d), [0a981a35f](https://github.com/isbeorn/nina/commit/0a981a35feb5468c7b8fd1dd6bb782575289893b) and [14a2d6023](https://github.com/isbeorn/nina/commit/14a2d60234e03c1ce889183bcafd23ad24f7c073) is relevant to the long-lived-branch strategy, but should be a separate release-engineering change. The last commit's title says only 'Bump version' even though it adds substantive version-PR merge policy. Evaluate the final combined workflow, not its intermediate failure states. Retain develop as the development branch for now, support release/3.2.x explicitly and keep the 3.2 .NET 8 build/payload. Do not copy the 3.3 version numbers, plugin minimum-version policy or new generator package inventory. Patch-number selection and channel/build-number behavior should be checked in the resulting 3.2 workflow.

## Verification and limits of this review

- The audit has one disposition for each of the 447 unique SHAs, including every merge and every version, translation, documentation, test and SDK-pointer commit. Candidate links and identifiers are checked against that inventory.
- Commits classified as version-only were checked for unexpected changed content. Mixed commits were given specific dispositions rather than classified by title alone.
- Read-only patch-context probes against a temporary baseline index found 75 forward-applicable and 31 reverse-applicable filtered patches. These probes ignore whitespace and omit tests/resources/release notes. They help detect overlap but are **not cherry-pick, build or compatibility validation**. Absence of a reverse match does not prove a fix is missing; release-side code was checked where relevant.
- No production backports were implemented, so no backported build, automated test suite, installed-plugin test, device run or installer run has been performed. The per-item validation lists describe work required before shipping, not work already completed. Vendor DLL behavior and physical equipment behavior remain unverified.
- Every prepared backport should get a failing reproduction where practical, its focused regression tests and the relevant broader suite. Retain existing .NET 8 interfaces/constructors, verify representative installed 3.2 plugins and compile/instantiate affected WPF views. Keep old sequence/profile loading and serialization in the release checks.
- For the final release candidate, exercise upgrade/repair from 3.2.0, representative acquisition/cancellation/shutdown sequences and image read/write round trips. Keep each fix attributable to its develop source with an explicit note where the port differs.

Review checklist: complete history accounted for; baseline duplicates checked; existing defects separated from new-feature regressions; plugin/runtime dependencies identified; paired operations and boundary validation specified; no application changes applied.
