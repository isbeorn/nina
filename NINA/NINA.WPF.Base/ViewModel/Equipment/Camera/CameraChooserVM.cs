#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using EDSDKLib;
using FLI;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyCamera.ToupTekAlike;
using NINA.Profile.Interfaces;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using QHYCCD;
using System;
using System.Collections.Generic;
using ZWOptical.ASISDK;
using NINA.Equipment.SDK.CameraSDKs.AtikSDK;
using NINA.Equipment.Utility;
using NINA.Core.Locale;
using NINA.Equipment.Equipment;
using NINA.Equipment.Interfaces;
using NINA.WPF.Base.Model.Equipment.MyCamera.Simulator;
using NINA.Equipment.SDK.CameraSDKs.SBIGSDK;
using NINA.Image.Interfaces;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Core.Interfaces;
using System.Threading.Tasks;

namespace NINA.WPF.Base.ViewModel.Equipment.Camera {

    public class CameraChooserVM : DeviceChooserVM<ICamera> {
        private readonly ITelescopeMediator telescopeMediator;
        private readonly ISbigSdk sbigSdk;
        private readonly IExposureDataFactory exposureDataFactory;
        private readonly IImageDataFactory imageDataFactory;

        public CameraChooserVM(IProfileService profileService,
                               ITelescopeMediator telescopeMediator,
                               ISbigSdk sbigSdk,
                               IExposureDataFactory exposureDataFactory,
                               IImageDataFactory imageDataFactory,
                               IEquipmentProviders<ICamera> equipmentProviders) : base(profileService, equipmentProviders) {
            this.telescopeMediator = telescopeMediator;
            this.sbigSdk = sbigSdk;
            this.exposureDataFactory = exposureDataFactory;
            this.imageDataFactory = imageDataFactory;
        }

        public override async Task GetEquipment() {
            await lockObj.WaitAsync();
            try {

                var devices = new List<IDevice>();

                devices.Add(new DummyDevice(Loc.Instance["LblNoCamera"]));

                /* ASI */
                try {
                    var asiCameras = ASICameras.Count;
                    Logger.Info($"Found {asiCameras} ASI Cameras");
                    for (int i = 0; i < asiCameras; i++) {
                        var cam = ASICameras.GetCamera(i, profileService, exposureDataFactory);
                        if (!string.IsNullOrEmpty(cam.Name)) {
                            Logger.Trace(string.Format("Adding {0}", cam.Name));
                            devices.Add(cam);
                        }
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                /* Altair */
                try {
                    var altairCameras = Altair.Altaircam.EnumV2();
                    Logger.Info($"Found {altairCameras?.Length} Altair Cameras");
                    foreach (var instance in altairCameras) {
                        var cam = new ToupTekAlikeCamera(instance.ToDeviceInfo(), new AltairSDKWrapper(), profileService, exposureDataFactory);
                        devices.Add(cam);
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                /* Atik */
                try {
                    var atikDevices = AtikCameraDll.GetDevicesCount();
                    Logger.Info($"Found {atikDevices} Atik Cameras");
                    if (atikDevices > 0) {
                        for (int i = 0; i < atikDevices; i++) {                            
                            var cam = new AtikCamera(i, profileService, exposureDataFactory);
                            devices.Add(cam);
                        }
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                /* FLI */
                try {
                    List<string> cameras = FLICameras.GetCameras();
                    Logger.Info($"Found {cameras.Count} FLI Cameras");

                    if (cameras.Count > 0) {
                        foreach (var entry in cameras) {
                            var camera = new FLICamera(entry, profileService, exposureDataFactory);

                            if (!string.IsNullOrEmpty(camera.Name)) {
                                Logger.Debug($"Adding FLI camera {camera.Id} (as {camera.Name})");
                                devices.Add(camera);
                            }
                        }
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                /* QHYCCD */
                try {
                    var qhy = new QHYCameras(exposureDataFactory);
                    uint numCameras = qhy.Count;
                    Logger.Info($"Found {numCameras} QHYCCD Cameras");

                    if (numCameras > 0) {
                        for (uint i = 0; i < numCameras; i++) {
                            var cam = qhy.GetCamera(i, profileService);
                            if (!string.IsNullOrEmpty(cam.Name)) {
                                Logger.Debug($"Adding QHY camera {i}: {cam.Id} (as {cam.Name})");
                                devices.Add(cam);
                            }
                        }
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                ///* Player One */
                //try {
                //    var provider = new PlayerOneProvider(profileService, exposureDataFactory);
                //    var playerOneCameras = provider.GetEquipment();
                //    Logger.Info($"Found {playerOneCameras?.Count} Player One Cameras");
                //    devices.AddRange(playerOneCameras);
                //} catch (Exception ex) {
                //    Logger.Error(ex);
                //}

                /* ToupTek */
                try {
                    var toupTekCameras = ToupTek.ToupCam.EnumV2();
                    Logger.Info($"Found {toupTekCameras?.Length} ToupTek Cameras");
                    foreach (var instance in toupTekCameras) {
                        var info = instance.ToDeviceInfo();
                        if(((ToupTekAlikeFlag)info.model.flag & ToupTekAlikeFlag.FLAG_FILTERWHEEL) > 0) { continue; }
                        if (((ToupTekAlikeFlag)info.model.flag & ToupTekAlikeFlag.FLAG_AUTOFOCUSER) > 0) { continue; }
                        var cam = new ToupTekAlikeCamera(info, new ToupTekSDKWrapper(), profileService, exposureDataFactory);
                        devices.Add(cam);
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                /* Ogma */
                try {
                    var ogmaCameras = Ogmacam.EnumV2();
                    Logger.Info($"Found {ogmaCameras?.Length} Ogma Cameras");
                    foreach (var instance in ogmaCameras) {
                        var info = instance.ToDeviceInfo();
                        if (((ToupTekAlikeFlag)info.model.flag & ToupTekAlikeFlag.FLAG_FILTERWHEEL) > 0) { continue; }
                        if (((ToupTekAlikeFlag)info.model.flag & ToupTekAlikeFlag.FLAG_AUTOFOCUSER) > 0) { continue; }
                        var cam = new ToupTekAlikeCamera(info, new OgmaSDKWrapper(), profileService, exposureDataFactory);
                        devices.Add(cam);
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                /* Omegon */
                try {
                    var omegonCameras = Omegon.Omegonprocam.EnumV2();
                    Logger.Info($"Found {omegonCameras?.Length} Omegon Cameras");
                    foreach (var instance in omegonCameras) {
                        var info = instance.ToDeviceInfo();
                        if (((ToupTekAlikeFlag)info.model.flag & ToupTekAlikeFlag.FLAG_FILTERWHEEL) > 0) { continue; }
                        if (((ToupTekAlikeFlag)info.model.flag & ToupTekAlikeFlag.FLAG_AUTOFOCUSER) > 0) { continue; }
                        var cam = new ToupTekAlikeCamera(info, new OmegonSDKWrapper(), profileService, exposureDataFactory);
                        devices.Add(cam);
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                /* Risingcam */
                try {
                    var risingCamCameras = Nncam.EnumV2();
                    Logger.Info($"Found {risingCamCameras?.Length} RisingCam Cameras");
                    foreach (var instance in risingCamCameras) {
                        var info = instance.ToDeviceInfo();
                        if (((ToupTekAlikeFlag)info.model.flag & ToupTekAlikeFlag.FLAG_FILTERWHEEL) > 0) { continue; }
                        if (((ToupTekAlikeFlag)info.model.flag & ToupTekAlikeFlag.FLAG_AUTOFOCUSER) > 0) { continue; }
                        var cam = new ToupTekAlikeCamera(info, new RisingcamSDKWrapper(), profileService, exposureDataFactory);
                        devices.Add(cam);
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                /* MallinCam */
                try {
                    var mallinCamCameras = MallinCam.Mallincam.EnumV2();
                    Logger.Info($"Found {mallinCamCameras?.Length} MallinCam Cameras");
                    foreach (var instance in mallinCamCameras) {
                        var info = instance.ToDeviceInfo();
                        if (((ToupTekAlikeFlag)info.model.flag & ToupTekAlikeFlag.FLAG_FILTERWHEEL) > 0) { continue; }
                        if (((ToupTekAlikeFlag)info.model.flag & ToupTekAlikeFlag.FLAG_AUTOFOCUSER) > 0) { continue; }
                        var cam = new ToupTekAlikeCamera(info, new MallinCamSDKWrapper(), profileService, exposureDataFactory);
                        devices.Add(cam);
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                ///* SVBony -- old sdk loaded via plugin loader */
                //try {
                //    var provider = new SVBonyProvider(profileService, exposureDataFactory);
                //    var svBonyCameras = provider.GetEquipment();
                //    Logger.Info($"Found {svBonyCameras?.Count} SVBony Cameras");
                //    devices.AddRange(svBonyCameras);
                //} catch (Exception ex) {
                //    Logger.Error(ex);
                //}

                /* SVBony - new touptek based sdk */
                try {
                    var svBonyCameras = Svbonycam.EnumV2();
                    Logger.Info($"Found {svBonyCameras?.Length} SVBony Cameras");
                    foreach (var instance in svBonyCameras) {
                        var info = instance.ToDeviceInfo();
                        if (((ToupTekAlikeFlag)info.model.flag & ToupTekAlikeFlag.FLAG_FILTERWHEEL) > 0) { continue; }
                        if (((ToupTekAlikeFlag)info.model.flag & ToupTekAlikeFlag.FLAG_AUTOFOCUSER) > 0) { continue; }
                        var cam = new ToupTekAlikeCamera(info, new SVBonySDKWrapper(), profileService, exposureDataFactory);
                        devices.Add(cam);
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                /* SBIG */
                try {
                    var provider = new SBIGCameraProvider(sbigSdk, profileService, exposureDataFactory);
                    var sbigCameras = provider.GetEquipment();
                    Logger.Info($"Found {sbigCameras?.Count} SBIG Cameras");
                    devices.AddRange(sbigCameras);
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                ///* ASTPAN */
                //try {
                //    var provider = new ASTPANProvider(profileService, exposureDataFactory);
                //    var astpanCameras = provider.GetEquipment();
                //    Logger.Info($"Found {astpanCameras?.Count} ASTPAN Cameras");
                //    devices.AddRange(astpanCameras );
                //} catch (Exception ex) {
                //    Logger.Error(ex);
                //}

                /* Plugin Providers */
                foreach (var provider in await equipmentProviders.GetProviders()) {
                    try {
                        var cameras = provider.GetEquipment();
                        Logger.Info($"Found {cameras?.Count} {provider.Name} Cameras");
                        devices.AddRange(cameras);
                    } catch (Exception ex) {
                        Logger.Error(ex);
                    }
                }

                /* ASCOM */
                try {
                    var ascomInteraction = new ASCOMInteraction(profileService);
                    var ascomCameras = ascomInteraction.GetCameras(exposureDataFactory);
                    foreach (ICamera cam in ascomCameras) {
                        devices.Add(cam);
                    }
                    Logger.Info($"Found {ascomCameras?.Count} ASCOM Cameras");
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                /* Alpaca */
                try {
                    var alpacaInteraction = new AlpacaInteraction(profileService);
                    var alpacaCameras = await alpacaInteraction.GetCameras(exposureDataFactory, default);
                    foreach (ICamera cam in alpacaCameras) {
                        devices.Add(cam);
                    }
                    Logger.Info($"Found {alpacaCameras?.Count} Alpaca Cameras");
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                /* CANON */
                try {
                    IntPtr cameraList;
                    uint err = EDSDK.EdsGetCameraList(out cameraList);
                    if (err == EDSDK.EDS_ERR_OK) {
                        int count;
                        err = EDSDK.EdsGetChildCount(cameraList, out count);

                        Logger.Info($"Found {count} Canon Cameras");
                        for (int i = 0; i < count; i++) {
                            IntPtr cam;
                            err = EDSDK.EdsGetChildAtIndex(cameraList, i, out cam);

                            EDSDK.EdsDeviceInfo info;
                            err = EDSDK.EdsGetDeviceInfo(cam, out info);

                            Logger.Trace(string.Format("Adding {0}", info.szDeviceDescription));
                            devices.Add(new EDCamera(cam, info, profileService, exposureDataFactory));
                        }
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                /* NIKON */
                if (!DllLoader.IsX86()) {
                    try {
                        devices.Add(new NikonCamera(profileService, telescopeMediator, exposureDataFactory));
                    } catch (Exception ex) {
                        Logger.Error(ex);
                    }
                }

                devices.Add(new FileCamera(profileService, telescopeMediator, imageDataFactory, exposureDataFactory));
                devices.Add(new SimulatorCamera(profileService, telescopeMediator, exposureDataFactory, imageDataFactory));

                DetermineSelectedDevice(devices, profileService.ActiveProfile.CameraSettings.Id, profileService.ActiveProfile.CameraSettings.LastDeviceName);

            } finally {
                lockObj.Release();
            }
        }        
    }
}