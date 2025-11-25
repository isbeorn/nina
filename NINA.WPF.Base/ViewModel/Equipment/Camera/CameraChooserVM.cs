#region "copyright"

/*
    Copyright © 2016 - 2025 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyCamera.ToupTekAlike;
using NINA.Profile.Interfaces;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using ZWOptical.ASISDK;
using NINA.Equipment.Utility;
using NINA.Core.Locale;
using NINA.Equipment.Equipment;
using NINA.Equipment.Interfaces;
using NINA.Image.Interfaces;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Core.Interfaces;
using System.Threading.Tasks;

namespace NINA.WPF.Base.ViewModel.Equipment.Camera {

    public class CameraChooserVM : DeviceChooserVM<ICamera> {
        private readonly ITelescopeMediator telescopeMediator;
        private readonly IExposureDataFactory exposureDataFactory;
        private readonly IImageDataFactory imageDataFactory;

        public CameraChooserVM(IProfileService profileService,
                               ITelescopeMediator telescopeMediator,
                               IExposureDataFactory exposureDataFactory,
                               IImageDataFactory imageDataFactory,
                               IEquipmentProviders<ICamera> equipmentProviders) : base(profileService, equipmentProviders) {
            this.telescopeMediator = telescopeMediator;
            this.exposureDataFactory = exposureDataFactory;
            this.imageDataFactory = imageDataFactory;
        }

        public override async Task GetEquipment() {
            await lockObj.WaitAsync();
            try {

                var devices = new List<IDevice>();

                devices.Add(new DummyDevice(Loc.Instance["LblNoCamera"]));

                /* Altair */
                try {
                    var altairCameras = Altair.Altaircam.EnumV2();
                    Logger.Info($"Found {altairCameras?.Length} Altair Cameras");
                    foreach (var instance in altairCameras) {
                        var info = instance.ToDeviceInfo();
                        if (((ToupTekAlikeFlag)info.model.flag & ToupTekAlikeFlag.FLAG_FILTERWHEEL) > 0) { continue; }
                        if (((ToupTekAlikeFlag)info.model.flag & ToupTekAlikeFlag.FLAG_AUTOFOCUSER) > 0) { continue; }
                        var cam = new ToupTekAlikeCamera(instance.ToDeviceInfo(), new AltairSDKWrapper(), profileService, exposureDataFactory);
                        devices.Add(cam);
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

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

                /* INDIGO camera */
                /*                try {
                                    var indigoInteraction = new INDIGOInteraction(profileService);
                                    var indigoCameras = indigoInteraction.GetCameras(exposureDataFactory);
                                    devices.AddRange(indigoCameras);
                                    Logger.Info($"Found {indigoCameras?.Count} INDIGO Cameras");
                                } catch (Exception ex) {
                                    Logger.Error(ex);
                                }
                */
                //                devices.Add(new FileCamera(profileService, telescopeMediator, imageDataFactory, exposureDataFactory));
                devices.Add(new SimulatorCamera(profileService, imageDataFactory, exposureDataFactory));

                DetermineSelectedDevice(devices, profileService.ActiveProfile.CameraSettings.Id, profileService.ActiveProfile.CameraSettings.LastDeviceName);

            } finally {
                lockObj.Release();
            }
        }        
    }
}
