using NINA.PlateSolving;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility.Mediator {
    class Mediator {
        private Mediator() { }

        private static readonly Lazy<Mediator> lazy =
            new Lazy<Mediator>(() => new Mediator());

        public static Mediator Instance { get { return lazy.Value; } }

        Dictionary<MediatorMessages, List<Action<Object>>> _internalList
            = new Dictionary<MediatorMessages, List<Action<Object>>>();

        Dictionary<AsyncMediatorMessages, List<Func<object, Task>>> _internalAsyncList
            = new Dictionary<AsyncMediatorMessages, List<Func<object, Task>>>();

        public void Register(Action<Object> callback,
              MediatorMessages message) {
            if (!_internalList.ContainsKey(message)) {
                _internalList[message] = new List<Action<object>>();
            }
            _internalList[message].Add(callback);
        }

        public void Notify(MediatorMessages message, object args) {
            if (_internalList.ContainsKey(message)) {
                //forward the message to all listeners
                foreach (Action<object> callback in _internalList[message]) {
                    callback(args);
                }
            }
        }

        public void RegisterAsync(Func<object, Task> callback,
              AsyncMediatorMessages message) {
            if (!_internalAsyncList.ContainsKey(message)) {
                _internalAsyncList[message] = new List<Func<object, Task>>();
            }
            _internalAsyncList[message].Add(callback);
        }


        public async Task NotifyAsync(AsyncMediatorMessages message, object args) {
            if (_internalAsyncList.ContainsKey(message)) {
                //forward the message to all listeners
                foreach (Func<object, Task> callback in _internalAsyncList[message]) {
                    await callback(args);
                }
            }
        }


        /// <summary>
        /// Holds reference to handlers and identified by message type name
        /// </summary>
        private Dictionary<string, AsyncMessageHandle> _handlers = new Dictionary<string, AsyncMessageHandle>();

        /// <summary>
        /// Register handler to react on requests
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public bool RegisterAsyncRequest(AsyncMessageHandle handle) {
            if (!_handlers.ContainsKey(handle.MessageType)) {
                _handlers.Add(handle.MessageType, handle);
                return true;
            } else {
                throw new Exception("Handle already registered");
            }
        }

        /// <summary>
        /// Request a value from a handler based on message
        /// </summary>
        /// <typeparam name="T">Has to match the return type of the handle.Send()</typeparam>
        /// <param name="msg"></param>
        /// <returns></returns>
        private async Task<T> RequestAsync<T>(AsyncMediatorMessage<T> msg) {
            var key = msg.GetType().Name;
            if (_handlers.ContainsKey(key)) {
                var entry = _handlers[key];
                var handle = (AsyncMessageHandle<T>)entry;
                return await handle.Send(msg);
            } else {
                return default(T);
            }
        }

        public async Task<bool> RequestAsync(AsyncMediatorMessage<bool> msg) {
            return await RequestAsync<bool>(msg);
        }

        public async Task<int> RequestAsync(AsyncMediatorMessage<int> msg) {
            return await RequestAsync<int>(msg);
        }
        
        public async Task<PlateSolveResult> RequestAsync(AsyncMediatorMessage<PlateSolveResult> msg) {
            return await RequestAsync<PlateSolveResult>(msg);
        }
    }


    public enum MediatorMessages {
        StatusUpdate = 1,
        IsExposingUpdate = 2,
        TelescopeChanged = 3,
        CameraChanged = 4,
        FilterWheelChanged = 5,
        ImageChanged = 6,
        AutoStrechChanged = 7,
        DetectStarsChanged = 8,
        SyncronizeTelescope = 13,
        ChangeAutoStretch = 14,
        ChangeDetectStars = 15,
        ActiveSequenceChanged = 16,
        LocaleChanged = 18,
        LocationChanged = 19,
        SlewToCoordinates = 21,
        AutoSelectGuideStar = 22,
        ImageStatisticsChanged = 23,
        TelescopeSnapPort = 25,
        FocuserTemperatureChanged = 26,
        FocuserIsMovingChanged = 27,
        FocuserConnectedChanged = 28,
        CameraConnectedChanged = 29,
        CameraTemperatureChanged = 30,
        CameraCoolerPowerChanged = 31,
        CameraStateChanged = 32,
        SetTelescopeTracking = 33
    };

    public enum AsyncMediatorMessages {
        CaptureImage = 2,
        StartAutoFocus = 19,
        ConnectFilterWheel = 20,
        ConnectFocuser = 21,
        ConnectTelescope = 22,
        ConnectCamera = 23
    }
}
