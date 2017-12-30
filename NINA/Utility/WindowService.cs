using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace NINA.Utility {
    /// <summary>
    /// A window should be associated to a viewmodel by the DataTemplates.xaml
    /// </summary>
    class WindowService : IWindowService {
        private Dispatcher dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

        public void ShowWindow(object viewModel) {
            var win = new Window();
            win.Content = viewModel;
            win.Show();
        }

        private Window _win;

        public void ShowDialog(object viewModel, string title = "", ResizeMode resizeMode = ResizeMode.NoResize, WindowStyle windowStyle = WindowStyle.None) {
            dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                _win = new Window() {
                    SizeToContent = SizeToContent.WidthAndHeight,
                    Title = title,
                    ResizeMode = resizeMode,
                    WindowStyle = windowStyle
                };
                _win.SizeChanged += Win_SizeChanged;
                _win.Content = viewModel;
                var mainwindow = System.Windows.Application.Current.MainWindow;
                mainwindow.Opacity = 0.8;
                _win.ShowDialog();
                mainwindow.Opacity = 1;

            }));
        }

        public async Task Close() {
            await dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                _win?.Close();
            }));
        }

        private static void Win_SizeChanged(object sender, SizeChangedEventArgs e) {
            var mainwindow = System.Windows.Application.Current.MainWindow;

            var win = (System.Windows.Window)sender;
            win.Left = mainwindow.Left + (mainwindow.Width - win.ActualWidth) / 2; ;
            win.Top = mainwindow.Top + (mainwindow.Height - win.ActualHeight) / 2;
        }
    }

    interface IWindowService {
        void ShowWindow(object dataContext);
    }
}
