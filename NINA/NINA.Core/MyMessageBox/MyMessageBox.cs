#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NINA.Core.Utility.Extensions;
using System;
using System.Windows;

namespace NINA.Core.MyMessageBox {

    public class MyMessageBox : BaseINPC {
        private string _title;

        public string Title {
            get => _title;
            set {
                _title = value;
                RaisePropertyChanged();
            }
        }

        private string _text;

        public string Text {
            get => _text;
            set {
                _text = value;
                RaisePropertyChanged();
            }
        }

        private bool? _dialogResult;

        public bool? DialogResult {
            get => _dialogResult;
            set {
                _dialogResult = value;
                RaisePropertyChanged();
            }
        }

        private Visibility _cancelVisibility;

        public Visibility CancelVisibility {
            get => _cancelVisibility;
            set {
                _cancelVisibility = value;
                RaisePropertyChanged();
            }
        }

        private Visibility _oKVisibility;

        public Visibility OKVisibility {
            get => _oKVisibility;
            set {
                _oKVisibility = value;
                RaisePropertyChanged();
            }
        }

        private Visibility _yesVisibility;

        public Visibility YesVisibility {
            get => _yesVisibility;
            set {
                _yesVisibility = value;
                RaisePropertyChanged();
            }
        }

        private Visibility _noVisibility;

        public Visibility NoVisibility {
            get => _noVisibility;
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
            var dialogresult = defaultresult;
            dialogresult = Application.Current.Dispatcher.Invoke(() => {
                var MyMessageBox = new MyMessageBox {
                    Title = caption,
                    Text = messageBoxText,
                };

                if (button == MessageBoxButton.OKCancel) {
                    MyMessageBox.CancelVisibility = Visibility.Visible;
                    MyMessageBox.OKVisibility = Visibility.Visible;
                    MyMessageBox.YesVisibility = Visibility.Hidden;
                    MyMessageBox.NoVisibility = Visibility.Hidden;
                } else if (button == MessageBoxButton.YesNo) {
                    MyMessageBox.CancelVisibility = Visibility.Hidden;
                    MyMessageBox.OKVisibility = Visibility.Hidden;
                    MyMessageBox.YesVisibility = Visibility.Visible;
                    MyMessageBox.NoVisibility = Visibility.Visible;
                } else if (button == MessageBoxButton.OK) {
                    MyMessageBox.CancelVisibility = Visibility.Hidden;
                    MyMessageBox.OKVisibility = Visibility.Visible;
                    MyMessageBox.YesVisibility = Visibility.Hidden;
                    MyMessageBox.NoVisibility = Visibility.Hidden;
                } else {
                    MyMessageBox.CancelVisibility = Visibility.Hidden;
                    MyMessageBox.OKVisibility = Visibility.Visible;
                    MyMessageBox.YesVisibility = Visibility.Hidden;
                    MyMessageBox.NoVisibility = Visibility.Hidden;
                }

                var mainwindow = Application.Current.MainWindow;
                Window win = new MyMessageBoxView {
                    DataContext = MyMessageBox,
                    Owner = mainwindow,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };
                win.Closed += (object sender, EventArgs e) => {
                    Application.Current.MainWindow.Focus();
                };

                mainwindow.Opacity = 0.8;
                win.ShowDialog();
                mainwindow.Opacity = 1;

                if (win.DialogResult == null) {
                    return defaultresult;
                } else if (win.DialogResult == true) {
                    if (MyMessageBox.YesVisibility == Visibility.Visible) {
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
            });
            return dialogresult;
        }
    }
}