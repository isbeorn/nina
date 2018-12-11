#region "copyright"

/*
    Copyright © 2016 - 2018 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace NINACustomControlLibrary {

    public class AsyncProcessButton : CancellableButton {

        static AsyncProcessButton() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(AsyncProcessButton), new FrameworkPropertyMetadata(typeof(AsyncProcessButton)));
        }

        public static readonly DependencyProperty ResumeCommandProperty =
                    DependencyProperty.Register(nameof(ResumeCommand), typeof(ICommand), typeof(AsyncProcessButton), new UIPropertyMetadata(null));

        public ICommand ResumeCommand {
            get {
                return (ICommand)GetValue(ResumeCommandProperty);
            }
            set {
                SetValue(ResumeCommandProperty, value);
            }
        }

        public static readonly DependencyProperty ResumeButtonImageProperty =
           DependencyProperty.Register(nameof(ResumeButtonImage), typeof(Geometry), typeof(AsyncProcessButton), new UIPropertyMetadata(null));

        public Geometry ResumeButtonImage {
            get {
                return (Geometry)GetValue(ResumeButtonImageProperty);
            }
            set {
                SetValue(ResumeButtonImageProperty, value);
            }
        }

        public static readonly DependencyProperty IsPausedProperty =
           DependencyProperty.Register(nameof(IsPaused), typeof(bool), typeof(AsyncProcessButton), new UIPropertyMetadata(false));

        public bool IsPaused {
            get {
                return (bool)GetValue(IsPausedProperty);
            }
            set {
                SetValue(IsPausedProperty, value);
            }
        }

        public static readonly DependencyProperty PauseCommandProperty =
                    DependencyProperty.Register(nameof(PauseCommand), typeof(ICommand), typeof(AsyncProcessButton), new UIPropertyMetadata(null));

        public ICommand PauseCommand {
            get {
                return (ICommand)GetValue(PauseCommandProperty);
            }
            set {
                SetValue(PauseCommandProperty, value);
            }
        }

        public static readonly DependencyProperty PauseButtonImageProperty =
           DependencyProperty.Register(nameof(PauseButtonImage), typeof(Geometry), typeof(AsyncProcessButton), new UIPropertyMetadata(null));

        public Geometry PauseButtonImage {
            get {
                return (Geometry)GetValue(PauseButtonImageProperty);
            }
            set {
                SetValue(PauseButtonImageProperty, value);
            }
        }

        public static readonly DependencyProperty LoadingImageBrushProperty =
           DependencyProperty.Register(nameof(LoadingImageBrush), typeof(Brush), typeof(AsyncProcessButton), new UIPropertyMetadata(new SolidColorBrush(Colors.White)));

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