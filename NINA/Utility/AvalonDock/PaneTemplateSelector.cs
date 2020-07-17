#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.ViewModel;
using NINA.ViewModel.Equipment.Camera;
using NINA.ViewModel.Equipment.FilterWheel;
using NINA.ViewModel.Equipment.FlatDevice;
using NINA.ViewModel.Equipment.Focuser;
using NINA.ViewModel.Equipment.Guider;
using NINA.ViewModel.Equipment.Rotator;
using NINA.ViewModel.Equipment.Switch;
using NINA.ViewModel.Equipment.Telescope;
using NINA.ViewModel.Equipment.WeatherData;
using NINA.ViewModel.Imaging;
using System.Windows;
using System.Windows.Controls;
using Xceed.Wpf.AvalonDock.Layout;

namespace NINA.Utility.AvalonDock {

    public class PaneTemplateSelector : DataTemplateSelector {

        public PaneTemplateSelector() {
        }

        public DataTemplate CameraTemplate { get; set; }

        public DataTemplate TelescopeTemplate { get; set; }

        public DataTemplate ImageControlTemplate { get; set; }

        public DataTemplate PlatesolveTemplate { get; set; }

        public DataTemplate PolarAlignmentTemplate { get; set; }

        public DataTemplate GuiderTemplate { get; set; }

        public DataTemplate FilterWheelTemplate { get; set; }

        public DataTemplate ImagingTemplate { get; set; }

        public DataTemplate ImageHistoryTemplate { get; set; }

        public DataTemplate ImageStatisticsTemplate { get; set; }

        public DataTemplate RotatorTemplate { get; set; }

        public DataTemplate SequenceTemplate { get; set; }

        public DataTemplate WeatherDataTemplate { get; set; }

        public DataTemplate FocuserTemplate { get; set; }

        public DataTemplate AutoFocusTemplate { get; set; }

        public DataTemplate ThumbnailTemplate { get; set; }

        public DataTemplate FocusTargetsTemplate { get; set; }

        public DataTemplate SwitchTemplate { get; set; }
        public DataTemplate FlatDeviceTemplate { get; set; }
        public DataTemplate ExposureCalculatorTemplate { get; set; }

        public override System.Windows.DataTemplate SelectTemplate(object item, System.Windows.DependencyObject container) {
            var itemAsLayoutContent = item as LayoutContent;

            switch (item) {
                case CameraVM _:
                    return CameraTemplate;

                case TelescopeVM _:
                    return TelescopeTemplate;

                case AnchorablePlateSolverVM _:
                    return PlatesolveTemplate;

                case PolarAlignmentVM _:
                    return PolarAlignmentTemplate;

                case GuiderVM _:
                    return GuiderTemplate;

                case FilterWheelVM _:
                    return FilterWheelTemplate;

                case AnchorableSnapshotVM _:
                    return ImagingTemplate;

                case ImageHistoryVM _:
                    return ImageHistoryTemplate;

                case ImageStatisticsVM _:
                    return ImageStatisticsTemplate;

                case ImageControlVM _:
                    return ImageControlTemplate;

                case SequenceVM _:
                    return SequenceTemplate;

                case WeatherDataVM _:
                    return WeatherDataTemplate;

                case FocuserVM _:
                    return FocuserTemplate;

                case AutoFocusVM _:
                    return AutoFocusTemplate;

                case ThumbnailVM _:
                    return ThumbnailTemplate;

                case RotatorVM _:
                    return RotatorTemplate;

                case FocusTargetsVM _:
                    return FocusTargetsTemplate;

                case SwitchVM _:
                    return SwitchTemplate;

                case FlatDeviceVM _:
                    return FlatDeviceTemplate;

                case ExposureCalculatorVM _:
                    return ExposureCalculatorTemplate;

                default:
                    return base.SelectTemplate(item, container);
            }
        }
    }
}