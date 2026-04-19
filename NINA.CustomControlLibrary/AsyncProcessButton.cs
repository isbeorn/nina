#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace NINA.CustomControlLibrary {

    public class AsyncProcessButton : CancellableButton {

        static AsyncProcessButton() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(AsyncProcessButton), new FrameworkPropertyMetadata(typeof(AsyncProcessButton)));
        }

        public static readonly DependencyProperty ResumeCommandProperty =
                    DependencyProperty.Register(nameof(ResumeCommand), typeof(ICommand), typeof(AsyncProcessButton), new UIPropertyMetadata(null));

        public ICommand ResumeCommand {
            get => (ICommand)GetValue(ResumeCommandProperty);
            set => SetValue(ResumeCommandProperty, value);
        }

        public static readonly DependencyProperty ResumeButtonImageProperty =
           DependencyProperty.Register(nameof(ResumeButtonImage), typeof(Geometry), typeof(AsyncProcessButton), new UIPropertyMetadata(null));

        public Geometry ResumeButtonImage {
            get => (Geometry)GetValue(ResumeButtonImageProperty);
            set => SetValue(ResumeButtonImageProperty, value);
        }

        public static readonly DependencyProperty IsPausedProperty =
           DependencyProperty.Register(nameof(IsPaused), typeof(bool), typeof(AsyncProcessButton), new UIPropertyMetadata(false));

        public bool IsPaused {
            get => (bool)GetValue(IsPausedProperty);
            set => SetValue(IsPausedProperty, value);
        }

        public static readonly DependencyProperty PauseCommandProperty =
                    DependencyProperty.Register(nameof(PauseCommand), typeof(ICommand), typeof(AsyncProcessButton), new UIPropertyMetadata(null));

        public ICommand PauseCommand {
            get => (ICommand)GetValue(PauseCommandProperty);
            set => SetValue(PauseCommandProperty, value);
        }

        public static readonly DependencyProperty PauseButtonImageProperty =
           DependencyProperty.Register(nameof(PauseButtonImage), typeof(Geometry), typeof(AsyncProcessButton), new UIPropertyMetadata(null));

        public Geometry PauseButtonImage {
            get => (Geometry)GetValue(PauseButtonImageProperty);
            set => SetValue(PauseButtonImageProperty, value);
        }

        public static readonly DependencyProperty LoadingImageProperty =
           DependencyProperty.Register(nameof(LoadingImage), typeof(Geometry), typeof(AsyncProcessButton), new UIPropertyMetadata(LoadingControl.DefaultLoadingImage));

        public Geometry LoadingImage {
            get => (Geometry)GetValue(LoadingImageProperty);
            set => SetValue(LoadingImageProperty, value);
        }

        public static readonly DependencyProperty LoadingImageBrushProperty =
           DependencyProperty.Register(nameof(LoadingImageBrush), typeof(Brush), typeof(AsyncProcessButton), new UIPropertyMetadata(LoadingControl.DefaultLoadingImageBrush));

        public Brush LoadingImageBrush {
            get => (Brush)GetValue(LoadingImageBrushProperty);
            set => SetValue(LoadingImageBrushProperty, value);
        }

        public static readonly DependencyProperty PauseToolTipProperty =
            DependencyProperty.Register(nameof(PauseToolTip), typeof(string), typeof(AsyncProcessButton), new UIPropertyMetadata(null));

        public string PauseToolTip {
            get => (string)GetValue(PauseToolTipProperty);
            set => SetValue(PauseToolTipProperty, value);
        }

        public static readonly DependencyProperty ResumeToolTipProperty =
            DependencyProperty.Register(nameof(ResumeToolTip), typeof(string), typeof(AsyncProcessButton), new UIPropertyMetadata(null));

        public string ResumeToolTip {
            get => (string)GetValue(ResumeToolTipProperty);
            set => SetValue(ResumeToolTipProperty, value);
        }
    }
}
