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