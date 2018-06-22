using NINA.Model.MyGuider;
using NINA.Utility;
using NINA.Utility.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                if (overallGuideSteps.Count > 0) {
                    var collection = new LinkedList<IGuideStep>();
                    var startIndex = overallGuideSteps.Count - historySize - 1;
                    if (startIndex < 0) startIndex = 0;
                    RMS.Clear();
                    for (int i = startIndex; i < overallGuideSteps.Count - 1; i++) {
                        var p = overallGuideSteps.ElementAt(i);
                        collection.AddLast(p);
                        RMS.AddDataPoint(p.RADistanceRaw, p.DecDistanceRaw);
                    }

                    GuideSteps = new AsyncObservableLimitedSizedStack<IGuideStep>(historySize, collection);
                }
                RaisePropertyChanged();
            }
        }

        private double pixelScale;

        public double PixelScale {
            get {
                return pixelScale;
            }
            set {
                pixelScale = value;
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
                if (scale == GuiderScaleEnum.ARCSECONDS) {
                    RMS.SetScale(pixelScale);
                } else {
                    RMS.SetScale(1);
                }
                RaisePropertyChanged();
            }
        }

        private void ConvertStepToArcSec(IGuideStep pixelStep) {
            // only displayed values are changed, not the raw ones
            pixelStep.RADistanceRawDisplay = pixelStep.RADistanceRaw * PixelScale;
            pixelStep.DecDistanceRawDisplay = pixelStep.DecDistanceRaw * PixelScale;
            pixelStep.RADistanceGuideDisplay = pixelStep.RADistanceGuide * PixelScale;
            pixelStep.DecDistanceGuideDisplay = pixelStep.DecDistanceGuide * PixelScale;
        }

        private void ConvertStepToPixels(IGuideStep arcsecStep) {
            arcsecStep.RADistanceRawDisplay = arcsecStep.RADistanceRaw / PixelScale;
            arcsecStep.DecDistanceRawDisplay = arcsecStep.DecDistanceRaw / PixelScale;
            arcsecStep.RADistanceGuideDisplay = arcsecStep.RADistanceGuide / PixelScale;
            arcsecStep.DecDistanceGuideDisplay = arcsecStep.DecDistanceGuide / PixelScale;
        }

        public void Clear() {
            overallGuideSteps.Clear();
            GuideSteps.Clear();
            RMS.Clear();
            MaxDurationY = 1;
        }

        public void AddGuideStep(IGuideStep step) {
            if (Scale == GuiderScaleEnum.ARCSECONDS) {
                ConvertStepToArcSec(step);
            }

            overallGuideSteps.AddLast(step);

            if (GuideSteps.Count == HistorySize) {
                var stepToRemove = GuideSteps.FirstOrDefault();
                RMS.RemoveDataPoint(stepToRemove.RADistanceRaw, stepToRemove.DecDistanceRaw);
            }

            GuideSteps.Add(step);
            RMS.AddDataPoint(step.RADistanceRaw, step.DecDistanceRaw);

            if (Math.Abs(step.DECDuration) > MaxDurationY || Math.Abs(step.RADuration) > MaxDurationY) {
                MaxDurationY = Math.Max(Math.Abs(step.RADuration), Math.Abs(step.DECDuration));
            }
        }
    }
}