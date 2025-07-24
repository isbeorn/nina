#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using CommunityToolkit.Mvvm.ComponentModel;
using Grpc.Core;
using Microsoft.Win32;
using Newtonsoft.Json;
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Model;
using NINA.Equipment.Utility;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using NINA.Image.RawConverter;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.SkySurvey;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Media3D;

namespace NINA.WPF.Base.Model.Equipment.MyCamera.Simulator {

    public class SimulatorCamera : BaseINPC, ICamera, ITelescopeConsumer {

        public SimulatorCamera(IProfileService profileService, ITelescopeMediator telescopeMediator, IExposureDataFactory exposureDataFactory, IImageDataFactory imageDataFactory) {
            this.profileService = profileService;
            this.telescopeMediator = telescopeMediator;
            this.exposureDataFactory = exposureDataFactory;
            this.imageDataFactory = imageDataFactory;

            LoadImageCommand = new AsyncCommand<bool>(() => LoadImageDialog()/*, (object o) => Settings.ImageSettings.RAWImageStream == null*/);
            LoadDirectoryCommand = new RelayCommand((object o) => LoadDirectoryDialog());
            //UnloadImageCommand = new RelayCommand((object o) => Settings.ImageSettings.Image = null);
            //LoadRAWImageCommand = new AsyncCommand<bool>(() => LoadRAWImage(), (object o) => Settings.ImageSettings.Image == null);
            //UnloadRAWImageCommand = new RelayCommand((object o) => { Settings.ImageSettings.RAWImageStream.Dispose(); Settings.ImageSettings.RAWImageStream = null; });

            var p = new NINA.Profile.PluginOptionsAccessor(profileService, Guid.Parse("65CA9FD7-8984-4609-B206-86F3F9E8C19D"));
            settings = new Settings(p);
        }

        private Settings settings;

        public Settings Settings {
            get => settings;
            set {
                settings = value;
                RaisePropertyChanged();
            }
        }

        public string Category { get; } = "N.I.N.A.";

        public bool HasShutter => false;

        public bool Connected { get; private set; }

        public double CCDTemperature => SimulatorImage?.RawImageData?.MetaData?.Camera.Temperature ?? double.NaN;

        public double SetCCDTemperature {
            get => SimulatorImage?.RawImageData?.MetaData?.Camera.Temperature ?? double.NaN;

            set {
            }
        }

        public short BinX {
            get => (short)(SimulatorImage?.RawImageData?.MetaData?.Camera?.BinX ?? 1);

            set {
            }
        }

        public short BinY {
            get => (short)(SimulatorImage?.RawImageData?.MetaData?.Camera?.BinY ?? 1);

            set {
            }
        }

        public string Description => "A basic simulator to generate random noise for a specific median or load in an image that will be displayed on capture";

        public string DriverInfo => string.Empty;

        public string DriverVersion => CoreUtil.Version;

        public string SensorName => "Simulated Sensor";

        public SensorType SensorType => SimulatorImage?.RawImageData?.MetaData?.Camera.SensorType ?? SensorType.Monochrome;

        public short BayerOffsetX => 0;

        public short BayerOffsetY => 0;

        public int CameraXSize => SimulatorImage?.RawImageData?.Properties?.Width ?? Settings.RandomSettings.ImageWidth;

        public int CameraYSize => SimulatorImage?.RawImageData?.Properties?.Height ?? Settings.RandomSettings.ImageHeight;

        public double ExposureMin => 0;

        public double ExposureMax => double.MaxValue;

        public double ElectronsPerADU => double.NaN;

        public short MaxBinX => 1;

        public short MaxBinY => 1;

        public double PixelSizeX => SimulatorImage?.RawImageData?.MetaData?.Camera?.PixelSize ?? profileService.ActiveProfile.CameraSettings.PixelSize;

        public double PixelSizeY => SimulatorImage?.RawImageData?.MetaData?.Camera?.PixelSize ?? profileService.ActiveProfile.CameraSettings.PixelSize;

        public string LensName => profileService.ActiveProfile.TelescopeSettings.Name;

        public bool CanSetCCDTemperature => false;

        public bool CoolerOn {
            get => false;

            set {
            }
        }

        public double CoolerPower => double.NaN;

        public CameraStates CameraState => CameraStates.NoState;

        private int offset;
        public int Offset {
            get => offset;
            set {
                offset = value;
                RaisePropertyChanged();
            }
        }

        private int usbLimit; 
        public int USBLimit {
            get => usbLimit;

            set {
                usbLimit = value;
                RaisePropertyChanged();
            }
        }

        public int USBLimitMax => 100;
        public int USBLimitMin => 0;
        public int USBLimitStep => 1;

        public bool CanSetOffset => true;

        public int OffsetMin => 0;

        public int OffsetMax => 1000;

        public bool CanSetUSBLimit => true;

        public bool CanGetGain => true;

        public bool CanSetGain => true;

        public int GainMax => 10000;

        public int GainMin => 0;

        private int gain;
        public int Gain {
            get => gain;

            set {
                gain = value;
                RaisePropertyChanged();
            }
        }

        public IList<int> Gains => new List<int>();

        private AsyncObservableCollection<BinningMode> binningModes;

        public AsyncObservableCollection<BinningMode> BinningModes {
            get {
                if (binningModes == null) {
                    binningModes = new AsyncObservableCollection<BinningMode> {
                        new BinningMode(1,1)
                    };
                }
                return binningModes;
            }
        }

        public bool HasSetupDialog => true;

        public string Id => "4C0BBF74-0D95-41F6-AAD8-D6D58668CF2C";

        public string Name => "N.I.N.A. Simulator Camera";

        public string DisplayName {
            get {
                string cameraName = Name;

                if (SimulatorImage?.RawImageData?.MetaData?.Camera.Name.Length > 0) {
                    cameraName = cameraName + " (" + SimulatorImage.RawImageData.MetaData.Camera.Name + ")";
                }

                return cameraName;
            }
        }

        public double Temperature => SimulatorImage?.RawImageData?.MetaData?.Camera.Temperature ?? double.NaN;

        public double TemperatureSetPoint {
            get => double.NaN;

            set => throw new NotImplementedException();
        }

        public bool CanSetTemperature => false;

        public bool CanSubSample => true;

        private bool _enableSubSample;
        public bool EnableSubSample {
            get => _enableSubSample;
            set {
                _enableSubSample = value;
                RaisePropertyChanged();
            }
        }

        // Extra bool because the EnableSubSample get's turned off again in CameraVm before images are downloaded
        private bool _enabledSubSampleDuringImaging;

        private int _subSampleX;
        public int SubSampleX {
            get => _subSampleX;
            set {
                _subSampleX = value;
                RaisePropertyChanged();
            }
        }

        private int _subSampleY;
        public int SubSampleY {
            get => _subSampleY;
            set {
                _subSampleY = value;
                RaisePropertyChanged();
            }
        }

        private int _subSampleWidth;
        public int SubSampleWidth {
            get => _subSampleWidth;
            set {
                _subSampleWidth = value;
                RaisePropertyChanged();
            }
        }

        private int _subSampleHeight;
        public int SubSampleHeight {
            get => _subSampleHeight;
            set {
                _subSampleHeight = value;
                RaisePropertyChanged();
            }
        }

        public bool CanShowLiveView => true;

        private bool _liveViewEnabled;

        public bool LiveViewEnabled {
            get => _liveViewEnabled;
            set {
                _liveViewEnabled = value;
                RaisePropertyChanged();
            }
        }

        public bool HasDewHeater => true;

        private bool dewHeaterOn;
        public bool DewHeaterOn {
            get => dewHeaterOn;

            set {
                dewHeaterOn = value;
                RaisePropertyChanged();
            }
        }

        public bool HasBattery => false;

        public int BatteryLevel => -1;

        public int BitDepth => (int)profileService.ActiveProfile.CameraSettings.BitDepth;

        public IList<string> ReadoutModes => new List<string> { "Default" };

        public short ReadoutMode {
            get => 0;
            set { }
        }

        public short ReadoutModeForSnapImages {
            get => 0;

            set {
            }
        }

        public short ReadoutModeForNormalImages {
            get => 0;

            set {
            }
        }

        public void AbortExposure() {
        }

        public async Task<bool> Connect(CancellationToken token) {
            return await Task.Run(async () => {
                telescopeMediator.RegisterConsumer(this);
                Connected = true;
                try {
                    // Trigger a download to ensure the the properties from the previously saved image settings are intialized
                    if(Settings.Type != CameraType.SKYSURVEY) { 
                        _ = await this.DownloadExposure(token);
                    }
                    currentFile = 0;
                } catch (Exception e) {
                    Logger.Error("Failed to download image on connect", e);
                    return false;
                }

                return true;
            }, token);
        }

        public void Disconnect() {
            this.telescopeMediator.RemoveConsumer(this);
            this.SimulatorImage = null;
            Connected = false;
        }

        public async Task WaitUntilExposureIsReady(CancellationToken token) {
            using (token.Register(() => AbortExposure())) {
                var remaining = exposureTime - (DateTime.Now - exposureStart);
                if (remaining > TimeSpan.Zero) {
                    await Task.Delay(remaining, token);
                }
            }
        }

        public async Task<IExposureData> DownloadExposure(CancellationToken token) {
            switch (Settings.Type) {
                case CameraType.RANDOM:
                    int width, height, mean, stdev;

                    width = Settings.RandomSettings.ImageWidth;
                    height = Settings.RandomSettings.ImageHeight;
                    mean = Settings.RandomSettings.ImageMean;
                    stdev = Settings.RandomSettings.ImageStdDev;

                    ushort[] input = new ushort[width * height];

                    Random rand = new Random();
                    for (int i = 0; i < width * height; i++) {
                        double u1 = 1.0 - rand.NextDouble(); //uniform(0,1] random doubles
                        double u2 = 1.0 - rand.NextDouble();
                        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                                     Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
                        double randNormal = mean + stdev * randStdNormal; //random normal(mean,stdDev^2)
                        input[i] = (ushort)randNormal;
                    }

                    var metaData = new ImageMetaData();
                    metaData.FromCamera(this);
                    return exposureDataFactory.CreateImageArrayExposureData(
                        input: input,
                        width: width,
                        height: height,
                        bitDepth: this.BitDepth,
                        isBayered: false,
                        metaData: metaData);

                case CameraType.IMAGE:
                    if (SimulatorImage == null && !string.IsNullOrWhiteSpace(Settings.ImageSettings.ImagePath)) {
                        await LoadImage(Settings.ImageSettings.ImagePath);
                    }

                    if (SimulatorImage != null) {
                        return exposureDataFactory.CreateCachedExposureData(SimulatorImage.RawImageData);
                    }

                    //if (Settings.ImageSettings.RAWImageStream != null) {
                    //    byte[] rawBytes = Settings.ImageSettings.RAWImageStream.ToArray();
                    //    Settings.ImageSettings.RAWImageStream.Position = 0;
                    //    return exposureDataFactory.CreateRAWExposureData(
                    //        converter: profileService.ActiveProfile.CameraSettings.RawConverter,
                    //        rawBytes: rawBytes,
                    //        rawType: Settings.ImageSettings.RawType,
                    //        bitDepth: this.BitDepth,
                    //        metaData: new ImageMetaData());
                    //}
                    throw new Exception("No Image source set in Simulator!");

                case CameraType.SKYSURVEY:
                    if (!telescopeInfo.Connected) {
                        throw new Exception("Telescope is not connected to get reference coordinates for Simulator Camera Image");
                    }

                    var survey = new ESOSkySurvey();
                    var initial = telescopeInfo.Coordinates.Transform(Astrometry.Epoch.J2000);

                    var coordinates = new Coordinates(Angle.ByDegree(initial.RADegrees + AstroUtil.ArcsecToDegree(Settings.SkySurveySettings.RAError)), Angle.ByDegree(initial.Dec + AstroUtil.ArcsecToDegree(Settings.SkySurveySettings.DecError)), Epoch.J2000);

                    var altAz = coordinates.Transform(Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Latitude), Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Longitude), profileService.ActiveProfile.AstrometrySettings.Elevation);

                    if (Settings.SkySurveySettings.AzShift != 0 || Settings.SkySurveySettings.AltShift != 0) {
                        coordinates = new TopocentricCoordinates(
                            azimuth: altAz.Azimuth + Angle.ByDegree(Settings.SkySurveySettings.AzShift / 60d / 60d),
                            altitude: altAz.Altitude + Angle.ByDegree(Settings.SkySurveySettings.AltShift / 60d / 60d),
                            latitude: altAz.Latitude, 
                            longitude: altAz.Longitude,
                            elevation: altAz.Elevation
                            ).Transform(Epoch.J2000);
                    }

                    if (!ImageCache.TryGetValue(coordinates.ToString(), out var image)) {
                        image = await survey.GetImage(string.Empty, coordinates, AstroUtil.DegreeToArcmin(settings.SkySurveySettings.FieldOfView), 2500, 2500, token, default);
                        ImageCache[coordinates.ToString()] = image;
                    }
                    var data = await exposureDataFactory.CreateImageArrayExposureDataFromBitmapSource(image.Image);

                    var arcsecPerPix = image.Image.PixelWidth / AstroUtil.ArcminToArcsec(image.FoVWidth);
                    // Assume a fixed pixel size
                    var pixelSize = 3.76;
                    var factor = AstroUtil.DegreeToArcsec(AstroUtil.ToDegree(1)) / 1000d;
                    // Calculate focal length based on assumed pixel size and result image
                    var focalLength = (factor * pixelSize) / arcsecPerPix;

                    profileService.ActiveProfile.CameraSettings.PixelSize = 3.8;
                    profileService.ActiveProfile.TelescopeSettings.FocalLength = (int)focalLength;

                    return data;

                case CameraType.DIRECTORY:
                    if (files == null || files.Length == 0) {
                        files = Directory.GetFiles(settings.DirectorySettings.DirectoryPath, "*", SearchOption.AllDirectories).Where(BaseImageData.FileIsSupported).ToArray();
                        if (files.Length == 0) {
                            throw new Exception("No Image found in directory set in Simulator!");
                        }
                    }

                    if (currentFile >= files.Length) {
                        currentFile = 0;
                    }

                    try {
                        await LoadImage(files[currentFile]);
                    } finally {
                        currentFile++;
                    }

                    if (SimulatorImage != null) {
                        return exposureDataFactory.CreateCachedExposureData(SimulatorImage.RawImageData);
                    }

                    throw new Exception("No Image found in directory set in Simulator!");
            }
            throw new NotSupportedException();
        }

        private Dictionary<string, SkySurveyImage> ImageCache = new Dictionary<string, SkySurveyImage>();

        private IProfileService profileService;
        private ITelescopeMediator telescopeMediator;
        private readonly IExposureDataFactory exposureDataFactory;
        private readonly IImageDataFactory imageDataFactory;

        public void SetBinning(short x, short y) {
        }

        private IWindowService windowService;

        public IWindowService WindowService {
            get {
                if (windowService == null) {
                    windowService = new WindowService();
                }
                return windowService;
            }
            set => windowService = value;
        }

        public void SetupDialog() {
            WindowService.Show(this, "Simulator Setup", System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.ToolWindow);
        }

        private async Task<bool> LoadImageDialog() {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Title = "Load Image";
            dialog.FileName = "Image";
            dialog.DefaultExt = ".tiff";

            if (dialog.ShowDialog() == true) {
                settings.ImageSettings.ImagePath = dialog.FileName;
                await LoadImage(dialog.FileName);

                return true;
            }
            return false;
        }

        private async Task LoadImage(string path) {
            if(File.Exists(path)) { 
                var rawData = await imageDataFactory.CreateFromFile(path, BitDepth, Settings.ImageSettings.IsBayered, profileService.ActiveProfile.CameraSettings.RawConverter);
                // Check if we need to crop the image
                if (_enabledSubSampleDuringImaging && SubSampleWidth != rawData.Properties.Width && SubSampleHeight != rawData.Properties.Height) {
                    var roiImage = ExtractROI(rawData.Data.FlatArray, rawData.Properties.Width);
                    rawData = imageDataFactory.CreateBaseImageData(roiImage, SubSampleWidth, SubSampleHeight, 16, rawData.Properties.IsBayered, rawData.MetaData);
                }
                this.SimulatorImage = rawData.RenderImage();

                if (SimulatorImage.RawImageData.MetaData?.Camera.SensorType != SensorType.Monochrome) {
                    Settings.ImageSettings.IsBayered = true;
                }
            }
        }

        private bool LoadDirectoryDialog() {
            if (string.IsNullOrWhiteSpace(settings.DirectorySettings.DirectoryPath)) {
                settings.DirectorySettings.DirectoryPath = Path.GetDirectoryName(profileService.ActiveProfile.ImageFileSettings.FilePath);
            }
            OpenFolderDialog dialog = new OpenFolderDialog();
            dialog.Title = "Load Image Directory";
            if(Directory.Exists(settings.DirectorySettings.DirectoryPath)) {
                dialog.InitialDirectory = settings.DirectorySettings.DirectoryPath;
            }
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.FolderName)) {
                settings.DirectorySettings.DirectoryPath = dialog.FolderName;
                files = Directory.GetFiles(dialog.FolderName).Where(BaseImageData.FileIsSupported).ToArray();
                currentFile = 0;
                return true;
            }
            return false;
        }

        private ushort[] ExtractROI(ushort[] fullImage, int fullImageWidth) {

            int startX = (int)SubSampleX;
            int startY = (int)SubSampleY;
            int endX = startX + (int)SubSampleWidth;
            int endY = startY + (int)SubSampleHeight;

            ushort[] roi = new ushort[(int)SubSampleWidth * (int)SubSampleHeight];

            for (int y = 0; y < (int)SubSampleHeight; y++) {
                for (int x = 0; x < (int)SubSampleWidth; x++) {
                    int fullIndex = (startY + y) * fullImageWidth + (startX + x);
                    int roiIndex = y * (int)SubSampleWidth + x;
                    roi[roiIndex] = fullImage[fullIndex];
                }
            }

            return roi;
        }

        public IAsyncCommand LoadImageCommand { get; private set; }
        public ICommand LoadDirectoryCommand { get; private set; }
        public ICommand UnloadImageCommand { get; private set; }
        public IAsyncCommand LoadRAWImageCommand { get; private set; }
        public ICommand UnloadRAWImageCommand { get; private set; }
        public IRenderedImage SimulatorImage { get; private set; }

        private TelescopeInfo telescopeInfo;

        private DateTime exposureStart;
        private TimeSpan exposureTime;

        private string[] files;
        private int currentFile;

        public void StartExposure(CaptureSequence captureSequence) {
            exposureStart = DateTime.Now;
            exposureTime = TimeSpan.FromSeconds(captureSequence.ExposureTime);
            _enabledSubSampleDuringImaging = EnableSubSample;
        }

        public void StopExposure() {
        }

        public void UpdateValues() {
        }

        public void StartLiveView(CaptureSequence sequence) {
            LiveViewEnabled = true;
            _enabledSubSampleDuringImaging = EnableSubSample;
        }

        public Task<IExposureData> DownloadLiveView(CancellationToken token) {
            return DownloadExposure(token);
        }

        public void StopLiveView() {
            LiveViewEnabled = false;
        }

        public void UpdateDeviceInfo(TelescopeInfo deviceInfo) {
            this.telescopeInfo = deviceInfo;
        }

        public void Dispose() {
            this.telescopeMediator.RemoveConsumer(this);
        }

        public IList<string> SupportedActions => new List<string>();

        public string Action(string actionName, string actionParameters) {
            throw new NotImplementedException();
        }

        public string SendCommandString(string command, bool raw) {
            throw new NotImplementedException();
        }

        public bool SendCommandBool(string command, bool raw) {
            throw new NotImplementedException();
        }

        public void SendCommandBlind(string command, bool raw) {
            throw new NotImplementedException();
        }

        public void UpdateSubSampleArea() {
            _enabledSubSampleDuringImaging = EnableSubSample;
        }
    }
}