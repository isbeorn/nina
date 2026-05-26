# NINA.Image Architecture

## Purpose

`NINA.Image` is the image-processing and file-format library. It turns exposures and files into N.I.N.A.'s internal image model, computes statistics and star analysis, and reads or writes supported image formats. It is the central place for image-file ingestion and persistence, so higher layers should not invent parallel FITS/XISF/TIFF/RAW loading or saving code.

Build shape from `NINA.Image.csproj`:

- Target framework: `net10.0-windows`
- Output type: `Library`
- WPF enabled
- Windows Forms enabled

## Top-Level Structure

- `ImageData/`
  Core image model and factories: `BaseImageData`, `ImageDataFactory`, `ExposureDataFactory`, `RenderedImage`, `ImageStatistics`, `ImageMetaData`, `StarDetectionAnalysis`
- `ImageAnalysis/`
  Processing and analysis algorithms: `StarDetection`, `StarAnnotator`, `BahtinovAnalysis`, `ContrastDetection`, histogram and image utility helpers
- `FileFormat/`
  File readers/writers for `FITS` and `XISF`, plus `FileSaveInfo`
- `RawConverter/`
  RAW conversion backend (`LibRawConverter`) and `RawConverterFactory`
- `Interfaces/`
  Image abstractions such as `IImageData`, `IRenderedImage`, `IExposureData`, `IRawConverter`

## Core Pipeline

The data flow is explicit in the code:

1. Exposure data is produced or loaded into an `IExposureData` implementation.
2. `ExposureDataFactory` and `ImageDataFactory` convert that data into `IImageData`.
3. `RenderedImage` turns `IImageData` into a `BitmapSource` and can re-render, stretch, debayer, annotate, and generate thumbnails.
4. `StarDetection` and related analysis classes update `StarDetectionAnalysis` and image statistics.
5. `BaseImageData.FromFile` and `SaveToDiskAsync` route supported on-disk formats through the shared image pipeline instead of having each feature parse files on its own.

## Key Types

- `ImageData/RenderedImage.cs`
  The main rendered-image wrapper used by higher layers. It bridges raw image data, WPF bitmap output, star detection, star annotation, stretching, and thumbnail generation.
- `ImageData/ExposureData.cs`
  Contains the `BaseExposureData` hierarchy and `ExposureDataFactory`.
- `ImageData/BaseImageData.cs`
  Hosts the internal image representation and `ImageDataFactory`, and contains the central file load/save path for FITS, XISF, TIFF, RAW, and WIC-backed bitmap formats such as GIF/JPEG/PNG.
- `ImageAnalysis/StarDetection.cs`
  The built-in star detection implementation used throughout imaging, focusing, and sequence workflows.
- `FileFormat/FITS/FITS.cs`
  FITS loader/writer built around the native CFITSIO wrapper.
- `FileFormat/XISF/XISF.cs`
  XISF loader/writer with checksum, compression, and attachment handling.
- `RawConverter/RawConverterFactory.cs`
  Creates the LibRaw converter. Legacy profile settings are retained only for migration compatibility.

## File Formats And Native Dependencies

The project is responsible for the code that understands supported image formats, but not for shipping every native dependency. The current code path in `BaseImageData` handles:

- FITS (`.fit`, `.fits`, `.fts`, `.fz`)
- XISF (`.xisf`)
- TIFF (`.tif`, `.tiff`)
- common WIC-backed bitmap formats such as GIF, JPEG, and PNG
- supported RAW DSLR formats through LibRaw (`.cr2`, `.cr3`, `.nef`, `.raf`, `.raw`, `.pef`, `.dng`, `.arw`, `.orf`, `.rw2`)

The executable project ships the runtime files such as:

- CFITSIO native DLLs used by FITS loading
- `libraw_0_22_1.dll`

The boundary is:

- parsing, conversion, and in-memory image handling live here
- runtime deployment of supporting binaries is handled by `NINA`
- callers should reuse this project's file-format support instead of inventing custom readers/writers elsewhere in the solution

## Dependency Position

Project references:

- `NINA.Core`
- `NINA.Astrometry`
- `NINA.Profile`
- `Accord.Imaging (NETStandard)`

`NINA.Image` is used by the equipment layer, plate solving, WPF/UI layers, the main app, and tests. That makes it a shared computational library rather than a feature module.

## Contribution Notes

- Keep new image algorithms and format parsers here instead of duplicating them in equipment or UI code.
- If a feature needs to load or save FITS, XISF, TIFF, RAW, or other already-supported image files, route it through `NINA.Image` instead of inventing feature-local file parsing or persistence code.
- Prefer working against `IImageData`, `IExposureData`, and `IRenderedImage`; those are the stable seams the rest of the application already uses.
- If a change requires an extra native binary or helper executable, update the consuming runtime project as well; code changes here alone will not ship the dependency.
- Be careful with changes to image statistics or star detection, because those results are consumed in autofocus, plate solving, image history, and sequencing flows.
