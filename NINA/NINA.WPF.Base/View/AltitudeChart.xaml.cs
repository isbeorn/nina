#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Astrometry;
using System.Windows;
using System.Windows.Controls;

namespace NINA.WPF.Base.View {

    /// <summary>
    /// Interaction logic for AltitudeChart.xaml
    /// </summary>
    public partial class AltitudeChart : UserControl {

        public AltitudeChart() {
            InitializeComponent();
        }

        public static DependencyProperty NighttimeDataProperty = DependencyProperty.Register("NighttimeData", typeof(NighttimeData), typeof(AltitudeChart));

        public NighttimeData NighttimeData {
            get => (NighttimeData)GetValue(NighttimeDataProperty);
            set => SetValue(NighttimeDataProperty, value);
        }

        public static DependencyProperty AnnotateAltitudeAxisProperty = DependencyProperty.Register("AnnotateAltitudeAxis", typeof(bool), typeof(AltitudeChart), new PropertyMetadata(true));

        public bool AnnotateAltitudeAxis {
            get => (bool)GetValue(AnnotateAltitudeAxisProperty);
            set => SetValue(AnnotateAltitudeAxisProperty, value);
        }

        public static DependencyProperty AnnotateTimeAxisProperty = DependencyProperty.Register("AnnotateTimeAxis", typeof(bool), typeof(AltitudeChart), new PropertyMetadata(true));

        public bool AnnotateTimeAxis {
            get => (bool)GetValue(AnnotateTimeAxisProperty);
            set => SetValue(AnnotateTimeAxisProperty, value);
        }

        public static DependencyProperty MoonHorizontalAlignmentProperty = DependencyProperty.Register("MoonHorizontalAlignment", typeof(HorizontalAlignment), typeof(AltitudeChart), new PropertyMetadata(HorizontalAlignment.Right));

        public HorizontalAlignment MoonHorizontalAlignment {
            get => (HorizontalAlignment)GetValue(MoonHorizontalAlignmentProperty);
            set => SetValue(MoonHorizontalAlignmentProperty, value);
        }

        public static DependencyProperty MoonVerticalAlignmentProperty = DependencyProperty.Register("MoonVerticalAlignment", typeof(VerticalAlignment), typeof(AltitudeChart), new PropertyMetadata(VerticalAlignment.Top));

        public VerticalAlignment MoonVerticalAlignment {
            get => (VerticalAlignment)GetValue(MoonVerticalAlignmentProperty);
            set => SetValue(MoonVerticalAlignmentProperty, value);
        }

        public static DependencyProperty MoonMarginProperty = DependencyProperty.Register("MoonMargin", typeof(Thickness), typeof(AltitudeChart), new PropertyMetadata(new Thickness(0, 10, 10, 0)));

        public Thickness MoonMargin {
            get => (Thickness)GetValue(MoonMarginProperty);
            set => SetValue(MoonMarginProperty, value);
        }

        public static DependencyProperty ShowMoonProperty = DependencyProperty.Register("ShowMoon", typeof(bool), typeof(AltitudeChart), new PropertyMetadata(true));

        public bool ShowMoon {
            get => (bool)GetValue(ShowMoonProperty);
            set => SetValue(ShowMoonProperty, value);
        }
    }
}