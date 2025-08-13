#region "copyright"
/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Equipment.SDK.CameraSDKs.ASTPANSDK;
using NINA.Equipment.SDK.CameraSDKs.PlayerOneSDK;
using NINA.Equipment.SDK.CameraSDKs.SBIGSDK.SbigSharp;
using NINA.Equipment.SDK.CameraSDKs.SVBonySDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.ViewModel {

    public class AboutSDKsVM {

        public AboutSDKsVM() {
            try {
                AltairSDKVersion = Altair.Altaircam.Version();
            } catch (Exception) {
                AltairSDKVersion = Loc.Instance["LblNotInstalled"];
            }
            try {
                ZWOSDKVersion = ZWOptical.ASISDK.ASICameraDll.GetSDKVersion();
            } catch (Exception) {
                ZWOSDKVersion = Loc.Instance["LblNotInstalled"];
            }
            try {
                AtikSDKVersion = NINA.Equipment.SDK.CameraSDKs.AtikSDK.AtikCameraDll.DriverVersion;
            } catch (Exception) {
                AtikSDKVersion = Loc.Instance["LblNotInstalled"];
            }

            try {
                CanonSDKVersion = DllLoader.DllVersion(Path.Combine("Canon", "EDSDK.dll")).ProductVersion;
            } catch (Exception) {
                CanonSDKVersion = Loc.Instance["LblNotInstalled"];
            }

            try {
                if (DllLoader.IsX86()) {
                    NikonSDKVersion = Loc.Instance["LblNikonX86Deprecated"];
                } else {
                    NikonSDKVersion = DllLoader.DllVersion(Path.Combine("Nikon", "Type0001", "NkdPTP.dll")).ProductVersion;
                }
            } catch (Exception) {
                NikonSDKVersion = Loc.Instance["LblNotInstalled"];
            }

            try {
                StringBuilder version = new StringBuilder(128);
                if (FLI.LibFLI.FLIGetLibVersion(version, 128) == 0) {
                    FLISDKVersion = version.ToString();
                }
            } catch (Exception) {
                FLISDKVersion = Loc.Instance["LblNotInstalled"];
            }

            try {
                OmegonSDKVersion = Omegon.Omegonprocam.Version();
            } catch (Exception) {
                OmegonSDKVersion = Loc.Instance["LblNotInstalled"];
            }

            try {
                QHYSDKVersion = QHYCCD.QhySdk.Instance.GetSdkVersion();
            } catch (Exception) {
                QHYSDKVersion = Loc.Instance["LblNotInstalled"];
            }

            try {
                RisingCamSDKVersion = Nncam.Version();
            } catch (Exception) {
                RisingCamSDKVersion = Loc.Instance["LblNotInstalled"];
            }

            try {
                ToupTekSDKVersion = ToupTek.ToupCam.Version();
            } catch (Exception) {
                ToupTekSDKVersion = Loc.Instance["LblNotInstalled"];
            }

            try {
                SVBonySDKVersion = SVBonyPInvoke.GetSDKVersion();
            } catch (Exception) {
                SVBonySDKVersion = Loc.Instance["LblNotInstalled"];
            }

            try {
                SBIGSDKVersion = SBIG.GetVersion().FileVersion;
            } catch (Exception) {
                SBIGSDKVersion = Loc.Instance["LblNotInstalled"];
            }

            try {
                MallinCamSDKVersion = MallinCam.Mallincam.Version();
            } catch (Exception) {
                MallinCamSDKVersion = Loc.Instance["LblNotInstalled"];
            }

            try {
                PlayerOneSDKVersion = new PlayerOnePInvokeProxy().POAGetSDKVersion();
            } catch (Exception) {
                PlayerOneSDKVersion = Loc.Instance["LblNotInstalled"];
            }

            try {
                ASTPANSDKVersion = new ASTPANPInvokeProxy().GetSDKVersion();
            } catch (Exception) {
                ASTPANSDKVersion = Loc.Instance["LblNotInstalled"];
            }

            try {
                OgmaSDKVersion = Ogmacam.Version();
            } catch (Exception) {
                OgmaSDKVersion = Loc.Instance["LblNotInstalled"];
            }
        }

        public string AltairSDKVersion { get; }
        public string ZWOSDKVersion { get; }
        public string AtikSDKVersion { get; }
        public string CanonSDKVersion { get; }
        public string NikonSDKVersion { get; }
        public string FLISDKVersion { get; }
        public string OmegonSDKVersion { get; }
        public string QHYSDKVersion { get; }
        public string RisingCamSDKVersion { get; }
        public string ToupTekSDKVersion { get; }
        public string SVBonySDKVersion { get; }
        public string SBIGSDKVersion { get; }
        public string MallinCamSDKVersion { get; }
        public string PlayerOneSDKVersion { get; }
        public string ASTPANSDKVersion { get; }
        public string OgmaSDKVersion { get; }
    }
}