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

using NINA.Model.MyGuider;
using NINA.Utility;
using NINA.Utility.Enum;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NINA.Model {

    public class GuideStepsHistory : BaseINPC {

        public GuideStepsHistory(int historySize) {
            RMS = new RMS();
            PixelScale = 1;
            HistorySize = historySize;
            GuideSteps = new AsyncObservableLimitedSizedStack<IGuideStep>(historySize);
            MaxY = 4;
            MaxDurationY = 1;
        }

        private readonly object lockObj = new object();

        private RMS rMS;

        public RMS RMS {
            get {
                return rMS;
            }
            private set {
                rMS = value;
                RaisePropertyChanged();
            }
        }

        public double Interval {
            get {
                return MaxY / 4;
            }
        }

        private double _maxY;

        public double MaxY {
            get {
                return _maxY;
            }

            set {
                _maxY = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(MinY));
                RaisePropertyChanged(nameof(Interval));
            }
        }

        public double MinY {
            get {
                return -MaxY;
            }
        }

        private double _maxDurationY;

        public double MaxDurationY {
            get {
                return _maxDurationY;
            }

            set {
                _maxDurationY = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(MinDurationY));
            }
        }

        public double MinDurationY {
            get {
                return -MaxDurationY;
            }
        }

        private LinkedList<IGuideStep> overallGuideSteps = new LinkedList<IGuideStep>();

        private AsyncObservableLimitedSizedStack<IGuideStep> guideSteps;

        public AsyncObservableLimitedSizedStack<IGuideStep> GuideSteps {
            get {
                return guideSteps;
            }
            set {
                guideSteps = value;
                RaisePropertyChanged();
            }
        }

        private int historySize;

        public int HistorySize {
            get {
                return historySize;
            }
            set {
                historySize = value;
                RebuildGuideScaleList();
                RaisePropertyChanged();
            }
        }

        private void RebuildGuideScaleList() {
            lock (lockObj) {
                if (overallGuideSteps.Count > 0) {
                    var collection = new LinkedList<IGuideStep>();
                    var startIndex = overallGuideSteps.Count - historySize;
                    if (startIndex < 0) startIndex = 0;
                    RMS.Clear();
                    for (int i = startIndex; i < overallGuideSteps.Count; i++) {
                        var p = overallGuideSteps.ElementAt(i);
                        RMS.AddDataPoint(p.RADistanceRaw, p.DECDistanceRaw);
                        if (Scale == GuiderScaleEnum.ARCSECONDS) {
                            p = ConvertStepToArcSec(p);
                        }
                        collection.AddLast(p);
                    }

                    GuideSteps = new AsyncObservableLimitedSizedStack<IGuideStep>(historySize, collection);
                    CalculateMaximumDurationY();
                }
            }
        }

        private void CalculateMaximumDurationY() {
            MaxDurationY = Math.Abs(GuideSteps.Max((x) => Math.Max(Math.Abs(x.RADuration), Math.Abs(x.DECDuration))));
        }

        private double pixelScale;

        public double PixelScale {
            get {
                return pixelScale;
            }
            set {
                pixelScale = value;
                RMS.SetScale(pixelScale);
                RaisePropertyChanged();
            }
        }

        private GuiderScaleEnum scale;

        public GuiderScaleEnum Scale {
            get {
                return scale;
            }
            set {
                scale = value;
                RebuildGuideScaleList();
                RaisePropertyChanged();
            }
        }

        private IGuideStep ConvertStepToArcSec(IGuideStep step) {
            var newStep = step.Clone();
            // only displayed values are changed, not the raw ones
            newStep.RADistanceRawDisplay = newStep.RADistanceRaw * PixelScale;
            newStep.DECDistanceRawDisplay = newStep.DECDistanceRaw * PixelScale;
            return newStep;
        }

        public void Clear() {
            lock (lockObj) {
                overallGuideSteps.Clear();
                GuideSteps.Clear();
                RMS.Clear();
                MaxDurationY = 1;
            }
        }

        public void AddGuideStep(IGuideStep step) {
            lock (lockObj) {
                overallGuideSteps.AddLast(step);

                if (GuideSteps.Count == HistorySize) {
                    var elementIdx = overallGuideSteps.Count - HistorySize;
                    if (elementIdx >= 0) {
                        var stepToRemove = overallGuideSteps.ElementAt(elementIdx);
                        RMS.RemoveDataPoint(stepToRemove.RADistanceRaw, stepToRemove.DECDistanceRaw);
                    }
                }

                RMS.AddDataPoint(step.RADistanceRaw, step.DECDistanceRaw);

                if (Scale == GuiderScaleEnum.ARCSECONDS) {
                    step = ConvertStepToArcSec(step);
                }
                GuideSteps.Add(step);
                CalculateMaximumDurationY();
            }
        }
    }
}