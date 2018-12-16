#region "copyright"

/*
    Copyright © 2016 - 2018 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

using System.Windows;
using System.Windows.Controls;

namespace NINA.View {

    /// <summary>
    /// Interaction logic for MeridianFlipView.xaml
    /// </summary>
    public partial class MeridianFlipView : UserControl {

        public MeridianFlipView() {
            InitializeComponent();
        }
    }

    public class MeridianFlipDataTemplateSelector : DataTemplateSelector {
        public DataTemplate RecenterTemplate { get; set; }
        public DataTemplate PassMeridianTemplate { get; set; }
        public DataTemplate DefaultTemplate { get; set; }
        public DataTemplate FlipDataTemplate { get; set; }
        public DataTemplate SettleTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item,
                   DependencyObject container) {
            ViewModel.WorkflowStep step = item as ViewModel.WorkflowStep;
            if (step.Id == "PassMeridian") {
                return PassMeridianTemplate;
            }
            if (step.Id == "StopAutoguider") {
                return DefaultTemplate;
            }
            if (step.Id == "Flip") {
                return FlipDataTemplate;
            }
            if (step.Id == "Recenter") {
                return RecenterTemplate;
            }
            if (step.Id == "ResumeAutoguider") {
                return DefaultTemplate;
            }
            if (step.Id == "Settle") {
                return SettleTemplate;
            }

            return DefaultTemplate;
        }
    }
}