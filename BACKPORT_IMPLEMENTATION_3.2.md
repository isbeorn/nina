# 3.2 Hotfix 1 implementation and verification

Approved scope: all 96 groups in [the reviewed list](BACKPORT_REVIEW_3.2.md), including the direct-IP Alpaca exception. The [commit review](BACKPORT_REVIEW_3.2_COMMITS.md) records the complete evaluation of develop.

Baseline: `2393eae581145ed5b8114bf07c48ca2580540fd5`. Pinned source: `develop` at `6a83379822e0e015d13591a5ede54574ecc3d0e3`. Target: `release/3.2.x`, version `3.2.1.9001`.

- [x] Implement every approved group, including direct-IP Alpaca and the selective adaptations.
- [x] Preserve .NET 8, existing plugin interfaces and profile/sequence compatibility.
- [x] Keep one cherry-picked commit per topic, with original source SHA references. Fold topic corrections and regression tests into those commits.
- [x] Verify cancellation, paired operations and boundaries at the closest available meaningful layer.
- [x] Add release notes under `3.2 Hotfix 1` and prepare the patch version.
- [x] Run targeted tests, broader regression tests, WPF compilation, installer compilation and final diff checks.
- [ ] Complete physical equipment soak tests and real installer fresh-install/upgrade/repair acceptance checks before publishing.

## Applied topics

Each row is one commit. Multiple upstream commits belonging to the same approved topic were cherry-picked together. Conflict resolutions and compatibility adaptations stay with that topic. Dependencies determine the commit order. The release version, release notes and review records are in one separate release-preparation commit.

| Group | Behavior | Backport commit |
| --- | --- | --- |
| R01 | Make installer upgrades and repairs restore the packaged files | `d5093c621860` |
| R02 | Propagate Abort Exposure cancellation into the active capture | `416221b31789` |
| R03 | Fix Nikon automatic exposure cancellation | `cbc77037d41a` |
| R04 | Apply readout mode before gain and offset | `36332b63ead5` |
| R05 | Keep QHY gain and offset correct across read mode changes | `0df8bea515e9` |
| R06 | Shut down QHY cooling and sensor workers during disconnect | `b743eaa06b3f` |
| R07 | Close QHY filter-wheel handles when discovery fails | `f9565eba1075` |
| R08 | Bound ASCOM binning enumeration on camera connection | `6998efdbc355` |
| R09 | Connect to ASCOM V1 telescope drivers with unsupported tracking properties | `b3223a6a75a3` |
| R10 | Connect to ASCOM V1 cameras without SensorType support | `b57eed1b968a` |
| R11 | Wait for Cool Camera targets from either temperature direction | `e641235d59e2` |
| R12 | Complete dome parking reliably and enforce its waiting timeout | `6054fb2a1138` |
| R13 | Serialize NOVAS calls and reject corrupt Sun/Moon results | `498f0bded07b` |
| R14 | Stop PHD2 guiding from every non-stopped state | `54c498601950` |
| R15 | Read complete PHD2 event messages across TCP packet boundaries | `5bbcc2e5a041` |
| R16 | Stop the OpenMeteo worker cleanly on cancellation | `49c5909b93a7` |
| R17 | Prevent overlapping Connect All and Disconnect All commands | `da4aeb93d30c` |
| R18 | Respect the active sequence scope in Center After Drift | `61a5fccbb728` |
| R19 | Guard missing trigger runners and null validation issue lists | `4f4b6455befe` |
| R20 | Preserve Annotation metadata when cloning | `954de220025c` |
| R21 | Make Sky Flat validation return failure when issues exist | `2705d7828e1a` |
| R22 | Use the correct altitude start-time field in Sky Atlas | `b099c48b8f4f` |
| R23 | Refresh horizon and altitude bindings after profile/date/site changes | `d49080930adf` |
| R24 | Raise the mechanical rotator event for mechanical moves | `cf26ea544a2c` |
| R25 | Track filter-wheel changes made by another client | `1de88a6ebad9` |
| R26 | Harden ToupTek-alike camera initialization and setting application | `dd886ade4c53` |
| R27 | Read ZWO camera properties by camera ID | `b787728cc59d` |
| R28 | Preserve FITS filenames containing brackets or parentheses | `d88839483ed1` |
| R29 | Handle XISF short reads explicitly | `7b346135e679` |
| R30 | Release image-analysis bitmaps and graphics resources | `ad065ee65d0f` |
| R31 | Fix Gaussian blur buffer selection and bitmap stride handling | `676c57ea145b` |
| R32 | Write SBIG electrons-per-ADU metadata | `a6ddbdf607d5` |
| R33 | Correct FITS site metadata guards | `5c80d1255517` |
| R34 | Correct XISF metadata values and Bayer offset keyword reading | `fdf0738f4a2f` |
| R35 | Correct guider RMS removal calculations | `8557fa1e7d8a` |
| R36 | Fix PHD2 guide-distance display and lock-position hashing | `10e9c436ea20` |
| R37 | Stop successful Retry actions being retried and fix async overload forwarding | `77e29d595b50` |
| R38 | Return correct image-pattern insertion results | `dc0fe4725459` |
| R39 | Reject malformed binning strings | `345cbfc5034c` |
| R40 | Fix byte-size formatting | `68d0f5fb6935` |
| R41 | Make manual rotator completion and cancellation consistent | `225c19347ac9` |
| R42 | Use invariant scale values and correct mirroring metadata with TheSkyX | `c37d2c5cf534` |
| R43 | Keep filter-wheel settings changes observable after reset | `8938b0779b39` |
| R44 | Preserve distinct trained flat-calibration records | `6ffdff8201cd` |
| R45 | Save new profiles without replacing nonexistent files or good backups | `b99e442f4a21` |
| R46 | Publish equipment state before connection events | `4f38425e8894` |
| R47 | Guard disconnected camera binning and guider operations | `8828e5bc2f5e` |
| R48 | Handle invalid plugin behavior/provider types without secondary null failures | `7bbaac6f0dc3` |
| R49 | Rebind Flat Wizard filter settings when the settings object changes | `506924211df1` |
| R50 | Allow null plate-solving progress reporters | `3aaa0cd805bb` |
| R51 | Accept comma-decimal sexagesimal coordinates | `f9886b909422` |
| R52 | Make command-line plate-solver timeout and cancellation effective | `5ea125d7df5c` |
| R53 | Restore the original filter after failed or canceled capture solves | `92e3e08df4f9` |
| R54 | Propagate failed centering slews and honor plate-solve gain | `6c4add7abed6` |
| R55 | Honor the requested coordinate projection type | `392482f49904` |
| R56 | Log malformed plugin manifests and assembly metadata clearly | `c06cbe71f9c5` |
| R57 | Report invalid profile IDs during startup | `0ed7385c2259` |
| R58 | Hide driver controls for unsupported focuser/rotator capabilities | `796fa4706902` |
| R59 | Show installed-plugin indicators reliably | `34877b4fe6a0` |
| R60 | Avoid parsing an empty PopupButton focus path | `cbd10c27f5b5` |
| R61 | Fix initial popup rendering without prematurely closing modal dialogs | `9b4d13764f5f` |
| A01 | Direct-IP Alpaca connections | `cd35c50c7614` |
| C01 | Correct ASCOM Bayer offsets during automatic debayering | `82912cc37e07` |
| C02 | Use actual ASCOM exposure start and duration safely | `58c1684847c7` |
| C03 | Serialize native camera SDK calls and retain native callbacks | `268af19af177` |
| C04 | Wait for a telescope sync position update | `51d44dfce5e4` |
| C05 | Improve no-sync centering correction geometry | `e5012c4adc5f` |
| C06 | Add padding to pier-side projection near the meridian | `b44e1e9005e8` |
| C07 | Keep tracking disabled after unpark | `0ec0bbbb0711` |
| C08 | Use native Alt/Az slews and expose tracking choice in the sequence instruction | `a612396b24c9` |
| C09 | Refresh built-in color schemes without losing custom colors | `b3b6b8df4be4` |
| C10 | Make PlayerOne filter-wheel connection wait for homing | `369452b745a8` |
| C11 | Fill missing capture filter metadata from the active wheel | `996604187613` |
| C12 | Improve Sky Atlas search responsiveness | `d7b395ffa114` |
| C13 | Correct cached sky-image orientation across zoom levels | `c355eb053301` |
| C14 | Surface asynchronous image-save timeouts clearly | `d9fb928475ab` |
| C15 | Match plugin compatibility entries by identifier | `69f6a0782ca8` |
| O01 | Offer per-filter or shared HFR autofocus trends | `b8b418fe4f93` |
| O02 | Add Zstandard compression for XISF | `63178aa4436b` |
| O03 | Add more bright stars for manual focusing | `00d0c9386799` |
| O04 | Filter and sort Sky Atlas by transit time | `8646b5776fb0` |
| O05 | Show meridian crossing time on altitude charts | `109f77f12f2d` |
| O06 | Show the selected readout mode in simple-sequence details | `cd3c57a76dd6` |
| O07 | Expose Reset All for Legacy Sequencer target sets | `a8f3798a8c03` |
| O08 | Hide empty thumbnail filter labels and freeze thumbnail drawing objects | `02d2a2c06bf3` |
| O09 | Show distinct DEBUG and TRACE logging indicators | `1096384b1e65` |
| O10 | Reduce loading-spinner rendering overhead | `28b442518b7b` |
| O11 | Improve plate-solving panel layout | `a9adadd23041` |
| O12 | Improve error messages and small display details | `c17ce3b7c2a1` |
| O13 | Improve existing diagnostic messages | `3d868bf9823a` |
| O14 | Include the source filename in sequence deserialization diagnostics | `a154749ce9c4` |
| O15 | Improve hyperlink handling and offer Copy URL | `7214adc146cc` |
| O16 | Use equatorial fallback for the equipment-panel Alt/Az slew | `8a8c29c9a871` |
| O17 | Refresh translations for retained 3.2 functionality | `595f5326a0e2` |
| O18 | Make drag/drop layout lookup tolerate missing application layout | `8f6f80e99a28` |
| O19 | Cache valid zero Earth-rotation corrections | `3394258a67a9` |

## Compatibility adaptations

- Kept .NET 8 and the 3.2 device, profile, mediator, sequencing and plugin interfaces. Existing constructor overloads and serialized field types remain available. New enum values are appended. ZstdSharp.Port 0.8.7 is the only new package dependency.
- Direct-IP Alpaca uses the 3.2 equipment/provider architecture. All ten device types have independent settings per profile, validated IP literals and cancellation/disconnect cleanup. A local HTTP safety-monitor simulator exercises connection, reads, disconnection and reconnection with a nonzero device number.
- QHY and ZWO retain the 3.2 SDK interfaces and native binaries. ToupTek-family native calls use ordinary .NET 8 locks. Callback dispatch avoids the native-call lock, and disconnect callbacks cancel pending images without closing the native camera from the callback thread.
- Telescope sync uses the existing timer and configured settle time. Its own cancellation also bounds a stalled timer wait. Driver failure returns immediately; a driver-accepted sync that does not converge logs the timeout, preserving the upstream return policy.
- Player One uses the existing driver exports, retains existing profile defaults and cancels or times out homing correctly. No unrelated filter-wheel implementations or SDK updates were imported.
- Spherical centering and cached-image orientation use the existing 3.2 projection APIs. Failed slew handling and plate-solving gain are in R54; projection forwarding is R55 and spherical centering is C05.
- Sky Atlas keeps the 3.2 database schema plus bright-star migration 15. No HiPS additions were imported. Transit calculations convert sidereal hours to clock hours and preserve local date semantics across midnight.
- Image-save diagnostics retain the existing success events and mediator contracts. Disk-full notifications are throttled while unrelated failures remain visible. Timeout diagnostics also handle cancellation wrapped by the existing retry implementation.
- Translation updates cover retained 3.2 keys and selected additions. Built-in color refreshes preserve custom primary and alternate schemes.

## Verification

Final automated verification uses .NET SDK 8.0.425 on Windows x64:

- Full regression suite: 3,668 passed, 3 existing skips and no failures.
- Nine isolated runs: 56 passed. These cover camera capture/live-view behavior, automatic Bayer selection in the real image VM, profile startup and persistence, readout-mode templates, hyperlink views and enable/disable behavior, telescope sync, real Sky Atlas transit searches, modal window construction and direct-IP Alpaca setup bindings.
- Real production deadlines: dome parking and command-line plate solving each passed a ten-minute timeout test; image saving passed its five-minute timeout and continued with the next queued image. The CLI check also covers child-process termination.
- Regression coverage includes all eight Sky Atlas database sort fields in both directions at limits 0, 2 and 3, missing aliases, cached-image zoom/orientation near both poles and RA wraparound, all seven ToupTek-family wrapper callbacks under native-call locking, both cooling directions, capture filter restoration and disk-full throttling boundaries.
- Relevant failures were reproduced before fixing camera cancellation/cooling, dome completion, NOVAS/altitude handling, plate solving, ASCOM exposure metadata, profile startup, Bayer offsets, cache orientation, telescope sync and Sky Atlas transit behavior. Detailed red/green results are retained in the local verification artifacts.
- Bright-star migration: initialized the original database, applied migrations through 14 then 15, repeated 15 and checked integrity. The catalogue grows from 57 to 212 stars and repeating migration 15 preserves identical rows.
- Release managed/WPF projects, NuGet packages and the x64 MSI compiled successfully. The installer packages 978 files including ZstdSharp and migration 15. MSI table inspection confirmed `REINSTALLMODE=amus` at sequence 799 before `CostInitialize` at 800 in both UI and execute sequences. Product version is 3.2.1.9001.
- The installer source already pins a versioned runtime filename from .NET 8.0.21. The validation build used `RuntimeFrameworkVersion=8.0.21` to match it. Building with this machine's default 8.0.31 runtime cannot resolve that existing filename. No runtime or installer-layout migration was folded into these backports.
- Existing NU1701, obsolete API, NUnit analyzer and WiX version/language warnings remain. Diff checks account for the repository's existing CRLF files and pass after removing whitespace introduced by the backports.

Reproduction commands (use an SDK 8.0.425 selection file or an SDK 8 build environment):

```powershell
dotnet test NINA.Test/NINA.Test.csproj -c Debug
dotnet msbuild NINA.sln -restore -t:NINA_Setup -p:RestoreForce=true -p:Configuration=Release -p:Platform=x64 -p:RuntimeFrameworkVersion=8.0.21 -m
```

Fixtures marked `Explicit` explain their isolation requirements. Run each WPF fixture in its own test process; select each long-running timeout test separately. Local TRX results and logs are under `.tmp/hotfix-3.2/test-results` and `.tmp/hotfix-3.2`.

## Remaining release acceptance checks

Physical QHY, ZWO, Nikon, ToupTek-family and Player One hardware was not available for sustained capture, disconnect and reconnect testing. A real ASCOM mount/dome and representative remote Alpaca servers still need acceptance testing. Simulator and mock coverage does not replace those checks.

The MSI was built and inspected but not installed over the user's NINA installation. Fresh installation, upgrade with modified/higher-version files and repair through both MSI and the release bootstrapper need a disposable Windows environment. A signed release/bootstrapper and an independent application's XISF/Zstandard interoperability check were not produced here.

Proposed pull request title: **Backport approved fixes and direct-IP Alpaca for 3.2 Hotfix 1**.
