using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Windows;

namespace NINA.Utility {
    public class PHD2Client : BaseINPC {
        public PHD2Client() {
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
            } set {
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

        private PhdEventGuideStep _prevGuideStep;
        public PhdEventGuideStep PrevGuideStep {
            get {
                return _prevGuideStep;
            }
            set {
                _prevGuideStep = value;
                RaisePropertyChanged();
            }
        }

        private PhdEventGuideStep _guideStep;
        public PhdEventGuideStep GuideStep {
            get {
                return _guideStep;
            }
            set {
                _guideStep = value;
                RaisePropertyChanged();
            }
        }

        /*private AsyncObservableCollection<PhdEventGuideStep> _guideSteps;
        public AsyncObservableCollection<PhdEventGuideStep> GuideSteps {
            get {
                if (_guideSteps == null) {
                    _guideSteps = new AsyncObservableCollection<PhdEventGuideStep>();
                }
                return _guideSteps;
            }
            set {
                _guideSteps = value;
                RaisePropertyChanged();
            }
        }*/



        private TcpClient _client;
        private NetworkStream _stream;
        private CancellationTokenSource _tokenSource;

        public bool Connected {
            get {
                if (_client == null) {
                    return false;
                }
                return _client.Connected;
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

        public async Task<bool> Connect() {
            
            try {                
                _client = new TcpClient();
                await _client.ConnectAsync(Settings.PHD2ServerUrl, Settings.PHD2ServerPort);
                _stream = _client.GetStream();
                RaisePropertyChanged("Connected");
                _tokenSource = new CancellationTokenSource();
                
                Notification.ShowSuccess("Connected to PHD2 Server");
               
                

                StartListener(_tokenSource.Token);
            }
            catch (SocketException e) {
                
                 Notification.ShowError("PHD2 Error: " + e.Message);

                //System.Windows.MessageBox.Show(e.Message);
            }
            return Connected;
        }

        public async Task<bool> Dither() {
            if(Connected) {
                IsDithering = true;
                await SendMessage(String.Format(PHD2Methods.DITHER, Settings.DitherPixels.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), Settings.DitherRAOnly.ToString().ToLower()));
            }

            return IsDithering;
        }

        private async Task<bool> SendMessage(string msg) {
            if(Connected) {
                // Translate the passed message into ASCII and store it as a byte array.
                Byte[] data = new Byte[10240];
                data = System.Text.Encoding.ASCII.GetBytes(msg);

                // Get a client stream for reading and writing.
                // Stream stream = client.GetStream();

                // Send the message to the connected TcpServer. 
                await _stream.WriteAsync(data, 0, data.Length);
                
            }
            return true;            
        }

        public bool Disconnect() {
            
            if (Connected) {
                _tokenSource.Cancel();
                _stream.Close();
                _client.Close();
                IsDithering = false;
                RaisePropertyChanged("Connected");
            }
            return !Connected;
        }
        
        private async void StartListener(CancellationToken token) {
            while(Connected) {
                try {
                    if (_stream.DataAvailable) {
                        token.ThrowIfCancellationRequested();
                        byte[] resp = new byte[4096];
                        var memStream = new MemoryStream();
                        var bytes = 0;
                        bytes = await _stream.ReadAsync(resp, 0, resp.Length);
                        await memStream.WriteAsync(resp, 0, bytes);
                        List<string> rows = new List<string>();
                        memStream.Position = 0;
                        using (var reader = new StreamReader(memStream, Encoding.ASCII)) {
                            string line;
                            while ((line = reader.ReadLine()) != null) {
                                rows.Add(line);
                            }
                        }
                        
                        foreach(string row in rows) {
                            if(!string.IsNullOrEmpty(row)) {

                            
                                JObject o  = JObject.Parse(row);                            
                                JToken t = o.GetValue("Event");
                                string phdevent = "";
                                if (t != null) {
                                    phdevent = t.ToString();
                                } else {
                                    t = o.GetValue("id");
                                    if(t!= null) {
                                        phdevent = t.ToString();
                                    }
                                }

                                switch (phdevent) {
                                    case PHD2EventId.DITHER: {
                                            PhdMethodResponse phdresp = o.ToObject<PhdMethodResponse>();
                                            if (phdresp.error != null) {
                                                    IsDithering = false;
                                                }
                                        
                                        
                                            break;
                                        }
                                    case PHD2EventId.GET_APP_STATE: {
                                            PhdMethodResponse phdresp = o.ToObject<PhdMethodResponse>();
                                            if (phdresp.error == null) {
                                                AppState.State = phdresp.result.ToString() ;
                                            }
                                        
                                            break;
                                        }
                                    case PHD2EventId.GET_STAR_IMAGE: {
                                            PhdMethodResponse phdresp = o.ToObject<PhdMethodResponse>();                                        

                                            if(phdresp.error == null) {
                                                PhdImageResult img = JObject.Parse(phdresp.result.ToString()).ToObject<PhdImageResult>();
                                                byte[] p = Convert.FromBase64String(img.pixels.Trim('\0'));      
                                                BitmapSource bmp = Utility.CreateSourceFromArray(p, img.width, img.height, System.Windows.Media.PixelFormats.Gray16);
                                                bmp.Freeze();
                                                await _dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                                                    Image = bmp;
                                                }));                                            
                                            }
                                        
                                        
                                            break;
                                        }
                                    case "Version": {
                                            Version = o.ToObject<PhdEventVersion>();                                        
                                            break;
                                        }
                                    case "AppState": {
                                            AppState = o.ToObject<PhdEventAppState>();
                                            break;
                                        }
                                    case "GuideStep": {
                                            PrevGuideStep = GuideStep;
                                            GuideStep = o.ToObject<PhdEventGuideStep>();
                                            break;
                                        }
                                    case "GuidingDithered": {
                                            SettleDone = null;
                                            GuidingDithered = o.ToObject<PhdEventGuidingDithered>();
                                            break;
                                        }
                                    case "Settling": {
                                            SettleDone = null;
                                            Settling = o.ToObject<PhdEventSettling>();
                                            break;
                                        }
                                    case "SettleDone": {
                                            GuidingDithered = null;
                                            Settling = null;
                                            IsDithering = false;
                                            SettleDone = o.ToObject<PhdEventSettleDone>();
                                            if(SettleDone.Error != null) {
                                                Notification.ShowError("PHD2 Error: " + SettleDone.Error);
                                            }
                                            break;
                                        }
                                    case "Paused": {
                                            break;
                                        }
                                    case "StartCalibration": {
                                            break;
                                        }
                                    case "LoopingExposures": {
                                            break;
                                        }
                                    case "LoopingExposuresStopped": {
                                            break;
                                        }
                                    case "StarLost": {
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
                        }
                        await Task.Delay(500);
                    }
                    else {
                        await Task.Delay(1000);
                       
                    }

                    
                    await SendMessage(PHD2Methods.GET_APP_STATE);
                    //await sendMessage(PHD2Methods.GET_STAR_IMAGE); 
                }
                catch(System.IO.IOException ex) {
                    Logger.Trace(ex.Message);
                    _stream.Close();
                    _client.Close();
                    IsDithering = false;
                    Notification.ShowError("PHD2 Error: " + ex.Message);
                    RaisePropertyChanged("Connected");
                }
                catch (Exception ex) {
                    Logger.Error(ex.Message);
                    Notification.ShowError("PHD2 Error: " + ex.Message);
                }
                
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

    public class PhdEvent : BaseINPC {
        public string Event;
        public string TimeStamp;
        public string Host;
        public int Inst;
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

    public class PhdEventAppState : PhdEvent {
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

    public class PhdEventGuideStep : PhdEvent {
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
                time = value;
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
