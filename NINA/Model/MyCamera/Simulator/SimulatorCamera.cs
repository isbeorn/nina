#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using NINA.Utility;
using NINA.Profile;
using NINA.Utility.RawConverter;
using NINA.Utility.WindowService;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using NINA.Model.ImageData;
using NINA.Utility.Mediator.Interfaces;
using NINA.Model.MyTelescope;
using NINA.Utility.SkySurvey;
using NINA.Utility.Astrometry;

namespace NINA.Model.MyCamera.Simulator {

    public class SimulatorCamera : BaseINPC, ICamera, ITelescopeConsumer {

        public SimulatorCamera(IProfileService profileService, ITelescopeMediator telescopeMediator) {
            this.profileService = profileService;
            this.telescopeMediator = telescopeMediator;

            LoadImageCommand = new AsyncCommand<bool>(() => LoadImage(), (object o) => Settings.ImageSettings.RAWImageStream == null);
            UnloadImageCommand = new RelayCommand((object o) => Settings.ImageSettings.Image = null);
            LoadRAWImageCommand = new AsyncCommand<bool>(() => LoadRAWImage(), (object o) => Settings.ImageSettings.Image == null);
            UnloadRAWImageCommand = new RelayCommand((object o) => { Settings.ImageSettings.RAWImageStream.Dispose(); Settings.ImageSettings.RAWImageStream = null; });
        }

        private Settings settings = new Settings();

        public Settings Settings {
            get => settings;
            set {
                settings = value;
                RaisePropertyChanged();
            }
        }

        public string Category { get; } = "N.I.N.A.";

        public bool HasShutter {
            get {
                return false;
            }
        }

        public bool Connected { get; private set; }

        public double CCDTemperature {
            get {
                return double.NaN;
            }
        }

        public double SetCCDTemperature {
            get {
                return double.NaN;
            }

            set {
            }
        }

        public short BinX {
            get {
                return -1;
            }

            set {
            }
        }

        public short BinY {
            get {
                return -1;
            }

            set {
            }
        }

        public string Description {
            get {
                return "A basic simulator to generate random noise for a specific median or load in an image that will be displayed on capture";
            }
        }

        public string DriverInfo {
            get {
                return string.Empty;
            }
        }

        public string DriverVersion {
            get {
                return Utility.Utility.Version;
            }
        }

        public string SensorName {
            get {
                return "Simulated Sensor";
            }
        }

        public SensorType SensorType {
            get {
                return SensorType.Monochrome;
            }
        }

        public int CameraXSize {
            get {
                return Settings.ImageSettings.Image?.RawImageData?.Properties?.Width ?? Settings.RandomSettings.ImageWidth;
            }
        }

        public int CameraYSize {
            get {
                return Settings.ImageSettings.Image?.RawImageData?.Properties?.Height ?? Settings.RandomSettings.ImageHeight;
            }
        }

        public double ExposureMin {
            get {
                return 0;
            }
        }

        public double ExposureMax {
            get {
                return double.MaxValue;
            }
        }

        public double ElectronsPerADU => double.NaN;

        public short MaxBinX {
            get {
                return 1;
            }
        }

        public short MaxBinY {
            get {
                return 1;
            }
        }

        public double PixelSizeX {
            get {
                return 3.8;
            }
        }

        public double PixelSizeY {
            get {
                return 3.8;
            }
        }

        public bool CanSetCCDTemperature {
            get {
                return false;
            }
        }

        public bool CoolerOn {
            get {
                return false;
            }

            set {
            }
        }

        public double CoolerPower {
            get {
                return double.NaN;
            }
        }

        public string CameraState {
            get {
                return "TestState";
            }
        }

        public int Offset {
            get {
                return -1;
            }

            set {
            }
        }

        public int USBLimit {
            get {
                return -1;
            }

            set {
            }
        }

        public bool CanSetOffset {
            get {
                return false;
            }
        }

        public int OffsetMin {
            get {
                return 0;
            }
        }

        public int OffsetMax {
            get {
                return 0;
            }
        }

        public bool CanSetUSBLimit {
            get {
                return false;
            }
        }

        public bool CanGetGain {
            get {
                return false;
            }
        }

        public bool CanSetGain {
            get {
                return false;
            }
        }

        public int GainMax {
            get {
                return -1;
            }
        }

        public int GainMin {
            get {
                return -1;
            }
        }

        public int Gain {
            get {
                return -1;
            }

            set {
            }
        }

        public ArrayList Gains {
            get {
                return null;
            }
        }

        public AsyncObservableCollection<BinningMode> BinningModes {
            get {
                return null;
            }
        }

        public bool HasSetupDialog {
            get {
                return true;
            }
        }

        public string Id {
            get {
                return "4C0BBF74-0D95-41F6-AAD8-D6D58668CF2C";
            }
        }

        public string Name {
            get {
                return "N.I.N.A. Simulator Camera";
            }
        }

        public double Temperature {
            get {
                return double.NaN;
            }
        }

        public double TemperatureSetPoint {
            get {
                return double.NaN;
            }

            set {
                throw new NotImplementedException();
            }
        }

        public bool CanSetTemperature {
            get {
                return false;
            }
        }

        public bool CanSubSample {
            get {
                return false;
            }
        }

        public bool EnableSubSample {
            get {
                return false;
            }

            set {
            }
        }

        public int SubSampleX { get; set; }

        public int SubSampleY { get; set; }

        public int SubSampleWidth { get; set; }

        public int SubSampleHeight { get; set; }

        public bool CanShowLiveView {
            get {
                return true;
            }
        }

        private bool _liveViewEnabled;

        public bool LiveViewEnabled {
            get {
                return _liveViewEnabled;
            }
            set {
                _liveViewEnabled = value;
                RaisePropertyChanged();
            }
        }

        public bool HasDewHeater {
            get {
                return false;
            }
        }

        public bool DewHeaterOn {
            get {
                return false;
            }

            set {
            }
        }

        public bool HasBattery {
            get {
                return false;
            }
        }

        public int BatteryLevel {
            get {
                return -1;
            }
        }

        public int BitDepth {
            get {
                return (int)profileService.ActiveProfile.CameraSettings.BitDepth;
            }
        }

        public ICollection ReadoutModes {
            get {
                return new List<string>() { "Default" };
            }
        }

        public short ReadoutModeForSnapImages {
            get {
                return 0;
            }

            set {
            }
        }

        public short ReadoutModeForNormalImages {
            get {
                return 0;
            }

            set {
            }
        }

        public void AbortExposure() {
        }

        public async Task<bool> Connect(CancellationToken token) {
            this.telescopeMediator.RegisterConsumer(this);
            Connected = true;
            return true;
        }

        public void Disconnect() {
            this.telescopeMediator.RemoveConsumer(this);
            Connected = false;
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

                    return new ImageArrayExposureData(
                        input: input,
                        width: width,
                        height: height,
                        bitDepth: this.BitDepth,
                        isBayered: false,
                        metaData: new ImageMetaData());

                case CameraType.IMAGE:
                    if (Settings.ImageSettings.Image != null) {
                        return new CachedExposureData(Settings.ImageSettings.Image.RawImageData);
                    }

                    if (Settings.ImageSettings.RAWImageStream != null) {
                        byte[] rawBytes = Settings.ImageSettings.RAWImageStream.ToArray();
                        Settings.ImageSettings.RAWImageStream.Position = 0;
                        var rawConverter = RawConverter.CreateInstance(profileService.ActiveProfile.CameraSettings.RawConverter);
                        return new RAWExposureData(
                            rawConverter: rawConverter,
                            rawBytes: rawBytes,
                            rawType: Settings.ImageSettings.RawType,
                            bitDepth: this.BitDepth,
                            metaData: new ImageMetaData());
                    }
                    throw new Exception("No Image source set in Simulator!");

                case CameraType.SKYSURVEY:
                    if (!telescopeInfo.Connected) {
                        throw new Exception("Telescope is not connected to get reference coordinates for Simulator Camera Image");
                    }

                    var survey = new ESOSkySurvey();
                    var initial = telescopeInfo.Coordinates.Transform(Utility.Astrometry.Epoch.J2000);

                    var coordinates = new Coordinates(Angle.ByDegree(initial.RADegrees + Astrometry.ArcsecToDegree(Settings.SkySurveySettings.RAError)), Angle.ByDegree(initial.Dec + Astrometry.ArcsecToDegree(Settings.SkySurveySettings.DecError)), Epoch.J2000);

                    var image = await survey.GetImage(string.Empty, coordinates, Astrometry.DegreeToArcmin(1), Settings.SkySurveySettings.WidthAndHeight, Settings.SkySurveySettings.WidthAndHeight, token, default);
                    var data = await ImageArrayExposureData.FromBitmapSource(image.Image);

                    var arcsecPerPix = image.Image.PixelWidth / Astrometry.ArcminToArcsec(image.FoVWidth);
                    // Assume a fixed pixel Size of 3
                    var pixelSize = 3;
                    var factor = Astrometry.DegreeToArcsec(Astrometry.ToDegree(1)) / 1000d;
                    // Calculate focal length based on assumed pixel size and result image
                    var focalLength = (factor * pixelSize) / arcsecPerPix;

                    profileService.ActiveProfile.CameraSettings.PixelSize = 3;
                    profileService.ActiveProfile.TelescopeSettings.FocalLength = (int)focalLength;

                    return data;
            }
            throw new NotSupportedException();
        }

        private IProfileService profileService;
        private ITelescopeMediator telescopeMediator;

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
            set {
                windowService = value;
            }
        }

        public void SetupDialog() {
            WindowService.Show(this, "Simulator Setup", System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.ToolWindow);
        }

        private async Task<bool> LoadRAWImage() {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Title = "Load Image";
            dialog.FileName = "Image";
            dialog.DefaultExt = ".cr2";

            if (dialog.ShowDialog() == true) {
                Settings.ImageSettings.RawType = Path.GetExtension(dialog.FileName).TrimStart('.').ToLower();
                await Task.Run(() => {
                    using (var fileStream = File.OpenRead(dialog.FileName)) {
                        var memStream = new MemoryStream();
                        memStream.SetLength(fileStream.Length);
                        fileStream.Read(memStream.GetBuffer(), 0, (int)fileStream.Length);
                        memStream.Position = 0;
                        Settings.ImageSettings.RAWImageStream = memStream;
                    }
                });
            }
            return true;
        }

        private async Task<bool> LoadImage() {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Title = "Load Image";
            dialog.FileName = "Image";
            dialog.DefaultExt = ".tiff";

            if (dialog.ShowDialog() == true) {
                var rawData = await ImageData.ImageData.FromFile(dialog.FileName, BitDepth, Settings.ImageSettings.IsBayered, profileService.ActiveProfile.CameraSettings.RawConverter);
                Settings.ImageSettings.Image = rawData.RenderImage();
                return true;
            }
            return false;
        }

        public IAsyncCommand LoadImageCommand { get; private set; }
        public ICommand UnloadImageCommand { get; private set; }
        public IAsyncCommand LoadRAWImageCommand { get; private set; }
        public ICommand UnloadRAWImageCommand { get; private set; }

        private TelescopeInfo telescopeInfo;

        public void StartExposure(CaptureSequence captureSequence) {
        }

        public void StopExposure() {
        }

        public void UpdateValues() {
        }

        public void StartLiveView() {
            LiveViewEnabled = true;
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
    }
}