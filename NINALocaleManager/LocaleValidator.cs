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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace NINALocaleManager {

    internal class LocaleValidator : BaseINPC {
        private IWindowService windowService = new WindowService();

        public LocaleValidator(ApplicationVM baseVM) {
            this.baseVM = baseVM;
        }

        public void Validate(ICollection<Locale> locales) {
            var keys = locales.SelectMany(x => x.Entries.Select(y => y.Key)).Distinct();
            var missingKeys = new Dictionary<string, List<FixLocale>>();

            foreach (var locale in locales) {
                var localeKeys = locale.Entries.Select(x => x.Key);

                var detectedKeys = keys.Where(item => !localeKeys.Any(item2 => item2 == item));
                if (detectedKeys.Count() > 0) {
                    missingKeys[locale.Name] = new List<FixLocale>();
                    foreach (var key in detectedKeys) {
                        missingKeys[locale.Name].Add(new FixLocale() { Key = key, Value = string.Empty });
                    }
                }
            }

            if (missingKeys.Count > 0) {
                MissingKeys = new Dictionary<string, List<FixLocale>>(missingKeys);
                windowService.Show(this, "Fix missing locale");
            } else {
                MessageBox.Show("No missing labels found");
            }

            OKCommand = new RelayCommand(OK);
            CancelCommand = new RelayCommand(Cancel);
        }

        private void Cancel(object obj) {
            windowService.Close();
        }

        private void OK(object obj) {
            foreach (var locale in MissingKeys) {
                var rootLocale = baseVM.Locales.Where(x => x.Name == locale.Key).First();

                foreach (var fix in locale.Value) {
                    if (!string.IsNullOrWhiteSpace(fix.Value)) {
                        rootLocale.Entries.Add(new LocaleEntry() {
                            Key = fix.Key,
                            Space = fix.Space,
                            Value = fix.Value
                        });
                    }
                }
            }
            windowService.Close();
        }

        public ICommand OKCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }

        private Dictionary<string, List<FixLocale>> missingKeys;
        private ApplicationVM baseVM;

        public Dictionary<string, List<FixLocale>> MissingKeys {
            get => missingKeys;
            set {
                missingKeys = value;
                RaisePropertyChanged();
            }
        }

        public class FixLocale : BaseINPC {
            private string key;

            public string Key {
                get => key;
                set {
                    key = value;
                    RaisePropertyChanged();
                }
            }

            public string Space { get; private set; } = "default";

            private string value;

            public string Value {
                get => value;
                set {
                    this.value = value;
                    if (this.value.Contains(Environment.NewLine)) {
                        Space = "preserve";
                    }
                    RaisePropertyChanged();
                }
            }
        }
    }
}