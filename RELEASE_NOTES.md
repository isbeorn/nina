# Version 1.10

## Improvements
- The autofocus crop ratio has been changed to Inner Crop Ratio, and an additional Outer Crop Ratio has been added. This lets users define centered ROI, or a centered "square doughnut" which will be used by star detection, thus avoiding stars in the center and at the edges of the FO
- HFR calculation is now computed using the mean background surrounding the star, rather than the image mean
- HFR calculation has been enhanced to provide more accurate results, especially for imaging systems with central obstructions
- Autofocus trend lines are now using a weighted fit based on HFR standard dev in each image rather than an unweighted fit. This provides much better slopes and final focus point.
- Autofocus has been enhanced to support multiple curve fitting methodologies:
	- Parabolic fitting, weighted by standard dev
	- Hyperbolic fitting, weighted by standard dev
	- Comnbination of parabolic or hyperbolic fitting with trend lines (average of fitting minimum and trend line intersection is then used)
- Added ability to keep guiding during autofocus
- The autofocus routine has been changed so that it doesn't attempt to measure the focus twice for the same point
- Added Spanish Translation
- The focuser temperature compensation feature is now turned off before an auto-focus session, and turned back on afterwards

# Version 1.9

## Features

### Camera Control
- Native support for Finger Lakes Instrumentation ProLine/MicroLine cameras and color filter wheels

### Switch Hubs
- A new equipment type "Switch" has been added which can connect to an AscomSwitch or to a PrimaLuceLab EAGLE PC

### Dither without guiding
- A new Direct Guider has been added, which doesn't require a guide camera. It can only perform random dithers. It connects directly to the telescope, and will if required perform a dither via a Pulse Guide of a user-provided duration in a random direction.
- Enhanced direct guider will accept decimal durations (e.g. 0.5s), and perform random angle selection in a way that minimizes target deviation from center, even after many dither operations.

### Plate Solving
- Added interface for ASTAP, the Astrometric STAcking Program, astrometric solver and FITS viewer as a plate solver
- Mid-sequence plate solve operations (when slewing to target, or after Meridian Flip) have been enhanced to have the following behavior:
  - If plate solve fails, it automatically falls back to blind failover
  - If blind failover also fails, plate solve can be set to await a certain time period (by default 10 minutes) before trying again, up to a certain number of attempts (user-defined)
  - If all attempts fail, or Meridian is getting close, plate solve will be considered failed, but sequence will continue as usual
- Added options to adjust downsample factor and maximum number of considered stars for ASTAP and local astrometry.net solvers

### GPS Assisted Location
- Added a NMEA GPS interface to retrieve the current location

### Interfacing with planetarium programs
- NINA can interface with Cartes du Ciel, HNSKY, Stellarium, and TheSkyX through their repsective TCP services to import the selected object for use in the Sequence Editor and Framing Assistant, as well as setting the observing location to match that which is set in those programs

### Manual Camera
- Inside the camera selection there is a new entry for "Manual Camera".
- This simulated camera will enable the use of cameras which lack an SDK, while still using the whole N.I.N.A. workflow.
- It will watch a specified folder for newly created files. These files will be stored inside an internal queue.
- Each time the application wants to download an exposure from the camera, the first item of this file queue is resolved and loaded into N.I.N.A.
- Additionally a manual Bulb Mode trigger can be activated, so it will use the selected Bulb Mode in Settings-&gt;Equipment-&gt;Camera on Exposure Start.
- When this trigger is deactivated the application will just skip the Start Exposure and wait for another file to roll into the specified folder

### Weather data sources
- The existing OpenWeatherMap implementation has been replaced with a full weather data interface.
- The new interface allows devices with ASCOM ObservingConditions class drivers to supply N.I.N.A. with weather data and other conditions.
- Native OpenWeatherMap functionality is maintained, and any configured OWM API key is retained and utilized by the new native OWM client.
- Weather data sources are now configured under the Equipment section.
- Any available weather data types (air temperature, pressure, wind speed, etc.) are inserted into images as FITS keywords and/or XISF image properties.

### Focusing
- Quick focuser movement buttons have been added (fine/coarse move IN/OUT) to the focuser views
- A new focuser settle time parameter has been added, in case the focuser shifts the image when moving (SCT, lens belt focusing, etc.). This should help with auto-focus in particular.
- Focuser backlash (in and out) can now be specified. The backlash will be applied to focuser movements whenever the focuser reverses directions.
- A new Measure Backlash tool has been added in the Auto-Focus view in the imaging tab. When launched, NINA will automatically measure focuser backlash IN and OUT.
- Sequence Auto-Focus can now be triggered if measured HFR of any frame is X% worse than the first frame taken after the previous auto-focus routine.
- More resilient autofocus:
  - Ability to automatically reattempt autofocus from scratch several times in case it failed
  - Automatically go back to original focus position if obtained HFR is significantly worse than original
  - Ability to take multiple autofocus exposures per focus point and average their HFR. This leads to smoother autofocus curves.
  - A crop ratio has been added to the autofocus, letting users autofocus only on the center of the frame
  - Autofocus can now be set to use only the top brightest stars, so the same stars are used throughout the autofocus routine. This will work best on sparse star fields, and for stable, well-aligned equipment (e.g. so stars don't move between auto-focus exposures)
  - Ability to use binning for autofocus

### Sequencing
- End of Sequence Options are now available, which include:
  - Parking the telescope - This will stop the guiding, and invoke the mount Park method if available, otherwise the mount will slew to near the celestial pole (on the same side of Meridian it last was) and stop tracking. **Before using this in production, test out the feature at the telescope, with your finger on the power switch. This is to avoid any crash into the pier for mounts that do not have limits.**
  - Warming the camera - the camera will be slowly cooled, with the cooler eventually turned off
- A pre-sequence check is now triggered, and will notify end users of a variety of potential issues (camera not cooled yet, guider not connected, telescope not connected but slew enabled, etc.) at sequence start
- It is now possible to reset the progress of a sequence item, or of a whole sequence target. If an item that occurred prior to the active sequence item is reset, stopping and starting the sequence will get back to it, but pausing/playing will keep going from the current item.
- The sequence start button is unavailable if an imaging loop is in progress in the imaging tab
- If telescope is capable of reporting SideOfPier there will now be a new option to consider this for calculating the need for meridian flips
- It is now possible to set the Offset in addition to the Gain within each sequence item

### Flat Wizard
- Progress bars have been added for remaining filters and exposures
- A new Slew to Zenith button has been added for easier flats. This includes an option for east or west pier side, depending on which side of pier the mount should approach zenith from.

### Interface
- Imaging tab - Equipment specific views will only show the "Connected" flag when the device is not connected to save space
- Added a layout reset button to the imaging tab to restore the default dock layout.
- Equipment chooser dropdowns are now grouped by driver categories to easily distinguish between for example ASCOM drivers and other vendor drivers

### File Handling
- All FITS keywords now have descriptive comments with units of measurement noted if applicable
- FITS keyword `DATE-OBS` now has millisecond resolution, eg: `2019-03-24T04:04:55.045`
- Additional FITS keywords are now added to images if their associated data is available:
	- `DATE-LOC`: Date and time of exposure adjusted for local time
	- `FOCRATIO`: Focal ratio of the telescope, user-configurable under Options->Equipment->Telescope
	- `SET-TEMP`: The configured CCD cooling setpoint
	- `SITEELEV`: Elevation of the observing site, in meters
	- `SWCREATE`: Contains `N.I.N.A. <version> <architecture>`
	- `TELESCOP`: Telescope name if provided under Options->Equipment->Telescope. Falls back to ASCOM mount driver name

### OSC Camera Handling
- Debayering is now applied prior to plate-solving or auto-focus star detection
- An Unlinked Stretch option has been added. When enabled, color channels will be stretched separately, helping hide the sky background. This results in more visible celestial objects, and helps enhance both autofocus and platesolving reliablity, especially in light polluted areas. Processing time is however increased.
- A Debayered HFR option has been added. When enabled, the HFR computation will be made on the Debayered image rather than the Bayered array, providing better Auto-focus results

### Star Detection Sensitivity
- Star Detection has been enhanced to detect more stars more accurately, while avoiding picking up noise by checking that the star is a local maximum and has sufficient bright pixels
- In addition, a new Star Sensitivity Parameter is available in the Imaging options. It has three settings:
	- Normal: use standard NINA star detection
	- High: More sensitive method, with little to no performance impact. Typically picks up 1.5x - 2.5x more stars than Normal
	- Highest: Most sensitive method, with some performance impact. Typically picks up 1.5x - 2.5x more stars than High
- The higher the detection level, the more likely lumps of noise are liable to be picked up (despite rejection parameters to avoid that)
- A noise reduction parameter has been added for better star detection in fairly noisy images, which can be important if using High or Highest star sensitivity levels, although the Normal sensitivity level will also benefit from it. Several settings are available:
	- None: no noise reduction on full size image done before star detection
	- Median: a Median filter is applied to the full size image before star detection. This is good if the sensor has many hot pixels, but time consuming
	- Normal: a standard fast Gaussian filter is applied to the full size image before star detection. This is good for smoothing out the thermal and bias noise of the sensor
	- High: a strong fast Gaussian filter is applied to the full size image before star detection. This is for fairly noisy images, but can make star detection more difficult
	- Highest: a very strong fast Gaussian filter is applied to the full size image before star detection. This is for noisy images, although star detection may suffer


## Bugfixes
- Fixed when FramingAssistant was not opened before and a DSO was selected from the SkyAtlas as Framing Source an error could occur
- Fixed scrolling through Framing Assistant Offline Sky Map while cursor was inside Rectangle ignored zooming
- Fixed Alitude charts displaying wrong Twilight/Night predictions for some scenarios
- Manual focus target list was not updating in some scenarios. Now it will always update. The interval for updates is one minute.
- Fixed an issue in FramingAssistant when reloading the image and having multiple panels selected, that the orientation was not considered properly resulting in wrong coordinates
- Fixed an issue in the Telescope Equipment tab that could potentially slew to the wrong Declination if the declination angle was negative

## Improvements
- When EOS Utility is running in the background, the x64 N.I.N.A. client will scan for this app and prevent a crash due to the EOS utility being open. Instead a notification will show up to close the EOS Utility.
- N.I.N.A. SQLite Database will be created on demand and migrated to new versions on application startup instead of just being overwritten by the installer.
- Setup Installer can be run in fewer clicks and is also capable of launching the application after successful installation.
- Image History Graph will only contain statistics from sequence items
- Further increased parallelism during sequencing. 
	- After capture during image download: Parallel dither and change filter (if required)
	- After download: Parallel image saving and processing to display the image
- Image HFR is now available as an image file name token (`$$HFR$$`)
- Focuser Temperature is now available as an image file name token (`$$FOCUSERTEMP$$`)
- Added a clear button on HFR History graph. The button will be displayed when hovering the control.
- A new button is added in the options to directly open the log destination folder

## Special Thanks
The N.I.N.A. team would like to sincerely thank:
- The staff at [Cloud Break Optics](https://cloudbreakoptics.com/) and [Finger Lakes Instrumentation](http://flicamera.com/) for arranging a ProLine PL09000 and CFW1-5 to assist in integrating native FLI camera and filter wheel support.

# Version 1.8 Hotfix 1

- Prevent an unnecessary profile saving on application start
- Profiles where always saved after a sequence exposure, which slows down the image saving process. During sequence capture the profile saving is disabled now.

# Version 1.8

## Features

### Sequence
- Enable/Disable sequence entries

### Plate Solving
- Added interface for All Sky Platesolver

### Camera Control
- Altair native driver support
- ToupTek native driver support
- QHYCCD native driver support
- Added support for anti-dew heaters in ZWO cameras
- On ASCOM drivers support for setting readout modes

### Framing Assistant
- Add SkyAtlas image source which allows for framing based on offline SkyAtlas data

### Flat Wizard
- Supports you taking flats

### Imaging
- Added a list of manual focus targets (bright stars) that are currently visible in the sky

### Framing Assistant Offline Sky Chart
- Based on Sky Atlas data a basic sky chart showing objects can be displayed
- Instead of dragging the rectangular through the initial image like in the other framing sources
  the background itself will be moved like in an orrery  

### Guiding
 - Added Synchronized PHD2 Guider (experimental)
 - Synchronized PHD2 Guider will synchronize your Dither requests between multiple instances of N.I.N.A.
	- Known limitations: Dithering will happen every possible synchronized frame and is not changeable

### Auto-Update Channels
 - Previously the auto update was always just looking for released versions. Now multiple sources (Release, Beta and Nightly) can be selected and the auto updater updates to the respective version accordingls.
 - Additionally the changelog for the new version will now be displayed prior to updating, too. 


## Bugfixes

- Corrected Max Binning level for ASI Cameras
- Focuser move command fixed where lots of move commands where sent by accident
- Rotation value now considered for sequence import|export
- Canon: Fixed bulb mode for exposure times <30s
- Canon: All shutter speeds now correctly added when step set custom function is set to 1/3
- Meridian Flip window does not get stuck anymore when clicking on cancel
- Log Level will now be set on application start based on profile settings.
- ASI Cameras will not shut down their cooling and progress on opening multiple instances of NINA
- In some cases the application stayed open in the background after closing. This should not happen anymore
- `$$DATETime$$` and `$$TIME$$` will now use timestamp on exposure start, not on exposure end
- Fixed Meridian Offset default values for plate solved polar alignment 
- On sequence target guiding will be paused, prior to slewing to a new target
- Autofocus will not trigger a pause/resume command anymore, as this was not reliable in some cases. Instead PHD2 is stopped and started, similar to what is done during meridian flip.
- Fixed Platesolve result reporting wrong Dec Error.
- Platesolve recenter now considers proper angular distance calculation instead of relying just on Right Ascension.

## Improvements

- Clear button for PHD2 Graph
- Hide camera cooler controls when not available for current camera model
- Zero Floating point numbers now displayed as "0.00" instead of ".00"
- Show better exception message when an ASCOM Interop Exception occurs
- Canon: Errors are now shown to users in a readable format if any occur
- Removed hard requirement of ASCOM platform. Application can now function without it
- Improved UI Style. 
    - New logo
    - Better version display
    - Tweaked some color themes for more consistent colors
    - Better spacing between elements to reduce wasted space
    - Two new background colors to better pronounce some ui elements
    - Reworked Imaging tab to have a common style.
    - Imaging tab tools pane (to hide/show panels) moved to the top and split into two separate categories
- Profiles don't get overriden when using multiple instances of N.I.N.A. with each one having a separate profile active
- Autostretch replaced by a better midpoint transformation function
- Autostretch now has black point clipping options
- Vastly improved image statistics calculation time.
- Estimated Finish Time will automatically update in the sequencing view
- Added copy button for existing color schemas to copy over to custom and alternative custom schemas    
- Framing tab: 
	- Moved coordinates out of framing boxes to not obscure target
	- Added a new button to be able to add the framing target to a sequence instead of replacing
	- Control to adjust opacity of framing box
- Improved Framing Assistant and Sequence Target Textboxes by giving up to 50 target hints based on input to select from
- Framing Assistant now can annotate DSO
- Sensor offset is now available as an image file name token (`$$OFFSET$$`)
- Attempt to start PHD2 and connect all equipment when connecting to guider and PHD2 is not running
- Adaptive Cooling: Duration for cool/warm camera is now a minimum duration. In case the cooler cannot keep up with the set duration, the application will wait for the camera to reach the checkpoints instead of just continuing setting new targets without the camera having any chance to reach those in the timeframe.
- Automatically import filter wheel filters to the profile on connection when profile filter list is still empty
- Load a default imaging tab layout in case the layout file is corrupted or not compatible anymore
- Removed Altitude Side combobox from plate solved polar alignment. It will be automatically determined based on alt/az coordinates.
- Changed log file format. Each application start will write to a separate log file for better distinction
- Improved XISF save speed and resulting file size by not embedding the image as base64 string, but instead as attached raw byte data
- Additional FITS keywords are now added to images if their associated data is available:
	- `OBJECT`: The name of the target, as supplied in the active Sequence
	- `DEC` and `RA`: The DEC and RA of the telescope at the time of the exposure
	- `INSTRUME`: The name of the connected camera
	- `OFFSET`: The sensor offset, if applicable
	- `FWHEEL`: Name of the connected filter wheel
	- `FOCNAME`: Name of the connected focuser
	- `FOCPOS` and `FOCUSPOS`: Position of the focuser, in steps
	- `FOCUSSZ`: Size of a focuser step, in microns
	- `FOCTEMP` and `FOCUSTEM`: Temperature reported by the focuser
	- `ROTNAME` Name of the connected rotator
	- `ROTATOR` and `ROTATANG`: Angle of the rotator
	- `ROTSTPSZ` Minimum rotator step size, in degrees
	- Applicable XISF Image Property analogs of the above, as defined by XISF 1.0 Section 11.5.3

## Special Thanks
The N.I.N.A. team would like to thank 

- Nick Smith from [Altair Astro](https://www.altairastro.com/) for providing a GPCAM2 290C to integrate Altair SDK
- Elias Erdnuess from [Astroshop.eu](https://www.astroshop.eu) for providing multiple Toupcam Cameras to integrate ToupTek SDK
- The staff at QHYCCD dealer [Cloud Break Optics](https://cloudbreakoptics.com/) for lending a QHY183C to integrate QHYCCD SDK

These items helped a lot during development and testing.  
Thank you for your support!

___

# Version 1.7

## Features

### Framing Assistant
- Mosaic planning for framing assistant
- Added multiple new SkySurveys to choose from for framing
- DSLR RAW files can now be loaded into framing assistant

### Sequences
- Sequence multiple target planning. Import/Export also available 
- Consider Rotation for framing assistant when set for a sequence during platesolves

### Filter Wheel
- Added a manual FilterWheel for users without a motorized wheel or with a filter drawer. There will pop up a window and prompt the user when a filter change is requested

### Rotators
- Support for ASCOM Rotators added
- A manual rotator option is added, for users without an automatic rotator. A pop up will show with the current angle and target angle for a user to manually rotate to

### Imaging
- DSLR Users: Images will now always be saved as RAW to prevent any loss of data due to conversions
- Aberration inspector added to imaging tab. It will show a 3x3 Panel containing the current image
- Battery display for DSLRs

### Settings
- Added ImageParameter `$$RMSARCSEC$$` and `$$FOCUSPOSITION$$`
- Latitude and Longitude can now be synced from application to telescope and vica versa (when supported).
- Serial Relay (via USB) interaction for Nikon Bulb

## Bugfixes

- Fixed Canon issue that a second exposure is accidentally triggered, causing the camera to be stuck
- Fixed memory leak when using Free Image RAW Converter

## Improvements

- Guider is also connected when pushing "Connect All"
- Changed FilterWheel UI for switching filters
- Some improvements to memory consumption
- Automatically triggered Autofocus and Platesolving now spawns inside a separate window
- Guiding Dither thresholds now configurable
- Flipped + and - for Stepper Controls
- Minor UI Layout improvements
- Made SkySurvey Cache directory configurable
- Local astrometry.net client will now downscale image for faster image solving
- ImagePatterns inside options can now be dragged from the list to the textbox
- Major code refactorings for better maintainability
- Lots and lots of minor bugfixes and improvements