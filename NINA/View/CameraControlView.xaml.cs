#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model.MyCamera;
using NINA.Utility;
using System.Collections;
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
            get {
                return (ICommand)GetValue(MyCommandProperty);
            }
            set {
                SetValue(MyCommandProperty, value);
            }
        }

        public static readonly DependencyProperty MyCancelCommandProperty =
           DependencyProperty.Register(nameof(MyCancelCommand), typeof(ICommand), typeof(CameraControlView), new UIPropertyMetadata(null));

        public ICommand MyCancelCommand {
            get {
                return (ICommand)GetValue(MyCancelCommandProperty);
            }
            set {
                SetValue(MyCancelCommandProperty, value);
            }
        }

        public static readonly DependencyProperty MyOrientationProperty =
          DependencyProperty.Register(nameof(MyOrientation), typeof(Orientation), typeof(CameraControlView), new UIPropertyMetadata(Orientation.Horizontal));

        public Orientation MyOrientation {
            get {
                return (Orientation)GetValue(MyOrientationProperty);
            }
            set {
                SetValue(MyOrientationProperty, value);
            }
        }

        public static readonly DependencyProperty MyButtonImageProperty =
           DependencyProperty.Register(nameof(MyButtonImage), typeof(Geometry), typeof(CameraControlView), new UIPropertyMetadata(null));

        public Geometry MyButtonImage {
            get {
                return (Geometry)GetValue(MyButtonImageProperty);
            }
            set {
                SetValue(MyButtonImageProperty, value);
            }
        }

        public static readonly DependencyProperty MyCancelButtonImageProperty =
           DependencyProperty.Register(nameof(MyCancelButtonImage), typeof(Geometry), typeof(CameraControlView), new UIPropertyMetadata(null));

        public Geometry MyCancelButtonImage {
            get {
                return (Geometry)GetValue(MyCancelButtonImageProperty);
            }
            set {
                SetValue(MyCancelButtonImageProperty, value);
            }
        }

        public static readonly DependencyProperty MyButtonTextProperty =
            DependencyProperty.Register(nameof(MyButtonText), typeof(string), typeof(CameraControlView), new UIPropertyMetadata(null));

        public string MyButtonText {
            get {
                return (string)GetValue(MyButtonTextProperty);
            }
            set {
                SetValue(MyButtonTextProperty, value);
            }
        }

        public static readonly DependencyProperty MyExposureDurationProperty =
            DependencyProperty.Register(nameof(MyExposureDuration), typeof(double), typeof(CameraControlView), new UIPropertyMetadata(null));

        public double MyExposureDuration {
            get {
                return (double)GetValue(MyExposureDurationProperty);
            }
            set {
                SetValue(MyExposureDurationProperty, value);
            }
        }

        public static readonly DependencyProperty MyFiltersProperty =
            DependencyProperty.Register(nameof(MyFilters), typeof(ObservableCollection<Model.MyFilterWheel.FilterInfo>), typeof(CameraControlView), new UIPropertyMetadata(null));

        public ObservableCollection<Model.MyFilterWheel.FilterInfo> MyFilters {
            get {
                return (ObservableCollection<Model.MyFilterWheel.FilterInfo>)GetValue(MyFiltersProperty);
            }
            set {
                SetValue(MyFiltersProperty, value);
            }
        }

        public static readonly DependencyProperty MySelectedFilterProperty =
            DependencyProperty.Register(nameof(MySelectedFilter), typeof(Model.MyFilterWheel.FilterInfo), typeof(CameraControlView), new UIPropertyMetadata(null));

        public Model.MyFilterWheel.FilterInfo MySelectedFilter {
            get {
                return (Model.MyFilterWheel.FilterInfo)GetValue(MySelectedFilterProperty);
            }
            set {
                SetValue(MySelectedFilterProperty, value);
            }
        }

        public static readonly DependencyProperty MyBinningModesProperty =
            DependencyProperty.Register(nameof(MyBinningModes), typeof(AsyncObservableCollection<BinningMode>), typeof(CameraControlView), new UIPropertyMetadata(null));

        public AsyncObservableCollection<BinningMode> MyBinningModes {
            get {
                return (AsyncObservableCollection<BinningMode>)GetValue(MyBinningModesProperty);
            }
            set {
                SetValue(MyBinningModesProperty, value);
            }
        }

        public static readonly DependencyProperty MySelectedBinningModeProperty =
            DependencyProperty.Register(nameof(MySelectedBinningMode), typeof(BinningMode), typeof(CameraControlView), new UIPropertyMetadata(null));

        public BinningMode MySelectedBinningMode {
            get {
                return (BinningMode)GetValue(MySelectedBinningModeProperty);
            }
            set {
                SetValue(MySelectedBinningModeProperty, value);
            }
        }

        public static readonly DependencyProperty MyLoopProperty =
           DependencyProperty.Register(nameof(MyLoop), typeof(bool), typeof(CameraControlView), new UIPropertyMetadata(null));

        public bool MyLoop {
            get {
                return (bool)GetValue(MyLoopProperty);
            }
            set {
                SetValue(MyLoopProperty, value);
            }
        }

        public static readonly DependencyProperty MyGainsProperty =
            DependencyProperty.Register(nameof(MyGains), typeof(ICollection), typeof(CameraControlView), new UIPropertyMetadata(null));

        public ICollection MyGains {
            get {
                return (ICollection)GetValue(MyGainsProperty);
            }
            set {
                SetValue(MyGainsProperty, value);
            }
        }

        public static readonly DependencyProperty MyCanGetGainProperty =
            DependencyProperty.Register(nameof(MyCanGetGain), typeof(bool), typeof(CameraControlView), new UIPropertyMetadata(false));

        public bool MyCanGetGain {
            get {
                return (bool)GetValue(MyCanGetGainProperty);
            }
            set {
                SetValue(MyCanGetGainProperty, value);
            }
        }

        public static readonly DependencyProperty MyCanSetGainProperty =
            DependencyProperty.Register(nameof(MyCanSetGain), typeof(bool), typeof(CameraControlView), new UIPropertyMetadata(false));

        public bool MyCanSetGain {
            get {
                return (bool)GetValue(MyCanSetGainProperty);
            }
            set {
                SetValue(MyCanSetGainProperty, value);
            }
        }

        public static readonly DependencyProperty MyMinGainProperty =
            DependencyProperty.Register(nameof(MyMinGain), typeof(short), typeof(CameraControlView), new UIPropertyMetadata((short)-1));

        public short MyMinGain {
            get {
                return (short)GetValue(MyMinGainProperty);
            }
            set {
                SetValue(MyMinGainProperty, value);
            }
        }

        public static readonly DependencyProperty MyMaxGainProperty =
            DependencyProperty.Register(nameof(MyMaxGain), typeof(short), typeof(CameraControlView), new UIPropertyMetadata((short)-1));

        public short MyMaxGain {
            get {
                return (short)GetValue(MyMaxGainProperty);
            }
            set {
                SetValue(MyMaxGainProperty, value);
            }
        }

        public static readonly DependencyProperty MySelectedGainProperty =
            DependencyProperty.Register(nameof(MySelectedGain), typeof(short), typeof(CameraControlView), new UIPropertyMetadata((short)-1));

        public short MySelectedGain {
            get {
                return (short)GetValue(MySelectedGainProperty);
            }
            set {
                SetValue(MySelectedGainProperty, value);
            }
        }
    }
}
