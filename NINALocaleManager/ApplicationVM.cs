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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace NINALocaleManager {

    internal class ApplicationVM : BaseINPC {
        private string solutionPath;

        public ApplicationVM() {
            Locales = new ObservableCollection<Locale>();

            AddCommand = new RelayCommand(Add);
            SaveCommand = new RelayCommand(Save);
            ValidateCommand = new RelayCommand(Validate);
            OrphanCheckCommand = new AsyncCommand<bool>(OrphanCheck);

            solutionPath = Directory.GetParent(Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).FullName).FullName).FullName;
            var localeFolder = Path.Combine(solutionPath, "NINA", "Locale");

            var dirInfo = new DirectoryInfo(localeFolder);
            foreach (var file in dirInfo.GetFiles("*.xaml")) {
                Locales.Add(new Locale(file.FullName));
            }
            SelectedLocale = Locales.First();
        }

        private void Validate(object obj) {
            var validator = new LocaleValidator(this);
            validator.Validate(Locales);
        }

        private Task<bool> OrphanCheck() {
            return Task<bool>.Run(() => {
                var ninaProjectFolder = Path.Combine(solutionPath, "NINA");

                var keys = Locales
                    .SelectMany(
                        x =>
                        x.Entries
                            // Exclude programatically concatenated labels
                            .Where(z => !z.Key.StartsWith("LblConstellation") && !z.Key.StartsWith("LblObjectType"))
                            .Select(y => y.Key)
                     ).Distinct()
                    .ToList();

                var xaml = Directory.GetFiles(ninaProjectFolder, "*.xaml", SearchOption.AllDirectories);
                var cs = Directory.GetFiles(ninaProjectFolder, "*.cs", SearchOption.AllDirectories);

                var files = xaml.Union(cs);

                foreach (var file in files) {
                    if (file.Contains("Locale")) {
                        continue;
                    }

                    Debug.Print($"Reading file {Path.GetFileName(file)}");
                    var text = File.ReadAllText(file);

                    var sw = Stopwatch.StartNew();
                    var containing = keys.Where(key => text.Contains(key));
                    keys = keys.Except(containing).ToList();

                    Debug.Print($"Scanned file in {sw.Elapsed}. Contained {containing.Count()} keys");

                    if (keys.Count == 0) {
                        break;
                    }
                }

                if (keys.Count > 0) {
                    MessageBox.Show(string.Join(Environment.NewLine, keys), "Possible orphaned Labels found!");
                } else {
                    MessageBox.Show("No Orphaned labels. All good!", "Orphan Check");
                }

                return true;
            });
        }

        private void Save(object obj) {
            foreach (var locale in Locales) {
                locale.Save();
            }

            MessageBox.Show("Saved!");
        }

        private void Add(object obj) {
            var context = new AddLocaleVM(this, Locales.Select(x => x.Name).ToList());
        }

        private Locale selectedLocale;

        public Locale SelectedLocale {
            get => selectedLocale;
            set {
                selectedLocale = value;
                RaisePropertyChanged();
            }
        }

        public ICommand SaveCommand { get; private set; }
        public ICommand AddCommand { get; private set; }
        public ICommand ValidateCommand { get; private set; }
        public IAsyncCommand OrphanCheckCommand { get; private set; }

        public ObservableCollection<Locale> Locales { get; private set; }
    }
}