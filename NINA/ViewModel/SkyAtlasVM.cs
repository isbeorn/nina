using NINA.Model;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Notification;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.ViewModel {
    public class SkyAtlasVM : BaseVM {
        public SkyAtlasVM() {
            SearchCommand = new AsyncCommand<bool>(() => Search());
            CancelSearchCommand = new RelayCommand(CancelSearch);
            



            InitializeFilters();

            Mediator.Instance.Register((object o) => {
                _nightDuration = null; //Clear cache
                InitializeElevationFilters();
            },MediatorMessages.LocationChanged);
            
        }

        

        private CancellationTokenSource _searchTokenSource;

        private void CancelSearch(object obj) {
            _searchTokenSource?.Cancel();
        }


        private AsyncObservableCollection<KeyValuePair<DateTime,double>> _nightDuration;
        public AsyncObservableCollection<KeyValuePair<DateTime,double>> NightDuration {
            get {
                if(_nightDuration == null) {

                    DateTime d = GetChartReferenceStartDate();

                    var twilight = Astrometry.GetNightTimes(d);
                    
                    if(twilight != null) { 
                        _nightDuration = new AsyncObservableCollection<KeyValuePair<DateTime,double>>() {
                        new KeyValuePair<DateTime,double>(twilight.RiseDate, 90),
                        new KeyValuePair<DateTime,double>(twilight.SetDate, 90) };
                    } else {
                        _nightDuration = new AsyncObservableCollection<KeyValuePair<DateTime,double>>();
                    }
                }
                return _nightDuration;
            }
        }

        private DateTime GetChartReferenceStartDate() {
            DateTime d;
            if (DateTime.Now.Hour > 12) {
                d = new DateTime(DateTime.Now.Year,DateTime.Now.Month,DateTime.Now.Day,12,0,0);
            }
            else {
                d = new DateTime(DateTime.Now.Year,DateTime.Now.Month,DateTime.Now.Day - 1,12,0,0);
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
                        SelectedDecFrom,
                        SelectedDecThrough,
                        SelectedSizeFrom,
                        SelectedSizeThrough,
                        types,
                        SelectedBrightnessFrom,
                        SelectedBrightnessThrough,
                        SelectedMagnitudeFrom,
                        SelectedMagnitudeThrough);


                    var longitude = Settings.Longitude;
                    var latitude = Settings.Latitude;

                    DateTime d = GetChartReferenceStartDate();

                    var siderealTime = Astrometry.GetLocalSiderealTime(d,longitude);

                    Parallel.ForEach(result,(obj) => {
                        var cloneDate = d;
                        obj.CalculateAltitude(cloneDate,siderealTime,latitude,longitude);
                        _searchTokenSource.Token.ThrowIfCancellationRequested();
                    });

                    /* Check if Altitude Filter is not default */
                    if (!(SelectedAltitudeTimeFrom == DateTime.MinValue && SelectedAltitudeTimeThrough == DateTime.MaxValue && SelectedMinimumAltitudeDegrees == 0)) {
                        /* Apply Altitude Filter */
                        SearchResult = new AsyncObservableCollection<DeepSkyObject>(result.Where((x) => {
                            return x.Altitudes.Where((y) => {
                                return (y.Key > SelectedAltitudeTimeFrom && y.Key < SelectedAltitudeTimeThrough);
                            }).All((z) => {
                                return z.Value > SelectedMinimumAltitudeDegrees;
                            });
                        }));
                    }
                    else {
                        SearchResult = result;
                    }
                } catch(OperationCanceledException) {

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

            DateTime d;
            if (DateTime.Now.Hour > 12) {
                d = new DateTime(DateTime.Now.Year,DateTime.Now.Month,DateTime.Now.Day,12,0,0);
            }
            else {
                d = new DateTime(DateTime.Now.Year,DateTime.Now.Month,DateTime.Now.Day - 1,12,0,0);
            }

            AltitudeTimesFrom.Add(DateTime.MinValue);
            AltitudeTimesThrough.Add(DateTime.MaxValue);

            for (int i = 0; i <= 24; i++) {                            
                AltitudeTimesFrom.Add(d);
                AltitudeTimesThrough.Add(d);
                d = d.AddHours(1);
            }

            for (int i = 0;i <= 90;i += 10) {
                MinimumAltitudeDegrees.Add(new KeyValuePair<double,string>(i,i + "°"));
            }
        }

        private void InitializeSizeFilters() {
            SizesFrom = new AsyncObservableCollection<KeyValuePair<string, string>>();
            SizesThrough = new AsyncObservableCollection<KeyValuePair<string,string>>();

            SizesFrom.Add(new KeyValuePair<string,string>(string.Empty,string.Empty));
            SizesThrough.Add(new KeyValuePair<string,string>(string.Empty,string.Empty));


            SizesFrom.Add(new KeyValuePair<string,string>("1","1 " + Locale.Loc.Instance["LblArcsec"]));
            SizesFrom.Add(new KeyValuePair<string,string>("5","5 " + Locale.Loc.Instance["LblArcsec"]));
            SizesFrom.Add(new KeyValuePair<string,string>("10","10 " + Locale.Loc.Instance["LblArcsec"]));
            SizesFrom.Add(new KeyValuePair<string,string>("30","30 " + Locale.Loc.Instance["LblArcsec"]));
            SizesFrom.Add(new KeyValuePair<string,string>("60","1 " + Locale.Loc.Instance["LblArcmin"]));
            SizesFrom.Add(new KeyValuePair<string,string>("300","5 " + Locale.Loc.Instance["LblArcmin"]));
            SizesFrom.Add(new KeyValuePair<string,string>("600","10 " + Locale.Loc.Instance["LblArcmin"]));
            SizesFrom.Add(new KeyValuePair<string,string>("1800","30 " + Locale.Loc.Instance["LblArcmin"]));
            SizesFrom.Add(new KeyValuePair<string,string>("3600","1 " + Locale.Loc.Instance["LblDegree"]));
            SizesFrom.Add(new KeyValuePair<string,string>("18000","5 " + Locale.Loc.Instance["LblDegree"]));
            SizesFrom.Add(new KeyValuePair<string,string>("36000","10 " + Locale.Loc.Instance["LblDegree"]));

            SizesThrough = new AsyncObservableCollection<KeyValuePair<string,string>>(SizesFrom);
        }

        private void InitializeBrightnessFilters() {
            BrightnessFrom = new AsyncObservableCollection<string>();
            BrightnessThrough = new AsyncObservableCollection<string>();

            BrightnessFrom.Add(string.Empty);
            BrightnessThrough.Add(string.Empty);
            
            for (var i = 2;i < 19;i++) {
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
            foreach(var t in l.Result) {
                ObjectTypes.Add(new DSOObjectType(t));
            }
        }

        private void InitializeMagnitudeFilters() {
            MagnitudesFrom = new AsyncObservableCollection<string>();
            MagnitudesThrough = new AsyncObservableCollection<string>();

            MagnitudesFrom.Add(string.Empty);
            MagnitudesThrough.Add(string.Empty);

            for(var i = 1; i < 22; i++) {
                MagnitudesFrom.Add(i.ToString());
                MagnitudesThrough.Add(i.ToString());
            }
        }


        private void InitializeRADecFilters() {
            RAFrom = new AsyncObservableCollection<KeyValuePair<double?,string>>();
            RAThrough = new AsyncObservableCollection<KeyValuePair<double?,string>>();
            DecFrom = new AsyncObservableCollection<KeyValuePair<double?,string>>();
            DecThrough = new AsyncObservableCollection<KeyValuePair<double?,string>>();

            RAFrom.Add(new KeyValuePair<double?,string>(null,string.Empty));
            RAThrough.Add(new KeyValuePair<double?,string>(null,string.Empty));
            DecFrom.Add(new KeyValuePair<double?,string>(null,string.Empty));
            DecThrough.Add(new KeyValuePair<double?,string>(null,string.Empty));
            
            SelectedRAFrom = null;
            SelectedRAThrough = null;
            SelectedDecFrom = null;
            SelectedDecThrough = null;

            for (int i = 0;i < 25;i++) {

                Astrometry.HoursToDegrees(i);

                RAFrom.Add(new KeyValuePair<double?,string>(Astrometry.HoursToDegrees(i), i.ToString()));
                RAThrough.Add(new KeyValuePair<double?,string>(Astrometry.HoursToDegrees(i),i.ToString()));
            }
            for (int i = -90;i < 91;i = i + 5) {
                DecFrom.Add(new KeyValuePair<double?,string>(i,i.ToString()));
                DecThrough.Add(new KeyValuePair<double?,string>(i,i.ToString()));
            }
        }

        private string _searchObjectName;
        private AsyncObservableCollection<DSOObjectType> _objectTypes;
        private AsyncObservableCollection<string> _constellations;
        private string _selectedConstellation;
        private AsyncObservableCollection<KeyValuePair<double?,string>> _rAFrom;
        private AsyncObservableCollection<KeyValuePair<double?,string>> _rAThrough;
        private AsyncObservableCollection<KeyValuePair<double?,string>> _decFrom;
        private AsyncObservableCollection<KeyValuePair<double?,string>> _decThrough;
        private double? _selectedRAFrom;
        private double? _selectedRAThrough;
        private double? _selectedDecFrom;
        private double? _selectedDecThrough;
        private AsyncObservableCollection<string> _brightnessFrom;
        private AsyncObservableCollection<string> _brightnessThrough;
        private string _selectedBrightnessFrom;
        private string _selectedBrightnessThrough;
        private AsyncObservableCollection<KeyValuePair<string,string>> _sizesFrom;
        private AsyncObservableCollection<KeyValuePair<string,string>> _sizesThrough;
        private string _selectedSizeFrom;
        private string _selectedSizeThrough;
        private AsyncObservableCollection<string> _magnitudesFrom;
        private AsyncObservableCollection<string> _magnitudesThrough;
        private string _selectedMagnitudeFrom;
        private string _selectedMagnitudeThrough;
        private AsyncObservableCollection<DeepSkyObject> _searchResult;
        private AsyncObservableCollection<DateTime> _altitudeTimesFrom;
        private AsyncObservableCollection<DateTime> _altitudeTimesThrough;
        private AsyncObservableCollection<KeyValuePair<double,string>> _minimumAltitudeDegrees;
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
        public AsyncObservableCollection<KeyValuePair<double,string>> MinimumAltitudeDegrees {
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

        public AsyncObservableCollection<KeyValuePair<double?,string>> RAFrom {
            get {
                return _rAFrom;
            }

            set {
                _rAFrom = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<KeyValuePair<double?,string>> RAThrough {
            get {
                return _rAThrough;
            }

            set {
                _rAThrough = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<KeyValuePair<double?,string>> DecFrom {
            get {
                return _decFrom;
            }

            set {
                _decFrom = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<KeyValuePair<double?,string>> DecThrough {
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

        public AsyncObservableCollection<KeyValuePair<string,string>> SizesFrom {
            get {
                return _sizesFrom;
            }

            set {
                _sizesFrom = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<KeyValuePair<string,string>> SizesThrough {
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
        
        public AsyncObservableCollection<DeepSkyObject> SearchResult {
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
                Selected = false;
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
}
