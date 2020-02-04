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

using NINA.Model.MyCamera;
using NINA.Utility;
using NINA.Profile;
using NINA.Model.ImageData;
using System.Windows.Input;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace NINA.ViewModel {

    public class ImageHistoryVM : DockableVM {

        public ImageHistoryVM(IProfileService profileService) : base(profileService) {
            Title = "LblHFRHistory";

            if (System.Windows.Application.Current != null) {
                ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["HFRHistorySVG"];
            }

            _nextStatHistoryId = 0;
            LimitedImageHistoryStack = new AsyncObservableLimitedSizedStack<ImageHistoryPoint>(100);
            AutoFocusPoints = new AsyncObservableCollection<ImageHistoryPoint>();

            PlotClearCommand = new RelayCommand((object o) => PlotClear());
        }

        private int _nextStatHistoryId;
        private AsyncObservableLimitedSizedStack<ImageHistoryPoint> _limitedImageHistoryStack;

        public AsyncObservableLimitedSizedStack<ImageHistoryPoint> LimitedImageHistoryStack {
            get {
                return _limitedImageHistoryStack;
            }
            set {
                _limitedImageHistoryStack = value;
                RaisePropertyChanged();
            }
        }

        private AsyncObservableCollection<ImageHistoryPoint> autoFocusPoints;

        public AsyncObservableCollection<ImageHistoryPoint> AutoFocusPoints {
            get {
                return autoFocusPoints;
            }
            set {
                autoFocusPoints = value;
                RaisePropertyChanged();
            }
        }

        public List<ImageHistoryPoint> ImageHistory { get; private set; } = new List<ImageHistoryPoint>();

        private object lockObj = new object();

        public void Add(IStarDetectionAnalysis starDetectionAnalysis) {
            if (starDetectionAnalysis != null) {
                lock (lockObj) {
                    var point = new ImageHistoryPoint(Interlocked.Increment(ref _nextStatHistoryId), starDetectionAnalysis);
                    ImageHistory.Add(point);
                    LimitedImageHistoryStack.Add(point);

                    //Clear AF point if stack that is limited to 100 does not contain this point anymore

                    var items = AutoFocusPoints.Except(LimitedImageHistoryStack).ToList();
                    foreach (var item in items) {
                        AutoFocusPoints.Remove(item);
                    }
                }
            }
        }

        public void AppendAutoFocusPoint(AutoFocus.AutoFocusReport report) {
            if (report != null) {
                lock (lockObj) {
                    var last = LimitedImageHistoryStack.LastOrDefault();
                    if (last != null) {
                        last.PopulateAFPoint(report);
                        AutoFocusPoints.Add(last);
                    }
                }
            }
        }

        public void PlotClear() {
            this.LimitedImageHistoryStack.Clear();
            this.AutoFocusPoints.Clear();
        }

        public ICommand PlotClearCommand { get; private set; }

        public class ImageHistoryPoint {

            public ImageHistoryPoint(int id, IStarDetectionAnalysis starDetectionAnalysis) {
                Id = id;
                HFR = starDetectionAnalysis.HFR;
                DetectedStars = starDetectionAnalysis.DetectedStars;
            }

            internal void PopulateAFPoint(AutoFocus.AutoFocusReport report) {
                AutoFocusPoint = new AutoFocusPoint();
                AutoFocusPoint.OldPosition = report.InitialFocusPoint.Position;
                AutoFocusPoint.NewPosition = report.CalculatedFocusPoint.Position;
                AutoFocusPoint.Temperature = report.Temperature;
            }

            public int Id { get; private set; }
            public double Zero { get; } = 0.05;
            public int DetectedStars { get; private set; }

            public AutoFocusPoint AutoFocusPoint { get; private set; }

            public double HFR { get; private set; }
        }

        public class AutoFocusPoint {
            public double NewPosition { get; set; }
            public double OldPosition { get; set; }
            public double Temperature { get; set; }
        }
    }
}