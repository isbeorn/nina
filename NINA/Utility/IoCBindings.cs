#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Microsoft.Extensions.DependencyInjection;
using NINA.Astrometry;
using NINA.Astrometry.Interfaces;
using NINA.Core.Interfaces;
using NINA.Core.Interfaces.Utility;
using NINA.Core.Model;
using NINA.Core.MyMessageBox;
using NINA.Core.Utility;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Equipment;
using NINA.Equipment.Equipment.MyDome;
using NINA.Equipment.Equipment.MyGPS;
using NINA.Equipment.Equipment.MyPlanetarium;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Equipment.SDK.CameraSDKs.SBIGSDK;
using NINA.Equipment.Utility;
using NINA.Image.ImageAnalysis;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using NINA.Imaging.ViewModel.Imaging;
using NINA.Interfaces;
using NINA.PlateSolving;
using NINA.PlateSolving.Interfaces;
using NINA.Plugin;
using NINA.Plugin.Interfaces;
using NINA.Plugin.Messaging;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.Sequencer.Mediator;
using NINA.ViewModel;
using NINA.ViewModel.FlatWizard;
using NINA.ViewModel.FramingAssistant;
using NINA.ViewModel.ImageHistory;
using NINA.ViewModel.Imaging;
using NINA.ViewModel.Interfaces;
using NINA.ViewModel.Plugins;
using NINA.ViewModel.Sequencer;
using NINA.WPF.Base.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.Utility;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.Mediator;
using NINA.WPF.Base.Utility;
using NINA.WPF.Base.ViewModel;
using NINA.WPF.Base.ViewModel.Equipment.Camera;
using NINA.WPF.Base.ViewModel.Equipment.Dome;
using NINA.WPF.Base.ViewModel.Equipment.FilterWheel;
using NINA.WPF.Base.ViewModel.Equipment.FlatDevice;
using NINA.WPF.Base.ViewModel.Equipment.Focuser;
using NINA.WPF.Base.ViewModel.Equipment.Guider;
using NINA.WPF.Base.ViewModel.Equipment.Rotator;
using NINA.WPF.Base.ViewModel.Equipment.SafetyMonitor;
using NINA.WPF.Base.ViewModel.Equipment.Switch;
using NINA.WPF.Base.ViewModel.Equipment.Telescope;
using NINA.WPF.Base.ViewModel.Equipment.WeatherData;
using System;

namespace NINA.Utility {

    internal class IoCBindings {
        private readonly IProfileService _profileService;
        private readonly ICommandLineOptions _commandLineArguments;

        public IoCBindings(IProfileService profileService, ICommandLineOptions commandLineOptions) {
            _profileService = profileService;
            _commandLineArguments = commandLineOptions;
        }

        public IServiceProvider Load() {
            try {
                var services = new ServiceCollection();

                services.AddSingleton<ProjectVersion>(f => new ProjectVersion(NINA.Core.Utility.CoreUtil.Version));

                services.AddSingleton<IProfileService>(f => _profileService);
                services.AddSingleton<IProfile>(f => f.GetService<ProfileService>().ActiveProfile);

                services.AddSingleton<IApplicationVM, ApplicationVM>();

                services.AddSingleton<ICommandLineOptions>(f => _commandLineArguments);

                services.AddSingleton<IMessageBroker, MessageBroker>();

                services.AddTransient<IUsbDeviceWatcher, UsbDeviceWatcher>();

                // Equipment Providers
                services.AddScoped<IEquipmentProviders<ICamera>, PluginEquipmentProviders<ICamera>>();
                services.AddScoped<IEquipmentProviders<IFilterWheel>, PluginEquipmentProviders<IFilterWheel>>();
                services.AddScoped<IEquipmentProviders<IFocuser>, PluginEquipmentProviders<IFocuser>>();
                services.AddScoped<IEquipmentProviders<IRotator>, PluginEquipmentProviders<IRotator>>();
                services.AddScoped<IEquipmentProviders<ITelescope>, PluginEquipmentProviders<ITelescope>>();
                services.AddScoped<IEquipmentProviders<IGuider>, PluginEquipmentProviders<IGuider>>();
                services.AddScoped<IEquipmentProviders<ISwitchHub>, PluginEquipmentProviders<ISwitchHub>>();
                services.AddScoped<IEquipmentProviders<IFlatDevice>, PluginEquipmentProviders<IFlatDevice>>();
                services.AddScoped<IEquipmentProviders<IWeatherData>, PluginEquipmentProviders<IWeatherData>>();
                services.AddScoped<IEquipmentProviders<IDome>, PluginEquipmentProviders<IDome>>();
                services.AddScoped<IEquipmentProviders<ISafetyMonitor>, PluginEquipmentProviders<ISafetyMonitor>>();
                services.AddSingleton<IEquipmentProviders[]>(f =>
                    new IEquipmentProviders[] {
                        f.GetService<IEquipmentProviders<ICamera>>(),
                        f.GetService<IEquipmentProviders<IFilterWheel>>(),
                        f.GetService<IEquipmentProviders<IFocuser>>(),
                        f.GetService<IEquipmentProviders<IRotator>>(),
                        f.GetService<IEquipmentProviders<ITelescope>>(),
                        f.GetService<IEquipmentProviders<IGuider>>(),
                        f.GetService<IEquipmentProviders<ISwitchHub>>(),
                        f.GetService<IEquipmentProviders<IFlatDevice>>(),
                        f.GetService<IEquipmentProviders<IWeatherData>>(),
                        f.GetService<IEquipmentProviders<IDome>>(),
                        f.GetService<IEquipmentProviders<ISafetyMonitor>>(),
                    }
                );
                services.AddSingleton<IPluginEquipmentProviderManager, PluginEquipmentProviderManager>();

                // Device Chooser Instances
                services.AddSingleton<CameraChooserVM>();
                services.AddSingleton<FilterWheelChooserVM>();
                services.AddSingleton<FocuserChooserVM>();
                services.AddSingleton<RotatorChooserVM>();
                services.AddSingleton<TelescopeChooserVM>();
                services.AddSingleton<GuiderChooserVM>();
                services.AddSingleton<SwitchChooserVM>();
                services.AddSingleton<FlatDeviceChooserVM>();
                services.AddSingleton<WeatherDataChooserVM>();
                services.AddSingleton<DomeChooserVM>();
                services.AddSingleton<SafetyMonitorChooserVM>();

                // Equipment Viewmodel creation
                services.AddSingleton<ICameraVM, CameraVM>(f =>
                    new CameraVM(f.GetService<IProfileService>(),
                                 f.GetService<ICameraMediator>(),
                                 f.GetService<IApplicationStatusMediator>(),
                                 f.GetService<CameraChooserVM>()));

                services.AddSingleton<IFilterWheelVM, FilterWheelVM>(f =>
                    new FilterWheelVM(f.GetService<IProfileService>(),
                                      f.GetService<IFilterWheelMediator>(),
                                      f.GetService<IFocuserMediator>(),
                                      f.GetService<IGuiderMediator>(),
                                      f.GetService<FilterWheelChooserVM>(),
                                      f.GetService<IApplicationStatusMediator>()));

                services.AddSingleton<IFocuserVM, FocuserVM>(f =>
                    new FocuserVM(f.GetService<IProfileService>(),
                                      f.GetService<IFocuserMediator>(),
                                      f.GetService<IApplicationStatusMediator>(),
                                      f.GetService<FocuserChooserVM>(),
                                      f.GetService<IImageGeometryProvider>()));

                services.AddSingleton<IRotatorVM, RotatorVM>(f =>
                    new RotatorVM(f.GetService<IProfileService>(),
                                      f.GetService<IRotatorMediator>(),
                                      f.GetService<RotatorChooserVM>(),
                                      f.GetService<IApplicationResourceDictionary>(),
                                      f.GetService<IApplicationStatusMediator>()));

                services.AddSingleton<ITelescopeVM, TelescopeVM>(f =>
                    new TelescopeVM(f.GetService<IProfileService>(),
                                      f.GetService<ITelescopeMediator>(),
                                      f.GetService<IApplicationStatusMediator>(),
                                      f.GetService<IDomeMediator>(),
                                      f.GetService<TelescopeChooserVM>()));

                services.AddSingleton<IGuiderVM, GuiderVM>(f =>
                    new GuiderVM(f.GetService<IProfileService>(),
                                      f.GetService<IGuiderMediator>(),
                                      f.GetService<IApplicationStatusMediator>(),
                                      f.GetService<GuiderChooserVM>()));

                services.AddSingleton<ISwitchVM, SwitchVM>(f =>
                    new SwitchVM(f.GetService<IProfileService>(),
                                      f.GetService<IApplicationStatusMediator>(),
                                      f.GetService<ISwitchMediator>(),
                                      f.GetService<SwitchChooserVM>()));

                services.AddSingleton<IFlatDeviceVM, FlatDeviceVM>(f =>
                    new FlatDeviceVM(f.GetService<IProfileService>(),
                                      f.GetService<IFlatDeviceMediator>(),
                                      f.GetService<IApplicationStatusMediator>(),
                                      f.GetService<ICameraMediator>(),
                                      f.GetService<FlatDeviceChooserVM>(),
                                      f.GetService<IImageGeometryProvider>()));

                services.AddSingleton<IWeatherDataVM, WeatherDataVM>(f =>
                    new WeatherDataVM(f.GetService<IProfileService>(),
                                      f.GetService<IWeatherDataMediator>(),
                                      f.GetService<IApplicationStatusMediator>(),
                                      f.GetService<WeatherDataChooserVM>()));

                services.AddSingleton<IDomeVM, DomeVM>(f =>
                    new DomeVM(f.GetService<IProfileService>(),
                                      f.GetService<IDomeMediator>(),
                                      f.GetService<IApplicationStatusMediator>(),
                                      f.GetService<ITelescopeMediator>(),
                                      f.GetService<DomeChooserVM>(),
                                      f.GetService<IDomeFollower>(),
                                      f.GetService<ISafetyMonitorMediator>(),
                                      f.GetService<IApplicationResourceDictionary>(),
                                      f.GetService<IDeviceUpdateTimerFactory>()));

                services.AddSingleton<ISafetyMonitorVM, SafetyMonitorVM>(f =>
                    new SafetyMonitorVM(f.GetService<IProfileService>(),
                                      f.GetService<ISafetyMonitorMediator>(),
                                      f.GetService<IApplicationStatusMediator>(),
                                      f.GetService<SafetyMonitorChooserVM>()));

                // Equipment Mediators
                services.AddSingleton<ICameraMediator, CameraMediator>();
                services.AddSingleton<IFilterWheelMediator, FilterWheelMediator>();
                services.AddSingleton<IFocuserMediator, FocuserMediator>();
                services.AddSingleton<IRotatorMediator, RotatorMediator>();
                services.AddSingleton<ITelescopeMediator, TelescopeMediator>();
                services.AddSingleton<IGuiderMediator, GuiderMediator>();
                services.AddSingleton<ISwitchMediator, SwitchMediator>();
                services.AddSingleton<IFlatDeviceMediator, FlatDeviceMediator>();
                services.AddSingleton<IWeatherDataMediator, WeatherDataMediator>();
                services.AddSingleton<IDomeMediator, DomeMediator>();
                services.AddSingleton<ISafetyMonitorMediator, SafetyMonitorMediator>();

                // General Viewmodel creation
                services.AddSingleton<IImagingVM, ImagingVM>();
                services.AddSingleton<IEquipmentVM, EquipmentVM>();
                services.AddSingleton<IApplicationDeviceConnectionVM, ApplicationDeviceConnectionVM>();
                services.AddSingleton<IImageGeometryProvider, ImageGeometryProvider>();
                services.AddSingleton<IOptionsVM, OptionsVM>();
                services.AddSingleton<ISkyAtlasVM, SkyAtlasVM>();
                services.AddSingleton<IFramingAssistantVM, FramingAssistantVM>();
                services.AddSingleton<IFocusTargetsVM, FocusTargetsVM>();
                services.AddSingleton<IAutoFocusToolVM, AutoFocusToolVM>();
                services.AddSingleton<IThumbnailVM, ThumbnailVM>();
                services.AddSingleton<IDockManagerVM, DockManagerVM>();
                services.AddSingleton<IApplicationStatusVM, ApplicationStatusVM>();
                services.AddSingleton<IVersionCheckVM, VersionCheckVM>();
                services.AddSingleton<IImageControlVM, ImageControlVM>();
                services.AddSingleton<IImageHistoryVM, ImageHistoryVM>();
                services.AddSingleton<IImageStatisticsVM, ImageStatisticsVM>();

                services.AddSingleton<ITwilightCalculator, TwilightCalculator>();
                services.AddSingleton<IMicroCacheFactory, DefaultMicroCacheFactory>();
                services.AddSingleton<ISbigSdk, SbigSdk>();

                // Pluggable Instances
                services.AddSingleton<StarDetection>();
                services.AddSingleton<StarAnnotator>();
                services.AddSingleton<BuiltInAutoFocusVMFactory>();
                services.AddSingleton<IPluggableBehaviorSelector<IStarDetection>, PluggableBehaviorSelector<IStarDetection, StarDetection>>();
                services.AddSingleton<IPluggableBehaviorSelector<IStarAnnotator>, PluggableBehaviorSelector<IStarAnnotator, StarAnnotator>>();
                services.AddSingleton<IPluggableBehaviorSelector<IAutoFocusVMFactory>, PluggableBehaviorSelector<IAutoFocusVMFactory, BuiltInAutoFocusVMFactory>>();
                services.AddSingleton<IPluggableBehaviorManager, PluggableBehaviorManager>();
                services.AddSingleton<IPluggableBehaviorSelector[]>(f =>
                    new IPluggableBehaviorSelector[] {
                        f.GetService<IPluggableBehaviorSelector<IStarDetection>>(),
                        f.GetService<IPluggableBehaviorSelector<IStarAnnotator>>(),
                        f.GetService<IPluggableBehaviorSelector<IAutoFocusVMFactory>>()
                    });
                services.AddSingleton<GlobalObjects>();

                services.AddSingleton<IImageDataFactory, ImageDataFactory>();
                services.AddSingleton<IExposureDataFactory, ExposureDataFactory>();
                services.AddSingleton<IAutoFocusVMFactory, PluggableAutoFocusVMFactory>();
                services.AddSingleton<IMeridianFlipVMFactory, MeridianFlipVMFactory>();

                services.AddSingleton<IAnchorablePlateSolverVM, AnchorablePlateSolverVM>();
                services.AddSingleton<IAnchorableSnapshotVM, AnchorableSnapshotVM>();

                services.AddSingleton<IImagingMediator, ImagingMediator>();
                services.AddSingleton<IApplicationStatusMediator, ApplicationStatusMediator>();
                services.AddSingleton<IApplicationMediator, ApplicationMediator>();
                services.AddSingleton<ISequenceMediator, SequenceMediator>();

                services.AddSingleton<IWindowServiceFactory, WindowServiceFactory>();
                services.AddSingleton<IPlanetariumFactory, PlanetariumFactory>();
                services.AddSingleton<IAllDeviceConsumer, AllDeviceConsumer>();
                services.AddSingleton<IGnssFactory, GnssFactory>();

                services.AddSingleton<IApplicationResourceDictionary, ApplicationResourceDictionary>();
                services.AddSingleton<INighttimeCalculator, NighttimeCalculator>();
                services.AddSingleton<IDeepSkyObjectSearchVM, DeepSkyObjectSearchVM>();
                services.AddSingleton<IDomeSynchronization, DomeSynchronization>();
                services.AddSingleton<IDomeFollower, DomeFollower>();
                services.AddSingleton<IDeviceUpdateTimerFactory, DefaultDeviceUpateTimerFactory>();

                if (DllLoader.IsX86()) {
                    services.AddSingleton<IImageSaveMediator, ImageSaveMediatorX86>();
                } else {
                    services.AddSingleton<IImageSaveMediator, ImageSaveMediator>();
                }

                services.AddSingleton<IFlatWizardVM>(f => new FlatWizardVM(f.GetService<IProfileService>(),
                    new ImagingVM(f.GetService<IProfileService>(),
                                  new ImagingMediator(),
                                  f.GetService<ICameraMediator>(),
                                  f.GetService<ITelescopeMediator>(),
                                  f.GetService<IFilterWheelMediator>(),
                                  f.GetService<IFocuserMediator>(),
                                  f.GetService<IRotatorMediator>(),
                                  f.GetService<IGuiderMediator>(),
                                  f.GetService<IWeatherDataMediator>(),
                                  f.GetService<IApplicationStatusMediator>(),
                                  new ImageControlVM(f.GetService<IProfileService>(),
                                                     f.GetService<ICameraMediator>(),
                                                     f.GetService<ITelescopeMediator>(),
                                                     f.GetService<IImagingMediator>(),
                                                     f.GetService<IApplicationStatusMediator>()),
                                  new ImageStatisticsVM(f.GetService<IProfileService>()),
                                  f.GetService<IImageHistoryVM>()),
                    f.GetService<ICameraMediator>(), f.GetService<IFilterWheelMediator>(), f.GetService<ITelescopeMediator>(),
                    f.GetService<IFlatDeviceMediator>(), f.GetService<IImageGeometryProvider>(), f.GetService<IApplicationStatusMediator>(), f.GetService<IMyMessageBoxVM>(),
                    f.GetService<INighttimeCalculator>(),
                    f.GetService<ITwilightCalculator>(),
                    f.GetService<IImageSaveMediator>()));

                services.AddSingleton<IImageSaveController, ImageSaveController>();
                services.AddSingleton<ISequenceNavigationVM, SequenceNavigationVM>();
                services.AddTransient<IMyMessageBoxVM, MyMessageBoxVM>();
                services.AddSingleton<IPlateSolverFactory, PlateSolverFactoryProxy>();
                services.AddSingleton<IPluginLoader, PluginLoader>();
                services.AddSingleton<IPluginsVM, PluginsVM>();

                return services.BuildServiceProvider();
            } catch (Exception e) {
                Logger.Error(e);
                throw;
            }
        }
    }
}