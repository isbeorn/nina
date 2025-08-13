using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Sequencer.SequenceItem.Utility {
    using Newtonsoft.Json;
    using NINA.Astrometry;
    using NINA.Core.Enum;
    using NINA.Core.Model;
    using NINA.Core.Utility;
    using NINA.Profile.Interfaces;

    [JsonObject(MemberSerialization.OptIn)]
    public class WaitLoopData : BaseINPC {

        private double targetAltitude;
        private double offset;
        private double currentAltitude;
        private string risingSettingDisplay;
        private string expectedTime;
        private string approximate = "";
        private DateTime expectedDateTime = DateTime.Now;
        private Action calculateExpectedTime;
        private ComparisonOperatorEnum comparator;
        private IProfileService profileService;

        public WaitLoopData(IProfileService profileService, bool useCustomHorizon, Action calculateExpectedTime, string name) {
            this.profileService = profileService;
            Latitude = profileService.ActiveProfile.AstrometrySettings.Latitude;
            Longitude = profileService.ActiveProfile.AstrometrySettings.Longitude;
            Horizon = profileService.ActiveProfile.AstrometrySettings.Horizon;
            Elevation = profileService.ActiveProfile.AstrometrySettings.Elevation;
            Observer = new ObserverInfo() { Latitude = Latitude, Longitude = Longitude, Elevation = Elevation };
            ExpectedDateTime = DateTime.MinValue;
            Coordinates = new InputCoordinates();
            Name = name;
            UseCustomHorizon = useCustomHorizon;
            this.calculateExpectedTime = calculateExpectedTime;
        }

        private WaitLoopData(WaitLoopData cloneMe) : this(cloneMe.profileService, cloneMe.UseCustomHorizon, cloneMe.calculateExpectedTime, cloneMe.Name) {
        }

        public WaitLoopData Clone() {
            return new WaitLoopData(this) {
                Coordinates = Coordinates == null ? new InputCoordinates() : Coordinates.Clone(),
                Offset = Offset,
                Comparator = Comparator,
                Name = Name,
                UseCustomHorizon = UseCustomHorizon
            };
        }
        [JsonProperty]
        public InputCoordinates Coordinates { get; set; }

        /// <summary>
        /// The Offset is the user input for the desired result
        /// For Horzions this is [current horizon] + offset => target horizon
        /// For Altitudes this is [0] + offset => target altitude
        /// </summary>
        [JsonProperty]
        public double Offset {
            get => offset;
            set {
                offset = value;
                if (UseCustomHorizon) {
                    SetTargetAltitudeWithHorizon();
                } else {
                    TargetAltitude = value;
                }
                calculateExpectedTime();
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public ComparisonOperatorEnum Comparator {
            get {
                // Backward compatibility
                if (comparator == ComparisonOperatorEnum.EQUALS || comparator == ComparisonOperatorEnum.NOT_EQUAL) {
                    comparator = ComparisonOperatorEnum.GREATER_THAN;
                }
                return comparator;
            }
            set {
                if (comparator == value) return;
                comparator = value;
                RaisePropertyChanged();
                calculateExpectedTime();
            }
        }

        public bool UseCustomHorizon { get; private set; }
        public string Name { get; private set; }
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }
        public double Elevation { get; private set; }
        public CustomHorizon Horizon { get; set; }
        public ObserverInfo Observer { get; private set; }
        
        public double TargetAltitude {
            get => targetAltitude;
            set {
                if (targetAltitude != value) {
                    targetAltitude = value;
                    // While "thinking" we show this. 
                    if(!ExpectedTime.EndsWith("\u231B")) {
                        ExpectedTime = ExpectedTime + "\u231B";
                    }
                    RaisePropertyChanged();
                }
            }
        }
                
        public ComparisonOperatorEnum[] ComparisonOperators => Enum.GetValues(typeof(ComparisonOperatorEnum))
            .Cast<ComparisonOperatorEnum>()
            .Where(p => p != ComparisonOperatorEnum.GREATER_THAN_OR_EQUAL)
            .Where(p => p != ComparisonOperatorEnum.LESS_THAN_OR_EQUAL)
            .Where(p => p != ComparisonOperatorEnum.EQUALS)
            .Where(p => p != ComparisonOperatorEnum.NOT_EQUAL)
            .ToArray();


        public void SetCoordinates(InputCoordinates coordinates) {
            // Don't do anything if we're really not changing coordinates
            // Otherwise, we'll reset expected time to midnight, etc.
            if (Coordinates == coordinates) return;
            Coordinates = coordinates;
            ExpectedDateTime = DateTime.MinValue;
        }


        public string RisingSettingDisplay {
            get => risingSettingDisplay;
            set {
                risingSettingDisplay = value;
                RaisePropertyChanged();
            }
        }

        private bool isRising;
        public bool IsRising {
            get => isRising;
            set {
                isRising = value;
                RisingSettingDisplay = isRising ? "\u2197" : "\u2198";
            }
        }

        public double CurrentAltitude {
            get => currentAltitude;
            set {
                currentAltitude = Math.Round(value, 2);
                RaisePropertyChanged();
            }
        }

        public string Approximate {
            get => approximate;
            set {
                approximate = value;
                RaisePropertyChanged();
            }
        }

        public void SetApproximate(bool isApproximate) {
            Approximate = isApproximate ? "\u2248" : "";
        }

        public DateTime ExpectedDateTime {
            get => expectedDateTime;
            set {
                expectedDateTime = value;
                ExpectedTime = value.ToString("t");
            }
        }

        public string ExpectedTime {
            get => expectedTime;
            set {
                expectedTime = value;
                RaisePropertyChanged();
            }
        }

        public void SetTargetAltitudeWithHorizon() {
            SetTargetAltitudeWithHorizon(DateTime.Now);
        }

        public double GetTargetAltitudeWithHorizon(DateTime when) {
            if (Coordinates == null) return 0;
            var altaz = Coordinates.Coordinates.Transform(Angle.ByDegree(Latitude), Angle.ByDegree(Longitude), Elevation, when);
            var horizonAltitude = 0d;
            if (Horizon != null) {
                horizonAltitude = Horizon.GetAltitude(altaz.Azimuth.Degree);
            }
            return Math.Round(horizonAltitude + Offset, 2);
        }
        public void SetTargetAltitudeWithHorizon(DateTime when) {
            TargetAltitude = GetTargetAltitudeWithHorizon(when);
        }
    }
}


