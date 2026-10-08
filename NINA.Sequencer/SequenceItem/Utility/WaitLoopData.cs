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
    using NINA.Core.Locale;
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
        private ComparisonOperatorEnum comparator;
        private IProfileService profileService;
        private static readonly TimeSpan TargetCrossingLifetime = TimeSpan.FromMinutes(5);

        private TargetCrossingRequest cachedRequest;
        private readonly object cacheLock = new();
        private DateTime cacheCreatedUtc;
        private DateTime cacheLastUsedUtc;
        private DateTime cachedEventUtc;

        private bool TryGetTargetCrossing(TargetCrossingRequest request, DateTime utc, out DateTime time) {
            lock (cacheLock) {
                time = cachedEventUtc;
                if (request.SameInputs(cachedRequest) && utc >= cacheLastUsedUtc && utc < cachedEventUtc
                    && utc - cacheCreatedUtc < TargetCrossingLifetime) {
                    cacheLastUsedUtc = utc;
                    return true;
                }

                cachedRequest = null;
                return false;
            }
        }

        private void CacheTargetCrossing(TargetCrossingRequest request, DateTime utc, TargetCrossingResult result) {
            lock (cacheLock) {
                cachedRequest = result.Status == TargetCrossingStatus.Found && result.Time > utc ? request : null;
                cacheCreatedUtc = utc;
                cacheLastUsedUtc = utc;
                cachedEventUtc = result.Time;
            }
        }

        private TargetCrossingRequest TargetRequest(TargetCrossingComparison comparison) =>
            new(
                Coordinates.Coordinates,
                Observer.Latitude,
                Observer.Longitude,
                Observer.Elevation,
                UseCustomHorizon ? Horizon : null,
                Offset,
                comparison);

        private void ClearCurrentTargetPosition() {
            CurrentAltitude = double.NaN;
            TargetAltitude = double.NaN;
            IsRising = false;
        }

        private void ClearTargetPrediction(DateTime utc) {
            ClearCurrentTargetPosition();
            CacheTargetCrossing(null, utc, default);
            ExpectedDateTime = DateTime.MinValue;
            ExpectedTime = "--";
            SetApproximate(false);
        }

        private void SetCurrentTargetPosition(TargetCrossingRequest request, TargetPosition current) {
            CurrentAltitude = current.Altitude;
            IsRising = current.IsRising;
            TargetAltitude = request.TargetAltitude(current);
        }

        internal bool UpdateCurrentTargetPosition(DateTime time) {
            if (Coordinates?.Coordinates == null) {
                return false;
            }

            var request = TargetRequest(TargetCrossingComparison.AboveInclusive);
            if (!request.Valid) {
                ClearCurrentTargetPosition();
                return false;
            }

            var position = request.Position(time.ToUniversalTime());
            if (!position.IsFinite) {
                ClearCurrentTargetPosition();
                return false;
            }

            SetCurrentTargetPosition(request, position);
            return true;
        }

        internal TargetCrossingResult CalculateTargetExpectedTime(DateTime time, TargetCrossingComparison comparison) {
            if (Coordinates?.Coordinates == null) {
                ClearTargetPrediction(time.ToUniversalTime());

                return new TargetCrossingResult(TargetCrossingStatus.Exhausted, default, 0);
            }

            var utc = time.ToUniversalTime();
            var request = TargetRequest(comparison);
            if (!request.Valid) {
                ClearTargetPrediction(utc);
                Logger.Debug($"{Name}: target crossing unresolved because its inputs are invalid");

                return new TargetCrossingResult(TargetCrossingStatus.Exhausted, default, 0);
            }

            var current = request.Position(utc);
            if (!current.IsFinite) {
                ClearTargetPrediction(utc);
                Logger.Debug($"{Name}: target crossing unresolved because the coordinate transform is not finite");

                return new TargetCrossingResult(TargetCrossingStatus.Exhausted, default, 1);
            }

            SetCurrentTargetPosition(request, current);
            SetApproximate(false);
            TargetCrossingResult result;
            // Live state always wins over the cached future prediction.
            if (request.Qualifies(current)) {
                result = new TargetCrossingResult(TargetCrossingStatus.AlreadySatisfied, utc, 1);
                CacheTargetCrossing(request, utc, result);
            } else if (TryGetTargetCrossing(request, utc, out var cachedTime)) {
                result = new TargetCrossingResult(TargetCrossingStatus.Found, cachedTime, 1);
            } else {
                var calculator = new TargetCrossingCalculator(request, TargetCrossingCalculator.MaximumEvaluations - 1);
                result = calculator.Find(utc, current);
                result = result with { Evaluations = result.Evaluations + 1 };
                CacheTargetCrossing(request, utc, result);
            }

            if (result.Status is TargetCrossingStatus.AlreadySatisfied or TargetCrossingStatus.Found) {
                ExpectedDateTime = result.Time.ToLocalTime();
                if (result.Status == TargetCrossingStatus.AlreadySatisfied) {
                    ExpectedTime = Loc.Instance["LblNow"];
                }
            } else {
                ExpectedDateTime = DateTime.MinValue;
                ExpectedTime = "--";
                if (result.Status == TargetCrossingStatus.Exhausted) {
                    Logger.Debug($"{Name}: target crossing unresolved after {result.Evaluations} coordinate evaluations");
                }
            }

            return result;
        }

        public WaitLoopData(IProfileService profileService, bool useCustomHorizon, string name) {
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
        }

        [Obsolete("Don't pass in the delegate. Use the other ctor instead")]
        public WaitLoopData(IProfileService profileService, bool useCustomHorizon, Action calculateExpectedTime, string name) : this(profileService, useCustomHorizon, name) { }

        private WaitLoopData(WaitLoopData cloneMe) : this(cloneMe.profileService, cloneMe.UseCustomHorizon, cloneMe.Name) {
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
                if (offset == value) {
                    return;
                }

                offset = value;
                if (UseCustomHorizon) {
                    SetTargetAltitudeWithHorizon();
                } else {
                    TargetAltitude = value;
                }
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
                currentAltitude = value;
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
            return Math.Round(CalculateTargetAltitudeWithHorizon(when), 2);
        }

        private double CalculateTargetAltitudeWithHorizon(DateTime when) {
            if (Coordinates == null) return 0;
            var horizonAltitude = 0d;
            if (Horizon != null) {
                var altaz = Coordinates.Coordinates.Transform(Angle.ByDegree(Latitude), Angle.ByDegree(Longitude), Elevation, when);
                horizonAltitude = Horizon.GetAltitude(altaz.Azimuth.Degree);
            }
            return horizonAltitude + Offset;
        }

        public void SetTargetAltitudeWithHorizon(DateTime when) {
            TargetAltitude = CalculateTargetAltitudeWithHorizon(when);
        }
    }
}


