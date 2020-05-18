#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NINA.View.Equipment {

    /// <summary>
    /// Interaction logic for Connector.xaml
    /// </summary>
    public partial class Connector : UserControl {

        public Connector() {
            InitializeComponent();

            LayoutRoot.DataContext = this;
        }

        public static readonly DependencyProperty ConnectCommandProperty =
            DependencyProperty.Register(nameof(ConnectCommand), typeof(ICommand), typeof(Connector), new UIPropertyMetadata(null));

        public ICommand ConnectCommand {
            get {
                return (ICommand)GetValue(ConnectCommandProperty);
            }
            set {
                SetValue(ConnectCommandProperty, value);
            }
        }

        public static readonly DependencyProperty DisconnectCommandProperty =
            DependencyProperty.Register(nameof(DisconnectCommand), typeof(ICommand), typeof(Connector), new UIPropertyMetadata(null));

        public ICommand DisconnectCommand {
            get {
                return (ICommand)GetValue(DisconnectCommandProperty);
            }
            set {
                SetValue(DisconnectCommandProperty, value);
            }
        }

        public static readonly DependencyProperty CancelCommandProperty =
            DependencyProperty.Register(nameof(CancelCommand), typeof(ICommand), typeof(Connector), new UIPropertyMetadata(null));

        public ICommand CancelCommand {
            get {
                return (ICommand)GetValue(CancelCommandProperty);
            }
            set {
                SetValue(CancelCommandProperty, value);
            }
        }

        public static readonly DependencyProperty ConnectedProperty =
            DependencyProperty.Register(nameof(Connected), typeof(bool), typeof(Connector), new UIPropertyMetadata(false));

        public bool Connected {
            get {
                return (bool)GetValue(ConnectedProperty);
            }
            set {
                SetValue(ConnectedProperty, value);
            }
        }

        public static readonly DependencyProperty SelectedDeviceProperty =
            DependencyProperty.Register(nameof(SelectedDevice), typeof(IDevice), typeof(Connector), new UIPropertyMetadata(null));

        public IDevice SelectedDevice {
            get {
                return (IDevice)GetValue(SelectedDeviceProperty);
            }
            set {
                SetValue(SelectedDeviceProperty, value);
            }
        }

        public static readonly DependencyProperty DevicesProperty =
            DependencyProperty.Register(nameof(Devices), typeof(ICollection<IDevice>), typeof(Connector), new UIPropertyMetadata(null));

        public ICollection<IDevice> Devices {
            get {
                return (ICollection<IDevice>)GetValue(DevicesProperty);
            }
            set {
                SetValue(DevicesProperty, value);
            }
        }

        public static readonly DependencyProperty HasSetupDialogProperty =
            DependencyProperty.Register(nameof(HasSetupDialog), typeof(bool), typeof(Connector), new UIPropertyMetadata(false));

        public bool HasSetupDialog {
            get {
                return (bool)GetValue(HasSetupDialogProperty);
            }
            set {
                SetValue(HasSetupDialogProperty, value);
            }
        }

        public static readonly DependencyProperty SetupCommandProperty =
            DependencyProperty.Register(nameof(SetupCommand), typeof(ICommand), typeof(Connector), new UIPropertyMetadata(null));

        public ICommand SetupCommand {
            get {
                return (ICommand)GetValue(SetupCommandProperty);
            }
            set {
                SetValue(SetupCommandProperty, value);
            }
        }

        public static readonly DependencyProperty RefreshCommandProperty =
            DependencyProperty.Register(nameof(RefreshCommand), typeof(ICommand), typeof(Connector), new UIPropertyMetadata(null));

        public ICommand RefreshCommand {
            get {
                return (ICommand)GetValue(RefreshCommandProperty);
            }
            set {
                SetValue(RefreshCommandProperty, value);
            }
        }
    }
}
