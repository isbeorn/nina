using NINA.Model;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Mediator;
using NINA.Utility.Notification;
using OxyPlot;
using OxyPlot.Axes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.ViewModel {
    public class SkyAtlasVM : BaseVM {
        public SkyAtlasVM() {
            SelectedDate = DateTime.Now;

            SearchCommand = new AsyncCommand<bool>(() => Search());
            CancelSearchCommand = new RelayCommand(CancelSearch);

            InitializeFilters();
            PageSize = 50;

            Mediator.Instance.Register((object o) => {
                _nightDuration = null; //Clear cache
                SelectedDate = DateTime.Now;
                InitializeElevationFilters();
                ResetRiseAndSetTimes();
            }, MediatorMessages.LocationChanged);

        }

        private void ResetRiseAndSetTimes() {
            MoonPhase = Astrometry.MoonPhase.Unknown;
            Illumination = null;
            MoonRiseAndSet = null;
            SunRiseAndSet = null;
            TwilightRiseAndSet = null;
            _nightDuration = null;
            _twilightDuration = null;
            _ticker?.Stop();
            _ticker = null;
        }

        private CancellationTokenSource _searchTokenSource;

        private void CancelSearch(object obj) {
            _searchTokenSource?.Cancel();
        }

        private Ticker _ticker;
        public Ticker Ticker {
            get {
                if (_ticker == null) {
                    _ticker = new Ticker(30000);
                }
                return _ticker;
            }
        }

        private AsyncObservableCollection<DataPoint> _nightDuration;
        public AsyncObservableCollection<DataPoint> NightDuration {
            get {
                if (_nightDuration == null) {

                    var twilight = TwilightRiseAndSet;
                    if (twilight != null) {
                        _nightDuration = new AsyncObservableCollection<DataPoint>() {
                        new DataPoint(DateTimeAxis.ToDouble(twilight.RiseDate), 90),
                        new DataPoint(DateTimeAxis.ToDouble(twilight.SetDate), 90) };
                    } else {
                        _nightDuration = new AsyncObservableCollection<DataPoint>();
                    }
                }
                return _nightDuration;
            }
        }

        private AsyncObservableCollection<DataPoint> _twilightDuration;
        public AsyncObservableCollection<DataPoint> TwilightDuration {
            get {
                if (_twilightDuration == null) {

                    var twilight = SunRiseAndSet;
                    var night = TwilightRiseAndSet;
                    if (twilight != null) {
                        _twilightDuration = new AsyncObservableCollection<DataPoint>();
                        _twilightDuration.Add(new DataPoint(DateTimeAxis.ToDouble(twilight.SetDate), 90));

                        if (night != null) {
                            _twilightDuration.Add(new DataPoint(DateTimeAxis.ToDouble(night.SetDate), 90));
                            _twilightDuration.Add(new DataPoint(DateTimeAxis.ToDouble(night.SetDate), 0));
                            _twilightDuration.Add(new DataPoint(DateTimeAxis.ToDouble(night.RiseDate), 0));
                            _twilightDuration.Add(new DataPoint(DateTimeAxis.ToDouble(night.RiseDate), 90));
                        }

                        _twilightDuration.Add(new DataPoint(DateTimeAxis.ToDouble(twilight.RiseDate), 90));

                    } else {
                        _twilightDuration = new AsyncObservableCollection<DataPoint>();
                    }
                }
                return _twilightDuration;
            }
        }

        private Astrometry.RiseAndSetAstroEvent _twilightRiseAndSet;
        public Astrometry.RiseAndSetAstroEvent TwilightRiseAndSet {
            get {
                if (_twilightRiseAndSet == null) {
                    var d = GetReferenceDate(SelectedDate);
                    _twilightRiseAndSet = Astrometry.GetNightTimes(d);
                }
                return _twilightRiseAndSet;
            }
            private set {
                _twilightRiseAndSet = value;
                RaisePropertyChanged();
            }
        }

        private Astrometry.RiseAndSetAstroEvent _moonRiseAndSet;
        public Astrometry.RiseAndSetAstroEvent MoonRiseAndSet {
            get {
                if (_moonRiseAndSet == null) {
                    var d = GetReferenceDate(SelectedDate);
                    _moonRiseAndSet = Astrometry.GetMoonRiseAndSet(d);
                }
                return _moonRiseAndSet;
            }
            private set {
                _moonRiseAndSet = value;
                RaisePropertyChanged();
            }
        }

        private double? _illumination;
        public double? Illumination {
            get {
                if (_illumination == null) {
                    var d = GetReferenceDate(SelectedDate);
                    _illumination = Astrometry.GetMoonIllumination(d);
                }
                return _illumination.Value;
            }
            private set {
                _illumination = value;
                RaisePropertyChanged();
            }
        }

        private Astrometry.MoonPhase _moonPhase;
        public Astrometry.MoonPhase MoonPhase {
            get {
                if (_moonPhase == Astrometry.MoonPhase.Unknown) {
                    var d = GetReferenceDate(SelectedDate);
                    _moonPhase = Astrometry.GetMoonPhase(d);
                }
                return _moonPhase;
            }
            private set {
                _moonPhase = value;
                RaisePropertyChanged();
            }
        }

        private Astrometry.RiseAndSetAstroEvent _sunRiseAndSet;
        public Astrometry.RiseAndSetAstroEvent SunRiseAndSet {
            get {
                if (_sunRiseAndSet == null) {
                    var d = GetReferenceDate(SelectedDate);
                    _sunRiseAndSet = Astrometry.GetSunRiseAndSet(d);
                }
                return _sunRiseAndSet;
            }
            private set {
                _sunRiseAndSet = value;
                RaisePropertyChanged();
            }
        }

        private DateTime _selectedDate;
        public DateTime SelectedDate {
            get {
                return _selectedDate;
            }
            set {
                _selectedDate = value;
                RaisePropertyChanged();
                InitializeElevationFilters();
                ResetRiseAndSetTimes();
            }
        }

        public static DateTime GetReferenceDate(DateTime reference) {
            DateTime d = reference;
            if (d.Hour > 12) {
                d = new DateTime(d.Year, d.Month, d.Day, 12, 0, 0);
            } else {
                var tmp = d.AddDays(-1);
                d = new DateTime(tmp.Year, tmp.Month, tmp.Day, 12, 0, 0);
            }
            return d;
        }

        private async Task<bool> Search() {
            _searchTokenSource = new CancellationTokenSource();
            return await Task.Run(async () => {
                try {
                    SearchResult = null;
                    var db = new DatabaseInteraction();
                    var types = ObjectTypes.Where((x) => x.Selected).Select((x) => x.Name).ToList();
                    var result = await db.GetDeepSkyObjects(_searchTokenSource.Token,
                        SelectedConstellation,
                        SelectedRAFrom,
                        SelectedRAThrough,
                        Nullable.Compare(SelectedDecFrom, SelectedDecThrough) > 0 ? SelectedDecThrough : SelectedDecFrom,
                        Nullable.Compare(SelectedDecFrom, SelectedDecThrough) > 0 ? SelectedDecFrom : SelectedDecThrough,
                        SelectedSizeFrom,
                        SelectedSizeThrough,
                        types,
                        SelectedBrightnessFrom,
                        SelectedBrightnessThrough,
                        SelectedMagnitudeFrom,
                        SelectedMagnitudeThrough,
                        SearchObjectName,
                        OrderByField.ToString().ToLower(),
                        OrderByDirection.ToString());


                    var longitude = Settings.Longitude;
                    var latitude = Settings.Latitude;

                    DateTime d = GetReferenceDate(SelectedDate);

                    Parallel.ForEach(result, (obj) => {
                        var cloneDate = d;
                        obj.SetDateAndPosition(cloneDate, latitude, longitude);
                        _searchTokenSource.Token.ThrowIfCancellationRequested();
                    });

                    /* Check if Altitude Filter is not default */
                    if (!(SelectedAltitudeTimeFrom == DateTime.MinValue && SelectedAltitudeTimeThrough == DateTime.MaxValue && SelectedMinimumAltitudeDegrees == 0)) {
                        var filteredList = result.Where((x) => {
                            return x.Altitudes.Where((y) => {
                                return (y.X > DateTimeAxis.ToDouble(SelectedAltitudeTimeFrom) && y.X < DateTimeAxis.ToDouble(SelectedAltitudeTimeThrough));
                            }).All((z) => {
                                return z.Y > SelectedMinimumAltitudeDegrees;
                            });
                        });

                        var count = filteredList.Count();
                        /* Apply Altitude Filter */
                        SearchResult = new PagedList<DeepSkyObject>(PageSize, filteredList);
                    } else {
                        SearchResult = new PagedList<DeepSkyObject>(PageSize, result);
                    }
                } catch (OperationCanceledException) {

                }
                return true;
            });
        }

        private void InitializeFilters() {
            InitializeRADecFilters();
            InitializeConstellationFilters();
            InitializeObjectTypeFilters();
            InitializeMagnitudeFilters();
            InitializeBrightnessFilters();
            InitializeSizeFilters();
            InitializeElevationFilters();
        }

        private void InitializeElevationFilters() {
            AltitudeTimesFrom = new AsyncObservableCollection<DateTime>();
            AltitudeTimesThrough = new AsyncObservableCollection<DateTime>();
            MinimumAltitudeDegrees = new AsyncObservableCollection<KeyValuePair<double, string>>();
            SelectedAltitudeTimeFrom = DateTime.MinValue;
            SelectedAltitudeTimeThrough = DateTime.MaxValue;
            SelectedMinimumAltitudeDegrees = 0;

            var d = GetReferenceDate(SelectedDate);

            AltitudeTimesFrom.Add(DateTime.MinValue);
            AltitudeTimesThrough.Add(DateTime.MaxValue);

            for (int i = 0; i <= 24; i++) {
                AltitudeTimesFrom.Add(d);
                AltitudeTimesThrough.Add(d);
                d = d.AddHours(1);
            }

            for (int i = 0; i <= 90; i += 10) {
                MinimumAltitudeDegrees.Add(new KeyValuePair<double, string>(i, i + "°"));
            }
        }

        private void InitializeSizeFilters() {
            SizesFrom = new AsyncObservableCollection<KeyValuePair<string, string>>();
            SizesThrough = new AsyncObservableCollection<KeyValuePair<string, string>>();

            SizesFrom.Add(new KeyValuePair<string, string>(string.Empty, string.Empty));
            SizesThrough.Add(new KeyValuePair<string, string>(string.Empty, string.Empty));


            SizesFrom.Add(new KeyValuePair<string, string>("1", "1 " + Locale.Loc.Instance["LblArcsec"]));
            SizesFrom.Add(new KeyValuePair<string, string>("5", "5 " + Locale.Loc.Instance["LblArcsec"]));
            SizesFrom.Add(new KeyValuePair<string, string>("10", "10 " + Locale.Loc.Instance["LblArcsec"]));
            SizesFrom.Add(new KeyValuePair<string, string>("30", "30 " + Locale.Loc.Instance["LblArcsec"]));
            SizesFrom.Add(new KeyValuePair<string, string>("60", "1 " + Locale.Loc.Instance["LblArcmin"]));
            SizesFrom.Add(new KeyValuePair<string, string>("300", "5 " + Locale.Loc.Instance["LblArcmin"]));
            SizesFrom.Add(new KeyValuePair<string, string>("600", "10 " + Locale.Loc.Instance["LblArcmin"]));
            SizesFrom.Add(new KeyValuePair<string, string>("1800", "30 " + Locale.Loc.Instance["LblArcmin"]));
            SizesFrom.Add(new KeyValuePair<string, string>("3600", "1 " + Locale.Loc.Instance["LblDegree"]));
            SizesFrom.Add(new KeyValuePair<string, string>("18000", "5 " + Locale.Loc.Instance["LblDegree"]));
            SizesFrom.Add(new KeyValuePair<string, string>("36000", "10 " + Locale.Loc.Instance["LblDegree"]));

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
            var l = new DatabaseInteraction().GetConstellations(new System.Threading.CancellationToken());
            Constellations = new AsyncObservableCollection<string>(l.Result);
        }

        private void InitializeObjectTypeFilters() {
            var l = new DatabaseInteraction().GetObjectTypes(new System.Threading.CancellationToken());
            ObjectTypes = new AsyncObservableCollection<DSOObjectType>();
            foreach (var t in l.Result) {
                ObjectTypes.Add(new DSOObjectType(t));
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

            SelectedRAFrom = null;
            SelectedRAThrough = null;
            SelectedDecFrom = null;
            SelectedDecThrough = null;

            for (int i = 0; i < 25; i++) {

                Astrometry.HoursToDegrees(i);

                RAFrom.Add(new KeyValuePair<double?, string>(Astrometry.HoursToDegrees(i), i.ToString()));
                RAThrough.Add(new KeyValuePair<double?, string>(Astrometry.HoursToDegrees(i), i.ToString()));
            }
            for (int i = -90; i < 91; i = i + 5) {
                DecFrom.Add(new KeyValuePair<double?, string>(i, i.ToString()));
                DecThrough.Add(new KeyValuePair<double?, string>(i, i.ToString()));
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
        private string _selectedBrightnessFrom;
        private string _selectedBrightnessThrough;
        private AsyncObservableCollection<KeyValuePair<string, string>> _sizesFrom;
        private AsyncObservableCollection<KeyValuePair<string, string>> _sizesThrough;
        private string _selectedSizeFrom;
        private string _selectedSizeThrough;
        private AsyncObservableCollection<string> _magnitudesFrom;
        private AsyncObservableCollection<string> _magnitudesThrough;
        private string _selectedMagnitudeFrom;
        private string _selectedMagnitudeThrough;
        private PagedList<DeepSkyObject> _searchResult;
        private AsyncObservableCollection<DateTime> _altitudeTimesFrom;
        private AsyncObservableCollection<DateTime> _altitudeTimesThrough;
        private AsyncObservableCollection<KeyValuePair<double, string>> _minimumAltitudeDegrees;
        private DateTime _selectedAltitudeTimeFrom;
        private DateTime _selectedAltitudeTimeThrough;
        private double _selectedMinimumAltitudeDegrees;

        public AsyncObservableCollection<DateTime> AltitudeTimesFrom {
            get {
                return _altitudeTimesFrom;
            }
            set {
                _altitudeTimesFrom = value;
                RaisePropertyChanged();
            }
        }
        public AsyncObservableCollection<DateTime> AltitudeTimesThrough {
            get {
                return _altitudeTimesThrough;
            }
            set {
                _altitudeTimesThrough = value;
                RaisePropertyChanged();
            }
        }
        public AsyncObservableCollection<KeyValuePair<double, string>> MinimumAltitudeDegrees {
            get {
                return _minimumAltitudeDegrees;
            }
            set {
                _minimumAltitudeDegrees = value;
                RaisePropertyChanged();
            }
        }
        public DateTime SelectedAltitudeTimeFrom {
            get {
                return _selectedAltitudeTimeFrom;
            }
            set {
                _selectedAltitudeTimeFrom = value;
                RaisePropertyChanged();
            }
        }
        public DateTime SelectedAltitudeTimeThrough {
            get {
                return _selectedAltitudeTimeThrough;
            }
            set {
                _selectedAltitudeTimeThrough = value;
                RaisePropertyChanged();
            }
        }
        public double SelectedMinimumAltitudeDegrees {
            get {
                return _selectedMinimumAltitudeDegrees;
            }
            set {
                _selectedMinimumAltitudeDegrees = value;
                RaisePropertyChanged();
            }
        }



        public string SearchObjectName {
            get {
                return _searchObjectName;
            }

            set {
                _searchObjectName = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<DSOObjectType> ObjectTypes {
            get {
                return _objectTypes;
            }

            set {
                _objectTypes = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<string> Constellations {
            get {
                return _constellations;
            }

            set {
                _constellations = value;
                RaisePropertyChanged();
            }
        }

        public string SelectedConstellation {
            get {
                return _selectedConstellation;
            }

            set {
                _selectedConstellation = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<KeyValuePair<double?, string>> RAFrom {
            get {
                return _rAFrom;
            }

            set {
                _rAFrom = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<KeyValuePair<double?, string>> RAThrough {
            get {
                return _rAThrough;
            }

            set {
                _rAThrough = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<KeyValuePair<double?, string>> DecFrom {
            get {
                return _decFrom;
            }

            set {
                _decFrom = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<KeyValuePair<double?, string>> DecThrough {
            get {
                return _decThrough;
            }

            set {
                _decThrough = value;
                RaisePropertyChanged();
            }
        }

        public double? SelectedRAFrom {
            get {
                return _selectedRAFrom;
            }

            set {
                _selectedRAFrom = value;
                RaisePropertyChanged();
            }
        }

        public double? SelectedRAThrough {
            get {
                return _selectedRAThrough;
            }

            set {
                _selectedRAThrough = value;
                RaisePropertyChanged();
            }
        }

        public double? SelectedDecFrom {
            get {
                return _selectedDecFrom;
            }

            set {
                _selectedDecFrom = value;
                RaisePropertyChanged();
            }
        }

        public double? SelectedDecThrough {
            get {
                return _selectedDecThrough;
            }

            set {
                _selectedDecThrough = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<string> BrightnessFrom {
            get {
                return _brightnessFrom;
            }

            set {
                _brightnessFrom = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<string> BrightnessThrough {
            get {
                return _brightnessThrough;
            }

            set {
                _brightnessThrough = value;
                RaisePropertyChanged();
            }
        }

        public string SelectedBrightnessFrom {
            get {
                return _selectedBrightnessFrom;
            }

            set {
                _selectedBrightnessFrom = value;
                RaisePropertyChanged();
            }
        }

        public string SelectedBrightnessThrough {
            get {
                return _selectedBrightnessThrough;
            }

            set {
                _selectedBrightnessThrough = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<KeyValuePair<string, string>> SizesFrom {
            get {
                return _sizesFrom;
            }

            set {
                _sizesFrom = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<KeyValuePair<string, string>> SizesThrough {
            get {
                return _sizesThrough;
            }

            set {
                _sizesThrough = value;
                RaisePropertyChanged();
            }
        }

        public string SelectedSizeFrom {
            get {
                return _selectedSizeFrom;
            }

            set {
                _selectedSizeFrom = value;
                RaisePropertyChanged();
            }
        }

        public string SelectedSizeThrough {
            get {
                return _selectedSizeThrough;
            }

            set {
                _selectedSizeThrough = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<string> MagnitudesFrom {
            get {
                return _magnitudesFrom;
            }

            set {
                _magnitudesFrom = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<string> MagnitudesThrough {
            get {
                return _magnitudesThrough;
            }

            set {
                _magnitudesThrough = value;
                RaisePropertyChanged();
            }
        }

        public string SelectedMagnitudeFrom {
            get {
                return _selectedMagnitudeFrom;
            }

            set {
                _selectedMagnitudeFrom = value;
                RaisePropertyChanged();
            }
        }

        public string SelectedMagnitudeThrough {
            get {
                return _selectedMagnitudeThrough;
            }

            set {
                _selectedMagnitudeThrough = value;
                RaisePropertyChanged();
            }
        }

        public ICommand SearchCommand { get; private set; }

        public ICommand CancelSearchCommand { get; private set; }

        private int _pageSize;
        public int PageSize {
            get {
                return _pageSize;
            }
            set {
                _pageSize = value;
                RaisePropertyChanged();
            }
        }

        private SkyAtlasOrderByDirectionEnum _orderByDirection;
        public SkyAtlasOrderByDirectionEnum OrderByDirection {
            get {
                return _orderByDirection;
            }
            set {
                _orderByDirection = value;
                RaisePropertyChanged();
            }
        }

        private SkyAtlasOrderByFieldsEnum _orderByField;
        public SkyAtlasOrderByFieldsEnum OrderByField {
            get {
                return _orderByField;
            }
            set {
                _orderByField = value;
                RaisePropertyChanged();
            }
        }

        public PagedList<DeepSkyObject> SearchResult {
            get {
                return _searchResult;
            }

            set {
                _searchResult = value;
                RaisePropertyChanged();
            }
        }

        public class DSOObjectType : BaseINPC {
            public DSOObjectType(string s) {
                Name = s;
                Selected = true;
            }

            private bool _selected;
            public bool Selected {
                get {
                    return _selected;
                }
                set {
                    _selected = value;
                    RaisePropertyChanged();
                }
            }

            private string _name;
            public string Name {
                get {
                    return _name;
                }
                set {
                    _name = value;
                    RaisePropertyChanged();
                }
            }

        }
    }

    public class PagedList<T> : BaseINPC {
        public PagedList(int pageSize, IEnumerable<T> items) {
            _items = new List<T>(items);
            PageSize = pageSize;

            var counter = 1;
            for (int i = 0; i < _items.Count; i += PageSize) {
                Pages.Add(counter++);
            }

            LoadFirstPage().Wait();

            FirstPageCommand = new AsyncCommand<bool>(LoadFirstPage, (object o) => { return CurrentPage > 1; });
            PrevPageCommand = new AsyncCommand<bool>(LoadPrevPage, (object o) => { return CurrentPage > 1; });
            NextPageCommand = new AsyncCommand<bool>(LoadNextPage, (object o) => { return CurrentPage < Pages.Count; });
            LastPageCommand = new AsyncCommand<bool>(LoadLastPage, (object o) => { return CurrentPage < Pages.Count; });
            PageByNumberCommand = new AsyncCommand<bool>(async () => await LoadPage(CurrentPage));
        }

        private List<T> _items;

        private async Task<bool> LoadFirstPage() {
            return await LoadPage(Pages.FirstOrDefault());
        }

        private async Task<bool> LoadNextPage() {
            return await LoadPage(CurrentPage + 1);
        }

        private async Task<bool> LoadPrevPage() {
            return await LoadPage(CurrentPage - 1);
        }

        private async Task<bool> LoadLastPage() {
            return await LoadPage(Pages.Count);
        }

        private async Task<bool> LoadPage(int page) {
            var idx = page - 1;
            if (idx < 0) {
                return false;
            } else if (idx > (Count / (double)PageSize)) {
                return false;
            }

            var itemChunk = await Task.Run(() => {
                var offset = Math.Min(_items.Count - (idx * PageSize), PageSize);
                return _items.GetRange(idx * PageSize, offset);
            });

            ItemPage = new AsyncObservableCollection<T>(itemChunk);

            CurrentPage = page;
            RaisePropertyChanged(nameof(Count));
            RaisePropertyChanged(nameof(PageStartIndex));
            RaisePropertyChanged(nameof(PageEndIndex));
            return true;
        }

        private AsyncObservableCollection<T> _itemPage = new AsyncObservableCollection<T>();
        public AsyncObservableCollection<T> ItemPage {
            get {
                return _itemPage;
            }
            private set {
                _itemPage = value;
                RaisePropertyChanged();
            }
        }

        public int PageStartIndex {
            get {
                if (_items.Count == 0) {
                    return 0;
                } else {
                    return PageSize * (CurrentPage - 1) + 1;
                }
            }
        }

        public int PageEndIndex {
            get {
                if (PageSize * CurrentPage > _items.Count) {
                    return _items.Count;
                } else {
                    return PageSize * CurrentPage;
                }
            }
        }

        private int _pageSize;
        public int PageSize {
            get {
                return _pageSize;
            }
            set {
                _pageSize = value;
                RaisePropertyChanged();
            }
        }

        private int _currentPage;
        public int CurrentPage {
            get {
                return _currentPage;
            }
            set {
                if (value >= Pages.FirstOrDefault() && value <= Pages.LastOrDefault()) {
                    _currentPage = value;
                    RaisePropertyChanged();
                }

            }
        }

        private AsyncObservableCollection<int> _pages = new AsyncObservableCollection<int>();
        public AsyncObservableCollection<int> Pages {
            get {
                return _pages;
            }
            private set {
                _pages = value;
                RaisePropertyChanged();
            }
        }

        public int Count {
            get {
                return _items.Count;
            }
        }

        public ICommand FirstPageCommand { get; private set; }
        public ICommand PrevPageCommand { get; private set; }
        public ICommand NextPageCommand { get; private set; }
        public ICommand LastPageCommand { get; private set; }
        public ICommand PageByNumberCommand { get; private set; }
    }

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum SkyAtlasOrderByFieldsEnum {
        [Description("LblSize")]
        SIZEMAX,
        [Description("LblApparentMagnitude")]
        MAGNITUDE,
        [Description("LblConstellation")]
        CONSTELLATION,
        [Description("LblRA")]
        RA,
        [Description("LblDec")]
        DEC,
        [Description("LblSurfaceBrightness")]
        SURFACEBRIGHTNESS,
        [Description("LblObjectType")]
        DSOTYPE
    }

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum SkyAtlasOrderByDirectionEnum {
        [Description("LblDescending")]
        DESC,
        [Description("LblAscending")]
        ASC
    }
}
