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

using NINA.Database;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINACustomControlLibrary;
using Nito.AsyncEx;
using Nito.Mvvm;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.ViewModel {

    internal class DeepSkyObjectSearchVM : BaseINPC {

        public DeepSkyObjectSearchVM() : base() {
        }

        private CancellationTokenSource targetSearchCts;

        private NotifyTask<List<IAutoCompleteItem>> targetSearchResult;

        public NotifyTask<List<IAutoCompleteItem>> TargetSearchResult {
            get {
                return targetSearchResult;
            }
            set {
                targetSearchResult = value;
                RaisePropertyChanged();
            }
        }

        public int Limit { get; set; } = 25;

        private bool SkipSearch { get; set; } = false;

        private string targetName;

        public string TargetName {
            get => targetName;
            set {
                ShowPopup = false;
                targetName = value;
                if (!SkipSearch) {
                    if (TargetName.Length > 1) {
                        targetSearchCts?.Cancel();
                        targetSearchCts?.Dispose();
                        targetSearchCts = new CancellationTokenSource();

                        if (TargetSearchResult != null) {
                            TargetSearchResult.PropertyChanged -= TargetSearchResult_PropertyChanged;
                        }
                        TargetSearchResult = NotifyTask.Create(SearchDSOs(TargetName, targetSearchCts.Token));
                        TargetSearchResult.PropertyChanged += TargetSearchResult_PropertyChanged;
                    }
                }
                RaisePropertyChanged();
            }
        }

        private void TargetSearchResult_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(TargetSearchResult.Result)) {
                if (targetSearchResult.Result.Count > 0) {
                    ShowPopup = true;
                } else {
                    ShowPopup = false;
                }
            }
        }

        public void SetTargetNameWithoutSearch(string targetName) {
            this.SkipSearch = true;
            this.TargetName = targetName;
            this.SkipSearch = false;
        }

        private Coordinates coordinates;

        public Coordinates Coordinates {
            get => coordinates;
            set {
                coordinates = value;
                RaisePropertyChanged();
            }
        }

        private IAutoCompleteItem selectedTargetSearchResult;

        public IAutoCompleteItem SelectedTargetSearchResult {
            get {
                return selectedTargetSearchResult;
            }
            set {
                selectedTargetSearchResult = value;
                if (selectedTargetSearchResult != null) {
                    this.SetTargetNameWithoutSearch(selectedTargetSearchResult.Column1);
                    Coordinates = new Coordinates(
                        Astrometry.HMSToDegrees(value.Column2),
                        Astrometry.DMSToDegrees(value.Column3),
                        Epoch.J2000,
                        Coordinates.RAType.Degrees);
                }
                RaisePropertyChanged();
            }
        }

        private bool showPopup;

        public bool ShowPopup {
            get {
                return showPopup;
            }
            set {
                showPopup = value;
                RaisePropertyChanged();
            }
        }

        private class DSOAutoCompleteItem : IAutoCompleteItem {
            public string Column1 { get; set; }

            public string Column2 { get; set; }

            public string Column3 { get; set; }
        }

        private Task<List<IAutoCompleteItem>> SearchDSOs(string searchString, CancellationToken ct) {
            return Task.Run(async () => {
                await Task.Delay(500, ct);
                var db = new DatabaseInteraction();
                var searchParams = new DatabaseInteraction.DeepSkyObjectSearchParams();
                searchParams.ObjectName = searchString;
                searchParams.Limit = Limit;
                var result = await db.GetDeepSkyObjects(string.Empty, searchParams, ct);
                var list = new List<IAutoCompleteItem>();
                foreach (var item in result) {
                    list.Add(new DSOAutoCompleteItem() { Column1 = item.Name, Column2 = item.Coordinates.RAString, Column3 = item.Coordinates.DecString });
                    ct.ThrowIfCancellationRequested();
                }
                return list;
            });
        }
    }
}