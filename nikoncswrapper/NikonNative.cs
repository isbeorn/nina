//
// This work is licensed under a Creative Commons Attribution 3.0 Unported License.
//
// Thomas Dideriksen (thomas@dideriksen.com)
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace Nikon
{
    #region Kernel32 Imports
    internal class Kernel32
    {
        const string _DLL = "Kernel32.dll";

        [DllImport(_DLL, CharSet = CharSet.Ansi)]
        internal static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)]string lpFileName);

        [DllImport(_DLL, CharSet = CharSet.Ansi)]
        internal static extern Int32 FreeLibrary(IntPtr hLibModule);

        [DllImport(_DLL, CharSet = CharSet.Ansi)]
        internal static extern IntPtr GetProcAddress(IntPtr hModule, [MarshalAs(UnmanagedType.LPStr)]string lpProcName);
    }
    #endregion

    #region Nikon Native Structs Extensions
    public static class NikonNativeStructExtensions
    {
        public static uint GetDataSize(this NkMAIDArray a)
        {
            return (uint)(a.ulElements * a.wPhysicalBytes);
        }

        public static uint GetDataSize(this NkMAIDEnum e)
        {
            return (uint)(e.ulElements * e.wPhysicalBytes);
        }

        public static unsafe string GetString(this NkMAIDString s)
        {
            return Marshal.PtrToStringAnsi(new IntPtr(s.str));
        }

        public static unsafe string GetDescription(this NkMAIDCapInfo c)
        {
            return Marshal.PtrToStringAnsi(new IntPtr(c.szDescription));
        }

        public static DateTime GetDateTime(this NkMAIDDateTime d)
        {
            return new DateTime(
                d.nYear,
                d.nMonth + 1, // Note: Convert range from [0-11] to [1-12]
                d.nDay,
                d.nHour,
                d.nMinute,
                d.nSecond);
        }

        public static NkMAIDDateTime Create(this NkMAIDDateTime d, DateTime value)
        {
            NkMAIDDateTime res = new NkMAIDDateTime();
            res.nYear = (UInt16)value.Year;
            res.nMonth = (UInt16)(value.Month - 1); // Note: Convert range from [1-12] to [0-11]
            res.nDay = (UInt16)value.Day;
            res.nHour = (UInt16)value.Hour;
            res.nMinute = (UInt16)value.Minute;
            res.nSecond = (UInt16)value.Second;
            return res;
        }

        public static bool CanStart(this NkMAIDCapInfo c)       { return (c.ulOperations & eNkMAIDCapOperation.kNkMAIDCapOperation_Start) != 0; }
        public static bool CanGet(this NkMAIDCapInfo c)         { return (c.ulOperations & eNkMAIDCapOperation.kNkMAIDCapOperation_Get) != 0; }
        public static bool CanSet(this NkMAIDCapInfo c)         { return (c.ulOperations & eNkMAIDCapOperation.kNkMAIDCapOperation_Set) != 0; }
        public static bool CanGetArray(this NkMAIDCapInfo c)    { return (c.ulOperations & eNkMAIDCapOperation.kNkMAIDCapOperation_GetArray) != 0; }
        public static bool CanGetDefault(this NkMAIDCapInfo c)  { return (c.ulOperations & eNkMAIDCapOperation.kNkMAIDCapOperation_GetDefault) != 0; }

        public static bool IsHidden(this NkMAIDCapInfo c)       { return (c.ulVisibility & eNkMAIDCapVisibility.kNkMAIDCapVisibility_Hidden) != 0; }
        public static bool IsAdvanced(this NkMAIDCapInfo c)     { return (c.ulVisibility & eNkMAIDCapVisibility.kNkMAIDCapVisibility_Advanced) != 0; }
        public static bool IsVendor(this NkMAIDCapInfo c)       { return (c.ulVisibility & eNkMAIDCapVisibility.kNkMAIDCapVisibility_Vendor) != 0; }
        public static bool IsGroup(this NkMAIDCapInfo c)        { return (c.ulVisibility & eNkMAIDCapVisibility.kNkMAIDCapVisibility_Group) != 0; }
        public static bool IsGroupMember(this NkMAIDCapInfo c)  { return (c.ulVisibility & eNkMAIDCapVisibility.kNkMAIDCapVisibility_GroupMember) != 0; }
        public static bool IsInvalid(this NkMAIDCapInfo c)      { return (c.ulVisibility & eNkMAIDCapVisibility.kNkMAIDCapVisibility_Invalid) != 0; }
    }
    #endregion

    #region NikonException class
    public class NikonException : Exception
    {
        eNkMAIDResult _errorCode;
        eNkMAIDCommand _errorCommand;
        UInt32 _errorParam;
        eNkMAIDDataType _errorDataType;
        IntPtr _errorData;

        public NikonException(eNkMAIDResult errorCode, eNkMAIDCommand errorCommand, UInt32 errorParam, eNkMAIDDataType errorDataType, IntPtr errorData)
            : base(string.Format("[{0}] ({1}, {2}, {3}, {4})",
                errorCode.ToString(),
                errorCommand.ToString(),
                errorParam.ToString(),
                errorDataType.ToString(),
                errorData.ToString()))
        {
            _errorCode = errorCode;
            _errorCommand = errorCommand;
            _errorParam = errorParam;
            _errorDataType = errorDataType;
            _errorData = errorData;
        }

        public NikonException(string message)
            : base(message)
        {
        }

        public override string Message
        {
            get { return ToString(); }
        }

        public override string ToString()
        {
            return base.Message;
        }

        public eNkMAIDResult ErrorCode
        {
            get { return _errorCode; }
        }
    }
    #endregion

    #region Md3 class
    internal unsafe class NikonMd3
    {
        MAIDEntryPointProcDelegate _entryPoint;
        IntPtr _handle;

        internal NikonMd3(string md3File, string md3EntryPoint)
        {
            _handle = Kernel32.LoadLibrary(md3File);

            if (_handle == IntPtr.Zero)
            {
                const string message = "You can download MD3 files from Nikons website: https://sdk.nikonimaging.com/apply/";

                if (!File.Exists(md3File))
                {
                    throw new NikonException("Couldn't find MD3 file: " + md3File + ", " + message);
                }

                MachineType machineType = GetDllMachineType(md3File);
                switch (machineType)
                {
                    case MachineType.x86:
                        if (IntPtr.Size == 8)
                        {
                            throw new NikonException("It looks like you're trying to use a 32-bit MD3 file (" + md3File + ") in your 64-bit application. Please use the 64-bit (x64) MD3 file instead. " + message);
                        }
                        break;

                    case MachineType.x64:
                        if (IntPtr.Size == 4)
                        {
                            throw new NikonException("It looks like you're trying to use a 64-bit MD3 file (" + md3File + ") in your 32-bit application. Please use the 32-bit (x86) MD3 file instead. " + message);
                        }
                        break;
                }

                throw new NikonException("Failed to load MD3 file: " + md3File + ". The file might be corrupted or in use by another process. " + message);
            }

            IntPtr entryPointPointer = Kernel32.GetProcAddress(_handle, md3EntryPoint);

            if (entryPointPointer == IntPtr.Zero)
            {
                throw new NikonException("GetProcAddress failed to get the address for: " + md3EntryPoint);
            }

            _entryPoint = (MAIDEntryPointProcDelegate)Marshal.GetDelegateForFunctionPointer(entryPointPointer, typeof(MAIDEntryPointProcDelegate));
        }

        internal void Close()
        {
            Kernel32.FreeLibrary(_handle);
            _handle = IntPtr.Zero;
        }

        internal MAIDEntryPointProcDelegate EntryPoint
        {
            get { return _entryPoint; }
        }

        private enum MachineType
        {
            x86,
            x64,
            Unknown,
        }

        private MachineType GetDllMachineType(string dllFile)
        {
            try
            {
                using (var stream = new FileStream(dllFile, FileMode.Open, FileAccess.Read))
                {
                    using (var reader = new BinaryReader(stream))
                    {
                        reader.BaseStream.Position = 60;
                        var offset = reader.ReadInt32();
                        reader.BaseStream.Position = offset + 4;
                        var machine = reader.ReadUInt16();
                        switch (machine)
                        {
                            case 0x014c: return MachineType.x86;
                            case 0x8664: return MachineType.x64;
                            default:     return MachineType.Unknown;
                        }
                    }
                }
            } 
            catch (IOException)
            {
                return MachineType.Unknown;
            }
        }
    };
    #endregion

    #region Structs for array types
    internal struct NikonEnumWithData
    {
        internal NkMAIDEnum nativeEnum;
        internal byte[] buffer;
    }

    internal struct NikonArrayWithData
    {
        internal NkMAIDArray nativeArray;
        internal byte[] buffer;
    }
    #endregion

    #region Native delegates
    internal unsafe delegate void MAIDEventProcDelegate(
        IntPtr refClient,               // Reference set by client
        eNkMAIDEvent ulEvent,           // One of eNkMAIDEvent
        IntPtr data);                   // Pointer or long integer

    internal unsafe delegate void MAIDCompletionProcDelegate(
        IntPtr pObject,                 // module, source, item, or data object
        eNkMAIDCommand ulCommand,       // Command, one of eNkMAIDCommand
        UInt32 ulParam,                 // parameter for the command
        eNkMAIDDataType ulDataType,     // Data type, one of eNkMAIDDataType
        IntPtr data,                    // Pointer or long integer
        IntPtr refComplete,             // Reference set by client
        eNkMAIDResult nResult);         // One of eNkMAIDResult

    internal unsafe delegate eNkMAIDResult MAIDDataProcDelegate(
        IntPtr refClient,               // Reference set by client
        IntPtr pDataInfo,               // Cast to LPNkMAIDImageInfo or LPNkMAIDSoundInfo
        IntPtr pData);

    internal unsafe delegate void MAIDProgressProcDelegate(
        eNkMAIDCommand ulCommand,       // Command, one of eNkMAIDCommand
        UInt32 ulParam,                 // parameter for the command
        IntPtr refComplete,             // Reference set by client
        UInt32 ulDone,                  // Numerator
        UInt32 ulTotal);                // Denominator

    internal unsafe delegate UInt32 MAIDUIRequestProcDelegate(
        IntPtr refProc,                 // reference set by the client
        IntPtr pUIRequest);             // information about the UI request

    internal unsafe delegate eNkMAIDResult MAIDEntryPointProcDelegate(
        IntPtr pObject,                 // module, source, item, or data object
        eNkMAIDCommand ulCommand,       // Command, one of eNkMAIDCommand
        UInt32 ulParam,                 // parameter for the command
        eNkMAIDDataType ulDataType,     // Data type, one of eNkMAIDDataType
        IntPtr data,                    // Pointer or long integer
        IntPtr pfnComplete,             // Completion function, may be NULL
        IntPtr refComplete);            // Value passed to pfnComplete
    #endregion

    #region NikonObject delegates
    internal delegate void InternalProgressDelegate(
        NikonObject sender,
        eNkMAIDCommand ulCommand,
        UInt32 ulParam,
        IntPtr refComplete,
        UInt32 ulDone,
        UInt32 ulTotal);

    internal delegate void InternalEventDelegate(
        NikonObject sender,
        IntPtr refClient,
        eNkMAIDEvent ulEvent,
        IntPtr data);

    internal delegate void InternalUIRequestDelegate(
        NikonObject sender,
        IntPtr refProc,
        IntPtr pUIRequest);

    internal delegate void InternalDataFileDelegate(
        NikonObject sender,
        NkMAIDFileInfo fileInfo,
        IntPtr data);

    internal delegate void InternalDataImageDelegate(
        NikonObject sender,
        NkMAIDImageInfo imageInfo,
        IntPtr data);

    internal delegate void InternalDataSoundDelegate(
        NikonObject sender,
        NkMAIDSoundInfo soundInfo,
        IntPtr data);
    #endregion

    #region NikonObject
    internal unsafe class NikonObject
    {
        //
        // Class Members
        //
        static uint _uniqueValue = 100;
        uint _id;
        NikonMd3 _md3;
        NikonObject _parent;
        NkMAIDObject _object;
        MAIDCompletionProcDelegate _completionProc;
        MAIDEventProcDelegate _eventProc;
        MAIDProgressProcDelegate _progressProc;
        MAIDDataProcDelegate _dataProc;
        MAIDUIRequestProcDelegate _uiRequestProc;

        //
        // Events
        //
        internal event InternalDataFileDelegate DataFile;
        internal event InternalDataImageDelegate DataImage;
        internal event InternalDataSoundDelegate DataSound;
        internal event InternalProgressDelegate Progress;
        internal event InternalEventDelegate Event;
        internal event InternalUIRequestDelegate UIRequest;

        //
        // Constructor
        //
        internal NikonObject(NikonMd3 md3, NikonObject parent, uint id)
        {
            _md3 = md3;
            _parent = parent;
            _id = id;

            _object = new NkMAIDObject();
            _object.refClient = new IntPtr(_uniqueValue);
            _uniqueValue++;

            _completionProc = new MAIDCompletionProcDelegate(CompletionProc);
            _eventProc = new MAIDEventProcDelegate(EventProc);
            _progressProc = new MAIDProgressProcDelegate(ProgressProc);
            _dataProc = new MAIDDataProcDelegate(DataProc);
            _uiRequestProc = new MAIDUIRequestProcDelegate(UIRequestProc);
        }

        //
        // Id
        //
        internal uint Id
        {
            get { return _id; }
        }

        //
        // Open
        //
        internal void Open()
        {
            // Get pointer to parent MAID object
            IntPtr parentPointer = IntPtr.Zero;
            NkMAIDObject parentObject;

            if (_parent != null)
            {
                parentObject = _parent._object;
                parentPointer = new IntPtr(&parentObject);
            }

            // Open MAID object
            fixed (NkMAIDObject* p = &_object)
            {
                CallEntryPoint(
                    parentPointer,
                    eNkMAIDCommand.kNkMAIDCommand_Open,
                    _id,
                    eNkMAIDDataType.kNkMAIDDataType_ObjectPtr,
                    new IntPtr(p),
                    IntPtr.Zero,
                    IntPtr.Zero);
            }

            // Set Callbacks
            SetSupportedCallbacks();
        }

        //
        // Set Callbacks
        //
        void SetSupportedCallbacks()
        {
            // Get supported caps and add them to a dictionary
            Dictionary<eNkMAIDCapability, NkMAIDCapInfo> caps = new Dictionary<eNkMAIDCapability, NkMAIDCapInfo>();
            NkMAIDCapInfo[] capsArray = GetCapInfo();
            foreach (NkMAIDCapInfo c in capsArray)
            {
                caps.Add(c.ulID, c);
            }

            // Callback capabilities
            eNkMAIDCapability[] procCaps = new eNkMAIDCapability[]
            {
                eNkMAIDCapability.kNkMAIDCapability_ProgressProc,
                eNkMAIDCapability.kNkMAIDCapability_EventProc,
                eNkMAIDCapability.kNkMAIDCapability_UIRequestProc,
                eNkMAIDCapability.kNkMAIDCapability_DataProc
            };

            // Corresponding callback functions
            Delegate[] procDelegates = new Delegate[]
            {
                _progressProc,
                _eventProc,
                _uiRequestProc,
                _dataProc
            };

            Debug.Assert(procCaps.Length == procDelegates.Length);

            // Set supported callbacks
            for (int i = 0; i < procCaps.Length; i++)
            {
                eNkMAIDCapability cap = procCaps[i];

                if (caps.ContainsKey(cap) &&
                    caps[cap].CanSet())
                {
                    NkMAIDCallback callback = new NkMAIDCallback();
                    callback.pProc = Marshal.GetFunctionPointerForDelegate(procDelegates[i]);

                    SetCallback(cap, callback);
                }
            }
        }

        //
        // Event Callback
        //
        void EventProc(
            IntPtr refClient,
            eNkMAIDEvent ulEvent,
            IntPtr data)
        {
            if (Event != null)
            {
                Event(
                    this,
                    refClient,
                    ulEvent,
                    data);
            }
        }

        //
        // Progress Callback
        //
        void ProgressProc(
            eNkMAIDCommand ulCommand,
            UInt32 ulParam,
            IntPtr refComplete,
            UInt32 ulDone,
            UInt32 ulTotal)
        {
            if (Progress != null)
            {
                Progress(
                    this,
                    ulCommand,
                    ulParam,
                    refComplete,
                    ulDone,
                    ulTotal);
            }
        }

        //
        // UIRequest Callback
        //
        UInt32 UIRequestProc(
            IntPtr refProc,
            IntPtr pUIRequest)
        {
            if (UIRequest != null)
            {
                UIRequest(
                    this,
                    refProc,
                    pUIRequest);
            }

            return 0;
        }

        //
        // Data Callback
        //
        eNkMAIDResult DataProc(
            IntPtr refClient,
            IntPtr pDataInfo,
            IntPtr pData)
        {
            NkMAIDDataInfo info = *((NkMAIDDataInfo*)pDataInfo.ToPointer());

            switch (info.ulType)
            {
                case eNkMAIDDataObjType.kNkMAIDDataObjType_File:
                case eNkMAIDDataObjType.kNkMAIDDataObjType_File | eNkMAIDDataObjType.kNkMAIDDataObjType_Image:
                case eNkMAIDDataObjType.kNkMAIDDataObjType_File | eNkMAIDDataObjType.kNkMAIDDataObjType_Sound:
                case eNkMAIDDataObjType.kNkMAIDDataObjType_File | eNkMAIDDataObjType.kNkMAIDDataObjType_Thumbnail:
                case eNkMAIDDataObjType.kNkMAIDDataObjType_File | eNkMAIDDataObjType.kNkMAIDDataObjType_Video:
                    if (DataFile != null)
                    {
                        NkMAIDFileInfo fileInfo = *((NkMAIDFileInfo*)pDataInfo.ToPointer());
                        DataFile(this, fileInfo, pData);
                    }
                    break;

                case eNkMAIDDataObjType.kNkMAIDDataObjType_Thumbnail:
                    if (DataImage != null)
                    {
                        NkMAIDImageInfo imageInfo = *((NkMAIDImageInfo*)pDataInfo.ToPointer());
                        DataImage(this, imageInfo, pData);
                    }
                    break;

                case eNkMAIDDataObjType.kNkMAIDDataObjType_Sound:
                    if (DataSound != null)
                    {
                        NkMAIDSoundInfo soundInfo = *((NkMAIDSoundInfo*)pDataInfo.ToPointer());
                        DataSound(this, soundInfo, pData);
                    }
                    break;

                default:
                    Debug.Print("Unexpected data object type: {0}", info.ulType);
                    break;
            }

            return eNkMAIDResult.kNkMAIDResult_NoError;
        }

        //
        // GetCapInfo
        //
        internal NkMAIDCapInfo[] GetCapInfo()
        {
            NkMAIDCapInfo[] capInfo = null;

            while (true)
            {
                // Get capability count
                uint count = GetCapCount();

                // Allocate capabiity info array
                capInfo = new NkMAIDCapInfo[count];

                // GetCapInfo
                if (GetCapInfo(capInfo))
                {
                    // If GetCapInfo succeeds, we break out of the loop
                    break;
                }
                else
                {
                    // Otherwise we keep trying
                    continue;
                }
            }

            // Return result
            return capInfo;
        }

        //
        // GetCapInfo
        //
        bool GetCapInfo(NkMAIDCapInfo[] capInfo)
        {
            fixed (NkMAIDCapInfo* p = capInfo)
            {
                try
                {
                    CallEntryPointAsync(
                       eNkMAIDCommand.kNkMAIDCommand_GetCapInfo,
                       (uint)capInfo.Length,
                       eNkMAIDDataType.kNkMAIDDataType_CapInfoPtr,
                       new IntPtr(p));
                }
                catch (NikonException ex)
                {
                    if (ex.ErrorCode == eNkMAIDResult.kNkMAIDResult_BufferSize)
                    {
                        return false;
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return true;
        }

        //
        // GetCapCount
        //
        uint GetCapCount()
        {
            uint count = 0;

            CallEntryPointAsync(
                eNkMAIDCommand.kNkMAIDCommand_GetCapCount,
                0,
                eNkMAIDDataType.kNkMAIDDataType_UnsignedPtr,
                new IntPtr(&count));

            return count;
        }

        //
        // Close
        //
        internal void Close()
        {
            CallEntryPointSync(
                eNkMAIDCommand.kNkMAIDCommand_Close,
                0,
                eNkMAIDDataType.kNkMAIDDataType_Null,
                IntPtr.Zero);
        }

        //
        // EnumChildren
        //
        internal void EnumChildren()
        {
            CallEntryPointAsync(
                eNkMAIDCommand.kNkMAIDCommand_EnumChildren,
                0,
                eNkMAIDDataType.kNkMAIDDataType_Null,
                IntPtr.Zero);
        }

        //
        // CapStart
        //
        internal void CapStart(eNkMAIDCapability cap, eNkMAIDDataType dataType, IntPtr data)
        {
            CallEntryPointAsync(
                eNkMAIDCommand.kNkMAIDCommand_CapStart,
                (uint)cap,
                dataType,
                data);
        }

        //
        // CapSet
        //
        internal void CapSet(eNkMAIDCapability capability, eNkMAIDDataType type, IntPtr data)
        {
            CallEntryPointAsync(
                eNkMAIDCommand.kNkMAIDCommand_CapSet,
                (uint)capability,
                type,
                data);
        }

        //
        // CapGet
        //
        internal void CapGet(eNkMAIDCapability capability, eNkMAIDDataType type, IntPtr data)
        {
            CallEntryPointAsync(
                eNkMAIDCommand.kNkMAIDCommand_CapGet,
                (uint)capability,
                type,
                data);
        }

        //
        // CapGetArray
        //
       internal void CapGetArray(eNkMAIDCapability capability, eNkMAIDDataType type, IntPtr data)
        {
            CallEntryPointAsync(
                eNkMAIDCommand.kNkMAIDCommand_CapGetArray,
                (uint)capability,
                type,
                data);
        }

        //
        // CapGetDefault
        //
        internal void CapGetDefault(eNkMAIDCapability capability, eNkMAIDDataType type, IntPtr data)
        {
            CallEntryPointAsync(
                eNkMAIDCommand.kNkMAIDCommand_CapGetDefault,
                (uint)capability,
                type,
                data);
        }

        //
        // Async
        //
        internal void Async()
        {
            CallEntryPointSync(
                eNkMAIDCommand.kNkMAIDCommand_Async,
                0,
                eNkMAIDDataType.kNkMAIDDataType_Null,
                IntPtr.Zero);
        }

        //
        // Get Children
        //
        internal NikonObject[] GetChildren()
        {
            List<NikonObject> children = new List<NikonObject>();

            NikonEnumWithData e = GetEnumWithData(eNkMAIDCapability.kNkMAIDCapability_Children);

            for (int i = 0; i < e.nativeEnum.ulElements; i++)
            {
                uint childId = BitConverter.ToUInt32(e.buffer, i * 4);

                NikonObject child = new NikonObject(_md3, this, childId);

                children.Add(child);
            }

            return children.ToArray();
        }

        //
        // Enum
        //
        internal void SetEnum(eNkMAIDCapability capability, NkMAIDEnum enumeration)
        {
            CapSet(
                capability,
                eNkMAIDDataType.kNkMAIDDataType_EnumPtr,
                new IntPtr(&enumeration));
        }

        internal NikonEnumWithData GetEnumWithData(eNkMAIDCapability cap)
        {
            NikonEnumWithData e;

            while (true)
            {
                CapGet(
                    cap,
                    eNkMAIDDataType.kNkMAIDDataType_EnumPtr,
                    new IntPtr(&e.nativeEnum));

                e.buffer = new byte[e.nativeEnum.GetDataSize()];

                fixed (byte* p = e.buffer)
                {
                    e.nativeEnum.pData = new IntPtr(p);

                    try
                    {
                        CapGetArray(
                            cap,
                            eNkMAIDDataType.kNkMAIDDataType_EnumPtr,
                            new IntPtr(&e.nativeEnum));
                    }
                    catch (NikonException ex)
                    {
                        if (ex.ErrorCode == eNkMAIDResult.kNkMAIDResult_BufferSize)
                        {
                            continue;
                        }
                        else
                        {
                            throw;
                        }
                    }
                }

                break;
            }

            return e;
        }

        //
        // Array
        //
        internal void SetArray(eNkMAIDCapability capability, NkMAIDArray array)
        {
            CapSet(
                capability,
                eNkMAIDDataType.kNkMAIDDataType_ArrayPtr,
                new IntPtr(&array));
        }

        internal NikonArrayWithData GetArrayWithData(eNkMAIDCapability cap)
        {
            NikonArrayWithData a;

            while (true)
            {
                CapGet(
                    cap,
                    eNkMAIDDataType.kNkMAIDDataType_ArrayPtr,
                    new IntPtr(&a.nativeArray));

                a.buffer = new byte[a.nativeArray.GetDataSize()];

                fixed (byte* p = a.buffer)
                {
                    a.nativeArray.pData = new IntPtr(p);

                    try
                    {
                        CapGetArray(
                            cap,
                            eNkMAIDDataType.kNkMAIDDataType_ArrayPtr,
                            new IntPtr(&a.nativeArray));
                    }
                    catch (NikonException ex)
                    {
                        if (ex.ErrorCode == eNkMAIDResult.kNkMAIDResult_BufferSize)
                        {
                            continue;
                        }
                        else
                        {
                            throw;
                        }
                    }
                }

                break;
            }

            return a;
        }

        //
        // Range
        //
        internal NkMAIDRange GetRange(eNkMAIDCapability capability)
        {
            NkMAIDRange result = new NkMAIDRange();

            CapGet(
                capability,
                eNkMAIDDataType.kNkMAIDDataType_RangePtr,
                new IntPtr(&result));

            return result;
        }

        internal void SetRange(eNkMAIDCapability capability, NkMAIDRange value)
        {
            CapSet(
                capability,
                eNkMAIDDataType.kNkMAIDDataType_RangePtr,
                new IntPtr(&value));
        }

        internal NkMAIDRange GetDefaultRange(eNkMAIDCapability capability)
        {
            NkMAIDRange result = new NkMAIDRange();

            CapGetDefault(
                capability,
                eNkMAIDDataType.kNkMAIDDataType_RangePtr,
                new IntPtr(&result));

            return result;
        }

        //
        // Point
        //
        internal NkMAIDPoint GetPoint(eNkMAIDCapability capability)
        {
            NkMAIDPoint result = new NkMAIDPoint();

            CapGet(
                capability,
                eNkMAIDDataType.kNkMAIDDataType_PointPtr,
                new IntPtr(&result));

            return result;
        }

        internal void SetPoint(eNkMAIDCapability capability, NkMAIDPoint value)
        {
            CapSet(
                capability,
                eNkMAIDDataType.kNkMAIDDataType_PointPtr,
                new IntPtr(&value));
        }

        internal NkMAIDPoint GetDefaultPoint(eNkMAIDCapability capability)
        {
            NkMAIDPoint result = new NkMAIDPoint();

            CapGetDefault(
                capability,
                eNkMAIDDataType.kNkMAIDDataType_PointPtr,
                new IntPtr(&result));

            return result;
        }

        //
        // DateTime
        //
        internal DateTime GetDateTime(eNkMAIDCapability capability)
        {
            NkMAIDDateTime result = new NkMAIDDateTime();

            CapGet(
                capability,
                eNkMAIDDataType.kNkMAIDDataType_DateTimePtr,
                new IntPtr(&result));

            return result.GetDateTime();
        }

        internal void SetDateTime(eNkMAIDCapability capability, DateTime value)
        {
            NkMAIDDateTime dateTime = new NkMAIDDateTime();
            dateTime = dateTime.Create(value);

            CapSet(
                capability,
                eNkMAIDDataType.kNkMAIDDataType_DateTimePtr,
                new IntPtr(&dateTime));
        }

        internal DateTime GetDefaultDateTime(eNkMAIDCapability capability)
        {
            NkMAIDDateTime result = new NkMAIDDateTime();

            CapGetDefault(
                capability,
                eNkMAIDDataType.kNkMAIDDataType_DateTimePtr,
                new IntPtr(&result));

            return result.GetDateTime();
        }

        //
        // Rect
        //
        internal void SetRect(eNkMAIDCapability capability, NkMAIDRect rectangle)
        {
            CapSet(
                capability,
                eNkMAIDDataType.kNkMAIDDataType_RectPtr,
                new IntPtr(&rectangle));
        }

        internal NkMAIDRect GetRect(eNkMAIDCapability capability)
        {
            NkMAIDRect rectangle = new NkMAIDRect();

            CapGet(
                capability,
                eNkMAIDDataType.kNkMAIDDataType_RectPtr,
                new IntPtr(&rectangle));

            return rectangle;
        }

        internal NkMAIDRect GetDefaultRect(eNkMAIDCapability capability)
        {
            NkMAIDRect rectangle = new NkMAIDRect();

            CapGetDefault(
                capability,
                eNkMAIDDataType.kNkMAIDDataType_RectPtr,
                new IntPtr(&rectangle));

            return rectangle;
        }

        //
        // Size
        //
        internal void SetSize(eNkMAIDCapability capability, NkMAIDSize size)
        {
            CapSet(
                capability,
                eNkMAIDDataType.kNkMAIDDataType_SizePtr,
                new IntPtr(&size));
        }

        internal NkMAIDSize GetSize(eNkMAIDCapability capability)
        {
            NkMAIDSize size = new NkMAIDSize();

            CapGet(
                capability,
                eNkMAIDDataType.kNkMAIDDataType_SizePtr,
                new IntPtr(&size));

            return size;
        }

        internal NkMAIDSize GetDefaultSize(eNkMAIDCapability capability)
        {
            NkMAIDSize size = new NkMAIDSize();

            CapGetDefault(
                capability,
                eNkMAIDDataType.kNkMAIDDataType_SizePtr,
                new IntPtr(&size));

            return size;
        }

        //
        // String
        //
        internal string GetString(eNkMAIDCapability capability)
        {
            NkMAIDString str = new NkMAIDString();

            CapGet(
                capability,
                eNkMAIDDataType.kNkMAIDDataType_StringPtr,
                new IntPtr(&str));

            return str.GetString();
        }

        internal void SetString(eNkMAIDCapability capability, string value)
        {
            NkMAIDString str = new NkMAIDString();

            byte[] ascii = ASCIIEncoding.ASCII.GetBytes(value);

            Marshal.Copy(
                ascii,
                0,
                new IntPtr(str.str),
                Math.Min(256, ascii.Length));

            CapSet(
                capability,
                eNkMAIDDataType.kNkMAIDDataType_StringPtr,
                new IntPtr(&str));
        }

        internal string GetDefaultString(eNkMAIDCapability capability)
        {
            NkMAIDString str = new NkMAIDString();

            CapGetDefault(
                capability,
                eNkMAIDDataType.kNkMAIDDataType_StringPtr,
                new IntPtr(&str));

            return str.GetString();
        }

        //
        // Integer
        //
        internal int GetInteger(eNkMAIDCapability capability)
        {
            int result;

            CapGet(
                capability,
                eNkMAIDDataType.kNkMAIDDataType_IntegerPtr,
                new IntPtr(&result));

            return result;
        }

        internal void SetInteger(eNkMAIDCapability capability, int value)
        {
            CapSet(
                capability,
                eNkMAIDDataType.kNkMAIDDataType_Integer,
                new IntPtr(value));
        }

        internal int GetDefaultInteger(eNkMAIDCapability capability)
        {
            int result;

            CapGetDefault(
                capability,
                eNkMAIDDataType.kNkMAIDDataType_IntegerPtr,
                new IntPtr(&result));

            return result;
        }

        //
        // Unsigned
        //
        internal uint GetUnsigned(eNkMAIDCapability capability)
        {
            uint result;

            CapGet(
                capability,
                eNkMAIDDataType.kNkMAIDDataType_UnsignedPtr,
                new IntPtr(&result));

            return result;
        }

        internal void SetUnsigned(eNkMAIDCapability capability, uint value)
        {
            CapSet(
                capability,
                eNkMAIDDataType.kNkMAIDDataType_Unsigned,
                new IntPtr((int)value));
        }

        internal uint GetDefaultUnsigned(eNkMAIDCapability capability)
        {
            uint result;

            CapGetDefault(
                capability,
                eNkMAIDDataType.kNkMAIDDataType_UnsignedPtr,
                new IntPtr(&result));

            return result;
        }

        //
        // Boolean
        //
        internal bool GetBoolean(eNkMAIDCapability capability)
        {
            bool result;

            CapGet(
                capability,
                eNkMAIDDataType.kNkMAIDDataType_BooleanPtr,
                new IntPtr(&result));

            return result;
        }

        internal void SetBoolean(eNkMAIDCapability capability, bool value)
        {
            CapSet(
                capability,
                eNkMAIDDataType.kNkMAIDDataType_Boolean,
                new IntPtr(value ? 1 : 0));
        }

        internal bool GetDefaultBoolean(eNkMAIDCapability capability)
        {
            bool result;

            CapGetDefault(
                capability,
                eNkMAIDDataType.kNkMAIDDataType_BooleanPtr,
                new IntPtr(&result));

            return result;
        }

        //
        // Float
        //
        internal double GetFloat(eNkMAIDCapability capability)
        {
            double result;

            CapGet(
                capability,
                eNkMAIDDataType.kNkMAIDDataType_FloatPtr,
                new IntPtr(&result));

            return result;
        }

        internal void SetFloat(eNkMAIDCapability capability, double value)
        {
            CapSet(
                capability,
                eNkMAIDDataType.kNkMAIDDataType_FloatPtr,
                new IntPtr(&value));
        }

        internal double GetDefaultFloat(eNkMAIDCapability capability)
        {
            double result;

            CapGetDefault(
                capability,
                eNkMAIDDataType.kNkMAIDDataType_FloatPtr,
                new IntPtr(&result));

            return result;
        }

        //
        // Callback
        //
        internal NkMAIDCallback GetCallback(eNkMAIDCapability capability)
        {
            NkMAIDCallback callback = new NkMAIDCallback();

            CapGet(
                capability,
                eNkMAIDDataType.kNkMAIDDataType_CallbackPtr,
                new IntPtr(&callback));

            return callback;
        }

        internal void SetCallback(eNkMAIDCapability capability, NkMAIDCallback callback)
        {
            CapSet(
                capability,
                eNkMAIDDataType.kNkMAIDDataType_CallbackPtr,
                new IntPtr(&callback));
        }

        //
        // Generic
        //
        internal void GetGeneric(eNkMAIDCapability capability, IntPtr destination)
        {
            CapGet(
                capability,
                eNkMAIDDataType.kNkMAIDDataType_GenericPtr,
                destination);
        }

        internal void SetGeneric(eNkMAIDCapability capability, IntPtr source)
        {
            CapSet(
                capability,
                eNkMAIDDataType.kNkMAIDDataType_GenericPtr,
                source);
        }

        internal void GetDefaultGeneric(eNkMAIDCapability capability, IntPtr destination)
        {
            CapGetDefault(
                capability,
                eNkMAIDDataType.kNkMAIDDataType_GenericPtr,
                destination);
        }

        internal void GetArrayGeneric(eNkMAIDCapability capability, IntPtr destination)
        {
            CapGetArray(
                capability,
                eNkMAIDDataType.kNkMAIDDataType_GenericPtr,
                destination);
        }

        //
        // Call Entry Point
        //
        void CallEntryPointSync(
            eNkMAIDCommand ulCommand,
            UInt32 ulParam,
            eNkMAIDDataType ulDataType,
            IntPtr data)
        {
            CallEntryPoint(
                ulCommand,
                ulParam,
                ulDataType,
                data,
                IntPtr.Zero,
                IntPtr.Zero);
        }

        struct Context
        {
            public bool Complete;
            public eNkMAIDResult Result;
        }

        void CompletionProc(
            IntPtr pObject,
            eNkMAIDCommand ulCommand,
            UInt32 ulParam,
            eNkMAIDDataType ulDataType,
            IntPtr data,
            IntPtr refComplete,
            eNkMAIDResult nResult)
        {
            Context* context = (Context*)refComplete.ToPointer();
            context->Complete = true;
            context->Result = nResult;
        }

        void CallEntryPointAsync(
            eNkMAIDCommand ulCommand,
            UInt32 ulParam,
            eNkMAIDDataType ulDataType,
            IntPtr data)
        {
            Context context;
            context.Complete = false;
            context.Result = eNkMAIDResult.kNkMAIDResult_NoError;

            CallEntryPoint(
                ulCommand,
                ulParam,
                ulDataType,
                data,
                Marshal.GetFunctionPointerForDelegate(_completionProc),
                new IntPtr(&context));

            while (!context.Complete)
            {
                Async();
            }

            // Note: Is it OK to ignore context.Result?
        }

        void CallEntryPoint(
            eNkMAIDCommand ulCommand,
            UInt32 ulParam,
            eNkMAIDDataType ulDataType,
            IntPtr data,
            IntPtr pfnComplete,
            IntPtr refComplete)
        {
            fixed (NkMAIDObject* p = &_object)
            {
                CallEntryPoint(
                    new IntPtr(p),
                    ulCommand,
                    ulParam,
                    ulDataType,
                    data,
                    pfnComplete,
                    refComplete);
            }
        }

        void CallEntryPoint(
            IntPtr pObject,
            eNkMAIDCommand ulCommand,
            UInt32 ulParam,
            eNkMAIDDataType ulDataType,
            IntPtr data,
            IntPtr pfnComplete,
            IntPtr refComplete)
        {
            Debug.Assert(_md3 != null);

            eNkMAIDResult result = _md3.EntryPoint(
                pObject,
                ulCommand,
                ulParam,
                ulDataType,
                data,
                pfnComplete,
                refComplete);

            switch (result)
            {
                // Note: Ignore these return values
                case eNkMAIDResult.kNkMAIDResult_NoError:
                case eNkMAIDResult.kNkMAIDResult_Pending:
                case eNkMAIDResult.kNkMAIDResult_OrphanedChildren:
                    break;

                default:
                    throw new NikonException(
                        result,
                        ulCommand,
                        ulParam,
                        ulDataType,
                        data);
            }
        }
    }
    #endregion
}
