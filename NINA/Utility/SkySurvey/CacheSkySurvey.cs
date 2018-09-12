using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using NINA.Utility.Astrometry;

namespace NINA.Utility.SkySurvey {

    internal class CacheSkySurvey {
        private string framingAssistantCachePath;
        private string framingAssistantCachInfo;

        public CacheSkySurvey(string framingAssistantCachePath) {
            this.framingAssistantCachePath = framingAssistantCachePath;
            this.framingAssistantCachInfo = Path.Combine(framingAssistantCachePath, "CacheInfo.xml");
            Initialize();
        }

        private void Initialize() {
            if (!Directory.Exists(framingAssistantCachePath)) {
                Directory.CreateDirectory(framingAssistantCachePath);
            }

            if (!File.Exists(framingAssistantCachInfo)) {
                XElement info = new XElement("ImageCacheInfo");
                info.Save(framingAssistantCachInfo);
                Cache = info;
                return;
            } else {
                Cache = XElement.Load(framingAssistantCachInfo);
                var elements = Cache.Elements("Image").Where(x => x.Attribute("Id") == null);
                foreach (var element in elements) {
                    element.Add(new XAttribute("Id", Guid.NewGuid()));
                }
            }
        }

        public void Clear() {
            System.IO.DirectoryInfo di = new DirectoryInfo(framingAssistantCachePath);

            foreach (FileInfo file in di.GetFiles()) {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.GetDirectories()) {
                dir.Delete(true);
            }
            Initialize();
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
                        if (!Directory.Exists(framingAssistantCachePath)) {
                            Directory.CreateDirectory(framingAssistantCachePath);
                        }

                        var imgFilePath = Path.Combine(framingAssistantCachePath, skySurveyImage.Name + ".jpg");

                        imgFilePath = Utility.GetUniqueFilePath(imgFilePath);
                        var name = Path.GetFileNameWithoutExtension(imgFilePath);

                        using (var fileStream = new FileStream(imgFilePath, FileMode.Create)) {
                            var encoder = new JpegBitmapEncoder();
                            encoder.QualityLevel = 70;
                            encoder.Frames.Add(BitmapFrame.Create(skySurveyImage.Image));
                            encoder.Save(fileStream);
                        }

                        XElement xml = new XElement("Image",
                            new XAttribute("Id", skySurveyImage.Id),
                            new XAttribute("RA", skySurveyImage.Coordinates.RA.ToString("R", CultureInfo.InvariantCulture)),
                            new XAttribute("Dec", skySurveyImage.Coordinates.Dec.ToString("R", CultureInfo.InvariantCulture)),
                            new XAttribute("Rotation", skySurveyImage.Rotation),
                            new XAttribute("FoVW", skySurveyImage.FoVWidth.ToString("R", CultureInfo.InvariantCulture)),
                            new XAttribute("FoVH", skySurveyImage.FoVHeight.ToString("R", CultureInfo.InvariantCulture)),
                            new XAttribute("FileName", imgFilePath),
                            new XAttribute("Source", skySurveyImage.Source),
                            new XAttribute("Name", name)
                        );

                        Cache.Add(xml);
                        Cache.Save(framingAssistantCachInfo);
                        return xml;
                    }
                }
                return element;
            } catch (Exception ex) {
                Logger.Error(ex);
                throw ex;
            }
            return null;
        }

        public XElement Cache { get; private set; }

        public object CulutureInfo { get; private set; }

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
            JpegBitmapDecoder JpgDec = new JpegBitmapDecoder(new Uri(filename), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            return JpgDec.Frames[0];
        }
    }
}