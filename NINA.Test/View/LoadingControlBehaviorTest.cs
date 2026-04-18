#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using NINA.CustomControlLibrary;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NINA.Test.View {

    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class LoadingControlBehaviorTest {

        [Test]
        public void DefaultLoadingVisualResources_AreFrozenAndShared() {
            LoadingControl loadingControl = new LoadingControl();
            AsyncProcessButton asyncProcessButton = new AsyncProcessButton();

            loadingControl.LoadingImage.IsFrozen.Should().BeTrue();
            loadingControl.LoadingImageBrush.IsFrozen.Should().BeTrue();
            asyncProcessButton.LoadingImage.Should().BeSameAs(loadingControl.LoadingImage);
            asyncProcessButton.LoadingImageBrush.Should().BeSameAs(loadingControl.LoadingImageBrush);
        }

        [Test]
        public void DefaultTemplate_CachesSpinnerVisualBeforeRotation() {
            LoadingControl loadingControl = new LoadingControl {
                Style = GetLoadingControlStyle(),
                Width = 40,
                Height = 40
            };

            loadingControl.ApplyTemplate();
            loadingControl.Measure(new Size(40, 40));
            loadingControl.Arrange(new Rect(0, 0, 40, 40));
            loadingControl.UpdateLayout();

            ControlTemplate template = loadingControl.Template.Should().NotBeNull().And.BeOfType<ControlTemplate>().Subject;
            Grid spinnerGrid = template.FindName("PART_Grid", loadingControl).Should().BeOfType<Grid>().Subject;
            BitmapCache cache = spinnerGrid.CacheMode.Should().BeOfType<BitmapCache>().Subject;

            cache.EnableClearType.Should().BeFalse();
            cache.RenderAtScale.Should().Be(1d);
            cache.SnapsToDevicePixels.Should().BeFalse();
        }

        private static Style GetLoadingControlStyle() {
            ResourceDictionary resourceDictionary = new ResourceDictionary {
                Source = new Uri("/NINA.CustomControlLibrary;component/Themes/Generic.xaml", UriKind.Relative)
            };

            return resourceDictionary[typeof(LoadingControl)].Should().BeOfType<Style>().Subject;
        }
    }
}
