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
using NINA.Core.Utility.Extensions;
using NINA.View.Sequencer;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace NINA.Sequencer.Container {

    [Export(typeof(ResourceDictionary))]
    public partial class Datatemplates {

        public Datatemplates() {
            InitializeComponent();
        }

        private void UnitTextBox_Pasting(object sender, DataObjectPastingEventArgs e) {
            try {
                var input = e.DataObject.GetData(typeof(string)) as string;
                if (input is not string) {
                    e.Handled = true;
                    e.CancelCommand();
                    return;
                }
                if (double.TryParse(input, CultureInfo.InvariantCulture, out _)) {
                    return;
                }

                var hmsMatch = Regex.Matches(input, AstroUtil.HMSPattern);

                double raDeg = double.NaN;
                double decDeg = double.NaN;
                if (hmsMatch.Count > 0) {
                    raDeg = AstroUtil.HMSToDegrees(hmsMatch.First().Value);
                    if (hmsMatch.Count > 1) {
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

                if(sender is TextBox tb) {
                    var container = tb.FindParent<HierarchicalSequenceContainerView>();
                    if(container.DataContext is IDeepSkyObjectContainer dsoContainer) {
                        if (double.IsNaN(raDeg)) {
                            // only Dec coordinates in clipboard. Use existing RA
                            raDeg = dsoContainer.Target?.InputCoordinates?.Coordinates?.RADegrees ?? 0;
                        }
                        if (double.IsNaN(decDeg)) {
                            // only RA coordinates in clipboard. Use existing Dec
                            decDeg = dsoContainer.Target?.InputCoordinates?.Coordinates?.Dec ?? 0;
                        }

                        dsoContainer.Target.InputCoordinates = new InputCoordinates() {
                            Coordinates = new Coordinates(Angle.ByDegree(raDeg), Angle.ByDegree(decDeg), Epoch.J2000)
                        };
                    }
                }

                e.Handled = true;
                // Call to cancel command to finish the paste action. Otherwise the clipboard text will end up in the textbox
                e.CancelCommand();
            } catch { }
        }
    }
}