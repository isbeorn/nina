#region "copyright"

/*
    Copyright © 2016 - 2018 Stefan Berg <isbeorn86+NINA@googlemail.com>

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
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NINA.Model.MyCamera {

    public class SimulatorCamera : ICamera {

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
                return "NINA_SIM_Simulator";
            }
        }

        public string DriverInfo {
            get {
                return "NINA_SIM_DriverInfo";
            }
        }

        public string DriverVersion {
            get {
                return Utility.Utility.Version;
            }
        }

        public string SensorName {
            get {
                return "NINA_SIM_Sensor";
            }
        }

        public SensorType SensorType {
            get {
                return SensorType.Monochrome;
            }
        }

        public int CameraXSize {
            get {
                return -1;
            }
        }

        public int CameraYSize {
            get {
                return -1;
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
                return 1000;
            }
        }

        public double PixelSizeY {
            get {
                return 1000;
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
                return "NINA_SIM_State";
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

        public short GainMax {
            get {
                return -1;
            }
        }

        public short GainMin {
            get {
                return -1;
            }
        }

        public short Gain {
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
                return "NINA_SIM_Id";
            }
        }

        public string Name {
            get {
                return "NINA_SIM";
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

        public int SubSampleX {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public int SubSampleY {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public int SubSampleWidth {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public int SubSampleHeight {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public bool CanShowLiveView {
            get {
                return false;
            }
        }

        public bool LiveViewEnabled {
            get {
                return false;
            }

            set {
                throw new NotImplementedException();
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
                return 16;
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

        public event PropertyChangedEventHandler PropertyChanged;

        public void AbortExposure() {
            throw new NotImplementedException();
        }

        public async Task<bool> Connect(CancellationToken token) {
            Connected = true;
            return true;
        }

        public void Disconnect() {
            Connected = false;
        }

        public async Task<ImageArray> DownloadExposure(CancellationToken token, bool calculateStatistics) {
            if (_image != null) {
                return _image;
            }

            int width = 3;
            int height = 3;
            ushort[] input = new ushort[width * height];
            input = new ushort[9] { 3, 8, 8, 8, 8, 9, 9, 9, 9 };

            /*Random rand = new Random(); //reuse this if you are generating manydouble mean = 5000;
            double stdDev = 500;
            for (int i = 0; i < width * height; i++) {
                double u1 = 1.0 - rand.NextDouble(); //uniform(0,1] random doubles
                double u2 = 1.0 - rand.NextDouble();
                double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                             Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
                double randNormal = mean + stdDev * randStdNormal; //random normal(mean,stdDev^2)
                input[i] = (ushort)randNormal;
            }*/

            return await ImageArray.CreateInstance(input, width, height, 16, false, true, 100);
        }

        public void SetBinning(short x, short y) {
        }

        public void SetupDialog() {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Title = Locale.Loc.Instance["LblLoadSequence"];
            dialog.FileName = "Image";
            dialog.DefaultExt = ".tiff";

            if (dialog.ShowDialog() == true) {
                TiffBitmapDecoder TifDec = new TiffBitmapDecoder(new Uri(dialog.FileName), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                BitmapFrame bmp = TifDec.Frames[0];
                int stride = bmp.PixelWidth * ((bmp.Format.BitsPerPixel + 7) / 8);
                int arraySize = stride * bmp.PixelHeight;
                ushort[] pixels = new ushort[(int)(bmp.Width * bmp.Height)];
                bmp.CopyPixels(pixels, stride, 0);
                Task.Run(async () => {
                    _image = await ImageArray.CreateInstance(pixels, (int)bmp.Width, (int)bmp.Height, 16, false, true, 100);
                });
            }
        }

        private ImageArray _image;

        public void StartExposure(CaptureSequence captureSequence) {
        }

        public void StopExposure() {
        }

        public void UpdateValues() {
        }

        public void StartLiveView() {
            throw new NotImplementedException();
        }

        public Task<ImageArray> DownloadLiveView(CancellationToken token) {
            throw new NotImplementedException();
        }

        public void StopLiveView() {
            throw new NotImplementedException();
        }
    }
}