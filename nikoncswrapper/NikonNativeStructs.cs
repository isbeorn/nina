// This work is licensed under a Creative Commons Attribution 3.0 Unported License.
//
// Thomas Dideriksen (thomas@dideriksen.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Nikon {
    // Note: This file is auto generated

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public unsafe struct NkMAIDActiveSelectionSelectedPictures {
        public UInt32 ulPicIDNum;
        public IntPtr pulSelPicIDs;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public unsafe struct NkMAIDArray {
        public eNkMAIDArrayType ulType; // one of eNkMAIDArrayType
        public UInt32 ulElements;       // total number of elements
        public UInt32 ulDimSize1;       // size of first dimention
        public UInt32 ulDimSize2;       // size of second dimention, zero for 1 dim
        public UInt32 ulDimSize3;       // size of third dimention, zero for 1 or 2 dim
        public UInt16 wPhysicalBytes;   // bytes per element
        public UInt16 wLogicalBits;     // must be <= wPhysicalBytes * 8
        public IntPtr pData;            // allocated by the client
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public unsafe struct NkMAIDCallback {
        public IntPtr pProc;
        public IntPtr refProc;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public unsafe struct NkMAIDCapInfo {
        public eNkMAIDCapability ulID;            // one of eNkMAIDCapability or vendor specified
        public eNkMAIDCapType ulType;             // one of eNkMAIDCapabilityType
        public eNkMAIDCapVisibility ulVisibility; // eNkMAIDCapVisibility bits
        public eNkMAIDCapOperation ulOperations;  // eNkMAIDCapOperations bits
        public fixed byte szDescription[256];     // text describing the capability
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public unsafe struct NkMAIDCaptureFocusShift {
        public UInt32 ulRemainingNumber;    // The number of shots remaining in the focus shift sequence
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public unsafe struct NkMAIDCaptureIntervalTimer {
        public UInt32 ulRemainingNumber;    // The number of shots remaining in the interval timer sequence
        public UInt32 ulRemainingTime;      // The remaining shooting time (units defined by camera firmware)
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public unsafe struct NkMAIDCMLProfile {
        public eNkMAIDColorSpace ulColorSpace;             // one of eNkMAIDColorSpace
        public UInt32 ulBits;                              // Bit depth of the supported image by this profile.
        public fixed UInt32 ulReserved[5];
        public NkMAIDString file;                          // DOS path and filename
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public unsafe struct NkMAIDDataInfo {
        public eNkMAIDDataObjType ulType; // one of eNkMAIDDataObjType
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public unsafe struct NkMAIDDateCounterData {
        public fixed UInt16 wcDate1[9]; // First date
        public fixed UInt16 wcDate2[9]; // Second date
        public fixed UInt16 wcDate3[9]; // Third date
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public unsafe struct NkMAIDDateTime {
        public UInt16 nYear;      // ie 1997, 1998
        public UInt16 nMonth;     // 0-11 = Jan-Dec
        public UInt16 nDay;       // 1-31
        public UInt16 nHour;      // 0-23
        public UInt16 nMinute;    // 0-59
        public UInt16 nSecond;    // 0-59
        public UInt32 nSubsecond; // Module dependent
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public unsafe struct NkMAIDEnum {
        public eNkMAIDArrayType ulType; // one of eNkMAIDArrayType
        public UInt32 ulElements;       // total number of elements
        public UInt32 ulValue;          // current index
        public UInt32 ulDefault;        // default index
        public Int16 wPhysicalBytes;    // bytes per element
        public IntPtr pData;            // allocated by the client
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public unsafe struct NkMAIDEventParam {
        public eNkMAIDEvent ulEvent;    // one of eNkMAIDEvent
        public UInt32 ulElements;       // total number of valid params
        public fixed UInt32 ulParam[2]; // event parameter
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public unsafe struct NkMAIDFileInfo {
        public NkMAIDDataInfo baseInfo;
        public eNkMAIDFileDataType ulFileDataType; // One of eNkMAIDFileDataTypes
        public UInt32 ulTotalLength;               // total number of bytes to be transferred
        public UInt32 ulStart;                     // index of starting byte (0-based)
        public UInt32 ulLength;                    // number of bytes in this delivery
        public UInt32 fDiskFile;                   // TRUE if the file is delivered on disk
        public UInt32 fRemoveObject;               // TRUE if the object should be removed
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public unsafe struct NkMAIDGetPicCtrlInfo {
        public UInt32 ulPicCtrlItem; // picture control item
        public UInt32 ulSize;        // the data sizer of pData
        public IntPtr pData;         // The pointer to Quick Adjust Param
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public unsafe struct NkMAIDGetFocusShiftCapInfo {
        public UInt32 ulStatus;                 // The current state of focus shift shooting (0 = stopped, 1 = executing)
        public UInt32 ulRemainingNumber;        // The number of shots remaining in the focus shift sequence
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public unsafe struct NkMAIDGetIntervalTimerCapInfo {
        public UInt32 ulStatus;                     // The current state of interval timer shooting (0 = stopped/halted, 1 = executing)
        public UInt32 ulRemainingNumber;            // The number of shots remaining in the interval timer sequence
        public UInt32 ulRemainingTime;              // The remaining shooting time (units defined by camera firmware)
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public unsafe struct NkMAIDGetSBAttrDesc {
        public UInt32 ulSBHandle;
        public UInt32 ulSBAttrID;
        public UInt32 ulSize;
        public IntPtr pData;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public unsafe struct NkMAIDGetSBGroupAttrDesc {
        public UInt32 ulSBGroupID;
        public UInt32 ulSBGroupAttrID;
        public UInt32 ulSize;
        public IntPtr pData;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public unsafe struct NkMAIDGetSBHandles {
        public UInt32 ulSBGroupID;
        public UInt32 ulNumber;
        public UInt32 ulSize;
        public IntPtr pData;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public unsafe struct NkMAIDGetVideoImage {
        public eNkMAIDArrayType ulType; // one of eNkMAIDArrayType
        public UInt32 ulOffset;         // Offset
        public UInt32 ulReadSize;       // size of get data
        public UInt32 ulDataSize;       // size of "pData"
        public IntPtr pData;            // allocated by the client
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public unsafe struct NkMAIDImageInfo {
        public NkMAIDDataInfo baseInfo;
        public NkMAIDSize szTotalPixels;       // total size of image to be transfered
        public eNkMAIDColorSpace ulColorSpace; // One of eNkMAIDColorSpace
        public NkMAIDRect rData;               // Coords of data, (0,0) = top,left
        public UInt32 ulRowBytes;              // number of bytes per row of pixels
        public fixed UInt16 wBits[4];          // number of bits per plane per pixel
        public UInt16 wPlane;                  // Plane of the image being delivered
        public UInt32 fRemoveObject;           // TRUE if the object should be removed
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public unsafe struct NkMAIDIPTCPresetInfo {
        public UInt32 ulPresetNo;
        public UInt32 ulSize;
        public IntPtr pData;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public unsafe struct NkMAIDLensTypeNikon1 {
        public UInt32 ulLensType1;
        public UInt32 ulLensType2;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public unsafe struct NkMAIDObject {
        public eNkMAIDObjectType ulType; // One of eNkMAIDObjectType
        public UInt32 ulID;
        public IntPtr refClient;
        public IntPtr refModule;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public unsafe struct NkMAIDPicCtrlData {
        public UInt32 ulPicCtrlItem; // picture control item
        public UInt32 ulSize;        // the data sizer of pData
        public UInt32 bModifiedFlag; // Flag to set New or current
        public IntPtr pData;         // The pointer to picture control data
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public unsafe struct NkMAIDPoint {
        public Int32 x;
        public Int32 y;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public unsafe struct NkMAIDRange {
        public double lfValue;
        public double lfDefault;
        public UInt32 ulValueIndex;
        public UInt32 ulDefaultIndex;
        public double lfLower;
        public double lfUpper;
        public UInt32 ulSteps;        // zero for infinite range, otherwise must be >= 2
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public unsafe struct NkMAIDRect {
        public Int32 x;  // left coordinate
        public Int32 y;  // top coordinate
        public UInt32 w; // width
        public UInt32 h; // height
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public unsafe struct NkMAIDSBAttrValue {
        public UInt32 ulSBHandle;
        public UInt32 ulSBAttrID;
        public UInt32 ulSize;
        public IntPtr pData;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public unsafe struct NkMAIDSBGroupAttrValue {
        public UInt32 ulSBGroupID;
        public UInt32 ulSBGroupAttrID;
        public UInt32 ulSize;
        public IntPtr pData;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public unsafe struct NkMAIDSize {
        public UInt32 w;
        public UInt32 h;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public unsafe struct NkMAIDSoundInfo {
        public NkMAIDDataInfo baseInfo;
        public UInt32 ulTotalSamples;   // number of full samples to be transferred
        public UInt32 fStereo;          // TRUE if stereo, FALSE if mono
        public UInt32 ulStart;          // index of starting sample of data
        public UInt32 ulLength;         // number of samples of data
        public UInt16 wBits;            // number of bits per channel
        public UInt16 wChannel;         // 0 = mono or L+R; 1,2 = left, right
        public UInt32 fRemoveObject;    // TRUE if the object should be removed
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public unsafe struct NkMAIDString {
        public fixed byte str[256]; // allows a 255 character null terminated string
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public unsafe struct NkMAIDTerminateCapture {
        public UInt32 ulParameter1; // Recognize client
        public UInt32 ulParameter2; // The shooting time specified 100msec unit
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public unsafe struct NkMAIDTestFlash {
        public UInt32 ulType;
        public UInt32 ulParam;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public unsafe struct NkMAIDUIRequestInfo {
        public eNkMAIDUIRequestType ulType;      // one of eNkMAIDUIReqestType
        public eNkMAIDUIRequestResult ulDefault; // default return value � one of eNkMAIDUIRequestResult
        public UInt32 fSync;                     // TRUE if user must respond before returning
        public IntPtr lpPrompt;                  // NULL terminated text to show to user
        public IntPtr lpDetail;                  // NULL terminated text indicating more detail
        public IntPtr pObject;                   // module, source, item, or data object
        public IntPtr data;                      // Pointer to an NkMAIDArray structure
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public unsafe struct NkMAIDWBPresetData {
        public UInt32 ulPresetNumber;    // Preset Number
        public UInt32 ulPresetGain;      // Preset Gain
        public UInt32 ulThumbnailSize;   // Thumbnail size of pThumbnailData
        public UInt32 ulThumbnailRotate; // add for D70 One of eNkMAIDThumbnailRotate
        public IntPtr pThumbnailData;    // The pointer to Thumbnail Data
    }

    // for kNkMAIDCapability_GetRecordingInfo
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public unsafe struct NkMAIDGetRecordingInfo {
        public UInt32 ulIndexOfMov;     // Index of the target movie file,
        public UInt32 ulTotalMovCount;  // The number of divided movie files
        public UInt64 ullTotalMovSize;  // Total size of all divided movie files
    }

    // for kNkMAIDCapability_GetVideoImageEx (64-bit offsets for >4GB video)
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public unsafe struct NkMAIDGetVideoImageEx {
        public UInt64 ullOffset;        // Offset address of file data before transfer
        public UInt64 ullReadSize;      // size of acquired data
        public UInt64 ullDataSize;      // size of buffer set to pData
        public IntPtr pData;            // pointer to buffer
    }

    // for kNkMAIDCapability_TimeCodeOrigin
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public unsafe struct NkMAIDTimeCodeOrigin {
        public byte ucTimeCodeInfo;         // 0 = Reset 1 = Enter manually 2 = Current time
        public fixed byte ucTimeCode[3];    // [0] Hour 00-23 [1] Minute 00-59 [2] Second 00-59 All default to 0
        public UInt32 ulFrame;              // Frame(*) 24fps (0-23) 25/50/100fps (0-24) 30/60/120fps (0-29)
    }

    // for kNkMAIDCapability_TrackingAFArea
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public unsafe struct NkMAIDTrackingAFArea {
        public UInt32 ulTrackingStatus;     // 1 = Start Tracking; 0 = Stop tracking
        public NkMAIDPoint stAfPoint;       // Coordinates of the AF area (used only when ulTrackingStatus == 1
    }

    // for kNkMAIDCapability_GetManualSettingLensData
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public unsafe struct NkMAIDGetManualSettingLensData {
        public UInt32 ulLensID;             // 0xFFFFFFFF = All lens setting info; N = Lens ID
        public UInt32 ulSize;               // size of pData
        public IntPtr pData;                // Pointer to ManualSettingLens DataSet
    }

    // for kNkMAIDCapability_GetPicCtrlDataList / kNkMAIDCapability_GetMoviePicCtrlDataList
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public unsafe struct NkMAIDGetPicCtrlDataList {
        public UInt32 ulPicCtrlItem;        // Target picture control
        public UInt32 ulSize;               // data size of buffer where picture control list is output
        public IntPtr pData;                // Pointer to picture control list
    }

    // for kNkMAIDCapability_GetPicCtrlHLGDataList
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public unsafe struct NkMAIDGetPicCtrlHLGDataList {
        public UInt32 ulPicCtrlHLGItem;     // Undocumented in SDK pdf but from headers
        public UInt32 ulSize;
        public IntPtr pData;
    }

    // for kNkMAIDCapability_SetPortraitImpressionBalanceData / SetMoviePortraitImpressionBalanceData
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public unsafe struct NkMAIDPortraitImpressionBalanceData {
        public UInt32 ulModeNumber;         // Mode number
        public UInt32 ulSize;               // size of data
        public IntPtr pData;                // pointer to data
    }

    // for kNkMAIDCapability_GetPortraitImpressionBalanceDataList / GetMoviePortraitImpressionBalanceDataList
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public unsafe struct NkMAIDGetPortraitImpressionBalanceDataList {
        public UInt32 ulModeNumber;         // Mode number
        public Int32 bDefaultFlg;           // Data to retrieve BOOL maps to Int32 in P/Invoke
        public UInt32 ulSize;               // Data size of the buffer max 2+(6*N) bytes 
        public IntPtr pData;                // Pointer of portrait impression balance data
    }

    // for kNkMAIDCapability_SetFlickerReductionShutterSpeed
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public unsafe struct NkMAIDSetFlickReductShutterSpeed {
        public UInt32 ulDir;                // 0 = High speed side; 1 = low speed side (direction of shutter speed change)
        public UInt32 ulStep;               // 1 ~ 10 steps of shutter speed
    }

}