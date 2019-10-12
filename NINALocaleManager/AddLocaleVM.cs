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
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINALocaleManager {

    internal class AddLocaleVM : BaseINPC {
        private IWindowService windowService = new WindowService();

        public AddLocaleVM(ApplicationVM baseVM, ICollection<string> locales) {
            this.baseVM = baseVM;
            Key = "Lbl";

            Entries = new ObservableCollection<AddLocaleEntry>();
            foreach (var locale in locales) {
                Entries.Add(new AddLocaleEntry() { Key = locale, Value = "" });
            }

            OKCommand = new RelayCommand(OK);
            CancelCommand = new RelayCommand(Cancel);

            windowService.Show(this, "Add Locale Entry");
        }

        private void Cancel(object obj) {
            windowService.Close();
        }

        private void OK(object obj) {
            foreach (var entry in Entries) {
                var context = baseVM.Locales.Where(x => x.Name == entry.Key).First();
                context.Entries.Add(new NINALocaleManager.LocaleEntry() { Key = this.Key, Space = entry.Space, Value = entry.Value });
            }
            windowService.Close();
        }

        public ICommand OKCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }

        private string key;
        private ApplicationVM baseVM;

        public string Key {
            get => key;
            set {
                key = value;
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<AddLocaleEntry> Entries { get; private set; }

        public class AddLocaleEntry : BaseINPC {
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