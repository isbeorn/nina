# NINA.Astrometry Architecture

## Purpose

`NINA.Astrometry` contains astronomy-specific domain logic and data access helpers. It is a library, not an application shell or device layer.

Build shape from `NINA.Astrometry.csproj`:

- Target framework: `net10.0-windows`
- Output type: `Library`
- WPF enabled

## What Lives Here

The project is organized around a small number of responsibilities:

- Coordinate and angle types
  `Angle.cs`, `Coordinates.cs`, `TopocentricCoordinates.cs`, `Location.cs`, `RectangularCoordinates.cs`, `WorldCoordinateSystem.cs`
- Astronomy calculations
  `AstroUtil.cs`, `TwilightCalculator.cs`, `NighttimeCalculator.cs`, `MeridianFlip.cs`, `MoonInfo.cs`, `ObserverInfo.cs`
- Solar system/body models
  `Body/BasicBody.cs`, `Body/Sun.cs`, `Body/Moon.cs`, `Body/Earth.cs`
- Rise/set event models
  `RiseAndSet/*`
- Catalog/domain models
  `DeepSkyObject.cs`, `FocusTarget.cs`, `Constellation.cs`, `Star.cs`
- SQLite-backed data access
  `DatabaseInteraction.cs`
- Earth rotation parameter refresh
  `EarthRotationParameterUpdater.cs`

## Key Entry Points

- `Coordinates`
  Encapsulates RA/Dec plus epoch handling and implements J2000/JNOW and topocentric transforms.
- `AstroUtil`
  Central astronomy utility layer that bridges internal angle/coordinate types with `SOFA` and `NOVAS` calculations for sidereal time, Delta-T, refraction, and sun/moon positions.
- `NighttimeCalculator`
  Uses the active profile's latitude, longitude, and elevation to cache and compute sunrise/sunset, twilight, moon rise/set, and moon phase data.
- `DatabaseInteraction`
  Opens `NINADbContext` and queries the SQLite catalog data used for bright stars, DSO search, constellation lines, and earth rotation parameters.
- `EarthRotationParameterUpdater`
  Downloads `finals2000A.daily.csv` from the IERS data center and bulk-upserts rows into the local SQLite database when the cached data is stale.

## Dependency Position

Project references are intentionally narrow:

- `NINA.Core`
- `NINA.Profile`

That matches the code:

- shared utility, logging, and database context come from `NINA.Core`
- location-dependent calculations read the active profile through `IProfileService`

There is no dependency on UI projects, plugin loading, or device mediators.

## Data Access Boundary

`DatabaseInteraction` is the only obvious data-access gateway in this project. It uses `NINA.Core.Database.NINADbContext`, but the SQL files themselves are shipped by the `NINA` executable project. In other words:

- schema/config types live in `NINA.Core`
- runtime SQL files live in `NINA`
- astronomy queries live here

## Contribution Notes

- Put pure astronomical calculations here when they do not require equipment or app-shell concerns.
- Reuse `Coordinates`, `Angle`, and `AstroUtil` instead of duplicating conversion logic in higher layers.
- For new or changed astronomical calculations, prefer formulas and models that can be traced to published papers, standards, or official reference documentation for the underlying library/model instead of introducing ad hoc math.
- If you add new SQLite-backed astronomy data, coordinate the entity/schema side with `NINA.Core.Database` and the shipped SQL under `NINA/Database`.
- Add or extend automated verification in `NINA.Test`, especially `AstrometryTest/` or the nearest subsystem fixture, using documented reference values and edge cases for numerically sensitive behavior.
- Avoid moving UI or device logic into this project; existing dependencies show it is treated as a domain library.
