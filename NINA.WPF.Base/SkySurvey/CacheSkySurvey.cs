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
using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

namespace NINA.WPF.Base.SkySurvey {

    public class CacheSkySurvey {
        public const string DefaultCacheName = "Default";
        public const string UserCacheName = "User Cache";
        private const string CacheInfoFileName = "CacheInfo.xml";

        public readonly string framingAssistantCachePath;
        private readonly Dictionary<string, XElement> skyAtlasCaches = new Dictionary<string, XElement>(StringComparer.OrdinalIgnoreCase);
        private string userCachePath;
        private string userCacheInfoPath;

        public CacheSkySurvey(string framingAssistantCachePath) {
            this.framingAssistantCachePath = framingAssistantCachePath;
            Initialize();
        }

        /// <summary>
        /// True when the cache directory contains cache subfolders. In this mode the root directory holds no cache of its own:
        /// sky atlas caches live in their own subfolders and newly saved images go to the reserved "User Cache" subfolder
        /// </summary>
        public bool IsMultiCache { get; private set; }

        /// <summary>
        /// Name of the subfolder cache that is used for the offline sky map. Null when not in multi cache mode
        /// </summary>
        public string SelectedSkyAtlasCacheSource { get; private set; }

        private void Initialize() {
            if (!Directory.Exists(framingAssistantCachePath)) {
                Directory.CreateDirectory(framingAssistantCachePath);
            }

            skyAtlasCaches.Clear();
            IsMultiCache = GetCacheSubfolders().Count > 0;

            if (IsMultiCache) {
                MigrateRootCache();
                userCachePath = Path.Combine(framingAssistantCachePath, UserCacheName);
            } else {
                userCachePath = framingAssistantCachePath;
            }
            userCacheInfoPath = Path.Combine(userCachePath, CacheInfoFileName);

            if (File.Exists(userCacheInfoPath)) {
                Cache = LoadCacheInfo(userCacheInfoPath);
            } else {
                Cache = new XElement("ImageCacheInfo");
                if (!IsMultiCache) {
                    // The user cache folder of the multi cache mode is only created once an image is saved
                    Cache.Save(userCacheInfoPath);
                }
            }

            var sources = GetSkyAtlasCacheSources();
            if (!sources.Contains(SelectedSkyAtlasCacheSource, StringComparer.OrdinalIgnoreCase)) {
                SelectedSkyAtlasCacheSource =
                    sources.FirstOrDefault(x => string.Equals(x, DefaultCacheName, StringComparison.OrdinalIgnoreCase))
                    ?? sources.FirstOrDefault();
            }
        }

        private static XElement LoadCacheInfo(string cacheInfoPath) {
            var cache = XElement.Load(cacheInfoPath);

            /* Ensure Backwards compatibility with v1.6.0.2 */
            var elements = cache.Elements("Image").Where(x => x.Attribute("Id") == null);
            foreach (var element in elements) {
                element.Add(new XAttribute("Id", Guid.NewGuid()));
            }
            elements = cache.Elements("Image").Where(x => x.Attribute("Source") == null);
            foreach (var element in elements) {
                if (element.Attribute("Rotation").Value != "0") {
                    element.Add(new XAttribute("Source", nameof(FileSkySurvey)));
                } else {
                    element.Add(new XAttribute("Source", nameof(NASASkySurvey)));
                }
            }
            return cache;
        }

        /// <summary>
        /// Names of all direct subfolders that contain a CacheInfo.xml
        /// </summary>
        private List<string> GetCacheSubfolders() {
            try {
                return Directory.GetDirectories(framingAssistantCachePath)
                    .Where(x => File.Exists(Path.Combine(x, CacheInfoFileName)))
                    .Select(Path.GetFileName)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            } catch (Exception ex) {
                Logger.Error("Error scanning cache subfolders", ex);
                return new List<string>();
            }
        }

        /// <summary>
        /// A cache is either a single root cache or a set of subfolder caches, but never a mix.
        /// When subfolder caches exist next to a root cache with entries, all direct root files are moved into a new "Default" subfolder
        /// </summary>
        private void MigrateRootCache() {
            var rootCacheInfo = Path.Combine(framingAssistantCachePath, CacheInfoFileName);
            if (!File.Exists(rootCacheInfo)) {
                return;
            }

            try {
                if (!XElement.Load(rootCacheInfo).Elements("Image").Any()) {
                    return;
                }

                var target = Path.Combine(framingAssistantCachePath, DefaultCacheName);
                for (var i = 1; Directory.Exists(target) || File.Exists(target); i++) {
                    target = Path.Combine(framingAssistantCachePath, $"{DefaultCacheName} ({i})");
                }
                Directory.CreateDirectory(target);

                // CacheInfo.xml is moved last, so that an interrupted migration is picked up again on the next start
                var files = Directory.GetFiles(framingAssistantCachePath)
                    .OrderBy(x => string.Equals(Path.GetFileName(x), CacheInfoFileName, StringComparison.OrdinalIgnoreCase));
                foreach (var file in files) {
                    File.Move(file, Path.Combine(target, Path.GetFileName(file)));
                }
                Logger.Info($"Migrated root sky survey cache to {target}");
            } catch (Exception ex) {
                Logger.Error("Failed to migrate root sky survey cache into a subfolder", ex);
            }
        }

        /// <summary>
        /// Clears the cache for saved images. In multi cache mode only the "User Cache" is cleared and the sky atlas caches are kept
        /// </summary>
        public void Clear() {
            if (IsMultiCache) {
                if (Directory.Exists(userCachePath)) {
                    Directory.Delete(userCachePath, true);
                }
            } else {
                System.IO.DirectoryInfo di = new DirectoryInfo(framingAssistantCachePath);

                foreach (FileInfo file in di.GetFiles()) {
                    file.Delete();
                }
                foreach (DirectoryInfo dir in di.GetDirectories()) {
                    dir.Delete(true);
                }
            }
            Initialize();
        }

        public void DeleteFromCache(XElement element) {
            if(Cache != null && element != null) {

                var fileNameAttribute = element.Attribute("FileName");

                if(fileNameAttribute != null) {
                    var fullFileName = Path.Combine(userCachePath, fileNameAttribute.Value);                 
                    var thumbnailBig = CacheImage.GetImagePathForThumbnail(fullFileName, CacheImage.BigThumbnailSize);
                    var thumbnailMedium = CacheImage.GetImagePathForThumbnail(fullFileName, CacheImage.MediumThumbnailSize);
                    var thumbnailSmall = CacheImage.GetImagePathForThumbnail(fullFileName, CacheImage.SmallThumbnailSize);

                    if(File.Exists(fullFileName)) {
                        try { File.Delete(fullFileName); } catch { }
                    }
                    if (File.Exists(thumbnailBig)) {
                        try { File.Delete(thumbnailBig); } catch { }
                    }
                    if (File.Exists(thumbnailMedium)) {
                        try { File.Delete(thumbnailMedium); } catch { }
                    }
                    if (File.Exists(thumbnailSmall)) {
                        try { File.Delete(thumbnailSmall); } catch { }
                    }

                }

                element.Remove();
                Cache.Save(userCacheInfoPath);
            }
        }

        public XElement SaveImageToCache(SkySurveyImage skySurveyImage) {
            try {
                var element =
                    Cache
                    .Elements("Image")
                    .Where(
                        x => x.Attribute("Id").Value == skySurveyImage.Id.ToString()
                    ).FirstOrDefault();
                if (element == null) {
                    element =
                    Cache
                    .Elements("Image")
                    .Where(
                        x =>
                            x.Attribute("RA").Value == skySurveyImage.Coordinates.RA.ToString("R", CultureInfo.InvariantCulture)
                            && x.Attribute("Dec").Value == skySurveyImage.Coordinates.Dec.ToString("R", CultureInfo.InvariantCulture)
                            && x.Attribute("FoVW").Value == skySurveyImage.FoVWidth.ToString("R", CultureInfo.InvariantCulture)
                            && x.Attribute("Source").Value == skySurveyImage.Source
                    ).FirstOrDefault();

                    if (element == null) {
                        if (!Directory.Exists(userCachePath)) {
                            Directory.CreateDirectory(userCachePath);
                        }

                        var sanitizedName = CoreUtil.ReplaceAllInvalidFilenameChars(skySurveyImage.Name);
                        var originalImgFilePath = Path.Combine(userCachePath, sanitizedName + ".jpg");


                        originalImgFilePath = RestoreNameFromUniqueBracket(originalImgFilePath);
                        
                        var imgFilePath = CoreUtil.GetUniqueFilePath(originalImgFilePath);
                        var name = Path.GetFileNameWithoutExtension(originalImgFilePath);

                        using (var fileStream = new FileStream(imgFilePath, FileMode.Create)) {
                            var encoder = new JpegBitmapEncoder();
                            encoder.QualityLevel = 70;
                            encoder.Frames.Add(BitmapFrame.Create(skySurveyImage.Image));
                            encoder.Save(fileStream);
                        }

                        SaveThumbnail(skySurveyImage.Image, CacheImage.BigThumbnailSize, imgFilePath);
                        SaveThumbnail(skySurveyImage.Image, CacheImage.MediumThumbnailSize, imgFilePath);
                        SaveThumbnail(skySurveyImage.Image, CacheImage.SmallThumbnailSize, imgFilePath);

                        XElement xml = new XElement("Image",
                            new XAttribute("Id", skySurveyImage.Id),
                            new XAttribute("RA", skySurveyImage.Coordinates.RA.ToString("R", CultureInfo.InvariantCulture)),
                            new XAttribute("Dec", skySurveyImage.Coordinates.Dec.ToString("R", CultureInfo.InvariantCulture)),
                            new XAttribute("Rotation", skySurveyImage.Rotation),
                            new XAttribute("FoVW", skySurveyImage.FoVWidth.ToString("R", CultureInfo.InvariantCulture)),
                            new XAttribute("FoVH", skySurveyImage.FoVHeight.ToString("R", CultureInfo.InvariantCulture)),
                            new XAttribute("FileName", Path.GetFileName(imgFilePath)),
                            new XAttribute("Source", skySurveyImage.Source),
                            new XAttribute("Name", name)
                        );

                        Cache.Add(xml);
                        Cache.Save(userCacheInfoPath);
                        return xml;
                    }
                }
                return element;
            } catch (Exception ex) {
                Logger.Error(ex);
                throw;
            }
        }
        
        private void SaveThumbnail(BitmapSource image, int size, string path) {
            var rescaledImage = new TransformedBitmap(image, new ScaleTransform((double)size / image.PixelWidth, (double)size / image.PixelHeight));
            var adjustedPath = CacheImage.GetImagePathForThumbnail(path, size);

            using (var fileStream = new FileStream(adjustedPath, FileMode.Create)) {
                var encoder = new JpegBitmapEncoder();
                encoder.QualityLevel = 70;
                encoder.Frames.Add(BitmapFrame.Create(rescaledImage));
                encoder.Save(fileStream);
            }
        }
        
        private string RestoreNameFromUniqueBracket(string originalImgFilePath) {
            var filename = Path.GetFileName(originalImgFilePath);

            var regex = new Regex(@"^(.*?)(?:\((\d+)\))?\.(.+)$");
            var matches = regex.Match(filename);
            var path = originalImgFilePath;
            if (matches.Groups.Count == 4 && !string.IsNullOrEmpty(matches.Groups[2].Value)) {
                var originalFileName = matches.Groups[1].Value + "." + matches.Groups[3].Value;

                path = Path.Combine(userCachePath, originalFileName);
            }
            return path;
        }

        public XElement Cache { get; private set; }

        public object CulutureInfo { get; private set; }

        /// <summary>
        /// Restores an image from cache based on given parameter
        /// </summary>
        /// <param name="source">The selected source</param>
        /// <param name="ra">Right Ascension</param>
        /// <param name="dec">Declination</param>
        /// <param name="rotation">Rotation of image</param>
        /// <param name="fov">Field of View in Arcminutes</param>
        /// <returns></returns>
        public Task<SkySurveyImage> GetImage(string source, double ra, double dec, double rotation, double fov) {
            return Task.Run(() => {
                var element =
                    Cache
                    .Elements("Image")
                    .Where(x => x.Attribute("Source").Value == source)
                    .Where(x => x.Attribute("RA").Value == ra.ToString("R", CultureInfo.InvariantCulture))
                    .Where(x => x.Attribute("Dec").Value == dec.ToString("R", CultureInfo.InvariantCulture))
                    .Where(x => x.Attribute("Rotation").Value == rotation.ToString(CultureInfo.InvariantCulture))
                    .Where(x => x.Attribute("FoVW").Value == fov.ToString("R", CultureInfo.InvariantCulture))
                    .FirstOrDefault();

                if (element != null) {
                    return Load(element);
                }

                return null;
            });
        }

        public Task<SkySurveyImage> GetImage(Guid id) {
            return Task.Run(() => {
                var element =
                    Cache
                    .Elements("Image")
                    .Where(
                        x => x.Attribute("Id").Value == id.ToString()
                    ).FirstOrDefault();
                if (element != null) {
                    return Load(element);
                } else {
                    return null;
                }
            });
        }

        private SkySurveyImage Load(XElement element) {
            var img = LoadJpg(element.Attribute("FileName").Value);
            Guid id = Guid.Parse(element.Attribute("Id").Value);
            var fovW = double.Parse(element.Attribute("FoVW").Value, CultureInfo.InvariantCulture);
            var fovH = double.Parse(element.Attribute("FoVH").Value, CultureInfo.InvariantCulture);
            var rotation = double.Parse(element.Attribute("Rotation").Value, CultureInfo.InvariantCulture);
            var ra = double.Parse(element.Attribute("RA").Value, CultureInfo.InvariantCulture);
            var dec = double.Parse(element.Attribute("Dec").Value, CultureInfo.InvariantCulture);
            var name = element.Attribute("Name").Value;
            var source = element.Attribute("Source")?.Value ?? string.Empty;

            img.Freeze();
            return new SkySurveyImage() {
                Id = id,
                Image = img,
                FoVHeight = fovH,
                FoVWidth = fovW,
                Coordinates = new Coordinates(ra, dec, Epoch.J2000, Coordinates.RAType.Hours),
                Name = name,
                Rotation = rotation
            };
        }

        private BitmapSource LoadJpg(string filename) {
            if (!Path.IsPathRooted(filename)) {
                filename = Path.Combine(userCachePath, filename);
            }

            JpegBitmapDecoder JpgDec = new JpegBitmapDecoder(new Uri(filename), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);

            return ConvertTo96Dpi(JpgDec.Frames[0]);
        }

        private BitmapSource ConvertTo96Dpi(BitmapSource image) {
            if (image.DpiX != 96) {
                byte[] pixelData = new byte[image.PixelWidth * 4 * image.PixelHeight];
                image.CopyPixels(pixelData, image.PixelWidth * 4, 0);

                return BitmapSource.Create(image.PixelWidth, image.PixelHeight, 96, 96, image.Format, null, pixelData,
                    image.PixelWidth * 4);
            }

            return image;
        }

        /// <summary>
        /// Names of the subfolder caches that can be used for the offline sky map. Empty when not in multi cache mode
        /// </summary>
        public IReadOnlyList<string> GetSkyAtlasCacheSources() {
            if (!IsMultiCache) {
                return new List<string>();
            }
            return GetCacheSubfolders().Where(x => !string.Equals(x, UserCacheName, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        /// <summary>
        /// Selects the subfolder cache that is used for the offline sky map
        /// </summary>
        public void LoadSkyAtlasCacheSource(string source) {
            if (GetSkyAtlasCacheSources().Contains(source, StringComparer.OrdinalIgnoreCase)) {
                SelectedSkyAtlasCacheSource = source;
            }
        }

        /// <summary>
        /// Directory of the cache that is used for the offline sky map
        /// </summary>
        public string SkyAtlasCachePath =>
            IsMultiCache && SelectedSkyAtlasCacheSource != null
                ? Path.Combine(framingAssistantCachePath, SelectedSkyAtlasCacheSource)
                : framingAssistantCachePath;

        /// <summary>
        /// Cache info of the cache that is used for the offline sky map.
        /// This is the single root cache, or the selected subfolder cache in multi cache mode
        /// </summary>
        public XElement SkyAtlasCache {
            get {
                if (!IsMultiCache) {
                    return Cache;
                }
                if (SelectedSkyAtlasCacheSource == null) {
                    return new XElement("ImageCacheInfo");
                }
                lock (skyAtlasCaches) {
                    if (!skyAtlasCaches.TryGetValue(SelectedSkyAtlasCacheSource, out var cache)) {
                        try {
                            cache = LoadCacheInfo(Path.Combine(SkyAtlasCachePath, CacheInfoFileName));
                        } catch (Exception ex) {
                            Logger.Error($"Error loading sky atlas cache {SelectedSkyAtlasCacheSource}", ex);
                            cache = new XElement("ImageCacheInfo");
                        }
                        skyAtlasCaches[SelectedSkyAtlasCacheSource] = cache;
                    }
                    return cache;
                }
            }
        }
    }
}