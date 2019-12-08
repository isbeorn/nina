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

using NINA.Utility;
using System;
using System.Globalization;
using System.Resources;
using System.Windows;
using System.Windows.Data;

namespace NINA.Locale {

    public class Loc : BaseINPC, ILoc {
        private ResourceManager _locale;
        private CultureInfo _activeCulture;

        private static readonly Lazy<Loc> lazy =
         new Lazy<Loc>(() => new Loc());

        private Loc() {
            _locale = new ResourceManager("NINA.Locale.Locale", typeof(Loc).Assembly);
        }

        public void ReloadLocale(string culture) {
            using (MyStopWatch.Measure()) {
                try {
                    _activeCulture = new CultureInfo(culture);
                } catch (Exception ex) {
                    Logger.Error(ex);
                }
                RaiseAllPropertiesChanged();
            }
        }

        public static Loc Instance { get { return lazy.Value; } }

        public string this[string key] {
            get {
                if (key == null) {
                    return string.Empty;
                }
                return this._locale?.GetString(key, this._activeCulture) ?? $"MISSING LABEL {key}";
            }
        }
    }

    public class LocExtension : Binding {

        public LocExtension(string name) : base($"[{name}]") {
            this.Mode = BindingMode.OneWay;
            this.Source = Loc.Instance;
        }
    }
}