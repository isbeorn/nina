using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility {
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
            if(!_internalList.ContainsKey(message)) {
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
        PlateSolveResultChanged = 9,
        SyncronizeTelescope = 13,
        ChangeAutoStretch = 14,
        ChangeDetectStars = 15,
        ActiveSequenceChanged = 16,
        FocuserChanged = 17,
        LocaleChanged = 18,
        LocationChanged = 19,
        SlewToCoordinates = 21,
        AutoSelectGuideStar = 22
    };

    public enum AsyncMediatorMessages {
        StartSequence = 1,
        CaptureImage = 2,
        SolveWithCapture = 3,
        Sync = 4,
        SyncTelescopeAndReslew = 5,
        ChangeFilterWheelPosition = 6,
        Solve = 7,
        CheckMeridianFlip = 8,
        CaputureSolveSyncAndReslew = 9,
        DitherGuider = 10,
        PauseGuider = 11,
        ResumeGuider = 12,
        AutoSelectGuideStar = 13,
        SetSequenceCoordinates = 14
    }

}
