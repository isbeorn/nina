#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FLI;
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using QHYCCD;
using System;
using System.Collections.Generic;
using ZWOptical.EFWSDK;
using NINA.Equipment.SDK.CameraSDKs.AtikSDK;
using NINA.Equipment.Utility;
using NINA.Core.Locale;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Equipment;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyCamera.ToupTekAlike;
using NINA.Equipment.SDK.CameraSDKs.SBIGSDK;
using System.Threading.Tasks;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Equipment.SDK.CameraSDKs.PlayerOneSDK;

namespace NINA.WPF.Base.ViewModel.Equipment.FilterWheel {

    public class FilterWheelChooserVM : DeviceChooserVM<IFilterWheel> {
        private readonly ISbigSdk sbigSdk;

        public FilterWheelChooserVM(ISbigSdk sbigSdk,
                                    IProfileService profileService,
                                    IEquipmentProviders<IFilterWheel> equipmentProviders) : base(profileService, equipmentProviders) {
            this.sbigSdk = sbigSdk;
        }

        public override async Task GetEquipment() {
            await lockObj.WaitAsync();
            try {
                var devices = new List<IDevice>();

                devices.Add(new DummyDevice(Loc.Instance["LblNoFilterwheel"]));

                /*
                 * FLI
                 */
                try {
                    Logger.Trace("Adding FLI filter wheels");
                    List<string> fwheels = FLIFilterWheels.GetFilterWheels();

                    if (fwheels.Count > 0) {
                        foreach (var entry in fwheels) {
                            var fwheel = new FLIFilterWheel(entry, profileService);

                            if (!string.IsNullOrEmpty(fwheel.Name)) {
                                Logger.Debug($"Adding FLI Filter Wheel {fwheel.Id} (as {fwheel.Name})");
                                devices.Add(fwheel);
                            }
                        }
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                // Atik EFW
                try {
                    Logger.Trace("Adding Atik EFW filter wheels");
                    for (int i = 0; i < 10; i++) {
                        if (AtikCameraDll.ArtemisEfwIsPresent(i)) {
                            var wheel = new AtikFilterWheel(i, profileService);
                            Logger.Debug($"Adding Atik Filter Wheel {i} as {wheel.Name}");
                            devices.Add(wheel);
                        }
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                // Atik internal Wheels
                try {
                    Logger.Trace("Adding Atik internal filter wheels");
                    var atikDevices = AtikCameraDll.GetDevicesCount();
                    Logger.Trace($"Cameras found: {atikDevices}");
                    for (int i = 0; i < atikDevices; i++) {
                        var wheel = new AtikInternalFilterWheel(i, profileService);
                        if (wheel.CameraHasInternalFilterWheel) {
                            Logger.Debug($"Adding Atik internal Filter Wheel {i} as {wheel.Name}");
                            devices.Add(wheel);
                        }
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                /*
                 * QHY - Integrated or 4-pin connected filter wheels only
                 */
                try {
                    var qhy = new QHYFilterWheels();
                    Logger.Trace("Adding QHY integrated/4-pin filter wheels");
                    List<string> fwheels = qhy.GetFilterWheels();

                    if (fwheels.Count > 0) {
                        foreach (var entry in fwheels) {
                            var fwheel = new QHYFilterWheel(entry, profileService);

                            if (!string.IsNullOrEmpty(fwheel.Name)) {
                                Logger.Debug($"Adding QHY Filter Wheel {fwheel.Id} (as {fwheel.Name})");
                                devices.Add(fwheel);
                            }
                        }
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                /* ZWO filter wheels */
                try {
                    Logger.Trace("Adding ZWOptical filter wheels");

                    var wheels = EFWdll.GetNum();

                    for (int i = 0; i < wheels; i++) {
                        var fw = new ASIFilterWheel(i, profileService);
                        Logger.Debug($"Adding ZWOptical Filter Wheel: {fw.Name}");
                        devices.Add(fw);
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                /* PlayerOne filter wheels */
                try {
                    Logger.Trace("Adding PlayerOne filter wheels");

                    var wheels = PlayerOneFilterWheelSDK.POAGetPWCount();

                    for (int i = 0; i < wheels; i++) {
                        var fw = new PlayerOneFilterWheel(i, profileService);
                        Logger.Debug($"Adding PlayerOne Filter Wheel {i})");
                        devices.Add(fw);
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                /* Altair */
                try {
                    var altairDevices = Altair.Altaircam.EnumV2();
                    Logger.Info($"Found {altairDevices?.Length} Altair Devices");
                    foreach (var instance in altairDevices) {
                        var info = instance.ToDeviceInfo();
                        if (((ToupTekAlikeFlag)info.model.flag & ToupTekAlikeFlag.FLAG_FILTERWHEEL) > 0) {
                            var wheel = new ToupTekAlikeFilterWheel(info, new AltairSDKWrapper(), profileService);
                            devices.Add(wheel);
                        }
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                /* ToupTek */
                try {
                    var toupTekDevices = ToupTek.ToupCam.EnumV2();
                    Logger.Info($"Found {toupTekDevices?.Length} ToupTek Devices");
                    foreach (var instance in toupTekDevices) {
                        var info = instance.ToDeviceInfo();
                        if (((ToupTekAlikeFlag)info.model.flag & ToupTekAlikeFlag.FLAG_FILTERWHEEL) > 0) {
                            var wheel = new ToupTekAlikeFilterWheel(info, new ToupTekSDKWrapper(), profileService);
                            devices.Add(wheel);
                        }
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                /* Ogma */
                try {
                    var ogmaDevices = Ogmacam.EnumV2();
                    Logger.Info($"Found {ogmaDevices?.Length} Ogma Devices");
                    foreach (var instance in ogmaDevices) {
                        var info = instance.ToDeviceInfo();
                        if (((ToupTekAlikeFlag)info.model.flag & ToupTekAlikeFlag.FLAG_FILTERWHEEL) > 0) {
                            var wheel = new ToupTekAlikeFilterWheel(info, new OgmaSDKWrapper(), profileService);
                            devices.Add(wheel);
                        }
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                /* Omegon */
                try {
                    var omegonDevices = Omegon.Omegonprocam.EnumV2();
                    Logger.Info($"Found {omegonDevices?.Length} Omegon Devices");
                    foreach (var instance in omegonDevices) {
                        var info = instance.ToDeviceInfo();
                        if (((ToupTekAlikeFlag)info.model.flag & ToupTekAlikeFlag.FLAG_FILTERWHEEL) > 0) {
                            var wheel = new ToupTekAlikeFilterWheel(info, new OmegonSDKWrapper(), profileService);
                            devices.Add(wheel);
                        }
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                /* Risingcam */
                try {
                    var risingCamDevices = Nncam.EnumV2();
                    Logger.Info($"Found {risingCamDevices?.Length} RisingCam Devices");
                    foreach (var instance in risingCamDevices) {
                        var info = instance.ToDeviceInfo();
                        if (((ToupTekAlikeFlag)info.model.flag & ToupTekAlikeFlag.FLAG_FILTERWHEEL) > 0) {
                            var wheel = new ToupTekAlikeFilterWheel(info, new RisingcamSDKWrapper(), profileService);
                            devices.Add(wheel);
                        }
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                /* MallinCam */
                try {
                    var mallinCamDevices = MallinCam.Mallincam.EnumV2();
                    Logger.Info($"Found {mallinCamDevices?.Length} MallinCam Devices");
                    foreach (var instance in mallinCamDevices) {
                        var info = instance.ToDeviceInfo();
                        if (((ToupTekAlikeFlag)info.model.flag & ToupTekAlikeFlag.FLAG_FILTERWHEEL) > 0) {
                            var wheel = new ToupTekAlikeFilterWheel(info, new MallinCamSDKWrapper(), profileService);
                            devices.Add(wheel);
                        }
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                /* SVBony */
                try {
                    var svBonyDevices = Svbonycam.EnumV2();
                    Logger.Info($"Found {svBonyDevices?.Length} SVBony Devices");
                    foreach (var instance in svBonyDevices) {
                        var info = instance.ToDeviceInfo();
                        if (((ToupTekAlikeFlag)info.model.flag & ToupTekAlikeFlag.FLAG_FILTERWHEEL) > 0) {
                            var wheel = new ToupTekAlikeFilterWheel(info, new SVBonySDKWrapper(), profileService);
                            devices.Add(wheel);
                        }
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                /* SBIG filter wheels */
                try {
                    var provider = new SBIGFilterWheelProvider(sbigSdk, profileService);
                    devices.AddRange(provider.GetEquipment());
                } catch (Exception ex) {
                    Logger.Error(ex);
                }
                
                /* Plugin Providers */
                foreach (var provider in await equipmentProviders.GetProviders()) {
                    try {
                        var cameras = provider.GetEquipment();
                        Logger.Info($"Found {cameras?.Count} {provider.Name} Filter Wheels");
                        devices.AddRange(cameras);
                    } catch (Exception ex) {
                        Logger.Error(ex);
                    }
                }

                /*
                 * ASCOM devices
                 */
                try {
                    var ascomInteraction = new ASCOMInteraction(profileService);
                    foreach (IFilterWheel fw in ascomInteraction.GetFilterWheels()) {
                        devices.Add(fw);
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                /* Alpaca */
                try {
                    var alpacaInteraction = new AlpacaInteraction(profileService);
                    var alpacaFilterWheels = await alpacaInteraction.GetFilterWheels(default);
                    foreach (IFilterWheel fw in alpacaFilterWheels) {
                        devices.Add(fw);
                    }
                    Logger.Info($"Found {alpacaFilterWheels?.Count} Alpaca Filter Wheels");
                } catch (Exception ex) {
                    Logger.Error(ex);
                }

                devices.Add(new ManualFilterWheel(this.profileService));

                DetermineSelectedDevice(devices, profileService.ActiveProfile.FilterWheelSettings.Id, profileService.ActiveProfile.FilterWheelSettings.LastDeviceName);

            } finally {
                lockObj.Release();
            }
        }
    }
}