#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace NINA.Utility.WindowService {

    /// <summary>
    /// A window should be associated to a viewmodel by the DataTemplates.xaml
    /// </summary>
    internal class WindowService : IWindowService {
        protected Dispatcher dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        protected CustomWindow window;

        public void Show(object content, string title = "", ResizeMode resizeMode = ResizeMode.NoResize, WindowStyle windowStyle = WindowStyle.None) {
            dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => {
                window = new CustomWindow() {
                    SizeToContent = SizeToContent.WidthAndHeight,
                    Title = title,
                    Background = Application.Current.TryFindResource("BackgroundBrush") as Brush,
                    ResizeMode = resizeMode,
                    WindowStyle = windowStyle,
                    MinHeight = 300,
                    MinWidth = 350,
                    Style = Application.Current.TryFindResource("NoResizeWindow") as Style,
                };
                window.CloseCommand = new RelayCommand((object o) => window.Close());
                window.ContentRendered += (object sender, EventArgs e) => window.InvalidateVisual();
                window.Content = content;
                window.Owner = Application.Current.MainWindow;
                window.Show();
            }));
        }

        public void DelayedClose(TimeSpan t) {
            Task.Run(async () => {
                await Utility.Wait(t);
                await this.Close();
            });
        }

        public async Task Close() {
            await dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                window?.Close();
            }));
        }

        public DispatcherOperation ShowDialog(object content, string title = "", ResizeMode resizeMode = ResizeMode.NoResize, WindowStyle windowStyle = WindowStyle.None, ICommand closeCommand = null) {
            return dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                window = new CustomWindow() {
                    SizeToContent = SizeToContent.WidthAndHeight,
                    Title = title,
                    Background = Application.Current.TryFindResource("BackgroundBrush") as Brush,
                    ResizeMode = resizeMode,
                    WindowStyle = windowStyle,
                    Style = Application.Current.TryFindResource("NoResizeWindow") as Style,
                };
                if (closeCommand == null) {
                    window.CloseCommand = new RelayCommand((object o) => window.Close());
                } else {
                    window.CloseCommand = closeCommand;
                }
                window.ContentRendered += (object sender, EventArgs e) => window.InvalidateVisual();

                window.SizeChanged += Win_SizeChanged;
                window.Content = content;
                var mainwindow = System.Windows.Application.Current.MainWindow;
                mainwindow.Opacity = 0.8;
                window.Owner = Application.Current.MainWindow;
                var result = window.ShowDialog();
                this.OnDialogResultChanged?.Invoke(this, new DialogResultEventArgs(result));
                mainwindow.Opacity = 1;
            }));
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

        void Show(object content, string title = "", ResizeMode resizeMode = ResizeMode.NoResize, WindowStyle windowStyle = WindowStyle.None);

        DispatcherOperation ShowDialog(object content, string title = "", ResizeMode resizeMode = ResizeMode.NoResize, WindowStyle windowStyle = WindowStyle.None, ICommand closeCommand = null);

        event EventHandler OnDialogResultChanged;

        void DelayedClose(TimeSpan t);

        Task Close();
    }

    public class DialogResultEventArgs : EventArgs {

        public DialogResultEventArgs(bool? dialogResult) {
            DialogResult = dialogResult;
        }

        public bool? DialogResult { get; set; }
    }
}