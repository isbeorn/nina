#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace NINA.Equipment.SDK.CameraSDKs.SVBonySDK {
    /* Code Coverage is disabled in this file, as these invoke external code and can't be properly unit tested */

    [ExcludeFromCodeCoverage]
    public class SVBonyPInvokeProxy : ISVBonyPInvokeProxy {

        [SecurityCritical]
        public int SVBGetNumOfConnectedCameras() {
            return SVBonyPInvoke.SVBGetNumOfConnectedCameras();
        }

        [SecurityCritical]
        public SVB_ERROR_CODE SVBGetCameraInfo(out SVB_CAMERA_INFO info, int index) {
            return SVBonyPInvoke.SVBGetCameraInfo(out info, index);
        }

        [SecurityCritical]
        public SVB_ERROR_CODE SVBGetCameraProperty(int iCameraID, out SVB_CAMERA_PROPERTY pCameraProperty) {
            return SVBonyPInvoke.SVBGetCameraProperty(iCameraID, out pCameraProperty);
        }

        [SecurityCritical]
        public SVB_ERROR_CODE SVBGetCameraPropertyEx(int iCameraID, out SVB_CAMERA_PROPERTY_EX pCameraProperty) {
            return SVBonyPInvoke.SVBGetCameraPropertyEx(iCameraID, out pCameraProperty);
        }

        [SecurityCritical]
        public SVB_ERROR_CODE SVBOpenCamera(int iCameraID) {
            return SVBonyPInvoke.SVBOpenCamera(iCameraID);
        }

        [SecurityCritical]
        public SVB_ERROR_CODE SVBCloseCamera(int iCameraID) {
            return SVBonyPInvoke.SVBCloseCamera(iCameraID);
        }

        [SecurityCritical]
        public SVB_ERROR_CODE SVBGetNumOfControls(int iCameraID, out int piNumberOfControls) {
            return SVBonyPInvoke.SVBGetNumOfControls(iCameraID, out piNumberOfControls);
        }

        [SecurityCritical]
        public SVB_ERROR_CODE SVBGetControlCaps(int iCameraID, int iControlIndex, out SVB_CONTROL_CAPS pControlCaps) {
            return SVBonyPInvoke.SVBGetControlCaps(iCameraID, iControlIndex, out pControlCaps);
        }

        [SecurityCritical]
        public SVB_ERROR_CODE SVBGetControlValue(int iCameraID, SVB_CONTROL_TYPE ControlType, out int value, out SVB_BOOL pbAuto) {
            return SVBonyPInvoke.SVBGetControlValue(iCameraID, ControlType, out value, out pbAuto);
        }

        [SecurityCritical]
        public SVB_ERROR_CODE SVBSetControlValue(int iCameraID, SVB_CONTROL_TYPE ControlType, int value, SVB_BOOL pbAuto) {
            return SVBonyPInvoke.SVBSetControlValue(iCameraID, ControlType, value, pbAuto);
        }

        [SecurityCritical]
        public SVB_ERROR_CODE SVBGetSensorPixelSize(int iCameraID, out float fPixelSize) {
            return SVBonyPInvoke.SVBGetSensorPixelSize(iCameraID, out fPixelSize);
        }

        [SecurityCritical]
        public SVB_ERROR_CODE SVBSetCameraMode(int iCameraID, SVB_CAMERA_MODE mode) {
            return SVBonyPInvoke.SVBSetCameraMode(iCameraID, mode);
        }

        [SecurityCritical]
        public SVB_ERROR_CODE SVBSendSoftTrigger(int iCameraID) {
            return SVBonyPInvoke.SVBSendSoftTrigger(iCameraID);
        }

        [SecurityCritical]
        public SVB_ERROR_CODE SVBSetOutputImageType(int iCameraID, SVB_IMG_TYPE ImageType) {
            return SVBonyPInvoke.SVBSetOutputImageType(iCameraID, ImageType);
        }

        [SecurityCritical]
        public SVB_ERROR_CODE SVBGetVideoDataMono8(int iCameraID, [Out] byte[] pBuffer, int lBuffSize, int iWaitms) {
            return SVBonyPInvoke.SVBGetVideoDataMono8(iCameraID, pBuffer, lBuffSize, iWaitms);
        }

        [SecurityCritical]
        public SVB_ERROR_CODE SVBGetVideoDataMono16(int iCameraID, [Out] ushort[] pBuffer, int lBuffSize, int iWaitms) {
            return SVBonyPInvoke.SVBGetVideoDataMono16(iCameraID, pBuffer, lBuffSize, iWaitms);
        }

        [SecurityCritical]
        public SVB_ERROR_CODE SVBStartVideoCapture(int iCameraID) {
            return SVBonyPInvoke.SVBStartVideoCapture(iCameraID);
        }

        [SecurityCritical]
        public SVB_ERROR_CODE SVBStopVideoCapture(int iCameraID) {
            return SVBonyPInvoke.SVBStopVideoCapture(iCameraID);
        }

        [SecurityCritical]
        public SVB_ERROR_CODE SVBSetROIFormat(int iCameraID, int iStartX, int iStartY, int iWidth, int iHeight, int iBi) {
            return SVBonyPInvoke.SVBSetROIFormat(iCameraID, iStartX, iStartY, iWidth, iHeight, iBi);
        }

        [SecurityCritical]
        public SVB_ERROR_CODE SVBGetROIFormat(int iCameraID, out int iStartX, out int iStartY, out int iWidth, out int iHeight, out int iBi) {
            return SVBonyPInvoke.SVBGetROIFormat(iCameraID, out iStartX, out iStartY, out iWidth, out iHeight, out iBi);
        }

        [SecurityCritical]
        private IntPtr SVBGetSDKVersion() {
            return SVBonyPInvoke.SVBGetSDKVersion();
        }

        [SecurityCritical]
        public string GetSDKVersion() {
            IntPtr p = SVBGetSDKVersion();
            return Marshal.PtrToStringAnsi(p);
        }

        [SecurityCritical]
        public SVB_CAMERA_INFO GetCameraInfo(int id) {
            SVBonyPInvoke.SVBGetCameraInfo(out var info, id);
            return info;
        }
    }

    [ExcludeFromCodeCoverage]
    public static class SVBonyPInvoke {
        private const string DLLNAME = "SVBCameraSDK.dll";

        static SVBonyPInvoke() {
            DllLoader.LoadDll(Path.Combine("SVBony", DLLNAME));
        }

        /// <summary>
        /// /***************************************************************************
        /// Descriptions:
        /// this should be the first API to be called
        /// get number of connected SVB cameras,
        ///
        /// Paras:
        ///
        /// return:number of connected SVB cameras. 1 means 1 camera connected.
        /// ***************************************************************************/
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = nameof(SVBGetNumOfConnectedCameras), CallingConvention = CallingConvention.Cdecl)]
        public static extern int SVBGetNumOfConnectedCameras();

        /// <summary>
        /// /***************************************************************************
        /// Descriptions:
        /// get the information of the connected cameras, you can do this without open the camera.
        /// here is the sample code:
        ///
        /// int iNumofConnectCameras = SVBGetNumOfConnectedCameras();
        /// SVB_CAMERA_INFO **ppSVBCameraInfo = (SVB_CAMERA_INFO **)malloc(sizeof(SVB_CAMERA_INFO *)*iNumofConnectCameras);
        /// for(int i = 0; i < iNumofConnectCameras; i++)
        /// {
        /// ppSVBCameraInfo[i] = (SVB_CAMERA_INFO *)malloc(sizeof(SVB_CAMERA_INFO ));
        /// SVBGetCameraInfo(ppSVBCameraInfo[i], i);
        /// }
		///
        /// Paras:
	    ///     SVB_CAMERA_INFO *pSVBCameraInfo: Pointer to structure containing the information of camera
		///						            user need to malloc the buffer
	    ///     int iCameraIndex: 0 means the first connect camera, 1 means the second connect camera
        ///
        /// return:
	    ///     SVB_SUCCESS: Operation is successful
	    ///     SVB_ERROR_INVALID_INDEX  :no camera connected or index value out of boundary
        ///
        /// ***************************************************************************/
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = nameof(SVBGetCameraInfo), CallingConvention = CallingConvention.Cdecl)]
        public static extern SVB_ERROR_CODE SVBGetCameraInfo(out SVB_CAMERA_INFO info, int index);

        /// <summary>
        /// /***************************************************************************
        /// Descriptions:
        /// get the property of the connected cameras
        /// here is the sample code:
        ///
        /// Paras:
        /// int CameraID: this is get from the camera property use the API SVBGetCameraProperty
        /// SVB_CAMERA_PROPERTY* pCameraProperty: Pointer to structure containing the property of camera
        ///
        ///                                 user need to malloc the buffer
        ///
        /// return:
        /// SVB_SUCCESS: Operation is successful
        /// SVB_ERROR_INVALID_INDEX  :no camera connected or index value out of boundary
        ///
        /// ***************************************************************************/
        /// </summary>
        /// <param name="iCameraID"></param>
        /// <param name="pCameraProperty"></param>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = nameof(SVBGetCameraProperty), CallingConvention = CallingConvention.Cdecl)]
        public static extern SVB_ERROR_CODE SVBGetCameraProperty(int iCameraID, out SVB_CAMERA_PROPERTY pCameraProperty);

        [DllImport(DLLNAME, EntryPoint = nameof(SVBGetCameraPropertyEx), CallingConvention = CallingConvention.Cdecl)]
        public static extern SVB_ERROR_CODE SVBGetCameraPropertyEx(int iCameraID, out SVB_CAMERA_PROPERTY_EX pCameraProperty);

        /// <summary>
        /// /***************************************************************************
        /// Descriptions:
        ///     open the camera before any operation to the camera, this will not affect the camera which is capturing
        ///     All APIs below need to open the camera at first.
        ///
        /// Paras:
        ///     int CameraID: this is get from the camera property use the API SVBGetCameraInfo
        ///
        /// return:
        /// SVB_SUCCESS: Operation is successful
        /// SVB_ERROR_INVALID_ID  : no camera of this ID is connected or ID value is out of boundary
        /// SVB_ERROR_CAMERA_REMOVED: failed to find the camera, maybe camera has been removed
        ///
        /// ***************************************************************************/
        /// </summary>
        /// <param name="iCameraID"></param>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = nameof(SVBOpenCamera), CallingConvention = CallingConvention.Cdecl)]
        public static extern SVB_ERROR_CODE SVBOpenCamera(int iCameraID);

        /// <summary>
        /// /***************************************************************************
        /// Descriptions:
        ///     you need to close the camera to free all the resource
        ///
        ///
        ///     Paras:
        ///     int CameraID: this is get from the camera property use the API SVBGetCameraInfo
        ///
        ///     return:
        ///     SVB_SUCCESS :it will return success even the camera already closed
        ///     SVB_ERROR_INVALID_ID  :no camera of this ID is connected or ID value is out of boundary
        ///
        ///     ***************************************************************************/
        /// </summary>
        /// <param name="iCameraID"></param>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = nameof(SVBCloseCamera), CallingConvention = CallingConvention.Cdecl)]
        public static extern SVB_ERROR_CODE SVBCloseCamera(int iCameraID);

        /// <summary>
        /// /***************************************************************************
        /// Descriptions:
        /// Get number of controls available for this camera. the camera need be opened at first.
        ///
        ///
        ///
        /// Paras:
        /// int CameraID: this is get from the camera property use the API SVBGetCameraInfo
        /// int * piNumberOfControls: pointer to an int to save the number of controls
        ///
        /// return:
        /// SVB_SUCCESS : Operation is successful
        /// SVB_ERROR_CAMERA_CLOSED : camera didn't open
        /// SVB_ERROR_INVALID_ID  :no camera of this ID is connected or ID value is out of boundary
        /// ***************************************************************************/
        /// </summary>
        /// <param name="iCameraID"></param>
        /// <param name="piNumberOfControls"></param>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = nameof(SVBGetNumOfControls), CallingConvention = CallingConvention.Cdecl)]
        public static extern SVB_ERROR_CODE SVBGetNumOfControls(int iCameraID, out int piNumberOfControls);

        /// <summary>
        /// /***************************************************************************
        /// Descriptions:
        /// Get controls property available for this camera. the camera need be opened at first.
        /// user need to malloc and maintain the buffer.
        ///
        ///
        ///
        /// Paras:
        /// int CameraID: this is get from the camera property use the API SVBGetCameraInfo
        /// int iControlIndex: index of control, NOT control type
        /// SVB_CONTROL_CAPS * pControlCaps: Pointer to structure containing the property of the control
        /// user need to malloc the buffer
        ///
        /// return:
        /// SVB_SUCCESS : Operation is successful
        /// SVB_ERROR_CAMERA_CLOSED : camera didn't open
        /// SVB_ERROR_INVALID_ID  :no camera of this ID is connected or ID value is out of boundary
        /// ***************************************************************************/
        /// </summary>
        /// <param name="iCameraID"></param>
        /// <param name="iControlIndex"></param>
        /// <param name="pControlCaps"></param>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = nameof(SVBGetControlCaps), CallingConvention = CallingConvention.Cdecl)]
        public static extern SVB_ERROR_CODE SVBGetControlCaps(int iCameraID, int iControlIndex, out SVB_CONTROL_CAPS pControlCaps);

        /// <summary>
        /// /***************************************************************************
        /// Descriptions:
        /// Get controls property value and auto value
        /// note:the value of the temperature is the float value * 10 to convert it to int type, control name is "Temperature"
        /// because int is the only type for control(except cooler's target temperature, because it is an integer)
        ///
        /// Paras:
        /// int CameraID: this is get from the camera property use the API SVBGetCameraInfo
        /// int ControlType: this is get from control property use the API SVBGetControlCaps
        /// int *plValue: pointer to the value you want to save the value get from control
        /// SVB_BOOL *pbAuto: pointer to the SVB_BOOL type
        ///
        /// return:
        /// SVB_SUCCESS : Operation is successful
        /// SVB_ERROR_CAMERA_CLOSED : camera didn't open
        /// SVB_ERROR_INVALID_ID  :no camera of this ID is connected or ID value is out of boundary
        /// SVB_ERROR_INVALID_CONTROL_TYPE, //invalid Control type
        /// ***************************************************************************/
        /// </summary>
        /// <param name="iCameraID"></param>
        /// <param name="ControlType"></param>
        /// <param name="value"></param>
        /// <param name="pbAuto"></param>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = nameof(SVBGetControlValue), CallingConvention = CallingConvention.Cdecl)]
        public static extern SVB_ERROR_CODE SVBGetControlValue(int iCameraID, SVB_CONTROL_TYPE ControlType, out int value, out SVB_BOOL pbAuto);

        /// <summary>
        /// /***************************************************************************
        /// Descriptions:
        /// Set controls property value and auto value
        /// it will return success and set the max value or min value if the value is beyond the boundary
        ///
        ///
        /// Paras:
        /// int CameraID: this is get from the camera property use the API SVBGetCameraInfo
        /// int ControlType: this is get from control property use the API SVBGetControlCaps
        /// int lValue: the value set to the control
        /// SVB_BOOL bAuto: set the control auto
        ///
        /// return:
        /// SVB_SUCCESS : Operation is successful
        /// SVB_ERROR_CAMERA_CLOSED : camera didn't open
        /// SVB_ERROR_INVALID_ID  :no camera of this ID is connected or ID value is out of boundary
        /// SVB_ERROR_INVALID_CONTROL_TYPE, //invalid Control type
        /// SVB_ERROR_GENERAL_ERROR,//general error, eg: value is out of valid range; operate to camera hareware failed
        /// ***************************************************************************/
        /// </summary>
        /// <param name="iCameraID"></param>
        /// <param name="ControlType"></param>
        /// <param name="value"></param>
        /// <param name="pbAuto"></param>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = nameof(SVBSetControlValue), CallingConvention = CallingConvention.Cdecl)]
        public static extern SVB_ERROR_CODE SVBSetControlValue(int iCameraID, SVB_CONTROL_TYPE ControlType, int value, SVB_BOOL pbAuto);

        /// <summary>
        /// /***************************************************************************
        /// Description:
        /// Get sensor pixel size in microns
        /// Paras:
        /// int CameraID: this is get from the camera property use the API SVBGetCameraInfo.
        /// float *fPixelSize: sensor pixel size in microns
        ///
        /// return:
        /// SVB_SUCCESS : Operation is successful
        /// SVB_ERROR_INVALID_ID  :no camera of this ID is connected or ID value is out of boundary
        /// SVB_ERROR_UNKNOW_SENSOR_TYPE : unknow sensor type
        /// ***************************************************************************/
        /// </summary>
        /// <param name="iCameraID"></param>
        /// <param name="fPixelSize"></param>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = nameof(SVBGetSensorPixelSize), CallingConvention = CallingConvention.Cdecl)]
        public static extern SVB_ERROR_CODE SVBGetSensorPixelSize(int iCameraID, out float fPixelSize);

        /// <summary>
        /// /***************************************************************************
        /// Description:
        /// Set the camera mode, only need to call when the IsTriggerCam in the CameraInfo is true
        /// Paras:
        /// int CameraID: this is get from the camera property use the API SVBGetCameraInfo
        /// SVB_CAMERA_MODE: this is get from the camera property use the API SVBGetCameraProperty
        ///
        /// return:
        /// SVB_SUCCESS : Operation is successful
        /// SVB_ERROR_CAMERA_CLOSED : camera didn't open
        /// SVB_ERROR_INVALID_SEQUENCE : camera is in capture now, need to stop capture first.
        /// SVB_ERROR_INVALID_MODE  : mode is out of boundary or this camera do not support this mode
        /// ***************************************************************************/
        /// </summary>
        /// <param name="iCameraID"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = nameof(SVBSetCameraMode), CallingConvention = CallingConvention.Cdecl)]
        public static extern SVB_ERROR_CODE SVBSetCameraMode(int iCameraID, SVB_CAMERA_MODE mode);

        /// <summary>
        /// /***************************************************************************
        /// Description:
        /// Send out a softTrigger. For edge trigger, it only need to set true which means send a
        /// rising trigger to start exposure. For level trigger, it need to set true first means
        /// start exposure, and set false means stop exposure.it only need to call when the
        /// IsTriggerCam in the CameraInfo is true
        /// Paras:
        /// int CameraID: this is get from the camera property use the API SVBGetCameraInfo
        ///
        /// return:
        /// SVB_SUCCESS : Operation is successful
        /// SVB_ERROR_CAMERA_CLOSED : camera didn't open
        /// ***************************************************************************/
        /// </summary>
        /// <param name="iCameraID"></param>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = nameof(SVBSendSoftTrigger), CallingConvention = CallingConvention.Cdecl)]
        public static extern SVB_ERROR_CODE SVBSendSoftTrigger(int iCameraID);

        /// <summary>
        /// /***************************************************************************
        /// Descriptions:
        /// Set the output image type, The value set must be the type supported by the SVBGetCameraProperty function.
        ///
        /// Paras:
        /// int CameraID: this is get from the camera property use the API SVBGetCameraInfo
        /// SVB_IMG_TYPE *pImageType: pointer to current image type.
        ///
        /// return:
        /// SVB_SUCCESS : Operation is successful
        /// SVB_ERROR_CAMERA_CLOSED : camera didn't open
        /// SVB_ERROR_INVALID_ID  :no camera of this ID is connected or ID value is out of boundary
        /// SVB_ERROR_INVALID_IMGTYPE, //invalid image type
        /// SVB_ERROR_GENERAL_ERROR,//general error, eg: value is out of valid range; operate to camera hareware failed
        /// ***************************************************************************/
        /// </summary>
        /// <param name="iCameraID"></param>
        /// <param name="ImageType"></param>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = nameof(SVBSetOutputImageType), CallingConvention = CallingConvention.Cdecl)]
        public static extern SVB_ERROR_CODE SVBSetOutputImageType(int iCameraID, SVB_IMG_TYPE ImageType);

        /// <summary>
        /// /***************************************************************************
        /// Descriptions:
        /// get data from the video buffer.the buffer is very small
        /// you need to call this API as fast as possible, otherwise frame will be discarded
        /// so the best way is maintain one buffer loop and call this API in a loop
        /// please make sure the buffer size is biger enough to hold one image
        /// otherwise the this API will crash
        ///
        ///
        /// Paras:
        /// int CameraID: this is get from the camera property use the API SVBGetCameraInfo
        /// unsigned char* pBuffer, caller need to malloc the buffer, make sure the size is big enough
        /// 		the size in byte:
        /// 		8bit mono:width*height
        /// 		16bit mono:width*height*2
        /// 		RGB24:width*height*3
        ///
        /// int iWaitms, this API will block and wait iWaitms to get one image. the unit is ms
        /// 		-1 means wait forever. this value is recommend set to exposure*2+500ms
        ///
        /// return:
        /// SVB_SUCCESS : Operation is successful
        /// SVB_ERROR_CAMERA_CLOSED : camera didn't open
        /// SVB_ERROR_INVALID_ID  :no camera of this ID is connected or ID value is out of boundary
        /// SVB_ERROR_TIMEOUT: no image get and timeout
        /// ***************************************************************************/
        /// </summary>
        /// <param name="iCameraID"></param>
        /// <param name="pBuffer"></param>
        /// <param name="lBuffSize"></param>
        /// <param name="iWaitms"></param>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = "SVBGetVideoData", CallingConvention = CallingConvention.Cdecl)]
        public static extern SVB_ERROR_CODE SVBGetVideoDataMono8(int iCameraID, [Out] byte[] pBuffer, int lBuffSize, int iWaitms);

        [DllImport(DLLNAME, EntryPoint = "SVBGetVideoData", CallingConvention = CallingConvention.Cdecl)]
        public static extern SVB_ERROR_CODE SVBGetVideoDataMono16(int iCameraID, [Out] ushort[] pBuffer, int lBuffSize, int iWaitms);

        /// <summary>
        /// /***************************************************************************
        /// Descriptions:
        /// Start video capture
        /// then you can get the data from the API SVBGetVideoData
        ///
        ///
        /// Paras:
        /// int CameraID: this is get from the camera property use the API SVBGetCameraInfo
        ///
        /// return:
        /// SVB_SUCCESS : Operation is successful, it will return success if already started
        /// SVB_ERROR_CAMERA_CLOSED : camera didn't open
        /// SVB_ERROR_INVALID_ID  :no camera of this ID is connected or ID value is out of boundary
        /// SVB_ERROR_EXPOSURE_IN_PROGRESS: snap mode is working, you need to stop snap first
        /// ***************************************************************************/
        /// </summary>
        /// <param name="iCameraID"></param>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = nameof(SVBStartVideoCapture), CallingConvention = CallingConvention.Cdecl)]
        public static extern SVB_ERROR_CODE SVBStartVideoCapture(int iCameraID);

        /// <summary>
        /// /***************************************************************************
        /// Descriptions:
        /// Stop video capture
        ///
        ///
        /// Paras:
        /// int CameraID: this is get from the camera property use the API SVBGetCameraInfo
        ///
        /// return:
        /// SVB_SUCCESS : Operation is successful, it will return success if already stopped
        /// SVB_ERROR_CAMERA_CLOSED : camera didn't open
        /// SVB_ERROR_INVALID_ID  :no camera of this ID is connected or ID value is out of boundary
        ///
        /// ***************************************************************************/
        /// </summary>
        /// <param name="iCameraID"></param>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = nameof(SVBStopVideoCapture), CallingConvention = CallingConvention.Cdecl)]
        public static extern SVB_ERROR_CODE SVBStopVideoCapture(int iCameraID);

        /// <summary>
        /// /***************************************************************************
        /// Descriptions:
        /// set the ROI area before capture.
        /// you must stop capture before call it.
        /// the width and height is the value after binning.
        /// ie. you need to set width to 640 and height to 480 if you want to run at 640X480@BIN2
        /// SVB120's data size must be times of 1024 which means width*height%1024=0SVBSetStartPos
        /// Paras:
        /// int CameraID: this is get from the camera property use the API SVBGetCameraInfo
        /// int iWidth,  the width of the ROI area. Make sure iWidth%8 = 0.
        /// int iHeight,  the height of the ROI area. Make sure iHeight%2 = 0,
        /// further, for USB2.0 camera SVB120, please make sure that iWidth*iHeight%1024=0.
        /// int iBin,   binning method. bin1=1, bin2=2
        ///
        /// return:
        /// SVB_SUCCESS : Operation is successful
        /// SVB_ERROR_CAMERA_CLOSED : camera didn't open
        /// SVB_ERROR_INVALID_ID  :no camera of this ID is connected or ID value is out of boundary
        /// SVB_ERROR_INVALID_SIZE, //wrong video format size
        /// SVB_ERROR_INVALID_IMGTYPE, //unsupported image format, make sure iWidth and iHeight and binning is set correct
        /// ***************************************************************************/
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = nameof(SVBSetROIFormat), CallingConvention = CallingConvention.Cdecl)]
        public static extern SVB_ERROR_CODE SVBSetROIFormat(int iCameraID, int iStartX, int iStartY, int iWidth, int iHeight, int iBi);

        /// <summary>
        /// /***************************************************************************
        /// Descriptions:
        /// Get the current ROI area setting .
        ///
        /// Paras:
        /// int CameraID: this is get from the camera property use the API SVBGetCameraInfo
        /// int *piWidth,  pointer to the width of the ROI area
        /// int *piHeight, pointer to the height of the ROI area.
        /// int *piBin,   pointer to binning method. bin1=1, bin2=2
        ///
        /// return:
        /// SVB_SUCCESS : Operation is successful
        /// SVB_ERROR_CAMERA_CLOSED : camera didn't open
        /// SVB_ERROR_INVALID_ID  :no camera of this ID is connected or ID value is out of boundary
        ///
        /// ***************************************************************************/
        /// </summary>
        /// <param name="iCameraID"></param>
        /// <param name="iStartX"></param>
        /// <param name="iStartY"></param>
        /// <param name="iWidth"></param>
        /// <param name="iHeight"></param>
        /// <param name="iBi"></param>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = nameof(SVBGetROIFormat), CallingConvention = CallingConvention.Cdecl)]
        public static extern SVB_ERROR_CODE SVBGetROIFormat(int iCameraID, out int iStartX, out int iStartY, out int iWidth, out int iHeight, out int iBi);

        /// <summary>
        /// /***************************************************************************
        /// Descriptions:
        /// get version string, like "1, 13, 0503"
        /// ***************************************************************************/
        /// </summary>
        /// <returns></returns>
        [DllImport(DLLNAME, EntryPoint = nameof(SVBGetSDKVersion), CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr SVBGetSDKVersion();

        public static string GetSDKVersion() {
            IntPtr p = SVBGetSDKVersion();
            string version = Marshal.PtrToStringAnsi(p);

            return version;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    [ExcludeFromCodeCoverage]
    public struct SVB_CAMERA_INFO {

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] friendlyName;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] cameraSN;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] portType;

        public uint DeviceID;

        public uint CameraID;

        public string FriendlyName {
            get { return friendlyName == null ? "" : Encoding.ASCII.GetString(friendlyName).TrimEnd((char)0); }
        }

        public string CameraSN {
            get { return cameraSN == null ? "" : Encoding.ASCII.GetString(cameraSN).TrimEnd((char)0); }
        }

        public string PortType {
            get { return portType == null ? "" : Encoding.ASCII.GetString(portType).TrimEnd((char)0); }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SVB_CAMERA_PROPERTY {
        public int MaxHeight; //the max height of the camera
        public int MaxWidth; //the max width of the camera
        public SVB_BOOL IsColorCam;
        public SVB_BAYER_PATTERN BayerPattern;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public int[] SupportedBins; //1 means bin1 which is supported by every camera, 2 means bin 2 etc.. 0 is the end of supported binning method

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public SVB_IMG_TYPE[] SupportedVideoFormat;

        public int MaxBitDepth;
        public SVB_BOOL IsTriggerCam;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SVB_CAMERA_PROPERTY_EX {
        public SVB_BOOL bSupportPulseGuide;
        public SVB_BOOL bSupportControlTemp;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public int[] Unused;
    }

    [ExcludeFromCodeCoverage]
    public struct SVB_CONTROL_CAPS {

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] name; //the name of the Control like Exposure, Gain etc..

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public byte[] description; //description of this control

        public int MaxValue;
        public int MinValue;
        public int DefaultValue;
        public SVB_BOOL IsAutoSupported; //support auto set 1, don't support 0
        public SVB_BOOL IsWritable; //some control like temperature can only be read by some cameras
        public SVB_CONTROL_TYPE ControlType;//this is used to get value and set value of the control

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public char[] Unused;

        public string Description {
            get { return description == null ? "" : Encoding.ASCII.GetString(description).TrimEnd((char)0); }
        }

        public string Name {
            get { return name == null ? "" : Encoding.ASCII.GetString(name).TrimEnd((char)0); }
        }
    }

    public enum SVB_BOOL {
        SVB_FALSE = 0,
        SVB_TRUE
    }

    public enum SVB_EXPOSURE_STATUS {
        SVB_EXP_IDLE = 0,//: idle states, you can start exposure now
        SVB_EXP_WORKING,//: exposing
        SVB_EXP_SUCCESS,//: exposure finished and waiting for download
        SVB_EXP_FAILED,//:exposure failed, you need to start exposure again
    }

    public enum SVB_CONTROL_TYPE { //Control type//
        SVB_GAIN = 0,
        SVB_EXPOSURE,
        SVB_GAMMA,
        SVB_GAMMA_CONTRAST,
        SVB_WB_R,
        SVB_WB_G,
        SVB_WB_B,
        SVB_FLIP,//reference: enum SVB_FLIP_STATUS
        SVB_FRAME_SPEED_MODE,//0:low speed, 1:medium speed, 2:high speed
        SVB_CONTRAST,
        SVB_SHARPNESS,
        SVB_SATURATION,

        SVB_AUTO_TARGET_BRIGHTNESS,
        SVB_BLACK_LEVEL, //black level offset
        SVB_COOLER_ENABLE,  //0:disable, 1:enable
        SVB_TARGET_TEMPERATURE,  //unit is 0.1C
        SVB_CURRENT_TEMPERATURE, //unit is 0.1C
        SVB_COOLER_POWER,  //range: 0-100

        SVB_BAD_PIXEL_CORRECTION_ENABLE,
        SVB_BAD_PIXEL_CORRECTION_THRESHOLD,
    }

    public enum SVB_IMG_TYPE {//Supported Video Format
        SVB_IMG_RAW8 = 0,
        SVB_IMG_RAW10,
        SVB_IMG_RAW12,
        SVB_IMG_RAW14,
        SVB_IMG_RAW16,
        SVB_IMG_Y8,
        SVB_IMG_Y10,
        SVB_IMG_Y12,
        SVB_IMG_Y14,
        SVB_IMG_Y16,
        SVB_IMG_RGB24,
        SVB_IMG_RGB32,
        SVB_IMG_END = -1
    }

    public enum SVB_FLIP_STATUS {
        SVB_FLIP_NONE = 0,//: original
        SVB_FLIP_HORIZ,//: horizontal flip
        SVB_FLIP_VERT,// vertical flip
        SVB_FLIP_BOTH,//:both horizontal and vertical flip
    }

    public enum SVB_CAMERA_MODE {
        SVB_MODE_NORMAL = 0,
        SVB_MODE_TRIG_SOFT,
        SVB_MODE_TRIG_RISE_EDGE,
        SVB_MODE_TRIG_FALL_EDGE,
        SVB_MODE_TRIG_DOUBLE_EDGE,
        SVB_MODE_TRIG_HIGH_LEVEL,
        SVB_MODE_TRIG_LOW_LEVEL,
        SVB_MODE_END = -1
    }

    public enum SVB_BAYER_PATTERN {
        SVB_BAYER_RG = 0,
        SVB_BAYER_BG,
        SVB_BAYER_GR,
        SVB_BAYER_GB
    }

    public enum SVB_ERROR_CODE {//SVB ERROR CODE
        SVB_SUCCESS = 0,
        SVB_ERROR_INVALID_INDEX, //no camera connected or index value out of boundary
        SVB_ERROR_INVALID_ID, //invalid ID
        SVB_ERROR_INVALID_CONTROL_TYPE, //invalid control type
        SVB_ERROR_CAMERA_CLOSED, //camera didn't open
        SVB_ERROR_CAMERA_REMOVED, //failed to find the camera, maybe the camera has been removed
        SVB_ERROR_INVALID_PATH, //cannot find the path of the file
        SVB_ERROR_INVALID_FILEFORMAT,
        SVB_ERROR_INVALID_SIZE, //wrong video format size
        SVB_ERROR_INVALID_IMGTYPE, //unsupported image formate
        SVB_ERROR_OUTOF_BOUNDARY, //the startpos is out of boundary
        SVB_ERROR_TIMEOUT, //timeout
        SVB_ERROR_INVALID_SEQUENCE,//stop capture first
        SVB_ERROR_BUFFER_TOO_SMALL, //buffer size is not big enough
        SVB_ERROR_VIDEO_MODE_ACTIVE,
        SVB_ERROR_EXPOSURE_IN_PROGRESS,
        SVB_ERROR_GENERAL_ERROR,//general error, eg: value is out of valid range
        SVB_ERROR_INVALID_MODE,//the current mode is wrong
        SVB_ERROR_INVALID_DIRECTION,//invalid guide direction
        SVB_ERROR_UNKNOW_SENSOR_TYPE,//unknow sensor type
        SVB_ERROR_END
    }
}