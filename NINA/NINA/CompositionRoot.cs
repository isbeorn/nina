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
using NINA.Core.Interfaces;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Image.ImageAnalysis;
using NINA.Interfaces;
using NINA.Profile.Interfaces;
using NINA.Utility;
using NINA.ViewModel;
using NINA.ViewModel.FlatWizard;
using NINA.ViewModel.FramingAssistant;
using NINA.ViewModel.ImageHistory;
using NINA.ViewModel.Interfaces;
using NINA.ViewModel.Sequencer;
using NINA.WPF.Base.Interfaces.ViewModel;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace NINA {

    internal static class CompositionRoot {

        public static IMainWindowVM Compose(IProfileService profileService, ICommandLineOptions commandLineOptions) {
            try {
                var serviceProvider = new IoCBindings(profileService, commandLineOptions).Load();
                Stopwatch sw;

                sw = Stopwatch.StartNew();
                var imageSaveController = serviceProvider.GetService<IImageSaveController>();
                Debug.Print($"Time to create IImageSaveController {sw.Elapsed}");

                sw = Stopwatch.StartNew();
                var imagingVM = serviceProvider.GetService<IImagingVM>();
                Debug.Print($"Time to create IImagingVM {sw.Elapsed}");

                sw = Stopwatch.StartNew();
                var appvm = serviceProvider.GetService<IApplicationVM>();
                Debug.Print($"Time to create IApplicationVM {sw.Elapsed}");

                sw = Stopwatch.StartNew();
                try {
                    EDSDKLib.EDSDKLocal.Initialize();
                } catch (Exception ex) {
                    Logger.Error(ex);
                }
                Debug.Print($"Time to initialize EDSDK {sw.Elapsed}");


                sw = Stopwatch.StartNew();
                var equipmentVM = serviceProvider.GetService<IEquipmentVM>();
                Debug.Print($"Time to create IEquipmentVM {sw.Elapsed}");

                sw = Stopwatch.StartNew();
                var skyAtlasVM = serviceProvider.GetService<ISkyAtlasVM>();
                Debug.Print($"Time to create ISkyAtlasVM {sw.Elapsed}");

                sw = Stopwatch.StartNew();
                var sequenceNavigationVM = serviceProvider.GetService<ISequenceNavigationVM>();
                Debug.Print($"Time to create ISequenceNavigationVM {sw.Elapsed}");

                sw = Stopwatch.StartNew();
                var framingAssistantVM = serviceProvider.GetService<IFramingAssistantVM>();
                Debug.Print($"Time to create IFramingAssistantVM {sw.Elapsed}");

                sw = Stopwatch.StartNew();
                var flatWizardVM = serviceProvider.GetService<IFlatWizardVM>();
                Debug.Print($"Time to create IFlatWizardVM {sw.Elapsed}");

                sw = Stopwatch.StartNew();
                var dockManagerVM = serviceProvider.GetService<IDockManagerVM>();
                Debug.Print($"Time to create IDockManagerVM {sw.Elapsed}");

                sw = Stopwatch.StartNew();
                var optionsVM = serviceProvider.GetService<IOptionsVM>();
                Debug.Print($"Time to create IOptionsVM {sw.Elapsed}");

                sw = Stopwatch.StartNew();
                var applicationDeviceConnectionVM = serviceProvider.GetService<IApplicationDeviceConnectionVM>();
                Debug.Print($"Time to create IApplicationDeviceConnectionVM {sw.Elapsed}");

                sw = Stopwatch.StartNew();
                var versionCheckVM = serviceProvider.GetService<IVersionCheckVM>();
                Debug.Print($"Time to create IVersionCheckVM {sw.Elapsed}");

                sw = Stopwatch.StartNew();
                var applicationStatusVM = serviceProvider.GetService<IApplicationStatusVM>();
                Debug.Print($"Time to create IApplicationStatusVM {sw.Elapsed}");

                sw = Stopwatch.StartNew();
                var imageHistoryVM = serviceProvider.GetService<IImageHistoryVM>();
                Debug.Print($"Time to create IImageHistoryVM {sw.Elapsed}");

                sw = Stopwatch.StartNew();
                var pluginsVM = serviceProvider.GetService<IPluginsVM>();
                Debug.Print($"Time to create IPluginsVM {sw.Elapsed}");

                sw = Stopwatch.StartNew();
                var globalObjects = serviceProvider.GetService<GlobalObjects>();
                Debug.Print($"Time to create GlobalObjects {sw.Elapsed}");

                sw = Stopwatch.StartNew();
                var mainWindowVM = new MainWindowVM {
                    AppVM = appvm,
                    ImageSaveController = imageSaveController,
                    ImagingVM = imagingVM,
                    EquipmentVM = equipmentVM,
                    SkyAtlasVM = skyAtlasVM,
                    SequenceNavigationVM = sequenceNavigationVM,
                    FramingAssistantVM = framingAssistantVM,
                    FlatWizardVM = flatWizardVM,
                    DockManagerVM = dockManagerVM,

                    OptionsVM = optionsVM,
                    ApplicationDeviceConnectionVM = applicationDeviceConnectionVM,
                    VersionCheckVM = versionCheckVM,
                    ApplicationStatusVM = applicationStatusVM,

                    ImageHistoryVM = imageHistoryVM,
                    PluginsVM = pluginsVM,
                    GlobalObjects = globalObjects,
                };
                Debug.Print($"Time to create MainWindowVM {sw.Elapsed}");

                return mainWindowVM;
            } catch (Exception ex) {
                Logger.Error(ex);
                throw;
            }
        }
    }
}