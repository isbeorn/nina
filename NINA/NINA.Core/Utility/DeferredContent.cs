#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Windows;

namespace NINA.Core.Utility {

    public class DeferredContent {

        public static readonly DependencyProperty ContentProperty =
        DependencyProperty.RegisterAttached(
            "Content",
            typeof(object),
            typeof(DeferredContent),
            new PropertyMetadata());

        public static object GetContent(DependencyObject obj) {
            return obj.GetValue(ContentProperty);
        }

        public static void SetContent(DependencyObject obj, object value) {
            obj.SetValue(ContentProperty, value);
        }
    }
}