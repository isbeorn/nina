#region "copyright"
/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using NINA.Astrometry;
using NINA.Astrometry.Interfaces;
using NINA.Core.Enum;
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.SkySurvey;
using NINA.WPF.Base.ViewModel;
using OxyPlot.Axes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.ViewModel {

    internal partial class SkyAtlasVM : BaseVM, ISkyAtlasVM {

        public SkyAtlasVM(IProfileService profileService, ITelescopeMediator telescopeMediator,
            IFramingAssistantVM framingAssistantVM, ISequenceMediator sequenceMediator, INighttimeCalculator nighttimeCalculator, IApplicationMediator applicationMediator) : base(profileService) {
            this.nighttimeCalculator = nighttimeCalculator;

            SearchCommand = new AsyncCommand<bool>(() => Search());
            CancelSearchCommand = new RelayCommand(CancelSearch);
            ResetFiltersCommand = new RelayCommand(ResetFilters);
            GetDSOTemplatesCommand = new RelayCommand((object o) => {
                DSOTemplates = sequenceMediator.GetDeepSkyObjectContainerTemplates();
                RaisePropertyChanged(nameof(DSOTemplates));
            });
            SetOldSequencerTargetCommand = new RelayCommand((object o) => {
                applicationMediator.ChangeTab(ApplicationTab.SEQUENCE);
                var dso = SearchResult.SelectedItem;
                dso.RotationPositionAngle = 360 - profileService.ActiveProfile.FramingAssistantSettings.LastRotationAngle;
                sequenceMediator.AddSimpleTarget(dso);
            });
            SetSequencerTargetCommand = new RelayCommand((object o) => {
                applicationMediator.ChangeTab(ApplicationTab.SEQUENCE);

                var template = o as IDeepSkyObjectContainer;

                var container = (IDeepSkyObjectContainer)template.Clone();
                container.Name = SearchResult.SelectedItem.Name;
                container.Target.TargetName = SearchResult.SelectedItem.Name;
                container.Target.PositionAngle = 360 - profileService.ActiveProfile.FramingAssistantSettings.LastRotationAngle;
                container.Target.InputCoordinates.Coordinates = SearchResult.SelectedItem.Coordinates;

                sequenceMediator.AddAdvancedTarget(container);
            });
            SlewToCoordinatesCommand = new AsyncCommand<bool>(async () => {
                return await telescopeMediator.SlewToCoordinatesAsync(SearchResult.SelectedItem.Coordinates, CancellationToken.None);
            });
            SetFramingAssistantCoordinatesCommand = new AsyncCommand<bool>(async () => {
                applicationMediator.ChangeTab(ApplicationTab.FRAMINGASSISTANT);
                var dso = SearchResult.SelectedItem;
                dso.RotationPositionAngle = 360 - profileService.ActiveProfile.FramingAssistantSettings.LastRotationAngle;
                return await framingAssistantVM.SetCoordinates(dso);
            });

            Task.Run(() => { NighttimeData = this.nighttimeCalculator.Calculate();  InitializeFilters(); ResetFilters(null); });

            profileService.LocationChanged += (object sender, EventArgs e) => {
                NighttimeData = this.nighttimeCalculator.Calculate();
                InitializeElevationFilters();
            };

            profileService.LocaleChanged += (object sender, EventArgs e) => {
                InitializeFilters();
                ResetFilters(null);
            };
            profileService.HorizonChanged += ProfileService_HorizonChanged;
            profileService.LocationChanged += ProfileService_LocationChanged;
            nighttimeCalculator.OnReferenceDayChanged += NighttimeCalculator_OnReferenceDayChanged;
        }

        private void ProfileService_LocationChanged(object sender, EventArgs e) {
            try {
                SearchResult = null;
                NighttimeCalculator_OnReferenceDayChanged(sender, e);
                InitializeFilters(); 
                ResetFilters(null);
            } catch { }
        }

        private void ProfileService_HorizonChanged(object sender, EventArgs e) {
            try {
                SearchResult = null;
            } catch { }
        }

        private void NighttimeCalculator_OnReferenceDayChanged(object sender, EventArgs e) {
            NighttimeData = nighttimeCalculator.Calculate();
            RaisePropertyChanged(nameof(NighttimeData));
        }

        private NighttimeData nighttimeData;

        public NighttimeData NighttimeData {
            get => nighttimeData;
            set {
                if (nighttimeData != value) {
                    nighttimeData = value;
                    RaisePropertyChanged();
                }
            }
        }

        public IList<IDeepSkyObjectContainer> DSOTemplates { get; private set; }

        private DateTime filterDate = NighttimeCalculator.GetReferenceDate(DateTime.Now);

        public DateTime FilterDate {
            get => filterDate;
            set {
                if(value.Hour == 0) {
                    value = value.AddHours(12);
                }
                value = NighttimeCalculator.GetReferenceDate(value);
                if (filterDate != value) {
                    filterDate = value;

                    var doSearch = SearchResult != null;
                    SearchResult = null;

                    SoftResetFilters();
                    RaisePropertyChanged();

                    if(doSearch) {
                        // The date was changed and a search result was already there. Do a search again with the adjusted date.
                        SearchCommand.Execute(null);
                    }
                }
            }
        }

        /// <summary>
        /// Re-evaluate items when FilterDate changes
        /// </summary>
        private void SoftResetFilters() {
            var prevAlt = SelectedMinimumAltitudeDegrees;
            var prevDuration = SelectedAltitudeDuration;
            var prevTimeFrom = SelectedAltitudeTimeFrom;
            var prevTimeThrough = SelectedAltitudeTimeThrough;
            var prevTransitTimeFrom = SelectedTransitTimeFrom;
            var prevTransitTimeThrough = SelectedTransitTimeThrough;
            var oldDusk = NighttimeData.SunRiseAndSet.Set.HasValue ? NighttimeData.SunRiseAndSet.Set.Value : DateTime.MinValue;
            var oldDawn = NighttimeData.SunRiseAndSet.Rise.HasValue ? NighttimeData.SunRiseAndSet.Rise.Value : DateTime.MinValue;
            var oldNauticalDusk = NighttimeData.NauticalTwilightRiseAndSet.Set.HasValue ? NighttimeData.NauticalTwilightRiseAndSet.Set.Value : DateTime.MinValue;
            var oldNauticalDawn = NighttimeData.NauticalTwilightRiseAndSet.Rise.HasValue ? NighttimeData.NauticalTwilightRiseAndSet.Rise.Value : DateTime.MinValue;
            var oldAstroDusk = NighttimeData.TwilightRiseAndSet.Set.HasValue ? NighttimeData.TwilightRiseAndSet.Set.Value : DateTime.MinValue;
            var oldAstroDawn = NighttimeData.TwilightRiseAndSet.Rise.HasValue ? NighttimeData.TwilightRiseAndSet.Rise.Value : DateTime.MinValue;

            NighttimeData = this.nighttimeCalculator.Calculate(FilterDate);
            
            InitializeElevationFilters();
            SelectedMinimumAltitudeDegrees = prevAlt;
            SelectedAltitudeDuration = prevDuration;

            if(prevTimeFrom == oldDusk) {
                if (NighttimeData.SunRiseAndSet.Set.HasValue) {
                    SelectedAltitudeTimeFrom = NighttimeData.SunRiseAndSet.Set.Value;
                } else {
                    SelectedAltitudeTimeFrom = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 12, 0, 0);
                }
            } else if(prevTimeFrom == oldNauticalDusk) {
                if (NighttimeData.NauticalTwilightRiseAndSet.Set.HasValue) {
                    SelectedAltitudeTimeFrom = NighttimeData.NauticalTwilightRiseAndSet.Set.Value;
                } else {
                    if (NighttimeData.SunRiseAndSet.Set.HasValue) {
                        SelectedAltitudeTimeFrom = NighttimeData.SunRiseAndSet.Set.Value;
                    } else {
                        SelectedAltitudeTimeFrom = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 12, 0, 0);
                    }
                }
            } else if(prevTimeFrom == oldAstroDusk) {
                if (NighttimeData.TwilightRiseAndSet.Set.HasValue) {
                    SelectedAltitudeTimeFrom = NighttimeData.TwilightRiseAndSet.Set.Value;
                } else {
                    if (NighttimeData.NauticalTwilightRiseAndSet.Set.HasValue) {
                        SelectedAltitudeTimeFrom = NighttimeData.NauticalTwilightRiseAndSet.Set.Value;
                    } else {
                        if (NighttimeData.SunRiseAndSet.Set.HasValue) {
                            SelectedAltitudeTimeFrom = NighttimeData.SunRiseAndSet.Set.Value;
                        } else {
                            SelectedAltitudeTimeFrom = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 12, 0, 0);
                        }
                    }
                }
            } else {
                SelectedAltitudeTimeFrom = prevTimeFrom;
            }

            if (prevTimeThrough == oldDawn) {
                if (NighttimeData.SunRiseAndSet.Rise.HasValue) {
                    SelectedAltitudeTimeThrough = NighttimeData.SunRiseAndSet.Rise.Value;
                } else {
                    SelectedAltitudeTimeThrough = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 11, 0, 0);
                }
            } else if(prevTimeThrough == oldNauticalDawn) {
                if (NighttimeData.NauticalTwilightRiseAndSet.Rise.HasValue) {
                    SelectedAltitudeTimeThrough = NighttimeData.NauticalTwilightRiseAndSet.Rise.Value;
                } else {
                    if (NighttimeData.SunRiseAndSet.Rise.HasValue) {
                        SelectedAltitudeTimeThrough = NighttimeData.SunRiseAndSet.Rise.Value;
                    } else {
                        SelectedAltitudeTimeThrough = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 11, 0, 0);
                    }
                }
            } else if(prevTimeThrough == oldAstroDawn) {
                if (NighttimeData.TwilightRiseAndSet.Rise.HasValue) {
                    SelectedAltitudeTimeThrough = NighttimeData.TwilightRiseAndSet.Rise.Value;
                } else {
                    if (NighttimeData.NauticalTwilightRiseAndSet.Rise.HasValue) {
                        SelectedAltitudeTimeThrough = NighttimeData.NauticalTwilightRiseAndSet.Rise.Value;
                    } else {
                        if (NighttimeData.SunRiseAndSet.Rise.HasValue) {
                            SelectedAltitudeTimeThrough = NighttimeData.SunRiseAndSet.Rise.Value;
                        } else {
                            SelectedAltitudeTimeThrough = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 11, 0, 0);
                        }
                    }
                }
            } else {
                SelectedAltitudeTimeThrough = prevTimeThrough;
            }

            // TransitTime Soft Reset
            if (prevTransitTimeFrom == oldDusk) {
                if (NighttimeData.SunRiseAndSet.Set.HasValue) {
                    SelectedTransitTimeFrom = NighttimeData.SunRiseAndSet.Set.Value;
                } else {
                    SelectedTransitTimeFrom = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 12, 0, 0);
                }
            } else if (prevTransitTimeFrom == oldNauticalDusk) {
                if (NighttimeData.NauticalTwilightRiseAndSet.Set.HasValue) {
                    SelectedTransitTimeFrom = NighttimeData.NauticalTwilightRiseAndSet.Set.Value;
                } else {
                    if (NighttimeData.SunRiseAndSet.Set.HasValue) {
                        SelectedTransitTimeFrom = NighttimeData.SunRiseAndSet.Set.Value;
                    } else {
                        SelectedTransitTimeFrom = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 12, 0, 0);
                    }
                }
            } else if (prevTransitTimeFrom == oldAstroDusk) {
                if (NighttimeData.TwilightRiseAndSet.Set.HasValue) {
                    SelectedTransitTimeFrom = NighttimeData.TwilightRiseAndSet.Set.Value;
                } else {
                    if (NighttimeData.NauticalTwilightRiseAndSet.Set.HasValue) {
                        SelectedTransitTimeFrom = NighttimeData.NauticalTwilightRiseAndSet.Set.Value;
                    } else {
                        if (NighttimeData.SunRiseAndSet.Set.HasValue) {
                            SelectedTransitTimeFrom = NighttimeData.SunRiseAndSet.Set.Value;
                        } else {
                            SelectedTransitTimeFrom = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 12, 0, 0);
                        }
                    }
                }
            } else {
                SelectedTransitTimeFrom = prevTransitTimeFrom;
            }

            if (prevTransitTimeThrough == oldDawn) {
                if (NighttimeData.SunRiseAndSet.Rise.HasValue) {
                    SelectedTransitTimeThrough = NighttimeData.SunRiseAndSet.Rise.Value;
                } else {
                    SelectedTransitTimeThrough = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 11, 0, 0);
                }
            } else if (prevTransitTimeThrough == oldNauticalDawn) {
                if (NighttimeData.NauticalTwilightRiseAndSet.Rise.HasValue) {
                    SelectedTransitTimeThrough = NighttimeData.NauticalTwilightRiseAndSet.Rise.Value;
                } else {
                    if (NighttimeData.SunRiseAndSet.Rise.HasValue) {
                        SelectedTransitTimeThrough = NighttimeData.SunRiseAndSet.Rise.Value;
                    } else {
                        SelectedTransitTimeThrough = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 11, 0, 0);
                    }
                }
            } else if (prevTransitTimeThrough == oldAstroDawn) {
                if (NighttimeData.TwilightRiseAndSet.Rise.HasValue) {
                    SelectedTransitTimeThrough = NighttimeData.TwilightRiseAndSet.Rise.Value;
                } else {
                    if (NighttimeData.NauticalTwilightRiseAndSet.Rise.HasValue) {
                        SelectedTransitTimeThrough = NighttimeData.NauticalTwilightRiseAndSet.Rise.Value;
                    } else {
                        if (NighttimeData.SunRiseAndSet.Rise.HasValue) {
                            SelectedTransitTimeThrough = NighttimeData.SunRiseAndSet.Rise.Value;
                        } else {
                            SelectedTransitTimeThrough = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 11, 0, 0);
                        }
                    }
                }
            } else {
                SelectedTransitTimeThrough = prevTransitTimeThrough;
            }
            // end Transit Soft Reset


        }

        private void ResetFilters(object obj) {
            if(obj is DateTime d) {
                FilterDate = NighttimeCalculator.GetReferenceDate(d);
            } else {
                FilterDate = NighttimeCalculator.GetReferenceDate(DateTime.Now);
            }            

            SearchObjectName = string.Empty;

            foreach (var objecttype in ObjectTypes) {
                objecttype.Selected = false;
            }

            ResetAltitudeTimeFilters();
            SelectedAltitudeDuration = 1;
            SelectedMinimumAltitudeDegrees = 0;

            SelectedBrightnessFrom = null;
            SelectedBrightnessThrough = null;

            SelectedConstellation = string.Empty;

            SelectedDecFrom = null;
            SelectedDecThrough = null;

            SelectedMagnitudeFrom = null;
            SelectedMagnitudeThrough = null;

            SelectedRAFrom = null;
            SelectedRAThrough = null;

            SelectedSizeFrom = null;
            SelectedSizeThrough = null;

            SelectedMinimumMoonDistanceDegrees = 0;

            SelectedTransitTimeFrom = DateTime.MinValue;
            SelectedTransitTimeThrough = DateTime.MaxValue;

            OrderByField = SkyAtlasOrderByFieldsEnum.SIZEMAX;
            OrderByDirection = SkyAtlasOrderByDirectionEnum.DESC;
        }

        private void ResetAltitudeTimeFilters() {
            if (NighttimeData.TwilightRiseAndSet.Set.HasValue) {
                SelectedAltitudeTimeFrom = NighttimeData.TwilightRiseAndSet.Set.Value;
            } else {
                if (NighttimeData.NauticalTwilightRiseAndSet.Set.HasValue) {
                    SelectedAltitudeTimeFrom = NighttimeData.NauticalTwilightRiseAndSet.Set.Value;
                } else {
                    if (NighttimeData.SunRiseAndSet.Set.HasValue) {
                        SelectedAltitudeTimeFrom = NighttimeData.SunRiseAndSet.Set.Value;
                    } else {
                        SelectedAltitudeTimeFrom = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 12, 0, 0);
                    }
                }
            }
            if (NighttimeData.TwilightRiseAndSet.Rise.HasValue) {
                SelectedAltitudeTimeThrough = NighttimeData.TwilightRiseAndSet.Rise.Value;
            } else {
                if (NighttimeData.NauticalTwilightRiseAndSet.Rise.HasValue) {
                    SelectedAltitudeTimeThrough = NighttimeData.NauticalTwilightRiseAndSet.Rise.Value;
                } else {
                    if (NighttimeData.SunRiseAndSet.Rise.HasValue) {
                        SelectedAltitudeTimeThrough = NighttimeData.SunRiseAndSet.Rise.Value;
                    } else {
                        SelectedAltitudeTimeThrough = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 11, 0, 0);
                    }
                }
            }
        }

        private CancellationTokenSource _searchTokenSource;

        private void CancelSearch(object obj) {
            try { _searchTokenSource?.Cancel(); } catch { }
        }

        private readonly INighttimeCalculator nighttimeCalculator;

        private async Task<bool> Search() {
            _searchTokenSource?.Dispose();
            _searchTokenSource = new CancellationTokenSource();
            return await Task.Run(async () => {
                try {
                    SearchResult = null;
                    var db = new DatabaseInteraction();
                    var types = ObjectTypes.Where((x) => x.Selected).Select((x) => x.Name).ToList();

                    var searchParams = new DatabaseInteraction.DeepSkyObjectSearchParams();
                    searchParams.Constellation = SelectedConstellation;
                    searchParams.DsoTypes = types;
                    searchParams.ObjectName = SearchObjectName;
                    searchParams.RightAscension.From = SelectedRAFrom;
                    searchParams.RightAscension.Thru = SelectedRAThrough;
                    searchParams.Declination.From = Nullable.Compare(SelectedDecFrom, SelectedDecThrough) > 0 ? SelectedDecThrough : SelectedDecFrom;
                    searchParams.Declination.Thru = Nullable.Compare(SelectedDecFrom, SelectedDecThrough) > 0 ? SelectedDecFrom : SelectedDecThrough;
                    searchParams.Brightness.From = SelectedBrightnessFrom;
                    searchParams.Brightness.Thru = SelectedBrightnessThrough;
                    searchParams.Magnitude.From = SelectedMagnitudeFrom;
                    searchParams.Magnitude.Thru = SelectedMagnitudeThrough;
                    searchParams.Size.From = SelectedSizeFrom;
                    searchParams.Size.Thru = SelectedSizeThrough;
                    searchParams.SearchOrder.Direction = OrderByDirection.ToString();

                    // handling for ordering by TRANSITTIME as is not a DB field
                    if (OrderByField == SkyAtlasOrderByFieldsEnum.TRANSITTIME) {
                        searchParams.SearchOrder.Field = nameof(SkyAtlasOrderByFieldsEnum.RA).ToLower();
                    } else {
                        searchParams.SearchOrder.Field = OrderByField.ToString().ToLower();
                    }

                    var cache = new CacheSkySurvey(profileService.ActiveProfile.ApplicationSettings.SkySurveyCacheDirectory);                    
                    // Keep one renderer per search. Recreating the renderer and forcing a GC for every DSO made
                    // result materialization and row image loading noticeably more expensive.
                    var cacheSkySurveyImageFactory = new CacheSkySurveyImageFactory(400, 190, cache);
                    var imageFactory = (SkyObjectBase obj) => {
                        var image = cacheSkySurveyImageFactory.Render(obj.Coordinates, AstroUtil.ArcsecToDegree(Math.Max((obj.Size ?? 0), 600)), 0);
                        return Task.FromResult(image);
                    };
                    
                    var customHorizon = profileService.ActiveProfile.AstrometrySettings.Horizon;

                    IEnumerable<DeepSkyObject> result = await db.GetDeepSkyObjects(
                        imageFactory,
                        customHorizon,
                        searchParams,
                        _searchTokenSource.Token
                    );

                    var longitude = profileService.ActiveProfile.AstrometrySettings.Longitude;
                    var latitude = profileService.ActiveProfile.AstrometrySettings.Latitude;

                    NighttimeData = this.nighttimeCalculator.Calculate(FilterDate);
                    DateTime d = NighttimeData.ReferenceDate;

                    Parallel.ForEach(result, (obj) => {
                        var cloneDate = d;
                        obj.SetDateAndPosition(cloneDate, latitude, longitude);
                        _searchTokenSource.Token.ThrowIfCancellationRequested();
                    });
                    
                    if(SelectedMinimumAltitudeDegrees > 0 && SelectedAltitudeDuration > 0) {
                        Func<DeepSkyObject, bool> filterFunction;
                        var fromDate = GetDateFromReferenceDate(SelectedAltitudeTimeFrom, NighttimeData.ReferenceDate);
                        var throughDate = GetDateFromReferenceDate(SelectedAltitudeTimeThrough, NighttimeData.ReferenceDate);
                        var fullDuration = throughDate - fromDate;

                        var minimumDuration = SelectedAltitudeDuration;
                        if (SelectedAltitudeDuration != double.MaxValue && TimeSpan.FromHours(SelectedAltitudeDuration) > fullDuration) {
                            minimumDuration = Math.Floor(fullDuration.TotalHours);
                        }

                        if (SelectedMinimumAltitudeDegrees == ALTITUDEABOVEHORIZONFILTER) {
                            // Do not read dso.Altitudes/dso.Horizon here. Those properties populate the full
                            // altitude chart for every candidate, while this filter only needs the selected window.
                            var siderealTimeAtReferenceDate = AstroUtil.GetLocalSiderealTime(NighttimeData.ReferenceDate, longitude);
                            filterFunction = (dso) => {
                                return IsAboveHorizonForDuration(
                                    dso,
                                    NighttimeData.ReferenceDate,
                                    fromDate,
                                    throughDate,
                                    latitude,
                                    siderealTimeAtReferenceDate,
                                    customHorizon,
                                    minimumDuration,
                                    _searchTokenSource.Token);
                            };
                        } else {
                            filterFunction = (x) => {
                                if(minimumDuration == double.MaxValue) {
                                    var altitudesBetweenDates = x.Altitudes
                                    .Where((y) => { return y.X >= DateTimeAxis.ToDouble(fromDate) && y.X <= DateTimeAxis.ToDouble(throughDate); })
                                    .ToList();

                                    if (minimumDuration == double.MaxValue) {
                                        return altitudesBetweenDates.All(item => item.Y > SelectedMinimumAltitudeDegrees);
                                    }
                                }

                                var aggregate = x.Altitudes.Where((y) => { return y.X > DateTimeAxis.ToDouble(fromDate) && y.X < DateTimeAxis.ToDouble(throughDate); })                                
                                    .Where((y) => y.Y > SelectedMinimumAltitudeDegrees)
                                    .Aggregate(new { min = double.MaxValue, max = double.MinValue }, (accumulator, o) => new { min = Math.Min(o.X, accumulator.min), max = o.X > accumulator.max ? o.X : accumulator.max });
                                if(aggregate.min != double.MaxValue) {
                                    return TimeSpan.FromDays(aggregate.max - aggregate.min) > TimeSpan.FromHours(minimumDuration);
                                }
                                return false;                                
                            };
                        }
                        result = result.Where(filterFunction);
                    }

                    if(SelectedMinimumMoonDistanceDegrees > 0) {
                        result = result.Where(x => x.Altitudes.Count > 0 && x.Moon.Separation > SelectedMinimumMoonDistanceDegrees);
                    }

                    var resultList = result.ToList();

                    var useTransitFilter = !(SelectedTransitTimeFrom == DateTime.MinValue && SelectedTransitTimeThrough == DateTime.MaxValue);
                    
                    if (useTransitFilter || OrderByField == SkyAtlasOrderByFieldsEnum.TRANSITTIME) {

                        var transitTimes = new ConcurrentDictionary<DeepSkyObject, DateTime>();

                        // LST is fixed for the reference date/site, so compute it once before the parallel pass.
                        double lstRef = AstroUtil.GetLocalSiderealTime(NighttimeData.ReferenceDate, longitude);

                        Parallel.ForEach(resultList, dso => {

                            var coords = dso.Coordinates;
                            if (coords == null) {
                                return;
                            }

                            double ra = coords.RA; // in hours

                            // time to next hour angle H = 0 (upper culmination) - must be within next 24 hrs
                            double hoursToUpperTransit = (ra - lstRef) % 24.0;
                            if (hoursToUpperTransit < 0) {
                                hoursToUpperTransit += 24.0;
                            }

                            DateTime tUpper = NighttimeData.ReferenceDate + TimeSpan.FromHours(hoursToUpperTransit);
                            transitTimes[dso] = tUpper;

                            _searchTokenSource.Token.ThrowIfCancellationRequested();
                        });

                        DateTime transitFromDate;
                        DateTime transitThroughDate;

                        if (SelectedTransitTimeFrom == DateTime.MinValue) {
                            transitFromDate = DateTime.MinValue;
                        } else {
                            transitFromDate = GetDateFromReferenceDate(SelectedTransitTimeFrom, NighttimeData.ReferenceDate);
                        }

                        if (SelectedTransitTimeThrough == DateTime.MaxValue) {
                            transitThroughDate = DateTime.MaxValue;
                        } else {
                            transitThroughDate = GetDateFromReferenceDate(SelectedTransitTimeThrough, NighttimeData.ReferenceDate);
                        }
                        
                        resultList = resultList
                            .Where(dso =>
                                transitTimes.TryGetValue(dso, out var tt) &&
                                tt >= transitFromDate &&
                                tt <= transitThroughDate)
                            .ToList();


                        if (OrderByField == SkyAtlasOrderByFieldsEnum.TRANSITTIME) {
                            if (OrderByDirection == SkyAtlasOrderByDirectionEnum.ASC) {
                                resultList = resultList
                                    .OrderBy(dso => transitTimes.TryGetValue(dso, out var tt) ? tt : DateTime.MaxValue)
                                    .ToList();
                            } else {
                                resultList = resultList
                                    .OrderByDescending(dso => transitTimes.TryGetValue(dso, out var tt) ? tt : DateTime.MinValue)
                                    .ToList();
                            }
                        }

                    }
                    
                    SearchResult = new PagedList<DeepSkyObject>(PageSize, resultList);
                } catch (OperationCanceledException) {
                }
                return true;
            });
        }

        private void InitializeFilters() {
            NighttimeData = this.nighttimeCalculator.Calculate(FilterDate);
            InitializeRADecFilters();
            InitializeConstellationFilters();
            InitializeObjectTypeFilters();
            InitializeMagnitudeFilters();
            InitializeBrightnessFilters();
            InitializeSizeFilters();
            InitializeElevationFilters();
            InitializeMoonDistanceDegreesFilter();
        }

        private const double ALTITUDEABOVEHORIZONFILTER = 999;
        private const double ALTITUDE_SAMPLE_HOURS = 0.1;
        private const int ALTITUDE_SAMPLE_COUNT = 240;

        // Keep the above-horizon filter aligned with the chart's 0.1h sampling grid without
        // populating SkyObjectBase.Altitudes/Horizon for every search result.
        internal static bool IsAboveHorizonForDuration(
            DeepSkyObject dso,
            DateTime referenceDate,
            DateTime fromDate,
            DateTime throughDate,
            double latitude,
            double siderealTimeAtReferenceDate,
            CustomHorizon customHorizon,
            double minimumDuration,
            CancellationToken token) {
            // Without coordinates there is no RA/Dec to sample, so the candidate cannot pass.
            var coords = dso.Coordinates;
            if (coords == null) {
                return false;
            }

            // Translate the selected time window into sample indexes on the same 24h/0.1h grid
            // used by DSO altitude charts, then clamp it to the reference day.
            var firstSample = Math.Max(0, (int)Math.Ceiling(((fromDate - referenceDate).TotalHours / ALTITUDE_SAMPLE_HOURS) - 1e-9));
            var lastSample = Math.Min(ALTITUDE_SAMPLE_COUNT - 1, (int)Math.Floor(((throughDate - referenceDate).TotalHours / ALTITUDE_SAMPLE_HOURS) + 1e-9));
            if (firstSample > lastSample) {
                // An empty clamped window only satisfies the "entire interval" sentinel case,
                // matching the previous behavior where All() over an empty sequence returned true.
                return minimumDuration == double.MaxValue;
            }

            // Convert RA to the hour angle at the reference date once. Each following sample is
            // just a fixed 0.1h step from that reference hour angle.
            var hourAngleAtReferenceDate = AstroUtil.GetHourAngle(siderealTimeAtReferenceDate, coords.RA);
            if (minimumDuration == double.MaxValue) {
                // "Full selected interval" mode: every sampled point in the selected window must
                // remain above the configured horizon.
                for (var sample = firstSample; sample <= lastSample; sample++) {
                    token.ThrowIfCancellationRequested();
                    var (altitude, horizonAltitude) = GetAltitudeAndHorizonAltitudeAtSample(coords, latitude, customHorizon, hourAngleAtReferenceDate, sample);
                    if (altitude <= horizonAltitude) {
                        return false;
                    }
                }

                return true;
            }

            if (firstSample == lastSample) {
                // A duration requires at least two samples to establish elapsed time.
                return false;
            }

            // Duration mode: scan for one continuous run above the horizon that exceeds the
            // requested minimum duration.
            var requiredDuration = TimeSpan.FromHours(minimumDuration);
            var firstAboveHorizon = referenceDate.AddHours(firstSample * ALTITUDE_SAMPLE_HOURS);
            for (var sample = firstSample; sample <= lastSample; sample++) {
                token.ThrowIfCancellationRequested();
                var sampleTime = referenceDate.AddHours(sample * ALTITUDE_SAMPLE_HOURS);
                var (altitude, horizonAltitude) = GetAltitudeAndHorizonAltitudeAtSample(coords, latitude, customHorizon, hourAngleAtReferenceDate, sample);

                if (altitude < horizonAltitude) {
                    // Falling below the horizon breaks the current run; use this sample as the
                    // reset point for the next above-horizon candidate duration.
                    firstAboveHorizon = sampleTime;
                    continue;
                }

                if (sampleTime - firstAboveHorizon > requiredDuration) {
                    return true;
                }
            }

            // No continuous above-horizon run was long enough.
            return false;
        }

        private static (double Altitude, double HorizonAltitude) GetAltitudeAndHorizonAltitudeAtSample(
            Coordinates coords,
            double latitude,
            CustomHorizon customHorizon,
            double hourAngleAtReferenceDate,
            int sample) {
            var hourAngle = hourAngleAtReferenceDate + (sample * ALTITUDE_SAMPLE_HOURS);
            var hourAngleDegrees = AstroUtil.HoursToDegrees(hourAngle);
            var altitude = AstroUtil.GetAltitude(hourAngleDegrees, latitude, coords.Dec);
            var horizonAltitude = 0d;

            if (customHorizon != null) {
                var azimuth = AstroUtil.GetAzimuth(hourAngleDegrees, altitude, latitude, coords.Dec);
                horizonAltitude = customHorizon.GetAltitude(azimuth);
            }

            return (altitude, horizonAltitude);
        }

        private void InitializeElevationFilters() {
            AltitudeTimesFrom = new AsyncObservableCollection<KeyValuePair<DateTime, string>>();
            AltitudeTimesThrough = new AsyncObservableCollection<KeyValuePair<DateTime, string>>();
            MinimumAltitudeDegrees = new AsyncObservableCollection<KeyValuePair<double, string>>();
            AltitudeDurations = new AsyncObservableCollection<KeyValuePair<double, string>>();
            TransitTimesFrom = new AsyncObservableCollection<KeyValuePair<DateTime, string>>();
            TransitTimesThrough = new AsyncObservableCollection<KeyValuePair<DateTime, string>>();

            var d = NighttimeData.ReferenceDate;

            var dates = new List<KeyValuePair<DateTime, string>>();
            for (int i = 0; i < 24; i++) {
                dates.Add(new KeyValuePair<DateTime, string>(d, d.ToString("HH:mm")));
                d = d.AddHours(1);
            }
            if (NighttimeData.SunRiseAndSet.Set.HasValue) {
                dates.Add(new KeyValuePair<DateTime, string>(NighttimeData.SunRiseAndSet.Set.Value, $"{NighttimeData.SunRiseAndSet.Set.Value.ToString("HH:mm")} ({Loc.Instance["LblSunSet"]})"));
            }
            if (NighttimeData.SunRiseAndSet.Rise.HasValue) {
                dates.Add(new KeyValuePair<DateTime, string>(NighttimeData.SunRiseAndSet.Rise.Value, $"{NighttimeData.SunRiseAndSet.Rise.Value.ToString("HH:mm")} ({Loc.Instance["LblSunRise"]})"));
            }
            if (NighttimeData.TwilightRiseAndSet.Set.HasValue) {
                dates.Add(new KeyValuePair<DateTime, string>(NighttimeData.TwilightRiseAndSet.Set.Value, $"{NighttimeData.TwilightRiseAndSet.Set.Value.ToString("HH:mm")} ({Loc.Instance["LblAstronomicalDusk"]})"));                
            }
            if (NighttimeData.TwilightRiseAndSet.Rise.HasValue) {
                dates.Add(new KeyValuePair<DateTime, string>(NighttimeData.TwilightRiseAndSet.Rise.Value, $"{NighttimeData.TwilightRiseAndSet.Rise.Value.ToString("HH:mm")} ({Loc.Instance["LblAstronomicalDawn"]})"));
            }
            if (NighttimeData.NauticalTwilightRiseAndSet.Set.HasValue) {
                dates.Add(new KeyValuePair<DateTime, string>(NighttimeData.NauticalTwilightRiseAndSet.Set.Value, $"{NighttimeData.NauticalTwilightRiseAndSet.Set.Value.ToString("HH:mm")} ({Loc.Instance["LblNauticalDusk"]})"));
            }
            if (NighttimeData.NauticalTwilightRiseAndSet.Rise.HasValue) {
                dates.Add(new KeyValuePair<DateTime, string>(NighttimeData.NauticalTwilightRiseAndSet.Rise.Value, $"{NighttimeData.NauticalTwilightRiseAndSet.Rise.Value.ToString("HH:mm")} ({Loc.Instance["LblNauticalDawn"]})"));
            }
            AltitudeTimesFrom = new AsyncObservableCollection<KeyValuePair<DateTime, string>>(dates.OrderBy(x => x.Key));
            AltitudeTimesThrough = new AsyncObservableCollection<KeyValuePair<DateTime, string>>(dates.OrderBy(x => x.Key));

            TransitTimesFrom = new AsyncObservableCollection<KeyValuePair<DateTime, string>>(dates.OrderBy(x => x.Key));
            TransitTimesThrough = new AsyncObservableCollection<KeyValuePair<DateTime, string>>(dates.OrderBy(x => x.Key));
            
            MinimumAltitudeDegrees.Add(new KeyValuePair<double, string>(0, Loc.Instance["LblAny"]));
            for (int i = 10; i <= 80; i += 10) {
                MinimumAltitudeDegrees.Add(new KeyValuePair<double, string>(i, i + "°"));
            }
            MinimumAltitudeDegrees.Add(new KeyValuePair<double, string>(ALTITUDEABOVEHORIZONFILTER, Loc.Instance["LblAboveHorizon"]));

            AltitudeDurations.Add(new KeyValuePair<double, string>(0, Loc.Instance["LblAnyDuration"]));
            for (double i = 1; i <= 12; i++) {
                AltitudeDurations.Add(new KeyValuePair<double, string>(i, i + "h"));
            }
            AltitudeDurations.Add(new KeyValuePair<double, string>(double.MaxValue, Loc.Instance["LblCompleteDuration"]));
        }

        private void InitializeSizeFilters() {
            SizesFrom = new AsyncObservableCollection<KeyValuePair<string, string>>();
            SizesThrough = new AsyncObservableCollection<KeyValuePair<string, string>>();

            SizesFrom.Add(new KeyValuePair<string, string>(string.Empty, string.Empty));
            SizesThrough.Add(new KeyValuePair<string, string>(string.Empty, string.Empty));

            SizesFrom.Add(new KeyValuePair<string, string>("1", "1 " + Loc.Instance["LblArcsec"]));
            SizesFrom.Add(new KeyValuePair<string, string>("5", "5 " + Loc.Instance["LblArcsec"]));
            SizesFrom.Add(new KeyValuePair<string, string>("10", "10 " + Loc.Instance["LblArcsec"]));
            SizesFrom.Add(new KeyValuePair<string, string>("30", "30 " + Loc.Instance["LblArcsec"]));
            SizesFrom.Add(new KeyValuePair<string, string>("60", "1 " + Loc.Instance["LblArcmin"]));
            SizesFrom.Add(new KeyValuePair<string, string>("300", "5 " + Loc.Instance["LblArcmin"]));
            SizesFrom.Add(new KeyValuePair<string, string>("600", "10 " + Loc.Instance["LblArcmin"]));
            SizesFrom.Add(new KeyValuePair<string, string>("1800", "30 " + Loc.Instance["LblArcmin"]));
            SizesFrom.Add(new KeyValuePair<string, string>("3600", "1 " + Loc.Instance["LblDegree"]));
            SizesFrom.Add(new KeyValuePair<string, string>("18000", "5 " + Loc.Instance["LblDegree"]));
            SizesFrom.Add(new KeyValuePair<string, string>("36000", "10 " + Loc.Instance["LblDegree"]));

            SizesThrough = new AsyncObservableCollection<KeyValuePair<string, string>>(SizesFrom);
        }

        private void InitializeBrightnessFilters() {
            BrightnessFrom = new AsyncObservableCollection<string>();
            BrightnessThrough = new AsyncObservableCollection<string>();

            BrightnessFrom.Add(string.Empty);
            BrightnessThrough.Add(string.Empty);

            for (var i = 2; i < 19; i++) {
                BrightnessFrom.Add(i.ToString());
                BrightnessThrough.Add(i.ToString());
            }
        }

        private void InitializeConstellationFilters() {
            var dbResult = new DatabaseInteraction().GetConstellations(new System.Threading.CancellationToken());
            var list = new AsyncObservableCollection<string>(dbResult.Result);
            list.Insert(0, string.Empty);
            Constellations = list;
        }

        private void InitializeObjectTypeFilters() {
            ObjectTypes.Clear();
            var task = new DatabaseInteraction().GetObjectTypes(new System.Threading.CancellationToken());
            var list = task.Result?.OrderBy(x => x).ToList();
            foreach (var type in list) {
                ObjectTypes.Add(new DSOObjectType(type));
            }
        }

        private void InitializeMagnitudeFilters() {
            MagnitudesFrom = new AsyncObservableCollection<string>();
            MagnitudesThrough = new AsyncObservableCollection<string>();

            MagnitudesFrom.Add(string.Empty);
            MagnitudesThrough.Add(string.Empty);

            for (var i = 1; i < 22; i++) {
                MagnitudesFrom.Add(i.ToString());
                MagnitudesThrough.Add(i.ToString());
            }
        }

        private void InitializeRADecFilters() {
            RAFrom = new AsyncObservableCollection<KeyValuePair<double?, string>>();
            RAThrough = new AsyncObservableCollection<KeyValuePair<double?, string>>();
            DecFrom = new AsyncObservableCollection<KeyValuePair<double?, string>>();
            DecThrough = new AsyncObservableCollection<KeyValuePair<double?, string>>();

            RAFrom.Add(new KeyValuePair<double?, string>(null, string.Empty));
            RAThrough.Add(new KeyValuePair<double?, string>(null, string.Empty));
            DecFrom.Add(new KeyValuePair<double?, string>(null, string.Empty));
            DecThrough.Add(new KeyValuePair<double?, string>(null, string.Empty));

            for (int i = 0; i < 25; i++) {
                AstroUtil.HoursToDegrees(i);

                RAFrom.Add(new KeyValuePair<double?, string>(AstroUtil.HoursToDegrees(i), i.ToString()));
                RAThrough.Add(new KeyValuePair<double?, string>(AstroUtil.HoursToDegrees(i), i.ToString()));
            }
            for (int i = -90; i < 91; i = i + 5) {
                DecFrom.Add(new KeyValuePair<double?, string>(i, i.ToString()));
                DecThrough.Add(new KeyValuePair<double?, string>(i, i.ToString()));
            }
        }

        private void InitializeMoonDistanceDegreesFilter() {
            MoonDistanceDegrees = new AsyncObservableCollection<double>();
            for (double i = 0; i < 180; i+=5) {
                MoonDistanceDegrees.Add(i);
            }
        }

        private DateTime GetDateFromReferenceDate(DateTime date, DateTime referenceDate) {
            if (date.Hour >= 12) {
                return new DateTime(referenceDate.Year, referenceDate.Month, referenceDate.Day, date.Hour, date.Minute, date.Second);
            } else {
                return new DateTime(referenceDate.Year, referenceDate.Month, referenceDate.Day, date.Hour, date.Minute, date.Second) + TimeSpan.FromDays(1);
            }
        }

        private string _searchObjectName;
        private AsyncObservableCollection<DSOObjectType> _objectTypes;
        private AsyncObservableCollection<string> _constellations;
        private string _selectedConstellation;
        private AsyncObservableCollection<KeyValuePair<double?, string>> _rAFrom;
        private AsyncObservableCollection<KeyValuePair<double?, string>> _rAThrough;
        private AsyncObservableCollection<KeyValuePair<double?, string>> _decFrom;
        private AsyncObservableCollection<KeyValuePair<double?, string>> _decThrough;
        private double? _selectedRAFrom;
        private double? _selectedRAThrough;
        private double? _selectedDecFrom;
        private double? _selectedDecThrough;
        private AsyncObservableCollection<string> _brightnessFrom;
        private AsyncObservableCollection<string> _brightnessThrough;
        private double? _selectedBrightnessFrom;
        private double? _selectedBrightnessThrough;
        private AsyncObservableCollection<KeyValuePair<string, string>> _sizesFrom;
        private AsyncObservableCollection<KeyValuePair<string, string>> _sizesThrough;
        private double? _selectedSizeFrom;
        private double? _selectedSizeThrough;
        private AsyncObservableCollection<string> _magnitudesFrom;
        private AsyncObservableCollection<string> _magnitudesThrough;
        private double? _selectedMagnitudeFrom;
        private double? _selectedMagnitudeThrough;
        private PagedList<DeepSkyObject> _searchResult;
        private AsyncObservableCollection<KeyValuePair<DateTime, string>> _altitudeTimesFrom;
        private AsyncObservableCollection<KeyValuePair<DateTime, string>> _altitudeTimesThrough;
        private AsyncObservableCollection<KeyValuePair<double, string>> _minimumAltitudeDegrees;
        private AsyncObservableCollection<KeyValuePair<double, string>> _altitudeDurations;
        private double _selectedAltitudeDuration;
        private DateTime _selectedAltitudeTimeFrom;
        private DateTime _selectedAltitudeTimeThrough;
        private double _selectedMinimumAltitudeDegrees;
        private AsyncObservableCollection<double> _moonDistanceDegrees;
        private double _selectedMinimumMoonDistanceDegrees;

        private AsyncObservableCollection<KeyValuePair<DateTime, string>> _transitTimesFrom;
        private AsyncObservableCollection<KeyValuePair<DateTime, string>> _transitTimesThrough;
        private DateTime _selectedTransitTimeFrom = DateTime.MinValue;
        private DateTime _selectedTransitTimeThrough = DateTime.MaxValue;
        
        public AsyncObservableCollection<KeyValuePair<DateTime, string>> AltitudeTimesFrom {
            get => _altitudeTimesFrom;
            set {
                _altitudeTimesFrom = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<KeyValuePair<DateTime, string>> AltitudeTimesThrough {
            get => _altitudeTimesThrough;
            set {
                _altitudeTimesThrough = value;
                RaisePropertyChanged();
            }
        }
        public AsyncObservableCollection<KeyValuePair<double, string>> MinimumAltitudeDegrees {
            get => _minimumAltitudeDegrees;
            set {
                _minimumAltitudeDegrees = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<KeyValuePair<double, string>> AltitudeDurations {
            get => _altitudeDurations;
            set {
                _altitudeDurations = value;
                RaisePropertyChanged();
            }
        }

        public double SelectedAltitudeDuration {
            get => _selectedAltitudeDuration;
            set {
                _selectedAltitudeDuration = value;
                RaisePropertyChanged();
            }
        }

        public DateTime SelectedAltitudeTimeFrom {
            get => _selectedAltitudeTimeFrom;
            set {
                if (value != _selectedAltitudeTimeFrom) {
                    _selectedAltitudeTimeFrom = value;
                    RaisePropertyChanged();
                }
            }
        }


        public DateTime SelectedAltitudeTimeThrough {
            get => _selectedAltitudeTimeThrough;
            set {
                if (value != _selectedAltitudeTimeThrough) {
                    _selectedAltitudeTimeThrough = value;
                    RaisePropertyChanged();
                }
            }
        }

        public double SelectedMinimumAltitudeDegrees {
            get => _selectedMinimumAltitudeDegrees;
            set {
                _selectedMinimumAltitudeDegrees = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<double> MoonDistanceDegrees {
            get => _moonDistanceDegrees;
            set {
                _moonDistanceDegrees = value;
                RaisePropertyChanged();
            }
        }

        public double SelectedMinimumMoonDistanceDegrees {
            get => _selectedMinimumMoonDistanceDegrees;
            set {
                if (value < 0) { value = 0; }
                if (value > 180) { value = 180; }
                _selectedMinimumMoonDistanceDegrees = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<KeyValuePair<DateTime, string>> TransitTimesFrom {
            get => _transitTimesFrom;
            set {
                _transitTimesFrom = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<KeyValuePair<DateTime, string>> TransitTimesThrough {
            get => _transitTimesThrough;
            set {
                _transitTimesThrough = value;
                RaisePropertyChanged();
            }
        }

        public DateTime SelectedTransitTimeFrom {
            get => _selectedTransitTimeFrom;
            set {
                if (value != _selectedTransitTimeFrom) {
                    _selectedTransitTimeFrom = value;
                    RaisePropertyChanged();
                }
            }
        }

        public DateTime SelectedTransitTimeThrough {
            get => _selectedTransitTimeThrough;
            set {
                if (value != _selectedTransitTimeThrough) {
                    _selectedTransitTimeThrough = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string SearchObjectName {
            get => _searchObjectName;

            set {
                _searchObjectName = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<DSOObjectType> ObjectTypes {
            get {
                if (_objectTypes == null) {
                    _objectTypes = new AsyncObservableCollection<DSOObjectType>();
                }
                return _objectTypes;
            }

            set {
                _objectTypes = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<string> Constellations {
            get => _constellations;

            set {
                _constellations = value;
                RaisePropertyChanged();
            }
        }

        public string SelectedConstellation {
            get => _selectedConstellation;

            set {
                _selectedConstellation = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<KeyValuePair<double?, string>> RAFrom {
            get => _rAFrom;

            set {
                _rAFrom = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<KeyValuePair<double?, string>> RAThrough {
            get => _rAThrough;

            set {
                _rAThrough = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<KeyValuePair<double?, string>> DecFrom {
            get => _decFrom;

            set {
                _decFrom = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<KeyValuePair<double?, string>> DecThrough {
            get => _decThrough;

            set {
                _decThrough = value;
                RaisePropertyChanged();
            }
        }

        public double? SelectedRAFrom {
            get => _selectedRAFrom;

            set {
                _selectedRAFrom = value;
                RaisePropertyChanged();
            }
        }

        public double? SelectedRAThrough {
            get => _selectedRAThrough;

            set {
                _selectedRAThrough = value;
                RaisePropertyChanged();
            }
        }

        public double? SelectedDecFrom {
            get => _selectedDecFrom;

            set {
                _selectedDecFrom = value;
                RaisePropertyChanged();
            }
        }

        public double? SelectedDecThrough {
            get => _selectedDecThrough;

            set {
                _selectedDecThrough = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<string> BrightnessFrom {
            get => _brightnessFrom;

            set {
                _brightnessFrom = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<string> BrightnessThrough {
            get => _brightnessThrough;

            set {
                _brightnessThrough = value;
                RaisePropertyChanged();
            }
        }

        public double? SelectedBrightnessFrom {
            get => _selectedBrightnessFrom;

            set {
                _selectedBrightnessFrom = value;
                RaisePropertyChanged();
            }
        }

        public double? SelectedBrightnessThrough {
            get => _selectedBrightnessThrough;

            set {
                _selectedBrightnessThrough = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<KeyValuePair<string, string>> SizesFrom {
            get => _sizesFrom;

            set {
                _sizesFrom = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<KeyValuePair<string, string>> SizesThrough {
            get => _sizesThrough;

            set {
                _sizesThrough = value;
                RaisePropertyChanged();
            }
        }

        public double? SelectedSizeFrom {
            get => _selectedSizeFrom;

            set {
                _selectedSizeFrom = value;
                RaisePropertyChanged();
            }
        }

        public double? SelectedSizeThrough {
            get => _selectedSizeThrough;

            set {
                _selectedSizeThrough = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<string> MagnitudesFrom {
            get => _magnitudesFrom;

            set {
                _magnitudesFrom = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<string> MagnitudesThrough {
            get => _magnitudesThrough;

            set {
                _magnitudesThrough = value;
                RaisePropertyChanged();
            }
        }

        public double? SelectedMagnitudeFrom {
            get => _selectedMagnitudeFrom;

            set {
                _selectedMagnitudeFrom = value;
                RaisePropertyChanged();
            }
        }

        public double? SelectedMagnitudeThrough {
            get => _selectedMagnitudeThrough;

            set {
                _selectedMagnitudeThrough = value;
                RaisePropertyChanged();
            }
        }

        public ICommand ResetFiltersCommand { get; private set; }
        public ICommand SearchCommand { get; private set; }

        public ICommand CancelSearchCommand { get; private set; }

        public ICommand SetOldSequencerTargetCommand { get; private set; }
        public ICommand SetSequencerTargetCommand { get; private set; }

        public IAsyncCommand SlewToCoordinatesCommand { get; private set; }

        public ICommand SetFramingAssistantCoordinatesCommand { get; private set; }
        public ICommand GetDSOTemplatesCommand { get; private set; }

        public int PageSize {
            get => profileService.ActiveProfile.ApplicationSettings.PageSize;
            set {
                profileService.ActiveProfile.ApplicationSettings.PageSize = value;
                RaisePropertyChanged();
            }
        }

        private SkyAtlasOrderByDirectionEnum _orderByDirection;

        public SkyAtlasOrderByDirectionEnum OrderByDirection {
            get => _orderByDirection;
            set {
                _orderByDirection = value;
                RaisePropertyChanged();
            }
        }

        private SkyAtlasOrderByFieldsEnum _orderByField;

        public SkyAtlasOrderByFieldsEnum OrderByField {
            get => _orderByField;
            set {
                _orderByField = value;
                RaisePropertyChanged();
            }
        }

        public PagedList<DeepSkyObject> SearchResult {
            get => _searchResult;

            set {
                _searchResult = value;
                RaisePropertyChanged();
            }
        }
    }
}
