//
// This work is licensed under a Creative Commons Attribution 3.0 Unported License.
//
// Thomas Dideriksen (thomas@dideriksen.com)
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;
using Nikon;

namespace Nikon
{
    #region Public Delegates
    public delegate void DeviceAddedDelegate(NikonManager sender, NikonDevice device);
    public delegate void DeviceRemovedDelegate(NikonManager sender, NikonDevice device);
    public delegate void PreviewReadyDelegate(NikonDevice sender, NikonPreview preview);
    public delegate void ThumbnailReadyDelegate(NikonDevice sender, NikonThumbnail thumbnail);
    public delegate void ImageReadyDelegate(NikonDevice sender, NikonImage image);
    public delegate void CaptureCompleteDelegate(NikonDevice sender, int data);
    public delegate void CapabilityChangedDelegate(NikonDevice sender, eNkMAIDCapability capability);
    public delegate void VideoFragmentReadyDelegate(NikonDevice sender, NikonVideoFragment fragment);
    public delegate void VideoRecordingInterruptedDelegate(NikonDevice sender, int error);
    public delegate void ProgressDelegate(NikonDevice sender, eNkMAIDDataObjType type, int done, int total);
    #endregion

    #region Internal Delegates
    internal delegate bool GetBooleanDelegate(eNkMAIDCapability cap);
    internal delegate int GetIntegerDelegate(eNkMAIDCapability cap);
    internal delegate uint GetUnsignedDelegate(eNkMAIDCapability cap);
    internal delegate double GetFloatDelegate(eNkMAIDCapability cap);
    internal delegate string GetStringDelegate(eNkMAIDCapability cap);
    internal delegate NkMAIDRange GetRangeDelegate(eNkMAIDCapability cap);
    internal delegate DateTime GetDateTimeDelegate(eNkMAIDCapability cap);
    internal delegate NkMAIDPoint GetPointDelegate(eNkMAIDCapability cap);
    internal delegate NkMAIDRect GetRectDelegate(eNkMAIDCapability cap);
    internal delegate NkMAIDSize GetSizeDelegate(eNkMAIDCapability cap);
    internal delegate NikonEnumWithData GetEnumWithDataDelegate(eNkMAIDCapability cap);
    internal delegate NikonArrayWithData GetArrayWithDataDelegate(eNkMAIDCapability capability);

    internal delegate void SetBooleanDelegate(eNkMAIDCapability cap, bool value);
    internal delegate void SetIntegerDelegate(eNkMAIDCapability cap, int value);
    internal delegate void SetUnsignedDelegate(eNkMAIDCapability cap, uint value);
    internal delegate void SetFloatDelegate(eNkMAIDCapability cap, double value);
    internal delegate void SetStringDelegate(eNkMAIDCapability cap, string value);
    internal delegate void SetEnumDelegate(eNkMAIDCapability cap, NkMAIDEnum value);
    internal delegate void SetArrayDelegate(eNkMAIDCapability cap, NkMAIDArray value);
    internal delegate void SetRangeDelegate(eNkMAIDCapability cap, NkMAIDRange value);
    internal delegate void SetDateTimeDelegate(eNkMAIDCapability cap, DateTime value);
    internal delegate void SetPointDelegate(eNkMAIDCapability cap, NkMAIDPoint value);
    internal delegate void SetRectDelegate(eNkMAIDCapability cap, NkMAIDRect value);
    internal delegate void SetSizeDelegate(eNkMAIDCapability cap, NkMAIDSize value);

    internal delegate void StartCapabilityDelegate(eNkMAIDCapability cap, eNkMAIDDataType type, IntPtr data);
    #endregion

    #region Enums
    internal enum NikonModuleType
    {
        Unknown,
        D2,
        D40,
        D50,
        D70,
        D80,
        D100,
        D200,
        Type0001,
        Type0002,
        Type0003,
        Type0004,
        Type0005,
        Type0006,
        Type0007,
        Type0008,
        Type0009,
        Type0010,
        Type0011,
        Type0012,
        Type0013,
        Type0014,
        Type0015,
        Type0016,
    }
    #endregion

    #region NikonBase
    abstract public class NikonBase
    {
        NikonMd3 _md3;
        NikonScheduler _scheduler;
        NikonObject _object;
        Dictionary<eNkMAIDCapability, NkMAIDCapInfo> _caps;
        NikonModuleType _moduleType;

        internal NikonBase(NikonMd3 md3, NikonScheduler scheduler)
        {
            _md3 = md3;
            _scheduler = scheduler;
            _moduleType = NikonModuleType.Unknown;
            _caps = new Dictionary<eNkMAIDCapability, NkMAIDCapInfo>();
        }

        internal NikonMd3 Md3
        {
            get { return _md3; }
        }

        internal NikonScheduler Scheduler
        {
            get { return _scheduler; }
        }

        internal NikonObject Object
        {
            get { return _object; }
        }

        internal NikonModuleType ModuleType
        {
            get { return _moduleType; }
            set { _moduleType = value; }
        }

        internal void InitializeObject(NikonObject obj)
        {
            _object = obj;
            _object.Open();
            _object.Event += _object_Event;

            RefreshCaps();
        }

        void _object_Event(NikonObject sender, IntPtr refClient, eNkMAIDEvent ulEvent, IntPtr data)
        {
            switch (ulEvent)
            {
                case eNkMAIDEvent.kNkMAIDEvent_CapChange:
                    RefreshCaps();
                    break;
            }

            HandleEvent(sender, ulEvent, data);
        }

        internal virtual void HandleEvent(NikonObject obj, eNkMAIDEvent currentEvent, IntPtr data)
        {
            // Note: Overridden in inheritors
        }

        void RefreshCaps()
        {
            Debug.Assert(Scheduler.WorkerThreadId == Thread.CurrentThread.ManagedThreadId);

            NkMAIDCapInfo[] caps = _object.GetCapInfo();

            lock (_caps)
            {
                _caps.Clear();

                foreach (NkMAIDCapInfo cap in caps)
                {
                    _caps[cap.ulID] = cap;
                }
            }
        }

        public uint Id
        {
            get { return Object.Id; }
        }

        public string Name
        {
            get { return GetString(eNkMAIDCapability.kNkMAIDCapability_Name); }
        }

        public bool SupportsCapability(eNkMAIDCapability capability)
        {
            bool supported = false;

            lock (_caps)
            {
                supported = _caps.ContainsKey(capability);
            }

            return supported;
        }

        public NkMAIDCapInfo GetCapabilityInfo(eNkMAIDCapability cap)
        {
            lock (_caps)
            {
                return _caps[cap];
            }
        }

        public NkMAIDCapInfo[] GetCapabilityInfo()
        {
            NkMAIDCapInfo[] result;

            lock (_caps)
            {
                result = _caps.Values.ToArray();
            }

            return result;
        }

        internal eNkMAIDCapType GetCapabilityType(eNkMAIDCapability cap)
        {
            NkMAIDCapInfo info;
            bool found = false;

            lock (_caps)
            {
                found = _caps.TryGetValue(cap, out info);
            }

            if (!found)
            {
                throw new NikonException("Capability (" + cap.ToString() + ") is not supported");
            }

            return info.ulType;
        }

        #region Get (Type Wrappers)
        public string GetString(eNkMAIDCapability cap)
        {
            return Get(cap) as string;
        }

        public uint GetUnsigned(eNkMAIDCapability cap)
        {
            return (uint)Get(cap);
        }

        public int GetInteger(eNkMAIDCapability cap)
        {
            return (int)Get(cap);
        }

        public bool GetBoolean(eNkMAIDCapability cap)
        {
            return (bool)Get(cap);
        }

        public double GetFloat(eNkMAIDCapability cap)
        {
            return (double)Get(cap);
        }

        public NikonEnum GetEnum(eNkMAIDCapability cap)
        {
            return Get(cap) as NikonEnum;
        }

        public NikonArray GetArray(eNkMAIDCapability cap)
        {
            return Get(cap) as NikonArray;
        }

        public NikonRange GetRange(eNkMAIDCapability cap)
        {
            return Get(cap) as NikonRange;
        }

        public DateTime GetDateTime(eNkMAIDCapability cap)
        {
            return (DateTime)Get(cap);
        }

        public NkMAIDPoint GetPoint(eNkMAIDCapability cap)
        {
            return (NkMAIDPoint)Get(cap);
        }

        public NkMAIDRect GetRect(eNkMAIDCapability cap)
        {
            return (NkMAIDRect)Get(cap);
        }

        public NkMAIDSize GetSize(eNkMAIDCapability cap)
        {
            return (NkMAIDSize)Get(cap);
        }

        public void GetGeneric(eNkMAIDCapability cap, IntPtr destination)
        {
            _scheduler.Invoke(() => { _object.GetGeneric(cap, destination); });
        }

        public void GetArrayGeneric(eNkMAIDCapability cap, IntPtr destination)
        {
            _scheduler.Invoke(() => { _object.GetArrayGeneric(cap, destination); });
        }
        #endregion

        #region Set (Type Wrappers)
        public void SetString(eNkMAIDCapability cap, string value)
        {
            Set(cap, value);
        }

        public void SetUnsigned(eNkMAIDCapability cap, uint value)
        {
            Set(cap, value);
        }

        public void SetInteger(eNkMAIDCapability cap, int value)
        {
            Set(cap, value);
        }

        public void SetBoolean(eNkMAIDCapability cap, bool value)
        {
            Set(cap, value);
        }

        public void SetFloat(eNkMAIDCapability cap, double value)
        {
            Set(cap, value);
        }

        public void SetEnum(eNkMAIDCapability cap, NikonEnum value)
        {
            Set(cap, value);
        }

        public void SetArray(eNkMAIDCapability cap, NikonArray value)
        {
            Set(cap, value);
        }

        public void SetRange(eNkMAIDCapability cap, NikonRange value)
        {
            Set(cap, value);
        }

        public void SetDateTime(eNkMAIDCapability cap, DateTime value)
        {
            Set(cap, value);
        }

        public void SetPoint(eNkMAIDCapability cap, NkMAIDPoint value)
        {
            Set(cap, value);
        }

        public void SetRect(eNkMAIDCapability cap, NkMAIDRect value)
        {
            Set(cap, value);
        }

        public void SetSize(eNkMAIDCapability cap, NkMAIDSize value)
        {
            Set(cap, value);
        }

        public void SetGeneric(eNkMAIDCapability cap, IntPtr source)
        {
            _scheduler.Invoke(() => { _object.SetGeneric(cap, source); });
        }
        #endregion

        #region Get Default (Type Wrappers)
        public string GetDefaultString(eNkMAIDCapability cap)
        {
            return GetDefault(cap) as string;
        }

        public uint GetDefaultUnsigned(eNkMAIDCapability cap)
        {
            return (uint)GetDefault(cap);
        }

        public int GetDefaultInteger(eNkMAIDCapability cap)
        {
            return (int)GetDefault(cap);
        }

        public bool GetDefaultBoolean(eNkMAIDCapability cap)
        {
            return (bool)GetDefault(cap);
        }

        public double GetDefaultFloat(eNkMAIDCapability cap)
        {
            return (double)GetDefault(cap);
        }

        public NikonRange GetDefaultRange(eNkMAIDCapability cap)
        {
            return GetDefault(cap) as NikonRange;
        }

        public DateTime GetDefaultDateTime(eNkMAIDCapability cap)
        {
            return (DateTime)GetDefault(cap);
        }

        public NkMAIDPoint GetDefaultPoint(eNkMAIDCapability cap)
        {
            return (NkMAIDPoint)GetDefault(cap);
        }

        public NkMAIDRect GetDefaultRect(eNkMAIDCapability cap)
        {
            return (NkMAIDRect)GetDefault(cap);
        }

        public NkMAIDSize GetDefaultSize(eNkMAIDCapability cap)
        {
            return (NkMAIDSize )GetDefault(cap);
        }

        public void GetDefaultGeneric(eNkMAIDCapability cap, IntPtr destination)
        {
            _scheduler.Invoke(() => { _object.GetDefaultGeneric(cap, destination); });
        }
        #endregion

        #region Get
        public object Get(eNkMAIDCapability cap)
        {
            switch (GetCapabilityType(cap))
            {
                case eNkMAIDCapType.kNkMAIDCapType_String:
                    return _scheduler.Invoke(new GetStringDelegate(_object.GetString), cap);

                case eNkMAIDCapType.kNkMAIDCapType_Unsigned:
                    return _scheduler.Invoke(new GetUnsignedDelegate(_object.GetUnsigned), cap);

                case eNkMAIDCapType.kNkMAIDCapType_Integer:
                    return _scheduler.Invoke(new GetIntegerDelegate(_object.GetInteger), cap);

                case eNkMAIDCapType.kNkMAIDCapType_Boolean:
                    return _scheduler.Invoke(new GetBooleanDelegate(_object.GetBoolean), cap);

                case eNkMAIDCapType.kNkMAIDCapType_Float:
                    return _scheduler.Invoke(new GetFloatDelegate(_object.GetFloat), cap);

                case eNkMAIDCapType.kNkMAIDCapType_Enum:
                    {
                        NikonEnumWithData result = (NikonEnumWithData)_scheduler.Invoke(new GetEnumWithDataDelegate(_object.GetEnumWithData), cap);
                        return new NikonEnum(result.nativeEnum, result.buffer);
                    }

                case eNkMAIDCapType.kNkMAIDCapType_Array:
                    {
                        NikonArrayWithData result = (NikonArrayWithData)_scheduler.Invoke(new GetArrayWithDataDelegate(_object.GetArrayWithData), cap);
                        return new NikonArray(result.nativeArray, result.buffer);
                    }

                case eNkMAIDCapType.kNkMAIDCapType_Range:
                    {
                        NkMAIDRange result = (NkMAIDRange)_scheduler.Invoke(new GetRangeDelegate(_object.GetRange), cap);
                        return new NikonRange(result);
                    }

                case eNkMAIDCapType.kNkMAIDCapType_DateTime:
                    return _scheduler.Invoke(new GetDateTimeDelegate(_object.GetDateTime), cap);

                case eNkMAIDCapType.kNkMAIDCapType_Point:
                    return _scheduler.Invoke(new GetPointDelegate(_object.GetPoint), cap);

                case eNkMAIDCapType.kNkMAIDCapType_Rect:
                    return _scheduler.Invoke(new GetRectDelegate(_object.GetRect), cap);

                case eNkMAIDCapType.kNkMAIDCapType_Size:
                    return _scheduler.Invoke(new GetSizeDelegate(_object.GetSize), cap);

                default:
                    return null;
            }
        }
        #endregion

        #region Set
        public void Set(eNkMAIDCapability cap, object value)
        {
            switch (GetCapabilityType(cap))
            {
                case eNkMAIDCapType.kNkMAIDCapType_String:
                    _scheduler.Invoke(new SetStringDelegate(_object.SetString), cap, value);
                    break;

                case eNkMAIDCapType.kNkMAIDCapType_Unsigned:
                    _scheduler.Invoke(new SetUnsignedDelegate(_object.SetUnsigned), cap, value);
                    break;

                case eNkMAIDCapType.kNkMAIDCapType_Integer:
                    _scheduler.Invoke(new SetIntegerDelegate(_object.SetInteger), cap, value);
                    break;

                case eNkMAIDCapType.kNkMAIDCapType_Boolean:
                    _scheduler.Invoke(new SetBooleanDelegate(_object.SetBoolean), cap, value);
                    break;

                case eNkMAIDCapType.kNkMAIDCapType_Float:
                    _scheduler.Invoke(new SetFloatDelegate(_object.SetFloat), cap, value);
                    break;

                case eNkMAIDCapType.kNkMAIDCapType_Enum:
                    _scheduler.Invoke(new SetEnumDelegate(_object.SetEnum), cap, (value as NikonEnum).Enum);
                    break;

                case eNkMAIDCapType.kNkMAIDCapType_Array:
                    _scheduler.Invoke(new SetArrayDelegate(_object.SetArray), cap, (value as NikonArray).Array);
                    break;

                case eNkMAIDCapType.kNkMAIDCapType_Range:
                    _scheduler.Invoke(new SetRangeDelegate(_object.SetRange), cap, (value as NikonRange).Range);
                    break;

                case eNkMAIDCapType.kNkMAIDCapType_DateTime:
                    _scheduler.Invoke(new SetDateTimeDelegate(_object.SetDateTime), cap, value);
                    break;

                case eNkMAIDCapType.kNkMAIDCapType_Point:
                    _scheduler.Invoke(new SetPointDelegate(_object.SetPoint), cap, value);
                    break;

                case eNkMAIDCapType.kNkMAIDCapType_Rect:
                    _scheduler.Invoke(new SetUnsignedDelegate(_object.SetUnsigned), cap, value);
                    break;

                case eNkMAIDCapType.kNkMAIDCapType_Size:
                    _scheduler.Invoke(new SetUnsignedDelegate(_object.SetUnsigned), cap, value);
                    break;
            }
        }
        #endregion

        #region Get Default
        public object GetDefault(eNkMAIDCapability cap)
        {
            switch (GetCapabilityType(cap))
            {
                case eNkMAIDCapType.kNkMAIDCapType_String:
                    return _scheduler.Invoke(new GetStringDelegate(_object.GetDefaultString), cap);

                case eNkMAIDCapType.kNkMAIDCapType_Unsigned:
                    return _scheduler.Invoke(new GetUnsignedDelegate(_object.GetDefaultUnsigned), cap);

                case eNkMAIDCapType.kNkMAIDCapType_Integer:
                    return _scheduler.Invoke(new GetIntegerDelegate(_object.GetDefaultInteger), cap);

                case eNkMAIDCapType.kNkMAIDCapType_Boolean:
                    return _scheduler.Invoke(new GetBooleanDelegate(_object.GetDefaultBoolean), cap);

                case eNkMAIDCapType.kNkMAIDCapType_Float:
                    return _scheduler.Invoke(new GetFloatDelegate(_object.GetDefaultFloat), cap);

                case eNkMAIDCapType.kNkMAIDCapType_Range:
                    {
                        NkMAIDRange result = (NkMAIDRange)_scheduler.Invoke(new GetRangeDelegate(_object.GetDefaultRange), cap);
                        return new NikonRange(result);
                    }

                case eNkMAIDCapType.kNkMAIDCapType_DateTime:
                    return _scheduler.Invoke(new GetDateTimeDelegate(_object.GetDefaultDateTime), cap);

                case eNkMAIDCapType.kNkMAIDCapType_Point:
                    return _scheduler.Invoke(new GetPointDelegate(_object.GetDefaultPoint), cap);

                case eNkMAIDCapType.kNkMAIDCapType_Rect:
                    return _scheduler.Invoke(new GetRectDelegate(_object.GetDefaultRect), cap);

                case eNkMAIDCapType.kNkMAIDCapType_Size:
                    return _scheduler.Invoke(new GetSizeDelegate(_object.GetDefaultSize), cap);

                // Note: 'Array' and 'Enum' are not here

                default:
                    return null;
            }
        }
        #endregion

        public void Start(eNkMAIDCapability cap)
        {
            Start(cap, eNkMAIDDataType.kNkMAIDDataType_Null, IntPtr.Zero);
        }

        public void Start(eNkMAIDCapability cap, eNkMAIDDataType dataType, IntPtr data)
        {
            _scheduler.Invoke(new StartCapabilityDelegate(_object.CapStart), cap, dataType, data);
        }
    }
    #endregion

    #region NikonManager
    public class NikonManager : NikonBase
    {
        Dictionary<uint, NikonDevice> _devices;
        const string _defaultMd3EntryPoint = "MAIDEntryPoint";

        event DeviceAddedDelegate _deviceAdded;
        event DeviceRemovedDelegate _deviceRemoved;

        // Note: Add and remove event handlers on the thread where they are fired

        public event DeviceAddedDelegate DeviceAdded
        {
            add     { Scheduler.AddOrRemoveEvent(() => { _deviceAdded += value; }); }
            remove  { Scheduler.AddOrRemoveEvent(() => { _deviceAdded -= value; }); }
        }

        public event DeviceRemovedDelegate DeviceRemoved
        {
            add     { Scheduler.AddOrRemoveEvent(() => { _deviceRemoved += value; }); }
            remove  { Scheduler.AddOrRemoveEvent(() => { _deviceRemoved -= value; }); }
        }

        public NikonManager(string md3File)
            : this(md3File, _defaultMd3EntryPoint, SynchronizationContext.Current)
        {
        }

        public NikonManager(string md3File, string md3EntryPoint)
            : this(md3File, md3EntryPoint, SynchronizationContext.Current)
        {
        }

        public NikonManager(string md3File, SynchronizationContext context)
            : this(md3File, _defaultMd3EntryPoint, context)
        {
        }

        public NikonManager(string md3File, string md3EntryPoint, SynchronizationContext context)
            : base(new NikonMd3(md3File, md3EntryPoint), new NikonScheduler(context))
        {
            _devices = new Dictionary<uint, NikonDevice>();

            Scheduler.Invoke(() =>
            {
                InitializeObject(new NikonObject(Md3, null, 0));

                string[] moduleName = Object.GetString(eNkMAIDCapability.kNkMAIDCapability_Name).Split(' ');

                NikonModuleType type;
                if (moduleName.Length > 0 && Enum.TryParse(moduleName[0], out type))
                {
                    ModuleType = type;
                }
                else
                {
                    ModuleType = NikonModuleType.Unknown;
                }

                double asyncRate = (double)Object.GetUnsigned(eNkMAIDCapability.kNkMAIDCapability_AsyncRate);

                Scheduler.SchedulePeriodicTask(() =>
                {
                    Async();
                },
                asyncRate);
            });
        }

        void Async()
        {
            Debug.Assert(Scheduler.WorkerThreadId == Thread.CurrentThread.ManagedThreadId);

            try
            {
                Object.Async();
            }
            catch (NikonException ex)
            {
                // Note: Allow 'CameraNotFound' - this is thrown from Async when the camera is removed
                if (ex.ErrorCode != eNkMAIDResult.kNkMAIDResult_CameraNotFound)
                {
                    throw;
                }
            }
        }

        internal override void HandleEvent(NikonObject obj, eNkMAIDEvent currentEvent, IntPtr data)
        {
            switch (currentEvent)
            {
                case eNkMAIDEvent.kNkMAIDEvent_AddChild:
                    HandleAddChild(data);
                    break;

                case eNkMAIDEvent.kNkMAIDEvent_RemoveChild:
                    HandleRemoveChild(data);
                    break;
            }
        }

        void HandleAddChild(IntPtr data)
        {
            uint id = (uint)data.ToInt32();

            NikonDevice device = new NikonDevice(Md3, Scheduler, Object, ModuleType, id);

            lock (_devices)
            {
                Debug.Assert(!_devices.ContainsKey(id));
                _devices[id] = device;
            }

            Scheduler.Callback(new DeviceAddedDelegate(OnDeviceAdded), this, device);
        }

        void HandleRemoveChild(IntPtr data)
        {
            uint id = (uint)data.ToInt32();

            NikonDevice device = null;

            lock (_devices)
            {
                Debug.Assert(_devices.ContainsKey(id));
                device = _devices[id];
                _devices.Remove(id);
            }

            device.Object.Close();

            Scheduler.Callback(new DeviceRemovedDelegate(OnDeviceRemoved), this, device);
        }

        void OnDeviceAdded(NikonManager sender, NikonDevice device)
        {
            if (_deviceAdded != null)
            {
                _deviceAdded(sender, device);
            }
        }

        void OnDeviceRemoved(NikonManager sender, NikonDevice device)
        {
            if (_deviceRemoved != null)
            {
                _deviceRemoved(sender, device);
            }
        }

        public int DeviceCount
        {
            get
            {
                int count = 0;
                lock (_devices)
                {
                    count = _devices.Count;
                }
                return count;
            }
        }

        public NikonDevice GetDeviceByIndex(uint index)
        {
            NikonDevice device = null;

            lock (_devices)
            {
                int i = 0;
                foreach (NikonDevice d in _devices.Values)
                {
                    if (i == index)
                    {
                        device = d;
                        break;
                    }

                    i++;
                }
            }

            return device;
        }

        public NikonDevice GetDeviceById(uint id)
        {
            NikonDevice device = null;

            lock (_devices)
            {
                if (!_devices.TryGetValue(id, out device))
                {
                    device = null;
                }
            }

            return device;
        }

        public void Shutdown()
        {
            Scheduler.Shutdown();

            lock (_devices)
            {
                foreach (NikonDevice device in _devices.Values)
                {
                    device.Object.Close();
                }

                _devices = null;
            }

            Object.Close();

            Md3.Close();
        }
    }
    #endregion

    #region NikonDevice
    public class NikonDevice : NikonBase
    {
        NikonImage _currentImage;
        uint _currentItemId;
        int _bulbCaptureShutterSpeedBackup;

        event PreviewReadyDelegate _previewReady;
        event PreviewReadyDelegate _lowResolutionPreviewReady;
        event ThumbnailReadyDelegate _thumbnailReady;
        event ImageReadyDelegate _imageReady;
        event CaptureCompleteDelegate _captureComplete;
        event CapabilityChangedDelegate _capabilityChanged;
        event CapabilityChangedDelegate _capabilityValueChanged;
        event VideoFragmentReadyDelegate _videoFragmentReady;
        event VideoRecordingInterruptedDelegate _videoRecordingInterrupted;
        event ProgressDelegate _progress;

        // Note: Add and remove event handlers on the thread where they are fired

        public event PreviewReadyDelegate PreviewReady
        {
            add     { Scheduler.AddOrRemoveEvent(() => { _previewReady += value; }); }
            remove  { Scheduler.AddOrRemoveEvent(() => { _previewReady -= value; }); }
        }

        public event PreviewReadyDelegate LowResolutionPreviewReady
        {
            add     { Scheduler.AddOrRemoveEvent(() => { _lowResolutionPreviewReady += value; }); }
            remove  { Scheduler.AddOrRemoveEvent(() => { _lowResolutionPreviewReady -= value; }); }
        }

        public event ThumbnailReadyDelegate ThumbnailReady
        {
            add     { Scheduler.AddOrRemoveEvent(() => { _thumbnailReady += value; }); }
            remove  { Scheduler.AddOrRemoveEvent(() => { _thumbnailReady -= value; }); }
        }

        public event ImageReadyDelegate ImageReady
        {
            add     { Scheduler.AddOrRemoveEvent(() => { _imageReady += value; }); }
            remove  { Scheduler.AddOrRemoveEvent(() => { _imageReady -= value; }); }
        }

        public event CaptureCompleteDelegate CaptureComplete
        {
            add     { Scheduler.AddOrRemoveEvent(() => { _captureComplete += value; }); }
            remove  { Scheduler.AddOrRemoveEvent(() => { _captureComplete -= value; }); }
        }

        public event CapabilityChangedDelegate CapabilityChanged
        {
            add     { Scheduler.AddOrRemoveEvent(() => { _capabilityChanged += value; }); }
            remove  { Scheduler.AddOrRemoveEvent(() => { _capabilityChanged -= value; }); }
        }

        public event CapabilityChangedDelegate CapabilityValueChanged
        {
            add     { Scheduler.AddOrRemoveEvent(() => { _capabilityValueChanged += value; }); }
            remove  { Scheduler.AddOrRemoveEvent(() => { _capabilityValueChanged -= value; }); }
        }

        public event VideoFragmentReadyDelegate VideoFragmentReady
        {
            add     { Scheduler.AddOrRemoveEvent(() => { _videoFragmentReady += value; }); }
            remove  { Scheduler.AddOrRemoveEvent(() => { _videoFragmentReady -= value; }); }
        }

        public event VideoRecordingInterruptedDelegate VideoRecordingInterrupted
        {
            add     { Scheduler.AddOrRemoveEvent(() => { _videoRecordingInterrupted += value; }); }
            remove  { Scheduler.AddOrRemoveEvent(() => { _videoRecordingInterrupted -= value; }); }
        }

        public event ProgressDelegate Progress
        {
            add     { Scheduler.AddOrRemoveEvent(() => { _progress += value; }); }
            remove  { Scheduler.AddOrRemoveEvent(() => { _progress -= value; }); }
        }

        internal NikonDevice(NikonMd3 md3, NikonScheduler scheduler, NikonObject parent, NikonModuleType moduleType, uint deviceId)
            : base(md3, scheduler)
        {
            Debug.Assert(Scheduler.WorkerThreadId == Thread.CurrentThread.ManagedThreadId);

            ModuleType = moduleType;

            NikonObject source = new NikonObject(md3, parent, deviceId);
            InitializeObject(source);
        }

        internal override void HandleEvent(NikonObject obj, eNkMAIDEvent currentEvent, IntPtr data)
        {
            switch (currentEvent)
            {
                case eNkMAIDEvent.kNkMAIDEvent_AddChild:
                case eNkMAIDEvent.kNkMAIDEvent_AddChildInCard:
                    HandleAddChild(data);
                    break;

                case eNkMAIDEvent.kNkMAIDEvent_AddPreviewImage:
                    HandleAddPreviewImage(data);
                    break;

                case eNkMAIDEvent.kNkMAIDEvent_CaptureComplete:
                    Scheduler.Callback(new CaptureCompleteDelegate(OnCaptureComplete), this, (int)data);
                    break;

                case eNkMAIDEvent.kNkMAIDEvent_CapChange:
                    Scheduler.Callback(new CapabilityChangedDelegate(OnCapabilityChanged), this, (eNkMAIDCapability)data);
                    break;

                case eNkMAIDEvent.kNkMAIDEvent_CapChangeValueOnly:
                    Scheduler.Callback(new CapabilityChangedDelegate(OnCapabilityValueChanged), this, (eNkMAIDCapability)data);
                    break;

                case eNkMAIDEvent.kNkMAIDEvent_RecordingInterrupted:
                    Scheduler.Callback(new VideoRecordingInterruptedDelegate(OnVideoRecordingInterrupted), this, (int)data);
                    break;

                default:
                    Debug.Print("Unhandled event: " + currentEvent + " (" + data.ToString() + ")");
                    break;
            }
        }

        void HandleAddPreviewImage(IntPtr data)
        {
            try
            {
                // Note:
                // The two event checks below are not thread safe, since
                // events are hooked up on the callback thread (and we're
                // currently on the worker thread). So there is a minor
                // race condition here. We choose to live with it for efficency
                // purposes.

                bool doPreview = SupportsCapability(eNkMAIDCapability.kNkMAIDCapability_GetPreviewImageNormal) &&
                    _previewReady != null;

                bool doLowResPreview = SupportsCapability(eNkMAIDCapability.kNkMAIDCapability_GetPreviewImageLow) &&
                    _lowResolutionPreviewReady != null;

                if (doPreview || doLowResPreview)
                {
                    Object.SetUnsigned(eNkMAIDCapability.kNkMAIDCapability_CurrentPreviewID, (uint)data.ToInt32());
                }

                if (doPreview)
                {
                    GetPreviewAndFireEvent(
                        eNkMAIDCapability.kNkMAIDCapability_GetPreviewImageNormal,
                        new PreviewReadyDelegate(OnPreviewReady));
                }

                if (doLowResPreview)
                {
                    GetPreviewAndFireEvent(
                        eNkMAIDCapability.kNkMAIDCapability_GetPreviewImageLow,
                        new PreviewReadyDelegate(OnLowResolutionPreviewReady));
                }
            }
            catch (NikonException ex)
            {
                Debug.Print("Failed to retrieve preview image (" + ex.ToString() + ")");

                // TODO: BUG(?): Why do we sometimes get 'ValueOutOfBounds' when retrieving the preview images?
                if (ex.ErrorCode != eNkMAIDResult.kNkMAIDResult_ValueOutOfBounds)
                {
                    throw;
                }
            }
        }

        void DataItemAcquire(NikonObject data)
        {
            // Listen for progress
            data.Progress += data_Progress;

            // Listen for data
            data.DataFile += data_DataFile;
            data.DataImage += data_DataImage;
            data.DataSound += data_DataSound;

            // Try to acquire the data object
            try
            {
                data.CapStart(
                    eNkMAIDCapability.kNkMAIDCapability_Acquire,
                    eNkMAIDDataType.kNkMAIDDataType_Null,
                    IntPtr.Zero);
            }
            catch (NikonException ex)
            {
                // Is this a 'NotSupported' exception?
                bool isNotSupported = (ex.ErrorCode == eNkMAIDResult.kNkMAIDResult_NotSupported);

                // Is this a 'Thumbnail' data object?
                bool isThumbnail = (data.Id == (uint)eNkMAIDDataObjType.kNkMAIDDataObjType_Thumbnail);

                // According to the documentation, acquiring a thumbnail data object
                // sometimes produces a NotSupported error. Apparently this is expected,
                // so we ignore this specific case here.
                bool isAllowed = isNotSupported && isThumbnail;

                if (isAllowed)
                {
                    Debug.Print("Failed to retrieve thumbnail image");
                }
                else
                {
                    // If this is some other case, rethrow the exception
                    throw;
                }
            }
            finally
            {
                // Unhook data object events
                data.Progress -= data_Progress;
                data.DataFile -= data_DataFile;
                data.DataImage -= data_DataImage;
                data.DataSound -= data_DataSound;
            }
        }

        unsafe void DataItemGetVideoImage(NikonObject data)
        {
            string name = data.GetString(eNkMAIDCapability.kNkMAIDCapability_Name);
            NkMAIDSize videoDimensions = data.GetSize(eNkMAIDCapability.kNkMAIDCapability_Pixels);

            NkMAIDGetVideoImage videoImage = new NkMAIDGetVideoImage();
            data.GetGeneric(eNkMAIDCapability.kNkMAIDCapability_GetVideoImage, new IntPtr(&videoImage));

            // Note: Download 4MB at the time
            const int chunkSize = 4 * 1024 * 1024;

            uint totalSize = videoImage.ulDataSize;

            for (uint offset = 0; offset < totalSize; offset += chunkSize)
            {
                uint fragmentSize = Math.Min(chunkSize, totalSize - offset);

                byte[] buffer = new byte[fragmentSize];

                fixed (byte* pBuffer = buffer)
                {
                    videoImage.ulOffset = offset;
                    videoImage.ulReadSize = (uint)buffer.Length;
                    videoImage.ulDataSize = (uint)buffer.Length;
                    videoImage.pData = new IntPtr(pBuffer);

                    data.GetArrayGeneric(eNkMAIDCapability.kNkMAIDCapability_GetVideoImage, new IntPtr(&videoImage));
                }

                NikonVideoFragment fragment = new NikonVideoFragment(
                    name,
                    totalSize,
                    offset,
                    buffer,
                    videoDimensions.w,
                    videoDimensions.h);

                Scheduler.Callback(new VideoFragmentReadyDelegate(OnVideoFragmentReady), this, fragment);
            }
        }

        void HandleAddChild(IntPtr id)
        {
            NikonObject item = new NikonObject(Md3, Object, (uint)id.ToInt32());

            List<uint> dataIds = new List<uint>();

            item.Open();
            item.Event += (NikonObject obj, IntPtr refClient, eNkMAIDEvent currentEvent, IntPtr data) =>
            {
                if (currentEvent == eNkMAIDEvent.kNkMAIDEvent_AddChild)
                {
                    dataIds.Add((uint)data.ToInt32());
                }
            };

            item.EnumChildren();

            _currentItemId = item.Id;

            foreach (var dataId in dataIds)
            {
                eNkMAIDDataObjType dataObjectType = (eNkMAIDDataObjType)dataId;

                NikonObject data = new NikonObject(Md3, item, dataId);

                data.Open();

                switch (dataObjectType)
                {
                    case eNkMAIDDataObjType.kNkMAIDDataObjType_Thumbnail:
                    case eNkMAIDDataObjType.kNkMAIDDataObjType_File | eNkMAIDDataObjType.kNkMAIDDataObjType_Thumbnail:
                        // Note:
                        // We do a 'thread-unsafe' check of the thumbnail-ready event here. No
                        // need to acquire if the user hasn't hooked up the thumbnail event.
                        if (_thumbnailReady != null)
                        {
                            DataItemAcquire(data);
                        }
                        break;

                    case eNkMAIDDataObjType.kNkMAIDDataObjType_Image:
                    case eNkMAIDDataObjType.kNkMAIDDataObjType_Sound:
                    case eNkMAIDDataObjType.kNkMAIDDataObjType_File:
                    case eNkMAIDDataObjType.kNkMAIDDataObjType_File | eNkMAIDDataObjType.kNkMAIDDataObjType_Image:
                    case eNkMAIDDataObjType.kNkMAIDDataObjType_File | eNkMAIDDataObjType.kNkMAIDDataObjType_Sound:
                        DataItemAcquire(data);
                        break;

                    case eNkMAIDDataObjType.kNkMAIDDataObjType_Video:
                        // Note:
                        // We do a 'thread-unsafe' check of the videofragment-ready event here. No
                        // need to download videos if the user hasn't hooked up the event.
                        if (_videoFragmentReady != null)
                        {
                            DataItemGetVideoImage(data);
                        }
                        break;

                    default:
                        Debug.Print("Unknown data object type: " + dataObjectType.ToString());
                        break;
                }

                data.Close();
            }

            item.Close();
        }

        void GetPreviewAndFireEvent(eNkMAIDCapability previewCapabilty, PreviewReadyDelegate d)
        {
            NikonArrayWithData previewArray = Object.GetArrayWithData(previewCapabilty);

            NikonPreview preview = new NikonPreview(previewArray.buffer);

            Scheduler.Callback(d, this, preview);
        }

        void data_Progress(NikonObject sender,
            eNkMAIDCommand ulCommand,
            UInt32 ulParam,
            IntPtr refComplete,
            UInt32 ulDone,
            UInt32 ulTotal)
        {
            Scheduler.Callback(new ProgressDelegate(OnProgress), this, (eNkMAIDDataObjType)sender.Id, (int)ulDone, (int)ulTotal);
        }

        void data_DataImage(NikonObject sender, NkMAIDImageInfo imageInfo, IntPtr data)
        {
            NikonThumbnail thumbnail = new NikonThumbnail(imageInfo, data);
            Scheduler.Callback(new ThumbnailReadyDelegate(OnThumbnailReady), this, thumbnail);
        }

        void data_DataSound(NikonObject sender, NkMAIDSoundInfo soundInfo, IntPtr data)
        {
            Debug.Print("DataProcSoundInfo event fired");
        }

        void data_DataFile(NikonObject sender, NkMAIDFileInfo fileInfo, IntPtr data)
        {
            if (fileInfo.ulStart == 0)
            {
                Debug.Assert(_currentImage == null);

                int size = (int)fileInfo.ulTotalLength;
                NikonImageType type = (NikonImageType)(_currentItemId >> 27);
                int number = (int)((_currentItemId << 8) >> 8);
                bool isFragmentOfRawPlusJpeg = (_currentItemId & (1 << 26)) != 0;

                _currentImage = new NikonImage(size, type, number, isFragmentOfRawPlusJpeg);
            }

            Debug.Assert(_currentImage != null);

            int offset = (int)fileInfo.ulStart;
            int length = (int)fileInfo.ulLength;

            _currentImage.CopyFrom(data, offset, length);

            bool complete = (fileInfo.ulTotalLength == fileInfo.ulStart + fileInfo.ulLength);

            if (complete)
            {
                NikonImage image = _currentImage;
                _currentImage = null;

                Scheduler.Callback(new ImageReadyDelegate(OnImageReady), this, image);
            }
        }

        void OnPreviewReady(NikonDevice sender, NikonPreview preview)
        {
            if (_previewReady != null)
            {
                _previewReady(sender, preview);
            }
        }

        void OnLowResolutionPreviewReady(NikonDevice sender, NikonPreview preview)
        {
            if (_lowResolutionPreviewReady != null)
            {
                _lowResolutionPreviewReady(sender, preview);
            }
        }

        void OnThumbnailReady(NikonDevice sender, NikonThumbnail thumbnail)
        {
            if (_thumbnailReady != null)
            {
                _thumbnailReady(sender, thumbnail);
            }
        }

        void OnImageReady(NikonDevice sender, NikonImage image)
        {
            if (_imageReady != null)
            {
                _imageReady(sender, image);
            }
        }

        void OnCaptureComplete(NikonDevice sender, int data)
        {
            if (_captureComplete != null)
            {
                _captureComplete(sender, data);
            }
        }

        void OnCapabilityChanged(NikonDevice sender, eNkMAIDCapability capability)
        {
            if (_capabilityChanged != null)
            {
                _capabilityChanged(sender, capability);
            }
        }

        void OnCapabilityValueChanged(NikonDevice sender, eNkMAIDCapability capability)
        {
            if (_capabilityValueChanged != null)
            {
                _capabilityValueChanged(sender, capability);
            }
        }

        void OnVideoFragmentReady(NikonDevice sender, NikonVideoFragment fragment)
        {
            if (_videoFragmentReady != null)
            {
                _videoFragmentReady(sender, fragment);
            }
        }

        void OnVideoRecordingInterrupted(NikonDevice sender, int error)
        {
            if (_videoRecordingInterrupted != null)
            {
                _videoRecordingInterrupted(sender, error);
            }
        }

        void OnProgress(NikonDevice sender, eNkMAIDDataObjType type, int done, int total)
        {
            if (_progress != null)
            {
                _progress(sender, type, done, total);
            }
        }

        public void StartRecordVideo()
        {
            SetUnsigned(eNkMAIDCapability.kNkMAIDCapability_MovRecInCardStatus, 1);
        }

        public void StopRecordVideo()
        {
            SetUnsigned(eNkMAIDCapability.kNkMAIDCapability_MovRecInCardStatus, 0);
        }

        public void Capture()
        {
            Start(eNkMAIDCapability.kNkMAIDCapability_Capture);
        }

        public bool LiveViewEnabled
        {
            set { SetUnsigned(eNkMAIDCapability.kNkMAIDCapability_LiveViewStatus, value ? 1U : 0U); }
            get { return GetUnsigned(eNkMAIDCapability.kNkMAIDCapability_LiveViewStatus) == 0 ? false : true; }
        }

        public NikonLiveViewImage GetLiveViewImage()
        {
            NikonArray a = GetArray(eNkMAIDCapability.kNkMAIDCapability_GetLiveViewImage);

            int headerSize = 0;

            switch (ModuleType)
            {
                case NikonModuleType.Type0001:
                case NikonModuleType.Type0002:
                    headerSize = 64;
                    break;

                case NikonModuleType.Type0003:
                    headerSize = 128;
                    break;

                default:
                    headerSize = 384;
                    break;
            }

            return new NikonLiveViewImage(a.Buffer, headerSize);
        }

        public void StartBulbCapture()
        {
            // Lock camera
            SetBoolean(
                eNkMAIDCapability.kNkMAIDCapability_LockCamera,
                true);

            // Change the exposure mode to 'Manual'
            NikonEnum exposureMode = GetEnum(eNkMAIDCapability.kNkMAIDCapability_ExposureMode);
            bool foundManual = false;
            for (int i = 0; i < exposureMode.Length; i++)
            {
                if ((uint)exposureMode[i] == (uint)eNkMAIDExposureMode.kNkMAIDExposureMode_Manual)
                {
                    exposureMode.Index = i;
                    foundManual = true;
                    SetEnum(eNkMAIDCapability.kNkMAIDCapability_ExposureMode, exposureMode);
                    break;
                }
            }

            // Throw exception if the 'Manual' exposure mode wasn't found
            if (!foundManual)
            {
                throw new NikonException("Failed to find the 'Manual' exposure mode");
            }

            // Change the shutterspeed to 'Bulb'
            NikonEnum shutterSpeed = GetEnum(eNkMAIDCapability.kNkMAIDCapability_ShutterSpeed);
            _bulbCaptureShutterSpeedBackup = shutterSpeed.Index;
            bool foundBulb = false;
            for (int i = 0; i < shutterSpeed.Length; i++)
            {
                if (shutterSpeed[i].ToString().ToLower().Contains("bulb"))
                {
                    shutterSpeed.Index = i;
                    foundBulb = true;
                    SetEnum(eNkMAIDCapability.kNkMAIDCapability_ShutterSpeed, shutterSpeed);
                    break;
                }
            }

            // Throw exception if the 'Bulb' shutterspeed wasn't found
            if (!foundBulb)
            {
                throw new NikonException("Failed to find the 'Bulb' shutter speed");
            }

            // Capture
            try
            {
                Capture();
            }
            catch (NikonException ex)
            {
                // Ignore 'BulbReleaseBusy' exception - it's expected
                if (ex.ErrorCode != eNkMAIDResult.kNkMAIDResult_BulbReleaseBusy)
                {
                    throw;
                }
            }
        }

        public void StopBulbCapture()
        {
            // Terminate capture
            NkMAIDTerminateCapture terminate = new NkMAIDTerminateCapture();
            terminate.ulParameter1 = 0;
            terminate.ulParameter2 = 0;

            unsafe
            {
                IntPtr terminatePointer = new IntPtr(&terminate);

                Start(
                    eNkMAIDCapability.kNkMAIDCapability_TerminateCapture,
                    eNkMAIDDataType.kNkMAIDDataType_GenericPtr,
                    terminatePointer);
            }

            // Restore original shutter speed
            NikonEnum shutterSpeed = GetEnum(eNkMAIDCapability.kNkMAIDCapability_ShutterSpeed);
            shutterSpeed.Index = _bulbCaptureShutterSpeedBackup;
            SetEnum(eNkMAIDCapability.kNkMAIDCapability_ShutterSpeed, shutterSpeed);

            // Unlock camera
            SetBoolean(
                eNkMAIDCapability.kNkMAIDCapability_LockCamera,
                false);
        }
    }
    #endregion
}
