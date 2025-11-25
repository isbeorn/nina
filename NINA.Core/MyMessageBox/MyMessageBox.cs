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

            // Check if running in headless mode (Linux without WPF)
            if (Application.Current == null) {
                return ShowViaDialogService(messageBoxText, caption, button, defaultresult);
            }

            // Create a MyMessageBox instance
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

            // Register with DialogService so Touch-N-Stars can see it
            int? dialogServiceId = null;
            try {
                var dialog = new System.Windows.DialogService.DialogInfo {
                    Title = caption,
                    Message = messageBoxText,
                    ContentType = "MyMessageBox",
                    DataContext = MyMessageBox
                };
                dialog.Content["Text"] = messageBoxText;
                dialog.Content["OKVisibility"] = MyMessageBox.OKVisibility.ToString();
                dialog.Content["CancelVisibility"] = MyMessageBox.CancelVisibility.ToString();
                dialog.Content["YesVisibility"] = MyMessageBox.YesVisibility.ToString();
                dialog.Content["NoVisibility"] = MyMessageBox.NoVisibility.ToString();

                dialogServiceId = System.Windows.DialogService.RegisterDialog(dialog);

                // Add buttons
                if (MyMessageBox.OKVisibility == Visibility.Visible) {
                    System.Windows.DialogService.AddButton(dialogServiceId.Value, "OK", "OK", isDefault: true);
                }
                if (MyMessageBox.CancelVisibility == Visibility.Visible) {
                    System.Windows.DialogService.AddButton(dialogServiceId.Value, "Cancel", "Cancel", isCancel: true);
                }
                if (MyMessageBox.YesVisibility == Visibility.Visible) {
                    System.Windows.DialogService.AddButton(dialogServiceId.Value, "Yes", "Yes", isDefault: true);
                }
                if (MyMessageBox.NoVisibility == Visibility.Visible) {
                    System.Windows.DialogService.AddButton(dialogServiceId.Value, "No", "No");
                }
            } catch {
                // DialogService not available or error - continue with WPF only
            }

            dialogresult = Application.Current.Dispatcher.Invoke(() => {

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

                // Close DialogService registration when WPF dialog closes
                if (dialogServiceId.HasValue) {
                    try {
                        System.Windows.DialogService.CloseDialog(dialogServiceId.Value, win.DialogResult ?? false);
                    } catch {
                        // Ignore errors closing DialogService entry
                    }
                }

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

        private static MessageBoxResult ShowViaDialogService(string messageBoxText, string caption, MessageBoxButton button, MessageBoxResult defaultresult) {
            // Register dialog with DialogService (works on Linux)
            var dialog = new System.Windows.DialogService.DialogInfo {
                Title = caption,
                Message = messageBoxText,
                ContentType = "MyMessageBox"
            };

            // Create a MyMessageBox instance to track visibility properties
            var messageBox = new MyMessageBox {
                Title = caption,
                Text = messageBoxText,
            };

            // Set button visibility based on button type
            if (button == MessageBoxButton.OKCancel) {
                messageBox.CancelVisibility = Visibility.Visible;
                messageBox.OKVisibility = Visibility.Visible;
                messageBox.YesVisibility = Visibility.Hidden;
                messageBox.NoVisibility = Visibility.Hidden;
            } else if (button == MessageBoxButton.YesNo) {
                messageBox.CancelVisibility = Visibility.Hidden;
                messageBox.OKVisibility = Visibility.Hidden;
                messageBox.YesVisibility = Visibility.Visible;
                messageBox.NoVisibility = Visibility.Visible;
            } else if (button == MessageBoxButton.OK) {
                messageBox.CancelVisibility = Visibility.Hidden;
                messageBox.OKVisibility = Visibility.Visible;
                messageBox.YesVisibility = Visibility.Hidden;
                messageBox.NoVisibility = Visibility.Hidden;
            } else {
                messageBox.CancelVisibility = Visibility.Hidden;
                messageBox.OKVisibility = Visibility.Visible;
                messageBox.YesVisibility = Visibility.Hidden;
                messageBox.NoVisibility = Visibility.Hidden;
            }

            // Store the DataContext for DialogManager to extract
            dialog.DataContext = messageBox;

            // Add content for serialization
            dialog.Content["Text"] = messageBoxText;
            dialog.Content["Title"] = caption;
            dialog.Content["OKVisibility"] = messageBox.OKVisibility.ToString();
            dialog.Content["CancelVisibility"] = messageBox.CancelVisibility.ToString();
            dialog.Content["YesVisibility"] = messageBox.YesVisibility.ToString();
            dialog.Content["NoVisibility"] = messageBox.NoVisibility.ToString();

            int dialogId = System.Windows.DialogService.RegisterDialog(dialog);

            // Add buttons based on type
            if (button == MessageBoxButton.OKCancel) {
                System.Windows.DialogService.AddButton(dialogId, "OK", "OK", isDefault: true, onClick: () => {
                    messageBox.DialogResult = true;
                });
                System.Windows.DialogService.AddButton(dialogId, "Cancel", "Cancel", isCancel: true, onClick: () => {
                    messageBox.DialogResult = false;
                });
            } else if (button == MessageBoxButton.YesNo) {
                System.Windows.DialogService.AddButton(dialogId, "Yes", "Yes", isDefault: true, onClick: () => {
                    messageBox.DialogResult = true;
                });
                System.Windows.DialogService.AddButton(dialogId, "No", "No", onClick: () => {
                    messageBox.DialogResult = false;
                });
            } else if (button == MessageBoxButton.OK) {
                System.Windows.DialogService.AddButton(dialogId, "OK", "OK", isDefault: true, onClick: () => {
                    messageBox.DialogResult = true;
                });
            }

            Logger.Info($"MyMessageBox: Dialog registered with DialogService ID {dialogId} (headless mode)");

            // In headless mode, the dialog stays open until the Android app interacts with it
            // Return the default result for now (dialog won't block execution)
            return defaultresult;
        }
    }
}
