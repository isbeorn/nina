#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

using System.IO;
using System.Linq;
using Xceed.Wpf.AvalonDock.Layout;

namespace NINA.Utility.AvalonDock {

    internal class LayoutInitializer : ILayoutUpdateStrategy {
        public static string LAYOUTFILEPATH = Path.Combine(Utility.APPLICATIONTEMPPATH, "avalondock.config");

        public bool BeforeInsertAnchorable(LayoutRoot layout, LayoutAnchorable anchorableToShow, ILayoutContainer destinationContainer) {
            //AD wants to add the anchorable into destinationContainer
            //just for test provide a new anchorablepane
            //if the pane is floating let the manager go ahead
            LayoutAnchorablePane destPane = destinationContainer as LayoutAnchorablePane;
            if (destinationContainer != null &&
                destinationContainer.FindParent<LayoutFloatingWindow>() != null)
                return false;

            var toolsPane = layout.Descendents().OfType<LayoutAnchorablePane>().FirstOrDefault(d => d.Name == "ToolsPane");
            if (toolsPane != null) {
                toolsPane.Children.Add(anchorableToShow);
                return true;
            }

            return false;
        }

        public void AfterInsertAnchorable(LayoutRoot layout, LayoutAnchorable anchorableShown) {
        }

        public bool BeforeInsertDocument(LayoutRoot layout, LayoutDocument anchorableToShow, ILayoutContainer destinationContainer) {
            return false;
        }

        public void AfterInsertDocument(LayoutRoot layout, LayoutDocument anchorableShown) {
        }
    }
}