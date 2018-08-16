using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace NINA.Utility {

    /// <summary>
    /// A window should be associated to a viewmodel by the DataTemplates.xaml
    /// </summary>
    internal class WindowService : IWindowService {
        private Dispatcher dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

        public void ShowWindow(object viewModel, string title = "", ResizeMode resizeMode = ResizeMode.NoResize, WindowStyle windowStyle = WindowStyle.None) {
            _win = new Window() {
                SizeToContent = SizeToContent.WidthAndHeight,
                Title = title,
                Background = Application.Current.TryFindResource("BackgroundBrush") as Brush,
                ResizeMode = resizeMode,
                WindowStyle = windowStyle,
                MinHeight = 300,
                MinWidth = 350
            };
            _win.Content = viewModel;
            _win.Show();
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
                var result = _win.ShowDialog();
                this.OnDialogResultChanged?.Invoke(this, new DialogResultEventArgs(result));
                mainwindow.Opacity = 1;
            }));
        }

        public class DialogResultEventArgs : EventArgs {

            public DialogResultEventArgs(bool? dialogResult) {
                DialogResult = dialogResult;
            }

            public bool? DialogResult { get; set; }
        }

        public event EventHandler OnDialogResultChanged;

        public async Task Close() {
            await dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                _win?.Close();
            }));
        }

        public void DelayedClose(TimeSpan t) {
            Task.Run(async () => {
                await Utility.Wait(t);
                await Close();
            });
        }

        private static void Win_SizeChanged(object sender, SizeChangedEventArgs e) {
            var mainwindow = System.Windows.Application.Current.MainWindow;

            var win = (System.Windows.Window)sender;
            win.Left = mainwindow.Left + (mainwindow.Width - win.ActualWidth) / 2; ;
            win.Top = mainwindow.Top + (mainwindow.Height - win.ActualHeight) / 2;
        }
    }

    internal interface IWindowService {

        void ShowWindow(object viewModel, string title = "", ResizeMode resizeMode = ResizeMode.NoResize, WindowStyle windowStyle = WindowStyle.None);

        void ShowDialog(object viewModel, string title = "", ResizeMode resizeMode = ResizeMode.NoResize, WindowStyle windowStyle = WindowStyle.None);

        Task Close();

        void DelayedClose(TimeSpan t);
    }
}