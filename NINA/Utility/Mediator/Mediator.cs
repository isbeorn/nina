using NINA.Model.MyCamera;
using NINA.Model.MyFilterWheel;
using NINA.PlateSolving;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NINA.Utility.Mediator {
    class Mediator {
        private Mediator() { }

        private static readonly Lazy<Mediator> lazy =
            new Lazy<Mediator>(() => new Mediator());

        public static Mediator Instance { get { return lazy.Value; } }

        Dictionary<MediatorMessages, List<Action<Object>>> _internalList
            = new Dictionary<MediatorMessages, List<Action<Object>>>();

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


        /// <summary>
        /// Holds reference to handlers and identified by message type name
        /// </summary>
        private Dictionary<string, MessageHandle> _handlers = new Dictionary<string, MessageHandle>();

        /// <summary>
        /// Register handler to react on requests
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public bool RegisterRequest(MessageHandle handle) {
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
        private T Request<T>(MediatorMessage<T> msg) {
            var key = msg.GetType().Name;
            if (_handlers.ContainsKey(key)) {
                var entry = _handlers[key];
                var handle = (MessageHandle<T>)entry;
                return handle.Send(msg);
            } else {
                return default(T);
            }
        }

        public bool Request(MediatorMessage<bool> msg) {
            return Request<bool>(msg);
        }

        public FilterInfo Request(MediatorMessage<FilterInfo> msg) {
            return Request<FilterInfo>(msg);
        }

        public ICollection<FilterInfo> Request(MediatorMessage<ICollection<FilterInfo>> msg) {
            return Request<ICollection<FilterInfo>>(msg);
        }


        /// <summary>
        /// Holds reference to handlers and identified by message type name
        /// </summary>
        private Dictionary<string, AsyncMessageHandle> _asyncHandlers = new Dictionary<string, AsyncMessageHandle>();

        /// <summary>
        /// Register handler to react on requests
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public bool RegisterAsyncRequest(AsyncMessageHandle handle) {
            if (!_asyncHandlers.ContainsKey(handle.MessageType)) {
                _asyncHandlers.Add(handle.MessageType, handle);
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
            if (_asyncHandlers.ContainsKey(key)) {
                var entry = _asyncHandlers[key];
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

        public async Task<double> RequestAsync(AsyncMediatorMessage<double> msg) {
            return await RequestAsync<double>(msg);
        }

        public async Task<ImageArray> RequestAsync(AsyncMediatorMessage<ImageArray> msg) {
            return await RequestAsync<ImageArray>(msg);
        }

        public async Task<BitmapSource> RequestAsync(AsyncMediatorMessage<BitmapSource> msg) {
            return await RequestAsync<BitmapSource>(msg);
        }

        public async Task<FilterInfo> RequestAsync(AsyncMediatorMessage<FilterInfo> msg) {
            return await RequestAsync<FilterInfo>(msg);
        }        
    }


    public enum MediatorMessages {
        TelescopeChanged = 3,
        CameraChanged = 4,
        AutoStrechChanged = 7,
        DetectStarsChanged = 8,
        ChangeAutoStretch = 14,
        ChangeDetectStars = 15,
        LocaleChanged = 18,
        LocationChanged = 19,
        FocuserTemperatureChanged = 26,
        FocuserConnectedChanged = 28,
        CameraConnectedChanged = 29,
        CameraPixelSizeChanged = 30
    };
}
