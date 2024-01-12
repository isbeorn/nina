#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Utility;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace NINA.View {

    /// <summary>
    /// Interaction logic for CameraControlView.xaml
    /// </summary>
    public partial class CameraControlView : UserControl {

        public CameraControlView() {
            InitializeComponent();
            LayoutRoot.DataContext = this;
        }

        public static readonly DependencyProperty MyCommandProperty =
            DependencyProperty.Register(nameof(MyCommand), typeof(ICommand), typeof(CameraControlView), new UIPropertyMetadata(null));

        public ICommand MyCommand {
            get => (ICommand)GetValue(MyCommandProperty);
            set => SetValue(MyCommandProperty, value);
        }

        public static readonly DependencyProperty MyCancelCommandProperty =
           DependencyProperty.Register(nameof(MyCancelCommand), typeof(ICommand), typeof(CameraControlView), new UIPropertyMetadata(null));

        public ICommand MyCancelCommand {
            get => (ICommand)GetValue(MyCancelCommandProperty);
            set => SetValue(MyCancelCommandProperty, value);
        }

        public static readonly DependencyProperty MyOrientationProperty =
          DependencyProperty.Register(nameof(MyOrientation), typeof(Orientation), typeof(CameraControlView), new UIPropertyMetadata(Orientation.Horizontal));

        public Orientation MyOrientation {
            get => (Orientation)GetValue(MyOrientationProperty);
            set => SetValue(MyOrientationProperty, value);
        }

        public static readonly DependencyProperty MyButtonImageProperty =
           DependencyProperty.Register(nameof(MyButtonImage), typeof(Geometry), typeof(CameraControlView), new UIPropertyMetadata(null));

        public Geometry MyButtonImage {
            get => (Geometry)GetValue(MyButtonImageProperty);
            set => SetValue(MyButtonImageProperty, value);
        }

        public static readonly DependencyProperty MyCancelButtonImageProperty =
           DependencyProperty.Register(nameof(MyCancelButtonImage), typeof(Geometry), typeof(CameraControlView), new UIPropertyMetadata(null));

        public Geometry MyCancelButtonImage {
            get => (Geometry)GetValue(MyCancelButtonImageProperty);
            set => SetValue(MyCancelButtonImageProperty, value);
        }

        public static readonly DependencyProperty MyButtonTextProperty =
            DependencyProperty.Register(nameof(MyButtonText), typeof(string), typeof(CameraControlView), new UIPropertyMetadata(null));

        public string MyButtonText {
            get => (string)GetValue(MyButtonTextProperty);
            set => SetValue(MyButtonTextProperty, value);
        }

        public static readonly DependencyProperty MyExposureDurationProperty =
            DependencyProperty.Register(nameof(MyExposureDuration), typeof(double), typeof(CameraControlView), new UIPropertyMetadata(null));

        public double MyExposureDuration {
            get => (double)GetValue(MyExposureDurationProperty);
            set => SetValue(MyExposureDurationProperty, value);
        }

        public static readonly DependencyProperty MyFiltersProperty =
            DependencyProperty.Register(nameof(MyFilters), typeof(ObservableCollection<FilterInfo>), typeof(CameraControlView), new UIPropertyMetadata(null));

        public IEnumerable<FilterInfo> MyFilters {
            get => (IEnumerable<FilterInfo>)GetValue(MyFiltersProperty);
            set => SetValue(MyFiltersProperty, value);
        }

        public static readonly DependencyProperty MySelectedFilterProperty =
            DependencyProperty.Register(nameof(MySelectedFilter), typeof(FilterInfo), typeof(CameraControlView), new UIPropertyMetadata(null));

        public FilterInfo MySelectedFilter {
            get => (FilterInfo)GetValue(MySelectedFilterProperty);
            set => SetValue(MySelectedFilterProperty, value);
        }

        public static readonly DependencyProperty MyBinningModesProperty =
            DependencyProperty.Register(nameof(MyBinningModes), typeof(AsyncObservableCollection<BinningMode>), typeof(CameraControlView), new UIPropertyMetadata(null));

        public AsyncObservableCollection<BinningMode> MyBinningModes {
            get => (AsyncObservableCollection<BinningMode>)GetValue(MyBinningModesProperty);
            set => SetValue(MyBinningModesProperty, value);
        }

        public static readonly DependencyProperty MySelectedBinningModeProperty =
            DependencyProperty.Register(nameof(MySelectedBinningMode), typeof(BinningMode), typeof(CameraControlView), new UIPropertyMetadata(null));

        public BinningMode MySelectedBinningMode {
            get => (BinningMode)GetValue(MySelectedBinningModeProperty);
            set => SetValue(MySelectedBinningModeProperty, value);
        }

        public static readonly DependencyProperty MyLoopProperty =
           DependencyProperty.Register(nameof(MyLoop), typeof(bool), typeof(CameraControlView), new UIPropertyMetadata(null));

        public bool MyLoop {
            get => (bool)GetValue(MyLoopProperty);
            set => SetValue(MyLoopProperty, value);
        }

        public static readonly DependencyProperty MyGainsProperty =
            DependencyProperty.Register(nameof(MyGains), typeof(ICollection), typeof(CameraControlView), new UIPropertyMetadata(null));

        public ICollection MyGains {
            get => (ICollection)GetValue(MyGainsProperty);
            set => SetValue(MyGainsProperty, value);
        }

        public static readonly DependencyProperty MyCanGetGainProperty =
            DependencyProperty.Register(nameof(MyCanGetGain), typeof(bool), typeof(CameraControlView), new UIPropertyMetadata(false));

        public bool MyCanGetGain {
            get => (bool)GetValue(MyCanGetGainProperty);
            set => SetValue(MyCanGetGainProperty, value);
        }

        public static readonly DependencyProperty MyCanSetGainProperty =
            DependencyProperty.Register(nameof(MyCanSetGain), typeof(bool), typeof(CameraControlView), new UIPropertyMetadata(false));

        public bool MyCanSetGain {
            get => (bool)GetValue(MyCanSetGainProperty);
            set => SetValue(MyCanSetGainProperty, value);
        }

        public static readonly DependencyProperty MyMinGainProperty =
            DependencyProperty.Register(nameof(MyMinGain), typeof(short), typeof(CameraControlView), new UIPropertyMetadata((short)-1));

        public short MyMinGain {
            get => (short)GetValue(MyMinGainProperty);
            set => SetValue(MyMinGainProperty, value);
        }

        public static readonly DependencyProperty MyMaxGainProperty =
            DependencyProperty.Register(nameof(MyMaxGain), typeof(short), typeof(CameraControlView), new UIPropertyMetadata((short)-1));

        public short MyMaxGain {
            get => (short)GetValue(MyMaxGainProperty);
            set => SetValue(MyMaxGainProperty, value);
        }

        public static readonly DependencyProperty MySelectedGainProperty =
            DependencyProperty.Register(nameof(MySelectedGain), typeof(int), typeof(CameraControlView), new UIPropertyMetadata((int)-1));

        public int MySelectedGain {
            get => (int)GetValue(MySelectedGainProperty);
            set => SetValue(MySelectedGainProperty, value);
        }

        public static readonly DependencyProperty MyDefaultGainProperty =
    DependencyProperty.Register(nameof(MyDefaultGain), typeof(int), typeof(CameraControlView), new UIPropertyMetadata((int)-1));

        public int MyDefaultGain {
            get => (int)GetValue(MyDefaultGainProperty);
            set => SetValue(MyDefaultGainProperty, value);
        }
    }
}