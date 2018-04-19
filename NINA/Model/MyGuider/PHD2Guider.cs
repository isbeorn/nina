using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NINA.Utility;
using NINA.Utility.Notification;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;

namespace NINA.Model.MyGuider {
    public class PHD2Guider : BaseINPC, IGuider {

        public PHD2Guider() {
            Paused = false;
        }


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

        private bool _paused;
        public bool Paused {
            get {
                return _paused;
            }
            set {
                _paused = value;
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
        public bool IsDithering {
            get {
                return _isDithering;
            }
            set {
                _isDithering = value;
                RaisePropertyChanged();
            }
        }

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

        private bool _isCalibrating;
        public bool IsCalibrating {
            get {
                return _isCalibrating;
            }
            set {
                _isCalibrating = value;
                RaisePropertyChanged();
            }
        }

        /*private async Task<TcpClient> ConnectClient() {
            var client = new TcpClient();
            await client.ConnectAsync(Settings.PHD2ServerUrl, Settings.PHD2ServerPort);
            return client;
        }*/
        TaskCompletionSource<bool> _tcs;

        public async Task<bool> Connect() {
            bool connected = false;
            try {
                _tcs = new TaskCompletionSource<bool>();
                StartListener();
                connected = await _tcs.Task;

                //await SendMessage(PHD2Methods.GET_PIXEL_SCALE);                

                Notification.ShowSuccess(Locale.Loc.Instance["LblGuiderConnected"]);

            } catch (SocketException e) {

                Notification.ShowError("PHD2 Error: " + e.Message);

                //System.Windows.MessageBox.Show(e.Message);
            }
            return connected;
        }

        public async Task<bool> Dither() {
            if (Connected) {
                IsDithering = true;
                await SendMessage(String.Format(PHD2Methods.DITHER, Settings.DitherPixels.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), Settings.DitherRAOnly.ToString().ToLower()));
            }

            return IsDithering;
        }

        public async Task<bool> Pause(bool pause) {
            if (Connected) {
                await SendMessage(String.Format(PHD2Methods.PAUSE, pause.ToString().ToLower()));
            }
            return true;
        }

        public async Task<bool> AutoSelectGuideStar() {
            if (Connected) {
                if (AppState.State != "Looping") {
                    await SendMessage(PHD2Methods.LOOP);
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
                await SendMessage(PHD2Methods.AUTO_SELECT_STAR);
            }
            return true;
        }

        public async Task<bool> StartGuiding() {
            if (Connected) {
                if (AppState.State == "Guiding") { return true; }
                IsCalibrating = true;
                return await SendMessage(String.Format(PHD2Methods.GUIDE, false.ToString().ToLower()));
            } else {
                return false;
            }
        }

        public async Task<bool> StopGuiding(CancellationToken token) {
            if (Connected) {
                await SendMessage(PHD2Methods.STOP_CAPTURE);
                return await Task.Run<bool>(async () => {
                    while (AppState.State != "Stopped") {
                        await Task.Delay(1000, token);
                    }
                    return true;
                });
            } else {
                return false;
            }
        }

        private async Task<bool> SendMessage(string msg) {

            using (var client = new TcpClient()) {
                try {
                    await client.ConnectAsync(Settings.PHD2ServerUrl, Settings.PHD2ServerPort);

                    var stream = client.GetStream();
                    var data = System.Text.Encoding.ASCII.GetBytes(msg);

                    await stream.WriteAsync(data, 0, data.Length);

                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8)) {
                        string line;
                        while ((line = reader.ReadLine()) != null) {
                            JObject o = JObject.Parse(line);
                            JToken t = o.GetValue("Event");
                            string phdevent = "";
                            if (t != null) {
                                phdevent = t.ToString();
                            } else {
                                t = o.GetValue("id");
                                if (t != null) {
                                    phdevent = t.ToString();
                                }
                            }

                            if (phdevent == PHD2EventId.GET_PIXEL_SCALE) {

                            }
                        }
                    }

                } finally {

                }
            }


            /*
                    if (Connected) {
                // Translate the passed message into ASCII and store it as a byte array.
                Byte[] data = new Byte[10240];
                data = System.Text.Encoding.ASCII.GetBytes(msg);

                // Get a client stream for reading and writing.
                // Stream stream = client.GetStream();

                // Send the message to the connected TcpServer.     

                await _client.GetStream().WriteAsync(data, 0, data.Length);
                
            }*/
            return true;
        }

        public bool Disconnect() {
            _clientCTS?.Cancel();
            return false;
        }

        private void ProcessEvent(string phdevent, JObject message) {
            switch (phdevent) {
                case "Resumed": {
                        Paused = false;
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
                        IsDithering = false;
                        IsCalibrating = false;
                        SettleDone = message.ToObject<PhdEventSettleDone>();
                        if (SettleDone.Error != null) {
                            Notification.ShowError("PHD2 Error: " + SettleDone.Error);
                        }
                        break;
                    }
                case "Paused": {
                        AppState = new PhdEventAppState() { State = "Paused" };
                        Paused = true;
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

        private void StartListener() {
            Task.Run(async () => {
                JsonLoadSettings jls = new JsonLoadSettings() { LineInfoHandling = LineInfoHandling.Ignore, CommentHandling = CommentHandling.Ignore };
                _clientCTS = new CancellationTokenSource();
                using (var client = new TcpClient()) {
                    try {
                        await client.ConnectAsync(Settings.PHD2ServerUrl, Settings.PHD2ServerPort);
                        Connected = true;
                        _tcs.TrySetResult(false);

                        using (NetworkStream s = client.GetStream()) {

                            while (true) {
                                    var message = string.Empty;
                                    while (s.DataAvailable) {
                                        byte[] response = new byte[1024];
                                        await s.ReadAsync(response, 0, response.Length, _clientCTS.Token);
                                        message += System.Text.Encoding.ASCII.GetString(response);
                                    }

                                    foreach (string line in message.Split(new[] { Environment.NewLine }, StringSplitOptions.None)) {

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
                                await Task.Delay(TimeSpan.FromMilliseconds(100), _clientCTS.Token);

                            }
                        }
                    } catch (OperationCanceledException) {
                    } catch (Exception ex) {
                        Logger.Error(ex);
                        Notification.ShowError("PHD2 Error: " + ex.Message);
                    } finally {
                        IsDithering = false;
                        Connected = false;
                        _tcs.TrySetResult(false);
                    }
                }
            });
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
            private double rADistanceGuide;
            private double decDistanceGuide;
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
                }
            }

            public double DecDistanceRaw {
                get {
                    return decDistanceRaw;
                }

                set {
                    decDistanceRaw = value;
                }
            }

            public double RADistanceGuide {
                get {
                    return rADistanceGuide;
                }

                set {
                    rADistanceGuide = value;
                }
            }

            public double DecDistanceGuide {
                get {
                    return -decDistanceGuide;
                }

                set {
                    decDistanceGuide = value;
                }
            }

            public double RADuration {
                get {
                    return rADuration;
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
                    return dECDuration;
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
