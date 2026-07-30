#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Astrometry;
using NINA.Core.Enum;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace NINA.WPF.Base.SkySurvey {

    public readonly record struct SkyMapImagePlacement(
        BitmapSource Image,
        Point Center,
        double Width,
        double Height,
        double Rotation,
        bool FlipHorizontally = false);

    public sealed class SkyMapImageCache {
        public const int DefaultDecodedImageCapacity = 32;
        public const long DefaultMaximumEstimatedBytes = 64L * 1024 * 1024;

        private readonly int decodedImageCapacity;
        private readonly Dictionary<ImageTile, CacheEntry> images = [];
        private readonly SemaphoreSlim loadGate = new SemaphoreSlim(1, 1);
        private readonly object lockObject = new object();
        private readonly long maximumEstimatedBytes;
        private readonly LinkedList<ImageTile> recentlyUsed = [];
        private readonly IReadOnlyList<ImageTile> tiles;
        private long estimatedBytes;

        public SkyMapImageCache(
            CacheSkySurvey cache,
            int decodedImageCapacity = DefaultDecodedImageCapacity,
            long maximumEstimatedBytes = DefaultMaximumEstimatedBytes) {
            ArgumentOutOfRangeException.ThrowIfLessThan(decodedImageCapacity, 1);
            ArgumentOutOfRangeException.ThrowIfLessThan(maximumEstimatedBytes, 1);
            this.decodedImageCapacity = decodedImageCapacity;
            this.maximumEstimatedBytes = maximumEstimatedBytes;
            tiles = cache?.Cache?.Elements("Image").Select(element => new ImageTile(
                double.Parse(element.Attribute("RA").Value, CultureInfo.InvariantCulture),
                double.Parse(element.Attribute("Dec").Value, CultureInfo.InvariantCulture),
                double.Parse(element.Attribute("FoVW").Value, CultureInfo.InvariantCulture),
                double.Parse(element.Attribute("FoVH").Value, CultureInfo.InvariantCulture),
                double.Parse(element.Attribute("Rotation").Value, CultureInfo.InvariantCulture),
                Path.Combine(cache.framingAssistantCachePath, element.Attribute("FileName").Value))).ToArray()
                ?? [];
        }

        public int DecodedImageCapacity => decodedImageCapacity;

        public int DecodedImageCount {
            get {
                lock (lockObject) {
                    return images.Count;
                }
            }
        }

        public long EstimatedBytes {
            get {
                lock (lockObject) {
                    return estimatedBytes;
                }
            }
        }

        public long MaximumEstimatedBytes => maximumEstimatedBytes;

        public IReadOnlyList<SkyMapImagePlacement> GetPlacements(SkyMapViewportProjection projection) {
            ViewportFoV viewport = projection.Viewport;
            List<SkyMapImagePlacement> result = [];
            foreach (ImageTile tile in RelevantTiles(viewport)) {
                BitmapSource image = GetLoadedImage(tile, viewport);
                if (image is null) {
                    continue;
                }

                double imageResolutionWidth = AstroUtil.ArcminToArcsec(tile.FieldOfViewWidth) / image.PixelWidth;
                double imageResolutionHeight = AstroUtil.ArcminToArcsec(tile.FieldOfViewHeight) / image.PixelHeight;
                double width = image.PixelWidth * imageResolutionWidth / viewport.ArcSecWidth;
                double height = image.PixelHeight * imageResolutionHeight / viewport.ArcSecHeight;
                System.Windows.Point center = projection.Project(tile.Coordinates);
                (double rotation, bool flipHorizontally) = CalculateTransform(tile, viewport, projection, center);
                result.Add(new SkyMapImagePlacement(image, center, width, height, rotation, flipHorizontally));
            }
            return result;
        }

        public async Task<bool> LoadAsync(ViewportFoV viewport, CancellationToken token) {
            ImageTile[] relevantTiles = RelevantTiles(viewport).ToArray();
            HashSet<ImageTile> activeTiles = relevantTiles.ToHashSet();
            await loadGate.WaitAsync(token);
            try {
                return await Task.Run(() => {
                    bool changed = false;
                    foreach (ImageTile tile in relevantTiles) {
                        token.ThrowIfCancellationRequested();
                        changed |= Load(tile, viewport, activeTiles, token);
                    }
                    return changed;
                }, token);
            } finally {
                loadGate.Release();
            }
        }

        private BitmapSource GetLoadedImage(ImageTile tile, ViewportFoV viewport) {
            lock (lockObject) {
                if (images.TryGetValue(tile, out CacheEntry loaded)) {
                    Touch(loaded.Node);
                    return loaded.Image;
                }
                return null;
            }
        }

        private bool Load(
            ImageTile tile,
            ViewportFoV viewport,
            IReadOnlySet<ImageTile> activeTiles,
            CancellationToken token) {
            int size = tile.DesiredSize(viewport);
            lock (lockObject) {
                if (images.TryGetValue(tile, out CacheEntry loaded) && loaded.Size == size) {
                    Touch(loaded.Node);
                    return false;
                }
            }

            BitmapSource image = tile.Load(size);
            if (image is null) {
                return false;
            }
            token.ThrowIfCancellationRequested();

            lock (lockObject) {
                if (images.TryGetValue(tile, out CacheEntry loaded) && loaded.Size == size) {
                    Touch(loaded.Node);
                    return false;
                }
                if (loaded is not null) {
                    Remove(tile);
                }
                LinkedListNode<ImageTile> node = recentlyUsed.AddFirst(tile);
                long imageBytes = EstimateBytes(image);
                images.Add(tile, new CacheEntry(image, size, imageBytes, node));
                estimatedBytes += imageBytes;
                Trim(activeTiles);
                return true;
            }
        }

        private void Touch(LinkedListNode<ImageTile> node) {
            recentlyUsed.Remove(node);
            recentlyUsed.AddFirst(node);
        }

        private void Trim(IReadOnlySet<ImageTile> activeTiles) {
            while ((images.Count > decodedImageCapacity || estimatedBytes > maximumEstimatedBytes) && images.Count > 1) {
                LinkedListNode<ImageTile> candidate = recentlyUsed.Last;
                while (candidate is not null && activeTiles.Contains(candidate.Value)) {
                    candidate = candidate.Previous;
                }
                if (candidate is null) {
                    return;
                }
                Remove(candidate.Value);
            }
        }

        private void Remove(ImageTile tile) {
            CacheEntry removed = images[tile];
            recentlyUsed.Remove(removed.Node);
            images.Remove(tile);
            estimatedBytes -= removed.EstimatedBytes;
        }

        private static long EstimateBytes(BitmapSource image) {
            int bitsPerPixel = Math.Max(image.Format.BitsPerPixel, 32);
            long stride = ((long)image.PixelWidth * bitsPerPixel + 7) / 8;
            return stride * image.PixelHeight;
        }

        private IEnumerable<ImageTile> RelevantTiles(ViewportFoV viewport) {
            double viewportRadius = Math.Max(viewport.HFoV, viewport.VFoV);
            return tiles
                .Where(x => x.FieldOfViewWidth >= 6 && x.FieldOfViewWidth <= 600)
                .Where(x => AngularDistance(x.Coordinates, viewport.CenterCoordinates)
                    < viewportRadius + AstroUtil.ArcminToDegree(Math.Max(x.FieldOfViewHeight, x.FieldOfViewWidth)))
                .OrderByDescending(x => x.FieldOfViewWidth);
        }

        private static double AngularDistance(Coordinates first, Coordinates second) {
            double firstDeclination = AstroUtil.ToRadians(first.Dec);
            double secondDeclination = AstroUtil.ToRadians(second.Dec);
            double cosine = Math.Sin(firstDeclination) * Math.Sin(secondDeclination)
                + Math.Cos(firstDeclination) * Math.Cos(secondDeclination)
                * Math.Cos(AstroUtil.ToRadians(first.RADegrees - second.RADegrees));
            return AstroUtil.ToDegree(Math.Acos(Math.Clamp(cosine, -1, 1)));
        }

        private static (double Rotation, bool FlipHorizontally) CalculateTransform(
            ImageTile tile,
            ViewportFoV viewport,
            SkyMapViewportProjection projection,
            System.Windows.Point center) {
            if (projection.Mode == SkyMapProjectionMode.AltAz) {
                return projection.ImageTransformFromEquatorial(tile.Coordinates, tile.Rotation, center);
            }

            double deltaX = center.X - viewport.ViewPortCenterPoint.X;
            double deltaY = center.Y - viewport.ViewPortCenterPoint.Y;
            Coordinates referenceCenter = viewport.CenterCoordinates.Shift(
                deltaX < 1E-10 ? 1 : 0,
                deltaY,
                viewport.Rotation,
                viewport.ArcSecWidth,
                viewport.ArcSecHeight);
            double equatorialRotation = -(90 - AstroUtil.CalculatePositionAngle(
                referenceCenter.RADegrees,
                tile.Coordinates.RADegrees,
                referenceCenter.Dec,
                tile.Coordinates.Dec));
            if (deltaX < 0) {
                equatorialRotation += 180;
            }
            if (tile.Coordinates.Dec < 0 || (referenceCenter.Dec < 0 && tile.Coordinates.Dec >= 0)) {
                equatorialRotation += 180;
            }
            return (equatorialRotation + tile.Rotation, false);
        }

        private sealed record CacheEntry(BitmapSource Image, int Size, long EstimatedBytes, LinkedListNode<ImageTile> Node);

        private sealed class ImageTile {
            public ImageTile(double rightAscension, double declination, double fieldOfViewWidth, double fieldOfViewHeight, double rotation, string path) {
                Coordinates = new Coordinates(Angle.ByHours(rightAscension), Angle.ByDegree(declination), Epoch.J2000);
                FieldOfViewWidth = fieldOfViewWidth;
                FieldOfViewHeight = fieldOfViewHeight;
                Rotation = rotation;
                Path = path;
            }

            public Coordinates Coordinates { get; }
            public double FieldOfViewWidth { get; }
            public double FieldOfViewHeight { get; }
            public double Rotation { get; }
            public string Path { get; }

            public BitmapSource Load(int size) {
                string imagePath = ThumbnailPath(size);
                if (!File.Exists(imagePath)) {
                    imagePath = Path;
                }
                if (!File.Exists(imagePath)) {
                    return null;
                }

                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.UriSource = new Uri(imagePath, UriKind.Absolute);
                if (size > 0 && imagePath == Path) {
                    image.DecodePixelWidth = size;
                }
                image.EndInit();
                image.Freeze();
                return image;
            }

            public int DesiredSize(ViewportFoV viewport) {
                double pixelWidth = AstroUtil.ArcminToDegree(FieldOfViewWidth) / viewport.HFoV * viewport.Width;
                if (pixelWidth <= CacheImage.MediumThumbnailSize) {
                    return CacheImage.SmallThumbnailSize;
                }
                if (pixelWidth <= CacheImage.MediumThumbnailSize * 2) {
                    return CacheImage.MediumThumbnailSize;
                }
                if (pixelWidth <= CacheImage.BigThumbnailSize * 2) {
                    return CacheImage.BigThumbnailSize;
                }
                return 0;
            }

            private string ThumbnailPath(int size) {
                return size == 0 ? Path : CacheImage.GetImagePathForThumbnail(Path, size);
            }
        }
    }
}
