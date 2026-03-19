#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace NINA.Core.Utility.Notification {
    public class CustomNotification : BaseINPC {
        public CustomNotification(
            string header,
            string message,
            Geometry symbol,
            Brush color,
            Brush background,
            TimeSpan lifetime,
            Action<CustomNotification> closeAction,
            Action closeAllAction) {

            Header = header;
            Message = message;
            Symbol = symbol;
            Color = color;
            Background = background ?? new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 42, 42, 42));
            Lifetime = lifetime;
            DateTime = DateTime.Now;

            CloseCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(() => closeAction.Invoke(this));
            CloseAllCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(closeAllAction);
        }

        public DateTime DateTime { get; }

        public TimeSpan Lifetime { get; }

        public string Header {
            get => field;
            set { field = value; RaisePropertyChanged(); }
        }

        public string Message {
            get => field;
            set { field = value; RaisePropertyChanged(); }
        }

        public Geometry Symbol {
            get => field;
            set { field = value; RaisePropertyChanged(); }
        }

        public Brush Color {
            get => field;
            set { field = value; RaisePropertyChanged(); }
        }

        public Brush Background {
            get => field;
            set { field = value; RaisePropertyChanged(); }
        }

        public ICommand CloseCommand { get; }
        public ICommand CloseAllCommand { get; }
    }
}