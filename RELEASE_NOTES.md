# Version 1.9

## Features

### Dither without guiding
- A new Direct Guider has been added, which doesn't require a guide camera. It can only perform random dithers. It connects directly to the telescope, and will if required perform a dither via a Pulse Guide of a user-provided duration in a random direction.

### Plate Solving
- Added interface for ASTAP, the Astrometric STAcking Program, astrometric solver and FITS viewer as a plate solver

### GPS Assisted Location
- Added a NMEA GPS interface to retrieve the current location

### Interfacing with planetarium programs
- NINA can interface with Cartes du Ciel, HNSKY, Stellarium, and TheSkyX through their repsective TCP services to import the selected object for use in the Sequence Editor and Framing Assistant, as well as setting the observing location to match that which is set in those programs

## Improvements
- If telescope is capable of reporting SideOfPier there will now be a new option to consider this for calculating the need for meridian flips
- Added options to adjust downsample factor and maximum number of considered stars for ASTAP and local astrometry.net solvers
- All FITS keywords now have descriptive comments with units of measurment noted if applicable
- FITS keyword `DATE-OBS` now has millisecond resolution, eg: `2019-03-24T04:04:55.045`
- Additional FITS keywords are now added to images if their associated data is available:
	- `DATE-LOC`: Date and time of exposure adjusted for local time
	- `FOCRATIO`: Focal ratio of the telescope, user-configurable under Options->Equipment->Telescope
	- `SET-TEMP`: The configured CCD cooling setpoint
	- `SITEELEV`: Elevation of the observing site, in meters
	- `SWCREATE`: Contains `N.I.N.A. <version> <architecture>`
	- `TELESCOP`: Telescope name if provided under Options->Equipment->Telescope. Falls back to ASCOM mount driver name
- A new focuser settle time parameter has been added, in case the focuser shifts the image when moving (SCT, lens belt focusing, etc.). This should help with auto-focus in particular.
- It is now possible to reset the progress of a sequence item, or of a whole sequence target. If an item that occurred prior to the active sequence item is reset, stopping and starting the sequence will get back to it, but pausing/playing will keep going from the current item.
- A pre-sequence check is now triggered, and will notify end users of a variety of potential issues (camera not cooled yet, guider not connected, etc.) at sequence start
- The sequence start button is unavailable if an imaging loop is in progress in the imaging tab
- Imaging tab - Equipment specific views will only show the "Connected" flag when the device is not connected to save space
- Enhanced direct guider to accept decimal durations (e.g. 0.5s), and to perform random angle selection in a way that minimizes target deviation from center.
- Added a layout reset button to the imaging tab to restore the default dock layout.

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