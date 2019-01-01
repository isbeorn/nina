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

using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Profile;
using NINACustomControlLibrary;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.ViewModel {

    internal class DeepSkyObjectSearchVM : BaseINPC {

        public DeepSkyObjectSearchVM(string databaseLocation) : base() {
            this.databaseLocation = databaseLocation;
        }

        private CancellationTokenSource targetSearchCts;

        private INotifyTaskCompletion<List<IAutoCompleteItem>> targetSearchResult;

        public INotifyTaskCompletion<List<IAutoCompleteItem>> TargetSearchResult {
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
                targetName = value;
                if (!SkipSearch && TargetName.Length > 1) {
                    targetSearchCts?.Cancel();
                    targetSearchCts = new CancellationTokenSource();
                    TargetSearchResult = NotifyTaskCompletion.Create(SearchDSOs(TargetName, targetSearchCts.Token));
                } else {
                    ShowPopup = false;
                }
                RaisePropertyChanged();
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
        private string databaseLocation;

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
                var db = new DatabaseInteraction(databaseLocation);
                var searchParams = new DatabaseInteraction.DeepSkyObjectSearchParams();
                searchParams.ObjectName = searchString;
                searchParams.Limit = Limit;
                var result = await db.GetDeepSkyObjects(string.Empty, searchParams, ct);
                var list = new List<IAutoCompleteItem>();
                foreach (var item in result) {
                    list.Add(new DSOAutoCompleteItem() { Column1 = item.Name, Column2 = item.Coordinates.RAString, Column3 = item.Coordinates.DecString });
                }

                ShowPopup = true;

                return list;
            });
        }
    }
}