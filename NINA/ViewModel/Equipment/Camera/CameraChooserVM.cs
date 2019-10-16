#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using EDSDKLib;
using NINA.Model.MyCamera;
using NINA.Utility;
using NINA.Utility.AtikSDK;
using NINA.Utility.Mediator.Interfaces;
using NINA.Profile;
using FLI;
using QHYCCD;
using System;
using System.Collections.Generic;
using ZWOptical.ASISDK;
using System.Linq;

namespace NINA.ViewModel.Equipment.Camera {

    internal class CameraChooserVM : EquipmentChooserVM {
        private ITelescopeMediator telescopeMediator;

        public CameraChooserVM(IProfileService profileService, ITelescopeMediator telescopeMediator) : base(profileService) {
            this.telescopeMediator = telescopeMediator;
        }

        public override void GetEquipment() {
            Devices.Clear();

            Devices.Add(new Model.DummyDevice(Locale.Loc.Instance["LblNoCamera"]));

            /* ASI */
            try {
                Logger.Trace("Adding ASI Cameras");
                for (int i = 0; i < ASICameras.Count; i++) {
                    var cam = ASICameras.GetCamera(i, profileService);
                    if (!string.IsNullOrEmpty(cam.Name)) {
                        Logger.Trace(string.Format("Adding {0}", cam.Name));
                        Devices.Add(cam);
                    }
                }
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            /* Altair */
            try {
                Logger.Trace("Adding Altair Cameras");
                foreach (var instance in Altair.AltairCam.EnumV2()) {
                    var cam = new AltairCamera(instance, profileService);
                    Devices.Add(cam);
                }
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            /* Atik */
            try {
                Logger.Trace("Adding Atik Cameras");
                var atikDevices = AtikCameraDll.GetDevicesCount();
                Logger.Trace($"Cameras found: {atikDevices}");
                if (atikDevices > 0) {
                    for (int i = 0; i < atikDevices; i++) {
                        var cam = new AtikCamera(i, profileService);
                        Devices.Add(cam);
                    }
                }
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            /* FLI */
            try {
                Logger.Trace("Adding FLI Cameras");
                List<string> cameras = FLICameras.GetCameras();

                if (cameras.Count > 0) {
                    foreach (var entry in cameras) {
                        var camera = new FLICamera(entry, profileService);

                        if (!string.IsNullOrEmpty(camera.Name)) {
                            Logger.Debug($"Adding FLI camera {camera.Id} (as {camera.Name})");
                            Devices.Add(camera);
                        }
                    }
                }
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            /* QHYCCD */
            try {
                Logger.Trace("Adding QHYCCD Cameras");
                uint numCameras = QHYCameras.Count;

                if (numCameras > 0) {
                    for (uint i = 0; i < numCameras; i++) {
                        var cam = QHYCameras.GetCamera(i, profileService);
                        if (!string.IsNullOrEmpty(cam.Name)) {
                            Logger.Debug($"Adding QHY camera {i}: {cam.Id} (as {cam.Name})");
                            Devices.Add(cam);
                        }
                    }
                }
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            /* ToupTek */
            try {
                Logger.Debug("Adding ToupTek Cameras");
                foreach (var instance in ToupTek.ToupCam.EnumV2()) {
                    var cam = new ToupTekCamera(instance, profileService);
                    Devices.Add(cam);
                }
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            /* ASCOM */
            try {
                foreach (ICamera cam in ASCOMInteraction.GetCameras(profileService)) {
                    Devices.Add(cam);
                }
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            /* CANON */
            try {
                IntPtr cameraList;
                try {
                    EDSDK.Initialize();
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Utility.Notification.Notification.ShowError(ex.Message);
                }

                uint err = EDSDK.EdsGetCameraList(out cameraList);
                if (err == (uint)EDSDK.EDS_ERR.OK) {
                    int count;
                    err = EDSDK.EdsGetChildCount(cameraList, out count);

                    for (int i = 0; i < count; i++) {
                        IntPtr cam;
                        err = EDSDK.EdsGetChildAtIndex(cameraList, i, out cam);

                        EDSDK.EdsDeviceInfo info;
                        err = EDSDK.EdsGetDeviceInfo(cam, out info);

                        Logger.Trace(string.Format("Adding {0}", info.szDeviceDescription));
                        Devices.Add(new EDCamera(cam, info, profileService));
                    }
                }
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            /* NIKON */
            try {
                Devices.Add(new NikonCamera(profileService, telescopeMediator));
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            Devices.Add(new Model.MyCamera.FileCamera(profileService, telescopeMediator));
            Devices.Add(new Model.MyCamera.Simulator.SimulatorCamera(profileService, telescopeMediator));

            DetermineSelectedDevice(profileService.ActiveProfile.CameraSettings.Id);
        }
    }
}