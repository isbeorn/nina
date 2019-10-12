#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace NINALocaleManager {

    /// <summary>
    /// A window should be associated to a viewmodel by the DataTemplates.xaml
    /// </summary>
    internal class WindowService : IWindowService {
        protected Dispatcher dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        protected Window window;

        public void Show(object content, string title = "", ResizeMode resizeMode = ResizeMode.CanResizeWithGrip, WindowStyle windowStyle = WindowStyle.SingleBorderWindow) {
            dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                window = new Window() {
                    SizeToContent = SizeToContent.WidthAndHeight,
                    Title = title,
                    ResizeMode = resizeMode,
                    WindowStyle = windowStyle,
                    MinHeight = 300,
                    MinWidth = 350,
                    Style = Application.Current.TryFindResource("NoResizeWindow") as Style,
                };
                window.ContentRendered += (object sender, EventArgs e) => window.InvalidateVisual();
                window.Content = content;
                window.Owner = Application.Current.MainWindow;
                window.Show();
            }));
        }

        public void Close() {
            window?.Close();
        }

        public event EventHandler OnDialogResultChanged;

        private static void Win_SizeChanged(object sender, SizeChangedEventArgs e) {
            var mainwindow = System.Windows.Application.Current.MainWindow;
            var win = (System.Windows.Window)sender;
            win.Left = mainwindow.Left + (mainwindow.Width - win.ActualWidth) / 2; ;
            win.Top = mainwindow.Top + (mainwindow.Height - win.ActualHeight) / 2;
        }
    }

    public interface IWindowService {

        void Show(object content, string title = "", ResizeMode resizeMode = ResizeMode.CanResizeWithGrip, WindowStyle windowStyle = WindowStyle.SingleBorderWindow);

        event EventHandler OnDialogResultChanged;

        void Close();
    }

    public class DialogResultEventArgs : EventArgs {

        public DialogResultEventArgs(bool? dialogResult) {
            DialogResult = dialogResult;
        }

        public bool? DialogResult { get; set; }
    }
}