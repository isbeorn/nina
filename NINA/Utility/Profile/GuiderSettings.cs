using NINA.Utility.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace NINA.Utility.Profile {
    [Serializable()]
    [XmlRoot(nameof(ApplicationSettings))]
    public class GuiderSettings {

        private double ditherPixels = 5;
        [XmlElement(nameof(DitherPixels))]
        public double DitherPixels {
            get {
                return ditherPixels;
            }
            set {
                ditherPixels = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }

        private bool ditherRAOnly = false;
        [XmlElement(nameof(DitherRAOnly))]
        public bool DitherRAOnly {
            get {
                return ditherRAOnly;
            }
            set {
                ditherRAOnly = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }

        private int settleTime = 10;
        [XmlElement(nameof(SettleTime))]
        public int SettleTime {
            get {
                return settleTime;
            }
            set {
                settleTime = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }

        private string pHD2ServerUrl = "localhost";
        [XmlElement(nameof(PHD2ServerUrl))]
        public string PHD2ServerUrl {
            get {
                return pHD2ServerUrl;
            }
            set {
                pHD2ServerUrl = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }

        private int pHD2ServerPort = 4400;
        [XmlElement(nameof(PHD2ServerPort))]
        public int PHD2ServerPort {
            get {
                return pHD2ServerPort;
            }
            set {
                pHD2ServerPort = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());

            }
        }

        private int pHD2LargeHistorySize = 100;
        [XmlElement(nameof(PHD2HistorySize))]
        public int PHD2HistorySize {
            get {
                return pHD2LargeHistorySize;
            }
            set {
                pHD2LargeHistorySize = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                Mediator.Mediator.Instance.Request(new GuideStepHistoryCountMessage() {
                    GuideSteps = pHD2LargeHistorySize,
                    HistoryType = ViewModel.GuiderVM.GuideStepsHistoryType.GuideStepsLarge
                });
            }
        }

        private int pHD2MinimalHistorySize = 10;
        [XmlElement(nameof(PHD2MinimalHistorySize))]
        public int PHD2MinimalHistorySize {
            get {
                return pHD2MinimalHistorySize;
            }
            set {
                pHD2MinimalHistorySize = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
                Mediator.Mediator.Instance.Request(new GuideStepHistoryCountMessage() {
                    GuideSteps = pHD2MinimalHistorySize,
                    HistoryType = ViewModel.GuiderVM.GuideStepsHistoryType.GuideStepsMinimal
                });
            }
        }

        private GuiderScaleEnum pHD2GuiderScale = GuiderScaleEnum.PIXELS;
        [XmlElement(nameof(PHD2GuiderScale))]
        public GuiderScaleEnum PHD2GuiderScale {
            get {
                return pHD2GuiderScale;
            }
            set {
                pHD2GuiderScale = value;
                Mediator.Mediator.Instance.Request(new SaveProfilesMessage());
            }
        }
    }
}
