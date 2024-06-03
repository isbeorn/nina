#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Astrometry;
using NINA.Core.Utility;
using NINA.Core.Utility.Converters;
using NINA.CustomControlLibrary;
using NINA.Profile;
using NINA.ViewModel.FramingAssistant;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace NINA.View {

    /// <summary>
    /// Interaction logic for FramingAssistantView.xaml
    /// </summary>
    public partial class FramingAssistantView : UserControl {

        public FramingAssistantView() {
            InitializeComponent();
        }

        public void OverlapUnitCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            Binding binding = new Binding("OverlapValue") { Mode = BindingMode.TwoWay };            

            if(e.AddedItems.Count > 0) { 
                if ((string)e.AddedItems[0] == "%") {
                    binding.Converter = new PercentageConverter();
                }
                OverlapValueStepperControl.SetBinding(IntStepperControl.ValueProperty, binding);
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e) {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void UnitTextBox_Pasting(object sender, System.Windows.DataObjectPastingEventArgs e) {
            
            try {
                var input = e.DataObject.GetData(typeof(string)) as string;
                if (input is not string) {
                    e.Handled = true;
                    e.CancelCommand();
                    return;
                }
                if(double.TryParse(input, CultureInfo.InvariantCulture, out _)) {
                    return;
                }

                var hmsMatch = Regex.Matches(input, AstroUtil.HMSPattern);

                double raDeg = double.NaN;
                double decDeg = double.NaN;
                if (hmsMatch.Count > 0) {
                    raDeg = AstroUtil.HMSToDegrees(hmsMatch.First().Value);
                    if(hmsMatch.Count > 1) {
                        decDeg = AstroUtil.HMSToDegrees(hmsMatch.First().Value);
                    }
                }
                
                var dmsMatch = Regex.Matches(input, AstroUtil.DMSPattern);
                if (dmsMatch.Count > 0) {
                    decDeg = AstroUtil.DMSToDegrees(dmsMatch.Last().Value);
                    if (dmsMatch.Count > 1 && double.IsNaN(raDeg)) {
                        raDeg = AstroUtil.DMSToDegrees(dmsMatch.First().Value);
                    }
                }

                if (double.IsNaN(raDeg) && double.IsNaN(decDeg)) {
                    // No coordinates found          
                    e.Handled = true;
                    e.CancelCommand();
                    return;
                }

                var viewModel = this.UC.DataContext as FramingAssistantVM;
                if(double.IsNaN(raDeg)) {
                    // only Dec coordinates in clipboard. Use existing RA
                    raDeg = viewModel.DSO?.Coordinates?.RADegrees ?? 0;
                }
                if(double.IsNaN(decDeg)) {
                    // only RA coordinates in clipboard. Use existing Dec
                    decDeg = viewModel.DSO?.Coordinates?.Dec ?? 0;
                }

                var coordinates = new Coordinates(Angle.ByDegree(raDeg), Angle.ByDegree(decDeg), Epoch.J2000);
                
                var dso = new DeepSkyObject(viewModel.DSO?.Name ?? "", coordinates, null as Func<SkyObjectBase, Task<BitmapSource>>, default);
                dso.RotationPositionAngle = 360 - viewModel.ActiveProfile.FramingAssistantSettings.LastRotationAngle;
                _ = viewModel.SetCoordinates(dso);
                                
                e.Handled = true;
                // Call to cancel command to finish the paste action. Otherwise the clipboard text will end up in the textbox
                e.CancelCommand();
            } catch { }

        }
    }
}