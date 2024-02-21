using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Linq;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces;
using NINA.Profile.Interfaces;
using NINA.Equipment.Equipment.MyGuider.SkyGuard.SkyGuardMessages;
using NINA.Core.Interfaces;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Core.Utility.Notification;
using NINA.Core.Model;
using NINA.Core.Utility.WindowService;
using NINA.Core.Locale;
using System.Windows.Threading;
using NINA.Core.Utility.Http;
using NINA.Astrometry;
using System.Collections.Generic;
using NINA.Equipment.Equipment.MyGuider.PHD2;

namespace NINA.Equipment.Equipment.MyGuider.SkyGuard
{
    public class SkyGuardGuider : BaseINPC, IGuider
    {
        private readonly Version minimumSkyGuardVersion = new Version("4.16");

        #region Fields
        private IProfileService profileService;
        private readonly IWindowServiceFactory windowServiceFactory;
        WebRequest request;

        private bool _connected;
        private TaskCompletionSource<bool> _tcs;
        private CancellationTokenSource skyGuardTCS;

        private bool _isDithering;
        private readonly object startGuidingLock = new object();
        private CancellationTokenSource startGuidingCancellationTokenSource;
        private Task<bool> startGuidingTask;
        private CancellationToken startFocusingCancellationTokenSource;
        private Task<bool> startFocusingTask;
        private IGuider guiderInstance;

        private CancellationToken tokenNINA;
        private Process skyGuardProcess;
        HttpListener skyGuardListener;
        DateTime dateTimeOut = new DateTime();

        private double errorX;
        private double errorY;
        #endregion

        #region Constructors
        /// <summary>
        /// 
        /// </summary>
        /// <param name="profileService"></param>
        /// <param name="windowServiceFactory"></param>
        public SkyGuardGuider(IProfileService profileService, IWindowServiceFactory windowServiceFactory)
        {
            this.profileService = profileService;
            this.windowServiceFactory = windowServiceFactory;
            _connected = false;

            OpenSkyGuardDiagCommand = new RelayCommand(OpenSkyGuardFileDiag);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Property indicating the name of the SkyGuard software
        /// </summary>
        public string Name => "SkyGuard";
        public string DisplayName => Name;

        /// <summary>
        /// Property indicating the id of the SkyGuard software
        /// </summary>
        public string Id => "SkyGuard_Guider";

        public double _pixelScale = 1.0;

        /// <summary>
        /// Property indicating connection status with skyguard
        /// </summary>
        private string _state = "Idle";

        /// <summary>
        /// Property indicating if skyguard was launched from NINA
        /// </summary>
        private bool startedByNina = false;

        public bool HasSetupDialog => !Connected;

        /// <summary>
        /// Property indicating category
        /// </summary>
        public string Category => "Guiders";

        /// <summary>
        /// Property showing description of SkyGuard
        /// </summary>
        public string Description => "SkyGuard Guider";

        /// <summary>
        /// Property indicating SkyGuard driver information
        /// </summary>
        public string DriverInfo => "SkyGuard Guider";

        /// <summary>
        /// Property indicating SkyGuard driver version
        /// </summary>
        public string DriverVersion => "1.0";

        public bool CanClearCalibration => true;


        /// <summary>
        /// Property indicating if skyguard is well connected
        /// </summary>
        public bool Connected {
            get => _connected;
            set {
                _connected = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Property indicating connection status with skyguard
        /// </summary>
        public string State {
            get => _state;
            set {
                _state = value;
                RaisePropertyChanged();
            }
        }

        public double PixelScale {
            get => _pixelScale;
            set {
                _pixelScale = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Property shortening the url to reach SkyGuard
        /// </summary>
        string SKSS_Uri => $"http://{profileService.ActiveProfile.GuiderSettings.SkyGuardServerUrl}:{profileService.ActiveProfile.GuiderSettings.SkyGuardServerPort}";

        public RelayCommand OpenSkyGuardDiagCommand { get; set; }

        public bool CanSetShiftRate { 
            get{
                Logger.Debug("This method is not implemented");
                return false;
            }
        }

        public bool CanGetLockPosition => false;


        public bool ShiftEnabled {
            get {
                Logger.Debug("This method is not implemented");
                return false;
            }
        }

        public double ShiftRateRA {
            get {
                Logger.Debug("This method is not implemented");
                throw new NotImplementedException();
            }
        }

        public double ShiftRateDec {
            get {
                Logger.Debug("This method is not implemented");
                throw new NotImplementedException();
            }
        }

        public SiderealShiftTrackingRate ShiftRate {
            get {
                Logger.Debug("This method is not implemented");
                throw new NotImplementedException();
            }
        }

        public event EventHandler<IGuideStep> GuideEvent;

        private Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;

        private TelescopeInfo telescopeInfo = DeviceInfo.CreateDefaultInstance<TelescopeInfo>();

        public IList<string> SupportedActions => new List<string>();

        #endregion

        #region Methods

        #region Public Methods

        public void UpdateDeviceInfo(TelescopeInfo telescopeInfo)
        {
            this.telescopeInfo = telescopeInfo;
        }

        #endregion

        #region Private Methods

        private void NewTimeOut() {
            double timeOut = profileService.ActiveProfile.GuiderSettings.SkyGuardTimeOutGuiding * 60;
            DateTime dateNow = DateTime.Now;

            dateTimeOut = dateNow.AddSeconds(timeOut);

            Logger.Info("A new timeout has been instantiated");
        }

        /// <summary>
        /// Method that launches SkyGuard
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task<bool> StartSkyProcess(CancellationToken token)
        {
            try
            {

                if (Process.GetProcessesByName("SkyGuard").Length == 0 && Process.GetProcessesByName("SkyGuide").Length == 0)
                {
                    if (!File.Exists(profileService.ActiveProfile.GuiderSettings.SkyGuardPath))
                    {
                        throw new FileNotFoundException();
                    }

                    skyGuardProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = profileService.ActiveProfile.GuiderSettings.SkyGuardPath,
                            Arguments = "/AutoStartMaxImDL",
                            WorkingDirectory = Path.GetDirectoryName(profileService.ActiveProfile.GuiderSettings.SkyGuardPath)
                        }
                    };
                    var processStarted = skyGuardProcess.Start();
                    startedByNina = true;
                }

                try
                {

                    bool skyGuardReady = false;
                    int count = 0;

                    do
                    {
                        if (count >= 15)
                        {
                            throw new Exception("The configured timeout for waiting on SkyGuard ready is reached. Operation is aborted");
                        }

                        try
                        {
                            string webResponse = ExecuteWebRequest($"{SKSS_Uri}/SKSS_ReadyForOperation");
                            if (!string.IsNullOrEmpty(webResponse))
                            {
                                skyGuardReady = Convert.ToBoolean(JsonConvert.DeserializeObject<SkyGuardStatusMessage>(webResponse).Data);
                            }

                            await Task.Delay(1000, token);
                            count++;
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex.Message);
                        }

                        if (token.IsCancellationRequested)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                    } while (!skyGuardReady);

                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Logger.Warning($"{Loc.Instance["LblSkyGuardNotReady"]} : {ex.Message}");
                    Notification.ShowError(Loc.Instance["LblSkyGuardNotReady"]);
                    throw;
                }

                if (token.IsCancellationRequested)
                {
                    token.ThrowIfCancellationRequested();
                }

                return true;
            }
            catch (FileNotFoundException ex)
            {
                Logger.Error(Loc.Instance["LblSkyGuardPathNotFound"], ex);
                Notification.ShowError(Loc.Instance["LblSkyGuardPathNotFound"]);
                throw;
            }
            catch (OperationCanceledException cancelException)
            {
                Logger.Warning($"{Loc.Instance["LblSkyGuardOperationCancelled"]} : {cancelException}");
                Notification.ShowWarning(Loc.Instance["LblSkyGuardOperationCancelled"]);
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                Notification.ShowError(Loc.Instance["LblSkyGuardStartProcessError"]);
                throw;
            }
        }

        /// <summary>
        /// Method that stops SkyGuard
        /// </summary>
        private void StopSkyProcess()
        {
            if (startedByNina == true)
            {
                if (!skyGuardProcess.HasExited) 
                {
                    skyGuardProcess.Kill();
                    skyGuardProcess.Dispose();
                }
            }
        }

        /// <summary>
        /// Method that launches an httplistener to retrieve SkyGuard events
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private bool RunListener(CancellationToken token)
        {
            try
            {
                skyGuardListener = new HttpListener();
                skyGuardListener.Prefixes.Add(@"http://+:" + profileService.ActiveProfile.GuiderSettings.SkyGuardCallbackPort + "/");

                if (skyGuardListener.IsListening == false)
                    skyGuardListener.Start();

                if (!ExecuteOfStatus($"SKSS_RegisterCallbackURI?uri=http://localhost:{profileService.ActiveProfile.GuiderSettings.SkyGuardCallbackPort}/SKSS_Callback"))
                    return false;

                if (!token.IsCancellationRequested)
                {

                    _ = skyGuardListener.BeginGetContext(GetSKSSCallbackMessage, skyGuardListener);
                }

                return true;
            }
            catch (OperationCanceledException cancelException)
            {
                Logger.Error($"{Loc.Instance["LblSkyGuardOperationCancelled"]} : {cancelException}");
                Notification.ShowError(Loc.Instance["LblSkyGuardOperationCancelled"]);
                throw;
            }
            catch (Exception ex)
            {
                Logger.Warning(ex.Message);
                return false;
            }

        }

        /// <summary>
        /// Method that retrieves messages sent by SkyGuard
        /// </summary>
        /// <param name="result"></param>
        private void GetSKSSCallbackMessage(IAsyncResult result)
        {

            HttpListener listener = (HttpListener)result.AsyncState;

            try
            {
                // Call EndGetContext to complete the asynchronous operation and read content of request.
                HttpListenerContext context = listener.EndGetContext(result);
                HttpListenerRequest requestListener = context.Request;

                if (requestListener.Url.ToString().EndsWith("/ends"))
                {
                    StopListener();
                }
                else
                {
                    var requestListenerVars = requestListener.Url.ToString().Split('/');

                    if (requestListenerVars[3] == "SKSS_Callback")
                    {
                        var sr = new StreamReader(requestListener.InputStream);
                        string resultContent = sr.ReadToEnd();

                        ProcessEvent(resultContent);
                    }
                }

                context.Response.Close();

            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            finally
            {
                if (skyGuardListener.IsListening)
                {
                    // Immediately configure to be ready for the next call
                    listener.BeginGetContext(new AsyncCallback(GetSKSSCallbackMessage), listener);
                }

            }

            string guidingStatusResponse = ExecuteWebRequest($"{SKSS_Uri}/SKSS_GetGuidingStatus");
            if (!string.IsNullOrEmpty(guidingStatusResponse))
            {
                var guidingStatus = JsonConvert.DeserializeObject<SkyGuardStatusMessage>(guidingStatusResponse);

                if (guidingStatus.Data != null)
                    State = guidingStatus.Data;
            }
        }

        /// <summary>
        /// Method that stops the listener
        /// </summary>
        private void StopListener()
        {
            try
            {
                WebRequest request = WebRequest.Create($"{SKSS_Uri}/SKSS_UnregisterCallbackURI?uri=http://localhost:{profileService.ActiveProfile.GuiderSettings.SkyGuardCallbackPort}/SKSS_Callback");
                skyGuardListener.Stop();
                skyGuardListener.Close();
            }
            catch (Exception ex)
            {
                Logger.Warning(ex.Message);
            }
        }

        /// <summary>
        /// Method that integrates SkyGuard's messages into NINA's graph
        /// </summary>
        /// <param name="responsMessageContent"></param>
        private void ProcessEvent(string responsMessageContent)
        {

            var callbackItem = JsonConvert.DeserializeObject<SkyGuardEventMessage>(responsMessageContent);
            var guideStep = new SkyGuardEventGuideStep();

            switch (callbackItem.Event)
            {
                case "SKSS_CameraOutputViewUpdated":
                break;
                case "SKSS_FocusingCorrelationCompleted":
                break;
                case "SKSS_GuidingCorrelationCompleted":


                if (callbackItem.Data.Units.Equals("arcSeconds"))
                {
                    guideStep.DECDistanceRaw = callbackItem.Data.GuidingErrorY == null ? 0 : Convert.ToDouble(callbackItem.Data.GuidingErrorY) / _pixelScale;
                    guideStep.RADistanceRaw = callbackItem.Data.GuidingErrorX == null ? 0 : Convert.ToDouble(callbackItem.Data.GuidingErrorX) / _pixelScale;

                }
                else
                {
                    guideStep.DECDistanceRaw = callbackItem.Data.GuidingErrorY == null ? 0 : Convert.ToDouble(callbackItem.Data.GuidingErrorY);
                    guideStep.RADistanceRaw = callbackItem.Data.GuidingErrorX == null ? 0 : Convert.ToDouble(callbackItem.Data.GuidingErrorX);
                }

                if (!callbackItem.Data.GuidingNoCorrectionY)
                    guideStep.DECDuration = callbackItem.Data.GuidingCorrectionY;
                if (!callbackItem.Data.GuidingNoCorrectionX)
                    guideStep.RADuration = callbackItem.Data.GuidingCorrectionX;

                errorX = guideStep.RADistanceRaw;
                errorY = guideStep.DECDistanceRaw;

                GuideEvent?.Invoke(this, guideStep);
                break;
                default:
                break;
            }
        }

        /// <summary>
        /// This method allow to open a FileDialog to set the path of SkyGuard exe file.
        /// </summary>
        /// <param name="o"></param>
        //TODO : Verify if [o] parameter could be removed.
        private void OpenSkyGuardFileDiag(object o)
        {
            var dialog = CoreUtil.GetFilteredFileDialog(profileService.ActiveProfile.GuiderSettings.SkyGuardPath, "SkyGuard.exe", "SkyGuard files :|SkyGuard.exe;SkyGuide.exe| All files(*.*) | *.*");
            if (dialog.ShowDialog() == true)
            {
                this.profileService.ActiveProfile.GuiderSettings.SkyGuardPath = dialog.FileName;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tcpClient"></param>
        /// <returns></returns>
        private static TcpState GetState(TcpClient tcpClient)
        {
            var foo = IPGlobalProperties.GetIPGlobalProperties()
              .GetActiveTcpConnections()
              .SingleOrDefault(x => x.LocalEndPoint.Equals(tcpClient.Client.LocalEndPoint));
            return foo != null ? foo.State : TcpState.Unknown;
        }

        /// <summary>
        /// Integration of a SkyGuard method: https://www.innovationsforesight.com/software/help/SKG/SkySurveyorHTML/SKSS_StartGuiderCameraExposure.html
        /// </summary>
        /// <returns></returns>
        private bool SKSS_StartGuiderCameraExposure()
        {
            try
            {
                string webResponseGuidingStatus = ExecuteWebRequest($"{SKSS_Uri}/SKSS_GetGuidingStatus");
                string webResponseFocusingStatus = ExecuteWebRequest($"{SKSS_Uri}/SKSS_GetFocusingStatus");
                var statusGuiding = JsonConvert.DeserializeObject<SkyGuardStatusMessage>(webResponseGuidingStatus);
                var statusFocusing = JsonConvert.DeserializeObject<SkyGuardStatusMessage>(webResponseFocusingStatus);

                if (statusGuiding.Data == "idle" && statusFocusing.Data == "idle")
                {
                    string webResponse = ExecuteWebRequest($"{SKSS_Uri}/SKSS_StartGuiderCameraExposure");
                    var statusItem = JsonConvert.DeserializeObject<SkyGuardStatusMessage>(webResponse);

                    if (!statusItem.Status.Equals("success"))
                        return false;
                }
                return true;

            }
            catch
            {
                Logger.Warning("SkyGuard endpoint [SKSS_StartGuiderCameraExposure] is not reachable.");
                Notification.ShowError(Loc.Instance["LblSkyGuardEndpointNotReachable"]);
                return false;
            }
        }

        /// <summary>
        /// Integration of a SkyGuard method: https://www.innovationsforesight.com/software/help/SKG/SkySurveyorHTML/SKSS_StartGuiding.html
        /// </summary>
        /// <param name="forceCalibration"></param>
        /// <returns></returns>
        private bool SKSS_StartGuiding()
        {
            try
            {
                string canGuideResponse = ExecuteWebRequest($"{SKSS_Uri}/SKSS_CanGuide");
                var canGuideStatus = JsonConvert.DeserializeObject<SkyGuardStatusMessage>(canGuideResponse);

                if (canGuideStatus.Status == "success")
                {
                    string guidingStatusResponse = ExecuteWebRequest($"{SKSS_Uri}/SKSS_GetGuidingStatus");
                    var guidingStatus = JsonConvert.DeserializeObject<SkyGuardStatusMessage>(guidingStatusResponse);

                    if (guidingStatus.Data == "idle" || guidingStatus.Data == "calibrating")
                    {
                        return ExecuteOfStatus("SKSS_StartGuiding");
                    }
                    else if (guidingStatus.Data == "paused")
                    {
                        return ExecuteOfStatus("SKSS_ResumeMoveGuider");
                    }
                    else
                        return true;

                }
                else
                    throw new Exception("SkyGuard endpoint [SKSS_CanGuide] is not reachable.");
            }
            catch (Exception ex)
            {
                Logger.Warning(ex.Message);
                Notification.ShowError(Loc.Instance["LblSkyGuardEndpointNotReachable"]);
                return false;
            }
        }

        /// <summary>
        /// Integration of a SkyGuard method: https://www.innovationsforesight.com/software/help/SKG/SkySurveyorHTML/SKSS_StartFocusing.html
        /// </summary>
        /// <param name="forceCalibration"></param>
        /// <returns></returns>
        private bool SKSS_StartFocusing()
        {
            try
            {
                string canFocusResponse = ExecuteWebRequest($"{SKSS_Uri}/SKSS_CanFocus");
                var CanFocusStatus = JsonConvert.DeserializeObject<SkyGuardStatusMessage>(canFocusResponse);

                if (CanFocusStatus.Status == "success")
                {
                    if (CanFocusStatus.Data != "true") 
                    {
                        Logger.Warning("[SKSS_CanFocus] returns false, make sure you have a version above SkyGuide");
                        return true;
                    }

                    string focusingStatusResponse = ExecuteWebRequest($"{SKSS_Uri}/SKSS_GetfocusingStatus");
                    var focusingStatus = JsonConvert.DeserializeObject<SkyGuardStatusMessage>(focusingStatusResponse);

                    if (focusingStatus.Data == "idle")
                    {
                        return ExecuteOfStatus("SKSS_Startfocusing");
                    }
                    else if (focusingStatus.Data == "paused")
                    {
                        return ExecuteOfStatus("SKSS_ResumeMoveFocuser");
                    }
                    else
                        return true;
                }
                else
                    throw new Exception("SkyGuard endpoint [SKSS_CanFocus] is not reachable.");
            }
            catch (Exception ex)
            {
                Logger.Warning(ex.Message);
                Notification.ShowError(Loc.Instance["LblSkyGuardEndpointNotReachable"]);
                return false;
            }
        }

        /// <summary>
        /// Method to execute a web request
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private string ExecuteWebRequest(string url)
        {

            try
            {

                var request = WebRequest.Create(url);
                var response = (HttpWebResponse)Task.Run(async () => await request.GetResponseAsync()).Result;

                if (response.StatusCode != HttpStatusCode.OK)
                    throw new Exception($"The url: {url} is not accessible, please check in the setup of SkyGuardGuider");

                using (StreamReader reader = new StreamReader(response.GetResponseStream())) {
                    return reader.ReadToEnd();
                }
            }
            catch (InvalidOperationException operationEx)
            {
                Logger.Warning(operationEx.Message);
                return string.Empty;
            }
            catch (Exception ex)
            {
                Logger.Warning(ex.Message);
                return string.Empty;
            }

        }

        /// <summary>
        /// Method that checks that the status is OK
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        private bool ExecuteOfStatus(string uri)
        {
            string guiderResponse = string.Empty;
            SkyGuardStatusMessage guiderStatus = new SkyGuardStatusMessage();

            try
            {
                guiderResponse = ExecuteWebRequest($"{SKSS_Uri}/{uri}");
                guiderStatus = JsonConvert.DeserializeObject<SkyGuardStatusMessage>(guiderResponse);
                if (guiderStatus.Status == "success")
                    return true;
                else
                    return false;
            }
            catch
            {
                Logger.Warning($"{uri} is not reachable");
                return false;
            }
        }

        /// <summary>
        /// Method that loops until the status is ok
        /// </summary>
        /// <param name="method"></param>
        /// <param name="status"></param>
        /// <param name="negative"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task<bool> StatusLoop(string method, string status, bool negative, CancellationToken token)
        {

            SkyGuardStatusMessage guidingStatus = new SkyGuardStatusMessage();

            if (negative)
            {
                do
                {
                    await Task.Delay(200, token);

                    string guidingStatusResponse = ExecuteWebRequest($"{SKSS_Uri}/{method}");
                    if (!string.IsNullOrEmpty(guidingStatusResponse))
                    {
                        guidingStatus = JsonConvert.DeserializeObject<SkyGuardStatusMessage>(guidingStatusResponse);
                    }

                    if (DateTime.Now >= dateTimeOut)
                        throw new TimeoutException();

                    if (token.IsCancellationRequested)
                        throw new OperationCanceledException(token);
                } while (!guidingStatus.Data.Equals(status));

            }
            else
            {
                do
                {
                    await Task.Delay(200, token);

                    string guidingStatusResponse = ExecuteWebRequest($"{SKSS_Uri}/{method}");
                    if (!string.IsNullOrEmpty(guidingStatusResponse))
                    {
                        guidingStatus = JsonConvert.DeserializeObject<SkyGuardStatusMessage>(guidingStatusResponse);
                    }

                    if (DateTime.Now >= dateTimeOut)
                        throw new TimeoutException();

                    if (token.IsCancellationRequested)
                        throw new OperationCanceledException(token);
                } while (guidingStatus.Data.Equals(status));
            }
            return true;
        }

        #endregion

        #endregion

        #region Implementations

        #region IDevice Implementation

        /// <summary>
        /// Connect SkyGuard to NINA
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<bool> Connect(CancellationToken token)
        {
            _tcs = new TaskCompletionSource<bool>();
            var startedSkyGuard = await StartSkyProcess(token);

            if (!startedSkyGuard)
                return _connected;

            try
            {
                string versionResponse = ExecuteWebRequest($"{SKSS_Uri}/SKSS_Version");
                var msgVersionNotCompatible = $"{Loc.Instance["lblSkyGuardWrongVersion"]}\n To connect N.I.N.A to SkyGuard you must install at least the version 4.16 or higher. \nYou can download the latest version from https://www.innovationsforesight.com/support/skg_download/";

                if (string.IsNullOrEmpty(versionResponse))
                {
                    Logger.Error(msgVersionNotCompatible);
                    Notification.ShowError(Loc.Instance["lblSkyGuardWrongVersion"]);
                    return _connected;
                }

                var version = JsonConvert.DeserializeObject<SkyGuardStatusMessage>(versionResponse);
                Version skyGuardVersion = new Version(version.Data);

                if (skyGuardVersion < minimumSkyGuardVersion) {
                    Logger.Error(msgVersionNotCompatible);
                    Notification.ShowError(Loc.Instance["lblSkyGuardWrongVersion"]);
                    return _connected;
                }

                // utilisser la méthode connect
                string connect = ExecuteWebRequest($"{SKSS_Uri}/SKSS_ConnectCamera");
                var connectStatus = JsonConvert.DeserializeObject<SkyGuardStatusMessage>(connect);

                if (connectStatus.Status == "success")
                {
                    string cameraPixelScale = ExecuteWebRequest($"{SKSS_Uri}/SKSS_GetGuiderCameraPixelScale");
                    var pixelScaleStatus = JsonConvert.DeserializeObject<SkyGuardStatusMessage>(cameraPixelScale);

                    PixelScale = float.Parse(pixelScaleStatus.Data);

                    _connected = RunListener(token);
                }

            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                Notification.ShowError(Loc.Instance["LblSkyGuardConnectError"]);
            }
            return _connected;
        }

        /// <summary>
        /// Disconnects SkyGuard from NINA
        /// </summary>
        public void Disconnect()
        {
            try
            {
                ExecuteWebRequest($"{SKSS_Uri}/SKSS_DisconnectCamera");
                StopSkyProcess();

                Connected = false;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                Notification.ShowError(Loc.Instance["LblSkyGuardDisonnectError"]);
            }
            finally
            {
                StopListener();
            }
        }

        /// <summary>
        /// Saves user information entered in SkyGuard Setup
        /// </summary>
        public void SetupDialog()
        {
            var windowService = windowServiceFactory.Create();
            windowService.ShowDialog(this, Loc.Instance["LblSkyGuardSetup"], System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.SingleBorderWindow);
        }

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

        #endregion

        #region IGuider Implementation
        /// <summary>
        /// Implementation of the Dither method
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<bool> Dither(IProgress<ApplicationStatus> progress, CancellationToken token) {
            try {
                // Variable declaration:
                double maxValueDithering = profileService.ActiveProfile.GuiderSettings.SkyGuardValueMaxDithering;
                double timeLaps = profileService.ActiveProfile.GuiderSettings.SkyGuardTimeLapsDithering;
                bool settleChecked = profileService.ActiveProfile.GuiderSettings.SkyGuardTimeLapsDitherChecked;

                NewTimeOut();

                SkyGuardStatusMessage isDitheringInProgressStatus = new SkyGuardStatusMessage();

                string guidingStatusResponse = ExecuteWebRequest($"{SKSS_Uri}/SKSS_GetGuidingStatus");
                var guidingStatus = JsonConvert.DeserializeObject<SkyGuardStatusMessage>(guidingStatusResponse);

                if (!guidingStatus.Data.Equals("guiding") && !guidingStatus.Data.Equals("looping")) {
                    Notification.ShowWarning(Loc.Instance["LblDitherSkyGuardSkippedBecauseNotGuiding"]);
                    return false;
                }

                //TODO : Add this funtionnality in the future
                string isDithering = ExecuteWebRequest($"{SKSS_Uri}/SKSS_IsDitheringEnabled");
                var ditheringStatus = JsonConvert.DeserializeObject<SkyGuardStatusMessage>(isDithering);

                if (ditheringStatus.Data.Equals("false")) {
                    ExecuteWebRequest($"{SKSS_Uri}/SKSS_StartDithering");
                    await Task.Delay(5000, token);
                }

                ExecuteWebRequest($"{SKSS_Uri}/SKSS_CalculateDitheringOffsets");

                string ditheringOffsetX = ExecuteWebRequest($"{SKSS_Uri}/SKSS_GetDitheringOffsetX");
                string ditheringOffsetY = ExecuteWebRequest($"{SKSS_Uri}/SKSS_GetDitheringOffsetY");
                var getX = JsonConvert.DeserializeObject<SkyGuardStatusMessage>(ditheringOffsetX);
                var getY = JsonConvert.DeserializeObject<SkyGuardStatusMessage>(ditheringOffsetY);

                double ditherX = Convert.ToDouble(getX.Data);
                double ditherY = Convert.ToDouble(getY.Data);

                ExecuteWebRequest($"{SKSS_Uri}/SKSS_SetDitheringOffsets?X={ditherX}&Y={ditherY}");

                await StatusLoop("SKSS_IsDitheringInProgress", "true", true, token);
                await StatusLoop("SKSS_IsDitheringInProgress", "true", false, token);

                if (settleChecked) {
                    do {
                        await Task.Delay(1000, token);

                        if (DateTime.Now >= dateTimeOut)
                            throw new TimeoutException();

                        if (token.IsCancellationRequested)
                            throw new OperationCanceledException(token);

                        timeLaps--;

                        Logger.Info($"Calculation of errors : {(errorX * errorX) + (errorY * errorY)} <<<<< must be smaller than : {maxValueDithering}");

                        if (Math.Sqrt((errorX * errorX) + (errorY * errorY)) > maxValueDithering) {
                            timeLaps = profileService.ActiveProfile.GuiderSettings.SkyGuardTimeLapsDithering;
                            var difference = (errorX * errorX) + (errorY * errorY) - maxValueDithering;

                            Logger.Warning($"the error exceeds the threshold of: {difference}! the timeout is reset");
                        }

                        Logger.Info($"Timeout in {timeLaps} sec");

                    } while (timeLaps > 0);
                }

                return true;

            } catch (OperationCanceledException) {
                var msg = $"Operation cancelled.";
                Logger.Warning(msg);
                Notification.ShowWarning(Loc.Instance["LblSkyGuardOperationCancelled"]);
                ExecuteWebRequest($"{SKSS_Uri}/SKSS_StopGuiderCameraExposure");
                return false;

            } catch (TimeoutException) {
                Logger.Error("TimeOut for Dithering");
                Notification.ShowError(Loc.Instance["LblSkyGuardDitheringError"]);
                return false;

            } catch (Exception ex) {
                Logger.Warning(ex.Message);
                Notification.ShowError(Loc.Instance["LblSkyGuardDitheringError"]);
                return false;

            }
        }

        /// <summary>
        /// Implementation of the StartGuiding method
        /// </summary>
        /// <param name="forceCalibration"></param>
        /// <param name="progress"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<bool> StartGuiding(bool forceCalibration, IProgress<ApplicationStatus> progress, CancellationToken token)
        {
            try
            {
                string guidingStatusResponse = ExecuteWebRequest($"{SKSS_Uri}/SKSS_IsGuiding");
                var guidingStatus = JsonConvert.DeserializeObject<SkyGuardStatusMessage>(guidingStatusResponse);

                if (guidingStatus.Data.Equals("true"))
                {
                    return true;
                }

                // Variable declaration:
                double maxValueGuiding = profileService.ActiveProfile.GuiderSettings.SkyGuardValueMaxGuiding;
                double timeLaps = profileService.ActiveProfile.GuiderSettings.SkyGuardTimeLapsGuiding;
                bool settleChecked = profileService.ActiveProfile.GuiderSettings.SkyGuardTimeLapsChecked;

                NewTimeOut();

                if (SKSS_StartGuiderCameraExposure())
                {

                    await Task.Delay(1000, token);

                    if (forceCalibration)
                    {
                        ExecuteOfStatus("SKSS_StartGuiderCalibration");

                        await StatusLoop("SKSS_GetGuidingStatus", "calibrating", true, token);
                        await StatusLoop("SKSS_GetGuidingStatus", "calibrating", false, token);
                    }
                    await StatusLoop("SKSS_GetGuidingStatus", "idle", true, token);

                    startGuidingTask = Task.FromResult(SKSS_StartGuiding());
                    startFocusingTask = Task.FromResult(SKSS_StartFocusing());

                    await StatusLoop("SKSS_GetGuidingStatus", "guiding", true, token);

                    if (settleChecked)
                    {
                        do
                        {
                            await Task.Delay(1000, token);

                            if (DateTime.Now >= dateTimeOut)
                                throw new TimeoutException();

                            if (token.IsCancellationRequested)
                                throw new OperationCanceledException(token);

                            timeLaps--;

                            if (Math.Sqrt((errorX * errorX) + (errorY * errorY)) > maxValueGuiding)
                            {
                                timeLaps = profileService.ActiveProfile.GuiderSettings.SkyGuardTimeLapsGuiding;
                                Logger.Warning("An error has occurred, the timeout is reset");
                            }

                        } while (timeLaps > 0);
                    }

                }

                if (startGuidingTask.Result == true && startFocusingTask.Result == true)
                {
                    return true;
                }
                else
                    return false;

            }
            catch (OperationCanceledException)
            {
                var msg = $"Operation cancelled.";
                Logger.Warning(msg);
                Notification.ShowWarning(Loc.Instance["LblSkyGuardOperationCancelled"]);
                ExecuteWebRequest($"{SKSS_Uri}/SKSS_StopGuiderCameraExposure");
                return false;

            }
            catch (TimeoutException)
            {
                Logger.Error("TimeOut for StartGuiding");
                Notification.ShowError(Loc.Instance["LblSkyGuardGuidingError"]);
                return false;

            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                Notification.ShowError(Loc.Instance["LblSkyGuardGuidingError"]);
                return false;

            }
        }

        public Task<bool> ClearCalibration(CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Implementation of the StopGuiding method
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<bool> StopGuiding(CancellationToken ct)
        {
            try
            {
                string guidingStatusResponse = ExecuteWebRequest($"{SKSS_Uri}/SKSS_GetGuidingStatus");
                string focusingStatusResponse = ExecuteWebRequest($"{SKSS_Uri}/SKSS_GetFocusingStatus");

                var guidingStatus = JsonConvert.DeserializeObject<SkyGuardStatusMessage>(guidingStatusResponse);
                var focusingStatus = JsonConvert.DeserializeObject<SkyGuardStatusMessage>(focusingStatusResponse);

                if (guidingStatus.Data != "idle")
                {
                    ExecuteOfStatus("SKSS_StopGuiding");
                }

                if (focusingStatus.Data != "idle")
                {
                    ExecuteOfStatus("SKSS_StopFocusing");
                }

                string cameraExposure = ExecuteWebRequest($"{SKSS_Uri}/SKSS_StopGuiderCameraExposure");
                var cameraStatus = JsonConvert.DeserializeObject<SkyGuardStatusMessage>(cameraExposure);

                if (!cameraStatus.Status.Equals("success"))
                    return false;

                return true;

            }
            catch (Exception ex)
            {
                Logger.Warning(ex.Message);
                Notification.ShowError(Loc.Instance["LblSkyGuardStopGuidingError"]);
                return false;
            }
        }

        public Task<bool> AutoSelectGuideStar()
        {
            return Task.FromResult(true);
        }

        public Task<bool> SetShiftRate(double raArcsecPerHour, double decArcsecPerHour, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task<bool> StopShifting(CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SetShiftRate(SiderealShiftTrackingRate shiftTrackingRate, CancellationToken ct) {
            throw new NotImplementedException();
        }

        public Task<LockPosition> GetLockPosition() {
            throw new NotImplementedException();
        }

        #endregion

        #endregion
    }
}