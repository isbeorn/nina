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

using Newtonsoft.Json.Linq;
using NINA.Utility;
using NINA.Utility.Notification;
using NINA.Utility.Profile;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;

namespace NINA.Model.MyGuider {

    public class PHD2Guider : BaseINPC, IGuider {

        public PHD2Guider(IProfileService profileService) {
            this.profileService = profileService;
        }

        private IProfileService profileService;

        private Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;

        private PhdEventVersion _version;

        public PhdEventVersion Version {
            get {
                return _version;
            }
            set {
                _version = value;
                RaisePropertyChanged();
            }
        }

        private ImageSource _image;

        public ImageSource Image {
            get {
                return _image;
            }
            set {
                _image = value;
                RaisePropertyChanged();
            }
        }

        private PhdEventAppState _appState;

        public PhdEventAppState AppState {
            get {
                return _appState;
            }
            set {
                _appState = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(State));
            }
        }

        private PhdEventSettling _settling;

        public PhdEventSettling Settling {
            get {
                return _settling;
            }
            set {
                _settling = value;
                RaisePropertyChanged();
            }
        }

        private PhdEventSettleDone _settleDone;

        public PhdEventSettleDone SettleDone {
            get {
                return _settleDone;
            }
            set {
                _settleDone = value;
                RaisePropertyChanged();
            }
        }

        private PhdEventGuidingDithered _guidingDithered;

        public PhdEventGuidingDithered GuidingDithered {
            get {
                return _guidingDithered;
            }
            set {
                _guidingDithered = value;
                RaisePropertyChanged();
            }
        }

        private IGuideStep _prevGuideStep;

        public IGuideStep PrevGuideStep {
            get {
                return _prevGuideStep;
            }
            set {
                _prevGuideStep = value;
                RaisePropertyChanged();
            }
        }

        private IGuideStep _guideStep;

        public IGuideStep GuideStep {
            get {
                return _guideStep;
            }
            set {
                _guideStep = value;
                RaisePropertyChanged();
            }
        }

        private CancellationTokenSource _clientCTS;

        private static object lockobj = new object();

        private bool _connected;

        public bool Connected {
            get {
                return _connected;
            }
            private set {
                lock (lockobj) {
                    _connected = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool _isDithering;

        private double _pixelScale;

        public double PixelScale {
            get {
                return _pixelScale;
            }
            set {
                _pixelScale = value;
                RaisePropertyChanged();
            }
        }

        public string State {
            get {
                return AppState?.State ?? string.Empty;
            }
        }

        /*private async Task<TcpClient> ConnectClient() {
            var client = new TcpClient();
            await client.ConnectAsync(Settings.PHD2ServerUrl, Settings.PHD2ServerPort);
            return client;
        }*/
        private TaskCompletionSource<bool> _tcs;
        private bool startedPHD2;

        public async Task<bool> Connect() {
            bool connected = false;
            _tcs = new TaskCompletionSource<bool>();
            StartPHD2AndListener();
            connected = await _tcs.Task;

            if (startedPHD2 && connected) {
                await ConnectPHD2Equipment();
                await Task.Delay(5);
                await SendMessage(PHD2EventId.LOOP, PHD2Methods.LOOP);
            }

            var resp = await SendMessage(PHD2EventId.GET_PIXEL_SCALE, PHD2Methods.GET_PIXEL_SCALE);
            PixelScale = double.Parse(resp.result.ToString().Replace(",", "."), CultureInfo.InvariantCulture);

            Notification.ShowSuccess(Locale.Loc.Instance["LblGuiderConnected"]);

            return connected;
        }

        public async Task<bool> Dither(CancellationToken ct) {
            if (Connected) {
                _isDithering = true;
                var ditherMsg = await SendMessage(
                    PHD2EventId.DITHER,
                    string.Format(
                        PHD2Methods.DITHER,
                        profileService.ActiveProfile.GuiderSettings.DitherPixels.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                        profileService.ActiveProfile.GuiderSettings.DitherRAOnly.ToString().ToLower(),
                        profileService.ActiveProfile.GuiderSettings.SettlePixels.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                        profileService.ActiveProfile.GuiderSettings.SettleTime.ToString(),
                        profileService.ActiveProfile.GuiderSettings.SettleTimeout.ToString()
                    )
                );
                if (ditherMsg.error != null) {
                    /* Dither failed */
                    _isDithering = false;
                    return false;
                }
                await Task.Run<bool>(async () => {
                    var elapsed = new TimeSpan();
                    while (_isDithering == true) {
                        elapsed += await Utility.Utility.Delay(500, ct);

                        if (elapsed.TotalSeconds > 120) {
                            //Failsafe when phd is not sending settlingdone message
                            Notification.ShowWarning(Locale.Loc.Instance["LblGuiderNoSettleDone"]);
                            _isDithering = false;
                        }
                    }
                    return true;
                });
            }
            return true;
        }

        public async Task<bool> Pause(bool pause, CancellationToken ct) {
            if (Connected) {
                await SendMessage(PHD2EventId.PAUSE, string.Format(PHD2Methods.PAUSE, pause.ToString().ToLower()));

                if (pause) {
                    var elapsed = new TimeSpan();
                    while (!(AppState.State == PhdAppState.PAUSED)) {
                        elapsed += await Utility.Utility.Delay(500, ct);
                    }
                } else {
                    var elapsed = new TimeSpan();
                    while ((AppState.State == PhdAppState.PAUSED)) {
                        elapsed += await Utility.Utility.Delay(500, ct);
                        if (elapsed.TotalSeconds > 60) {
                            //Failsafe when phd is not sending resume message
                            Notification.ShowWarning(Locale.Loc.Instance["LblGuiderNoResume"]/*, ToastNotifications.NotificationsSource.NeverEndingNotification*/);
                            break;
                        }
                    }
                }
            }
            return true;
        }

        private void CheckPhdError(PhdMethodResponse m) {
            if (m.error != null) {
                Notification.ShowError("PHDError: " + m.error.message + "\n CODE: " + m.error.code);
                Logger.Warning("PHDError: " + m.error.message + " CODE: " + m.error.code);
            }
        }

        public async Task<bool> AutoSelectGuideStar() {
            if (Connected) {
                if (AppState.State != PhdAppState.LOOPING) {
                    await SendMessage(PHD2EventId.LOOP, PHD2Methods.LOOP);
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }

                await SendMessage(PHD2EventId.AUTO_SELECT_STAR, PHD2Methods.AUTO_SELECT_STAR);

                return true;
            }
            return false;
        }

        public async Task<bool> StartGuiding(CancellationToken ct) {
            if (Connected) {
                if (AppState.State == PhdAppState.GUIDING) { return true; }
                if (!(AppState.State == PhdAppState.CALIBRATING)) {
                    var guideMsg = await SendMessage(PHD2EventId.GUIDE, string.Format(PHD2Methods.GUIDE, false.ToString().ToLower()));
                    if (guideMsg.error != null) {
                        /* Guide start failed */
                        return false;
                    }
                }
                return await Task.Run<bool>(async () => {
                    while (AppState.State != PhdAppState.GUIDING) {
                        await Task.Delay(1000, ct);
                    }
                    return true;
                });
            } else {
                return false;
            }
        }

        public async Task<bool> StopGuiding(CancellationToken token) {
            if (Connected) {
                var stopCapture = await SendMessage(PHD2EventId.STOP_CAPTURE, PHD2Methods.STOP_CAPTURE);
                if (stopCapture.error != null) {
                    /*stop capture failed */
                    return false;
                }

                return await Task.Run<bool>(async () => {
                    while (AppState.State != PhdAppState.STOPPED) {
                        await Task.Delay(1000, token);
                    }
                    return true;
                });
            } else {
                return false;
            }
        }

        private async Task<PhdMethodResponse> SendMessage(string msgId, string msg) {
            using (var client = new TcpClient()) {
                try {
                    await client.ConnectAsync(profileService.ActiveProfile.GuiderSettings.PHD2ServerUrl, profileService.ActiveProfile.GuiderSettings.PHD2ServerPort);

                    var stream = client.GetStream();
                    var data = System.Text.Encoding.ASCII.GetBytes(msg);

                    await stream.WriteAsync(data, 0, data.Length);

                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8)) {
                        string line;
                        while ((line = reader.ReadLine()) != null) {
                            JObject o = JObject.Parse(line);
                            string phdevent = "";
                            var t = o.GetValue("id");
                            if (t != null) {
                                phdevent = t.ToString();
                            }

                            if (phdevent == msgId) {
                                var response = o.ToObject<PhdMethodResponse>();
                                CheckPhdError(response);
                                return response;
                            }
                        }
                    }
                } finally {
                }
            }
            return null;
        }

        public bool Disconnect() {
            _clientCTS?.Cancel();
            return false;
        }

        private void ProcessEvent(string phdevent, JObject message) {
            switch (phdevent) {
                case "Resumed": {
                        break;
                    }
                case "Version": {
                        Version = message.ToObject<PhdEventVersion>();
                        break;
                    }
                case "AppState": {
                        AppState = message.ToObject<PhdEventAppState>();
                        break;
                    }
                case "GuideStep": {
                        AppState = new PhdEventAppState() { State = "Guiding" };
                        PrevGuideStep = GuideStep;
                        GuideStep = message.ToObject<PhdEventGuideStep>();
                        break;
                    }
                case "GuidingDithered": {
                        SettleDone = null;
                        GuidingDithered = message.ToObject<PhdEventGuidingDithered>();
                        break;
                    }
                case "Settling": {
                        SettleDone = null;
                        Settling = message.ToObject<PhdEventSettling>();
                        break;
                    }
                case "SettleDone": {
                        GuidingDithered = null;
                        Settling = null;
                        _isDithering = false;
                        SettleDone = message.ToObject<PhdEventSettleDone>();
                        if (SettleDone.Error != null) {
                            Notification.ShowError("PHD2 Error: " + SettleDone.Error);
                        }
                        break;
                    }
                case "Paused": {
                        AppState = new PhdEventAppState() { State = "Paused" };
                        break;
                    }
                case "StartCalibration": {
                        AppState = new PhdEventAppState() { State = "Calibrating" };
                        break;
                    }
                case "LoopingExposures": {
                        AppState = new PhdEventAppState() { State = "Looping" };
                        break;
                    }
                case "LoopingExposuresStopped": {
                        AppState = new PhdEventAppState() { State = "Stopped" };
                        break;
                    }
                case "StarLost": {
                        AppState = new PhdEventAppState() { State = "LostLock" };
                        break;
                    }
                case "LockPositionLost": {
                        break;
                    }
                default: {
                        break;
                    }
            }
        }

        public static TcpState GetState(TcpClient tcpClient) {
            var foo = IPGlobalProperties.GetIPGlobalProperties()
              .GetActiveTcpConnections()
              .SingleOrDefault(x => x.LocalEndPoint.Equals(tcpClient.Client.LocalEndPoint));
            return foo != null ? foo.State : TcpState.Unknown;
        }

        private void StartPHD2AndListener() {
            Task.Run(() => {
                try {
                    startedPHD2 = StartPHD2Process();

                    Task.Run(RunListener);
                } catch (OperationCanceledException) { } catch (FileNotFoundException) {
                    Notification.ShowError("PHD2 path not found!");
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError("Something happened");
                }
            });
        }

        private async Task ConnectPHD2Equipment() {
            var connectMsg = await SendMessage(
                PHD2EventId.SET_CONNECTED,
                string.Format(PHD2Methods.SET_CONNECTED, "true"));
            if (connectMsg.error != null) {
                Notification.ShowWarning("Failed to connect to equipment");
            }
        }

        private bool StartPHD2Process() {
            // if phd2 is not running start it
            if (Process.GetProcessesByName("phd2").Length == 0) {
                if (!File.Exists(profileService.ActiveProfile.GuiderSettings.PHD2Path)) {
                    throw new FileNotFoundException();
                }
                Process.Start(profileService.ActiveProfile.GuiderSettings.PHD2Path);
                return true;
            }

            return false;
        }

        private async Task RunListener() {
            JsonLoadSettings jls = new JsonLoadSettings() { LineInfoHandling = LineInfoHandling.Ignore, CommentHandling = CommentHandling.Ignore };
            _clientCTS = new CancellationTokenSource();
            using (var client = new TcpClient()) {
                try {
                    await client.ConnectAsync(profileService.ActiveProfile.GuiderSettings.PHD2ServerUrl,
                        profileService.ActiveProfile.GuiderSettings.PHD2ServerPort);
                    Connected = true;
                    _tcs.TrySetResult(true);

                    using (NetworkStream s = client.GetStream()) {
                        while (true) {
                            var state = GetState(client);
                            if (state == TcpState.CloseWait) {
                                throw new Exception(Locale.Loc.Instance["LblPhd2ServerConnectionLost"]);
                            }

                            var message = string.Empty;
                            while (s.DataAvailable) {
                                byte[] response = new byte[1024];
                                await s.ReadAsync(response, 0, response.Length, _clientCTS.Token);
                                message += System.Text.Encoding.ASCII.GetString(response);
                            }

                            foreach (string line in message.Split(new[] { Environment.NewLine },
                                StringSplitOptions.None)) {
                                if (!string.IsNullOrEmpty(line) && !line.StartsWith("\0")) {
                                    JObject o = JObject.Parse(line, jls);
                                    JToken t = o.GetValue("Event");
                                    string phdevent = "";
                                    if (t != null) {
                                        phdevent = t.ToString();
                                        ProcessEvent(phdevent, o);
                                    }
                                }
                            }

                            await Task.Delay(TimeSpan.FromMilliseconds(500), _clientCTS.Token);
                        }
                    }
                } catch (OperationCanceledException) {
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError("PHD2 Error: " + ex.Message);
                    throw;
                } finally {
                    _isDithering = false;
                    AppState = new PhdEventAppState() { State = "" };
                    PixelScale = 0.0d;
                    Connected = false;
                    _tcs.TrySetResult(false);
                }
            }
        }

        public class PhdMethodResponse {
            public string jsonrpc;
            public object result;
            public PhdError error;
            public int id;
        }

        public class PhdImageResult {
            public int frame;
            public int width;
            public int height;
            public double[] star_pos;
            public string pixels;
        }

        public class PhdError {
            public int code;
            public string message;
        }

        public class PhdEvent : BaseINPC, IGuideEvent {
            public string Event { get; set; }
            public string TimeStamp { get; set; }
            public string Host { get; set; }
            public int Inst { get; set; }
        }

        public class PhdEventVersion : PhdEvent {
            public string PHDVersion;
            public string PHDSubver;
            public int MsgVersion;
        }

        public class PhdEventLockPositionSet : PhdEvent {
            public int X;
            public int Y;
        }

        public class PhdEventCalibrationComplete : PhdEvent {
            public string Mount;
        }

        public class PhdEventStarSelected : PhdEvent {
            public int X;
            public int Y;
        }

        public class PhdEventStartGuiding : PhdEvent {
        }

        public class PhdEventPaused : PhdEvent {
        }

        public class PhdEventStartCalibration : PhdEvent {
            public string Mount;
        }

        public class PhdEventAppState : PhdEvent, IGuiderAppState {
            private string state;

            public string State {
                get {
                    return state;
                }

                set {
                    state = value;
                    RaisePropertyChanged();
                }
            }
        }

        public sealed class PhdAppState {
            public static readonly string STOPPED = "Stopped";
            public static readonly string SELECTED = "Selected";
            public static readonly string CALIBRATING = "Calibrating";
            public static readonly string GUIDING = "Guiding";
            public static readonly string LOSTLOCK = "LostLock";
            public static readonly string PAUSED = "Paused";
            public static readonly string LOOPING = "Looping";
        }

        public class PhdEventCalibrationFailed : PhdEvent {
            public string Reason;
        }

        public class PhdEventCalibrationDataFlipped : PhdEvent {
            public string Mount;
        }

        public class PhdEventLoopingExposures : PhdEvent {
            public int Frame;
        }

        public class PhdEventLoopingExposuresStopped : PhdEvent {
        }

        public class PhdEventSettling : PhdEvent {
            public int Distance;
            public int Time;
            public int SettleTime;
        }

        public class PhdEventSettleDone : PhdEvent {
            public int Status;
            public string Error;
        }

        public class PhdEventStarLost : PhdEvent {
            public int Frame;
            public int Time;
            public int StarMass;
            public int SNR;
            public int AvgDist;
            public int ErrorCode;
            public int Status;
        }

        public class PhdEventGuidingStopped : PhdEvent {
        }

        public class PhdEventResumed : PhdEvent {
        }

        public class PhdEventGuideStep : PhdEvent, IGuideStep {
            private double frame;
            private double time;
            private string mount;
            private double dx;
            private double dy;
            private double rADistanceRaw;
            private double decDistanceRaw;
            private double raDistanceDisplay;
            private double decDistanceDisplay;
            private double rADistanceGuide;
            private double decDistanceGuide;
            private double raDistanceGuideDisplay;
            private double decDistanceGuideDisplay;
            private double rADuration;
            private string rADirection;
            private double dECDuration;
            private string decDirection;
            private double starMass;
            private double sNR;
            private double avgDist;
            private bool rALimited;
            private bool decLimited;
            private double errorCode;

            public double RADistanceRawDisplay {
                get {
                    return raDistanceDisplay;
                }
                set {
                    raDistanceDisplay = value;
                }
            }

            public double DecDistanceRawDisplay {
                get {
                    return decDistanceDisplay;
                }
                set {
                    decDistanceDisplay = value;
                }
            }

            public double RADistanceGuideDisplay {
                get {
                    return raDistanceGuideDisplay;
                }
                set {
                    raDistanceGuideDisplay = value;
                }
            }

            public double DecDistanceGuideDisplay {
                get {
                    return decDistanceGuideDisplay;
                }
                set {
                    decDistanceGuideDisplay = value;
                }
            }

            public double Frame {
                get {
                    return frame;
                }

                set {
                    frame = value;
                }
            }

            public double Time {
                get {
                    return time;
                }

                set {
                    time = DateTime.UtcNow
                   .Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))
                   .TotalSeconds;
                }
            }

            public double TimeRA {
                get {
                    return Time - 0.15;
                }
            }

            public double TimeDec {
                get {
                    return Time + 0.15;
                }
            }

            public string Mount {
                get {
                    return mount;
                }

                set {
                    mount = value;
                }
            }

            public double Dx {
                get {
                    return dx;
                }

                set {
                    dx = value;
                }
            }

            public double Dy {
                get {
                    return dy;
                }

                set {
                    dy = value;
                }
            }

            public double RADistanceRaw {
                get {
                    return -rADistanceRaw;
                }

                set {
                    rADistanceRaw = value;
                    RADistanceRawDisplay = RADistanceRaw;
                }
            }

            public double DecDistanceRaw {
                get {
                    return decDistanceRaw;
                }

                set {
                    decDistanceRaw = value;
                    DecDistanceRawDisplay = DecDistanceRaw;
                }
            }

            public double RADistanceGuide {
                get {
                    return rADistanceGuide;
                }

                set {
                    rADistanceGuide = value;
                    RADistanceGuideDisplay = RADistanceGuide;
                }
            }

            public double DecDistanceGuide {
                get {
                    return decDistanceGuide;
                }

                set {
                    decDistanceGuide = value;
                    DecDistanceGuideDisplay = DecDistanceRaw;
                }
            }

            public double RADuration {
                get {
                    if (RADirection == "East") {
                        return -rADuration;
                    } else {
                        return rADuration;
                    }
                }

                set {
                    rADuration = value;
                }
            }

            public string RADirection {
                get {
                    return rADirection;
                }

                set {
                    rADirection = value;
                }
            }

            public double DECDuration {
                get {
                    if (DecDirection == "South") {
                        return -dECDuration;
                    } else {
                        return dECDuration;
                    }
                }

                set {
                    dECDuration = value;
                }
            }

            public string DecDirection {
                get {
                    return decDirection;
                }

                set {
                    decDirection = value;
                }
            }

            public double StarMass {
                get {
                    return starMass;
                }

                set {
                    starMass = value;
                }
            }

            public double SNR {
                get {
                    return sNR;
                }

                set {
                    sNR = value;
                }
            }

            public double AvgDist {
                get {
                    return avgDist;
                }

                set {
                    avgDist = value;
                }
            }

            public bool RALimited {
                get {
                    return rALimited;
                }

                set {
                    rALimited = value;
                }
            }

            public bool DecLimited {
                get {
                    return decLimited;
                }

                set {
                    decLimited = value;
                }
            }

            public double ErrorCode {
                get {
                    return errorCode;
                }

                set {
                    errorCode = value;
                }
            }

            public IGuideStep Clone() {
                return (PhdEventGuideStep)this.MemberwiseClone();
            }
        }

        public class PhdEventGuidingDithered : PhdEvent {
            public int dx;
            public int dy;
        }

        public class PhdEventLockPositionLost : PhdEvent {
        }

        public class PhdEventAlert : PhdEvent {
            public string Msg;
            public string Type;
        }
    }
}