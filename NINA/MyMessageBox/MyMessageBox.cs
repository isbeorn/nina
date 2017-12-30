using NINA.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NINA.MyMessageBox {
    class MyMessageBox : BaseVM {

        private string _title;
        public string Title {
            get {
                return _title;
            }
            set {
                _title = value;
                RaisePropertyChanged();
            }
        }

        private string _text;
        public string Text {
            get {
                return _text;
            }
            set {
                _text = value;
                RaisePropertyChanged();
            }
        }

        private bool? _dialogResult;
        public bool? DialogResult {
            get {
                return _dialogResult;
            }
            set {
                _dialogResult = value;
                RaisePropertyChanged();
            }
        }

        private Visibility _cancelVisibility;
        public Visibility CancelVisibility {
            get {
                return _cancelVisibility;
            }
            set {
                _cancelVisibility = value;
                RaisePropertyChanged();
            }
        }

        private Visibility _oKVisibility;
        public Visibility OKVisibility {
            get {
                return _oKVisibility;
            }
            set {
                _oKVisibility = value;
                RaisePropertyChanged();
            }
        }

        private Visibility _yesVisibility;
        public Visibility YesVisibility {
            get {
                return _yesVisibility;
            }
            set {
                _yesVisibility = value;
                RaisePropertyChanged();
            }
        }

        private Visibility _noVisibility;
        public Visibility NoVisibility {
            get {
                return _noVisibility;
            }
            set {
                _noVisibility = value;
                RaisePropertyChanged();
            }
        }

        public static MessageBoxResult Show(string messageBoxText) {
            return Show(messageBoxText, "", MessageBoxButton.OK, MessageBoxResult.OK);
        }

        public static MessageBoxResult Show(string messageBoxText, string caption) {
            return Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxResult.OK);
        }

        public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxResult defaultresult) {


            var MyMessageBox = new MyMessageBox();
            MyMessageBox.Title = caption;
            MyMessageBox.Text = messageBoxText;

            if (button == MessageBoxButton.OKCancel) {
                MyMessageBox.CancelVisibility = System.Windows.Visibility.Visible;
                MyMessageBox.OKVisibility = System.Windows.Visibility.Visible;
                MyMessageBox.YesVisibility = System.Windows.Visibility.Hidden;
                MyMessageBox.NoVisibility = System.Windows.Visibility.Hidden;
            } else if (button == MessageBoxButton.YesNo) {
                MyMessageBox.CancelVisibility = System.Windows.Visibility.Hidden;
                MyMessageBox.OKVisibility = System.Windows.Visibility.Hidden;
                MyMessageBox.YesVisibility = System.Windows.Visibility.Visible;
                MyMessageBox.NoVisibility = System.Windows.Visibility.Visible;
            } else if (button == MessageBoxButton.OK) {
                MyMessageBox.CancelVisibility = System.Windows.Visibility.Hidden;
                MyMessageBox.OKVisibility = System.Windows.Visibility.Visible;
                MyMessageBox.YesVisibility = System.Windows.Visibility.Hidden;
                MyMessageBox.NoVisibility = System.Windows.Visibility.Hidden;
            } else {
                MyMessageBox.CancelVisibility = System.Windows.Visibility.Hidden;
                MyMessageBox.OKVisibility = System.Windows.Visibility.Visible;
                MyMessageBox.YesVisibility = System.Windows.Visibility.Hidden;
                MyMessageBox.NoVisibility = System.Windows.Visibility.Hidden;
            }


            System.Windows.Window win = new MyMessageBoxView {
                DataContext = MyMessageBox
            };
            win.SizeChanged += Win_SizeChanged;

            var mainwindow = System.Windows.Application.Current.MainWindow;
            mainwindow.Opacity = 0.8;

            win.ShowDialog();
            mainwindow.Opacity = 1;

            if (win.DialogResult == null) {
                return defaultresult;
            } else if (win.DialogResult == true) {
                if(MyMessageBox.YesVisibility == Visibility.Visible) {
                    return MessageBoxResult.Yes;
                } else {
                    return MessageBoxResult.OK;
                }                
            } else if (win.DialogResult == false) {
                if (MyMessageBox.NoVisibility == Visibility.Visible) {
                    return MessageBoxResult.No;
                } else {
                    return MessageBoxResult.Cancel;
                }                
            } else {
                return defaultresult;
            }
        }

        private static void Win_SizeChanged(object sender, SizeChangedEventArgs e) {
            var mainwindow = System.Windows.Application.Current.MainWindow;

            var win = (System.Windows.Window)sender;
            win.Left = mainwindow.Left + (mainwindow.Width - win.ActualWidth) / 2; ;
            win.Top = mainwindow.Top + (mainwindow.Height - win.ActualHeight) / 2;
        }
    }
}
