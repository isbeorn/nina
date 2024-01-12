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

namespace NINA.Equipment.SDK.CameraSDKs.PlayerOneSDK {

    public interface IPlayerOnePInvokeProxy {

        int POAGetCameraCount();

        POAErrors POAGetCameraProperties(int nIndex, out POACameraProperties pProp);

        POAErrors POAGetCameraPropertiesByID(int nCameraID, out POACameraProperties pProp);

        POAErrors POAOpenCamera(int nCameraID);

        POAErrors POAInitCamera(int nCameraID);

        POAErrors POACloseCamera(int nCameraID);

        POAErrors POAGetConfigsCount(int nCameraID, out int pConfCount);

        POAErrors POAGetConfigAttributes(int nCameraID, int nConfIndex, out POAConfigAttributes pConfAttr);

        POAErrors POAGetConfigAttributesByConfigID(int nCameraID, POAConfig confID, out POAConfigAttributes pConfAttr);

        POAErrors POASetConfig(int nCameraID, POAConfig confID, POAConfigValue confValue, POABool isAuto);

        POAErrors POAGetConfig(int nCameraID, POAConfig confID, out POAConfigValue confValue, out POABool isAuto);

        POAErrors POAGetConfigValueType(POAConfig confID, out POAValueType pConfValueType);

        POAErrors POASetImageStartPos(int nCameraID, int startX, int startY);

        POAErrors POAGetImageStartPos(int nCameraID, out int pStartX, out int pStartY);

        POAErrors POASetImageSize(int nCameraID, int width, int height);

        POAErrors POAGetImageSize(int nCameraID, out int pWidth, out int pHeight);

        POAErrors POASetImageBin(int nCameraID, int bin);

        POAErrors POAGetImageBin(int nCameraID, out int pBin);

        POAErrors POASetImageFormat(int nCameraID, POAImgFormat imgFormat);

        POAErrors POAGetImageFormat(int nCameraID, out POAImgFormat pImgFormat);

        POAErrors POAStartExposure(int nCameraID, POABool bSignalFrame);

        POAErrors POAStopExposure(int nCameraID);

        POAErrors POAGetCameraState(int nCameraID, out POACameraState pCameraState);

        POAErrors POAGetImageData(int nCameraID, [Out] ushort[] pBuf, int nBufSize, int nTimeoutms);

        POAErrors POAGetDroppedImagesCount(int nCameraID, out int pDroppedCount);

        POAErrors POASetUserCustomID(int nCameraID, IntPtr pCustomID, int len);

        POAErrors POAGetGainOffset(int nCameraID, out int pOffsetHighestDR, out int pOffsetUnityGain, out int pGainLowestRN, out int pOffsetLowestRN, out int pHCGain);

        POAErrors POAImageReady(int nCameraID, out POABool pIsReady);

        IntPtr POAGetErrorString(POAErrors err);

        int POAGetAPIVersion();

        string POAGetSDKVersion();
        POAErrors POASetTrgModeEnable(int nCameraId, POABool enable);
        POAErrors POAGetSensorModeCount(int nCameraID, out int pModeCount);

        POAErrors POAGetSensorMode(int nCameraID, out int pModeIndex);

        POAErrors POAGetSensorModeInfo(int nCameraID, int index, out POASensorModeInfo pSenModeInfo);

        POAErrors POASetSensorMode(int nCameraID, int modeIndex);
    }
}