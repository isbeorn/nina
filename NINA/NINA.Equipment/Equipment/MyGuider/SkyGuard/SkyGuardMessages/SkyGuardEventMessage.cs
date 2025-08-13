using Newtonsoft.Json;
using NINA.Core.Interfaces;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces;
using System;
using System.Runtime.Serialization;

namespace NINA.Equipment.Equipment.MyGuider.SkyGuard.SkyGuardMessages {

    /// <summary>
    /// https://www.innovationsforesight.com/software/help/SKG/SkySurveyorHTML/SKSS_GuidingCorrelationCompleted.html
    /// </summary>
    public class Data {
        public double? GuidingErrorX { get; set; }
        public double? GuidingErrorY { get; set; }
        public double GuidingCorrectionX { get; set; }
        public bool GuidingNoCorrectionX { get; set; }
        public double GuidingCorrectionY { get; set; }
        public bool GuidingNoCorrectionY { get; set; }
        public string Units { get; set; }

    }

    /// <summary>
    /// https://www.innovationsforesight.com/software/help/SKG/SkySurveyorHTML/RESTAPIcallbackreferences.html
    /// </summary>
    public class SkyGuardEventMessage {
        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }

        [JsonProperty(PropertyName = "event")]
        public string Event { get; set; }

        [JsonProperty(PropertyName = "data")]
        public Data Data { get; set; }

        [JsonProperty(PropertyName = "message")]
        public object Message { get; set; }

    }

    [DataContract]
    public class SkyGuardEvent : BaseINPC, IGuideEvent {

        [DataMember]
        public string Event { get; set; }

        [DataMember]
        public string TimeStamp { get; set; }

        [DataMember]
        public string Host { get; set; }

        [DataMember]
        public int Inst { get; set; }
    }

    /// <summary>
    /// The relationship between SkyGuard events and the IGuideStep interface
    /// </summary>
    [DataContract]
    public class SkyGuardEventGuideStep : SkyGuardEvent, IGuideStep {

        [DataMember]
        private string status;

        [DataMember]
        private double frame;

        [DataMember]
        private double time;

        [DataMember]
        private double rADistanceRaw;

        [DataMember]
        private double decDistanceRaw;

        [DataMember]
        private double rADuration;

        [DataMember]
        private double dECDuration;

        [DataMember]
        private string units;

        public SkyGuardEventGuideStep() {
        }

        [DataMember]
        public string Status {
            get => status;

            set => status = value;
        }

        [DataMember]
        public double Frame {
            get => frame;

            set => frame = value;
        }

        [DataMember]
        public double Time {
            get => time;

            set => time = DateTime.UtcNow
               .Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))
               .TotalSeconds;
        }


        [DataMember]
        public double RADistanceRaw {
            get => rADistanceRaw;

            set {
                rADistanceRaw = value;
            }
        }

        [DataMember]
        public double DECDistanceRaw {
            get => decDistanceRaw;

            set => decDistanceRaw = value;
        }



        [DataMember]
        public double RADuration {
            get => rADuration;

            set => rADuration = value;
        }



        [DataMember]
        public double DECDuration {
            get => dECDuration;

            set => dECDuration = value;
        }

        public class SkyGuardEventAppState : SkyGuardEventMessage, IGuiderAppState {
            private string state;

            public string State {
                get => state;

                set => state = value;
            }
        }

        private SkyGuardEventAppState _appState;

        public SkyGuardEventAppState AppState {
            get => _appState;
            set {
                _appState = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(State));
            }
        }

        public string State => AppState?.State ?? string.Empty;

        [DataMember]
        public string Units {
            get => units;

            set => units = value;
        }

        public IGuideStep Clone() {
            return (SkyGuardEventGuideStep)this.MemberwiseClone();
        }
    }
}

