#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Runtime.InteropServices;

namespace NINA.Equipment.SDK.CameraSDKs.SVBonySDK {

    public interface ISVBonyPInvokeProxy {

        SVB_CAMERA_INFO GetCameraInfo(int id);

        SVB_ERROR_CODE SVBCloseCamera(int iCameraID);

        SVB_ERROR_CODE SVBGetCameraInfo(out SVB_CAMERA_INFO info, int index);

        SVB_ERROR_CODE SVBGetCameraProperty(int iCameraID, out SVB_CAMERA_PROPERTY pCameraProperty);

        SVB_ERROR_CODE SVBGetCameraPropertyEx(int iCameraID, out SVB_CAMERA_PROPERTY_EX pCameraProperty);

        SVB_ERROR_CODE SVBGetControlCaps(int iCameraID, int iControlIndex, out SVB_CONTROL_CAPS pControlCaps);

        SVB_ERROR_CODE SVBGetControlValue(int iCameraID, SVB_CONTROL_TYPE ControlType, out int value, out SVB_BOOL pbAuto);

        SVB_ERROR_CODE SVBGetNumOfControls(int iCameraID, out int piNumberOfControls);

        SVB_ERROR_CODE SVBGetROIFormat(int iCameraID, out int iStartX, out int iStartY, out int iWidth, out int iHeight, out int iBi);

        SVB_ERROR_CODE SVBGetSensorPixelSize(int iCameraID, out float fPixelSize);

        SVB_ERROR_CODE SVBGetVideoDataMono16(int iCameraID, [Out] ushort[] pBuffer, int lBuffSize, int iWaitms);

        SVB_ERROR_CODE SVBGetVideoDataMono8(int iCameraID, [Out] byte[] pBuffer, int lBuffSize, int iWaitms);

        SVB_ERROR_CODE SVBOpenCamera(int iCameraID);

        SVB_ERROR_CODE SVBSendSoftTrigger(int iCameraID);

        SVB_ERROR_CODE SVBSetCameraMode(int iCameraID, SVB_CAMERA_MODE mode);

        SVB_ERROR_CODE SVBSetControlValue(int iCameraID, SVB_CONTROL_TYPE ControlType, int value, SVB_BOOL pbAuto);

        SVB_ERROR_CODE SVBSetOutputImageType(int iCameraID, SVB_IMG_TYPE ImageType);

        SVB_ERROR_CODE SVBSetROIFormat(int iCameraID, int iStartX, int iStartY, int iWidth, int iHeight, int iBi);

        SVB_ERROR_CODE SVBStartVideoCapture(int iCameraID);

        SVB_ERROR_CODE SVBStopVideoCapture(int iCameraID);

        string GetSDKVersion();

        int SVBGetNumOfConnectedCameras();
    }
}