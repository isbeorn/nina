#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NINACustomControlLibrary {

    public class LoadingControl : UserControl {

        static LoadingControl() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(LoadingControl), new FrameworkPropertyMetadata(typeof(LoadingControl)));
        }

        public static readonly DependencyProperty TextProperty =
           DependencyProperty.Register(nameof(Text), typeof(string), typeof(LoadingControl), new UIPropertyMetadata(string.Empty));

        public string Text {
            get {
                return (string)GetValue(TextProperty);
            }
            set {
                SetValue(TextProperty, value);
            }
        }

        public static readonly DependencyProperty LoadingImageProperty =
           DependencyProperty.Register(nameof(LoadingImage), typeof(Geometry), typeof(LoadingControl), new UIPropertyMetadata(Geometry.Parse("M 551.88,276.125C 551.167,238.98 543.109,201.953 528.224,168.093C 513.391,134.203 491.859,103.505 465.421,78.136C 439.005,52.7507 407.676,32.6827 373.765,19.4427C 339.875,6.15076 303.432,-0.197266 267.432,0.573364C 231.427,1.28674 195.583,9.11475 162.801,23.5467C 130,37.9321 100.281,58.808 75.7293,84.4227C 51.1613,110.021 31.7493,140.369 18.9573,173.199C 6.12001,206.011 1.07288e-005,241.265 0.765344,276.125C 1.48401,310.995 9.07734,345.656 23.0627,377.355C 37,409.073 57.2187,437.813 82.0147,461.547C 106.796,485.297 136.167,504.047 167.911,516.391C 199.64,528.781 233.713,534.667 267.432,533.907C 301.161,533.188 334.635,525.817 365.255,512.287C 395.896,498.797 423.651,479.24 446.567,455.26C 469.505,431.292 487.593,402.907 499.484,372.245C 506.729,353.656 511.64,334.245 514.229,314.579C 514.921,314.62 515.62,314.647 516.323,314.647C 535.957,314.647 551.875,298.729 551.875,279.089C 551.875,278.093 551.823,277.104 551.744,276.131M 495.38,370.541C 482.343,400.099 463.448,426.875 440.287,448.975C 417.135,471.089 389.729,488.527 360.151,499.969C 330.577,511.459 298.875,516.885 267.432,516.125C 235.989,515.401 204.88,508.495 176.421,495.865C 147.953,483.271 122.156,465.036 100.869,442.688C 79.572,420.355 62.8013,393.932 51.8013,365.432C 40.7653,336.943 35.5667,306.427 36.3227,276.125C 37.0467,245.817 43.7293,215.896 55.9107,188.521C 68.052,161.131 85.6293,136.313 107.156,115.844C 128.672,95.3654 154.12,79.2507 181.531,68.7027C 208.943,58.1147 238.271,53.1467 267.432,53.9014C 296.599,54.6307 325.339,61.0787 351.635,72.8134C 377.943,84.5054 401.776,101.423 421.427,122.136C 441.093,142.833 456.541,167.297 466.645,193.625C 476.781,219.959 481.52,248.104 480.765,276.125L 480.9,276.125C 480.823,277.104 480.765,278.089 480.765,279.089C 480.765,297.427 494.656,312.516 512.484,314.432C 509.025,333.781 503.296,352.667 495.38,370.541 Z ")));

        public Geometry LoadingImage {
            get {
                return (Geometry)GetValue(LoadingImageProperty);
            }
            set {
                SetValue(LoadingImageProperty, value);
            }
        }

        public static readonly DependencyProperty LoadingImageBrushProperty =
           DependencyProperty.Register(nameof(LoadingImageBrush), typeof(Brush), typeof(LoadingControl), new UIPropertyMetadata(new SolidColorBrush(Colors.White)));

        public Brush LoadingImageBrush {
            get {
                return (Brush)GetValue(LoadingImageBrushProperty);
            }
            set {
                SetValue(LoadingImageBrushProperty, value);
            }
        }
    }
}