using System.Runtime.InteropServices;

namespace NINA.Equipment.SDK.CameraSDKs.ASTPANSDK {
    public interface IASTPANPInvokeProxy {
        ASTPAN_RET_TYPE ASTPANCloseCamera(int id);
        ASTPAN_RET_TYPE ASTPANGetAutoConfigInfo(int id, ref ASTPAN_AUTO_CONFIG_INFO pAutoConfigInfo);
        ASTPAN_RET_TYPE ASTPANGetAutoConfigValue(int id, ASTPAN_AUTO_TYPE index, out int value, out int bAuto);
        ASTPAN_RET_TYPE ASTPANGetCameraInfo(out ASTPAN_CAMERA_INFO pCameraInfo, int iCameraIndex);
        ASTPAN_RET_TYPE ASTPANGetCameraInfoByID(int id, out ASTPAN_CAMERA_INFO pCameraInfo);
        ASTPAN_RET_TYPE ASTPANGetConfigValue(int id, ref ASTPAN_CONFIG pConfig, ASTPAN_CFG_TYPE index);
        ASTPAN_RET_TYPE ASTPANGetDataAfterExpMono16(int iCameraID, [Out] ushort[] pBuffer, int lBuffSize);
        ASTPAN_RET_TYPE ASTPANGetExpStatus(int id, out ASTPAN_EXP_TYPE pExpStatus);
        ASTPAN_RET_TYPE ASTPANGetDataAfterExpMono8(int iCameraID, [Out] byte[] pBuffer, int lBuffSize);
        ASTPAN_RET_TYPE ASTPANGetNumOfCameras(out int number);
        ASTPAN_RET_TYPE ASTPANGetVideoDataMono16(int iCameraID, [Out] ushort[] pBuffer, int lBuffSize, int iWaitms);
        ASTPAN_RET_TYPE ASTPANGetVideoDataMono8(int iCameraID, [Out] byte[] pBuffer, int lBuffSize, int iWaitms);
        ASTPAN_RET_TYPE ASTPANInitCamera(int id);
        ASTPAN_RET_TYPE ASTPANOpenCamera(int id);
        ASTPAN_RET_TYPE ASTPANSetAutoConfigValue(int id, ASTPAN_AUTO_TYPE index, int value, int bAuto);
        ASTPAN_RET_TYPE ASTPANSetConfigValue(int id, ref ASTPAN_CONFIG pConfig, ASTPAN_CFG_TYPE index);
        ASTPAN_RET_TYPE ASTPANStartExposure(int id);
        ASTPAN_RET_TYPE ASTPANStartVideoCapture(int id);
        ASTPAN_RET_TYPE ASTPANStopExposure(int id);
        ASTPAN_RET_TYPE ASTPANStopVideoCapture(int id);
    }
}