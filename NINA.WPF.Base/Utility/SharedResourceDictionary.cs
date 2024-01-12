#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.WPF.Base.Utility {

    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Text;
    using System.Windows;

    public class SharedResourceDictionary : ResourceDictionary {

        /// <summary>
        /// Internal cache of loaded dictionaries.
        /// </summary>
        private static Dictionary<Uri, WeakReference> sharedDictionaries =
            new Dictionary<Uri, WeakReference>();

        /// <summary>
        /// A value indicating whether the application is in designer mode.
        /// </summary>
        private static bool isInDesignerMode;

        /// <summary>
        /// Local member of the source uri
        /// </summary>
        private Uri sourceUri;

        /// <summary>
        /// Initializes static members of the <see cref="SharedResourceDictionary"/> class.
        /// </summary>
        static SharedResourceDictionary() {
            isInDesignerMode = (bool)DesignerProperties.IsInDesignModeProperty.GetMetadata(typeof(DependencyObject)).DefaultValue;
        }

        /// <summary>
        /// Gets the internal cache of loaded dictionaries.
        /// </summary>
        public static Dictionary<Uri, WeakReference> SharedDictionaries => sharedDictionaries;

        /// <summary>
        /// Gets or sets the uniform resource identifier (URI) to load resources from.
        /// </summary>
        public new Uri Source {
            get => this.sourceUri;

            set {
                this.sourceUri = new Uri(value.OriginalString, UriKind.RelativeOrAbsolute);

                if (!sharedDictionaries.ContainsKey(value) || isInDesignerMode) {
                    base.Source = value;

                    if (!isInDesignerMode) {
                        AddToCache();
                    }
                } else {
                    WeakReference weakReference = sharedDictionaries[sourceUri];
                    if (weakReference != null && weakReference.IsAlive) {
                        MergedDictionaries.Add((ResourceDictionary)weakReference.Target);
                    } else {
                        AddToCache();
                    }
                }
            }
        }

        private void AddToCache() {
            base.Source = sourceUri;
            sharedDictionaries.Remove(sourceUri);
            sharedDictionaries.Add(sourceUri, new WeakReference(this, false));
        }
    }
}