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

using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Notification;
using NINA.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace NINA.Model {

    [Serializable()]
    [XmlRoot(nameof(CaptureSequenceList))]
    public class CaptureSequenceList : BaseINPC {

        public CaptureSequenceList() {
            TargetName = string.Empty;
            Mode = SequenceMode.STANDARD;
            Coordinates = new Coordinates(0, 0, Epoch.J2000, Coordinates.RAType.Hours);
            AltitudeVisible = false;
        }

        private AsyncObservableCollection<CaptureSequence> _items = new AsyncObservableCollection<CaptureSequence>();

        [XmlElement(nameof(CaptureSequence))]
        public AsyncObservableCollection<CaptureSequence> Items {
            get {
                return _items;
            }
            set {
                _items = value;
                RaisePropertyChanged();
            }
        }

        public IEnumerator<CaptureSequence> GetEnumerator() {
            return Items.GetEnumerator();
        }

        public int Count {
            get {
                return Items.Where(i => i.Enabled).Count();
            }
        }

        public void Add(CaptureSequence s) {
            Items.Add(s);
            if (Items.Count(i => i.Enabled) == 1) {
                ActiveSequence = Items.First(i => i.Enabled);
            }
        }

        public void RemoveAt(int idx) {
            if (Items.Count > idx) {
                if (Items[idx] == ActiveSequence) {
                    if (idx == Items.Count - 1) {
                        ActiveSequence = null;
                    } else {
                        ActiveSequence = Items[idx + 1];
                    }
                }
                Items.RemoveAt(idx);
            }
        }

        public void Save(string path) {
            try {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(CaptureSequenceList));

                using (StreamWriter writer = new StreamWriter(path)) {
                    xmlSerializer.Serialize(writer, this);
                }
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(ex.Message);
            }
        }

        public static CaptureSequenceList Load(Stream stream, ICollection<MyFilterWheel.FilterInfo> filters, double latitude, double longitude) {
            CaptureSequenceList l = null;
            try {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(CaptureSequenceList));

                l = (CaptureSequenceList)xmlSerializer.Deserialize(stream);
                foreach (CaptureSequence s in l) {
                    if (s.FilterType != null) {
                        //first try to match by name; otherwise match by position.
                        var filter = filters.Where((f) => f.Name == s.FilterType.Name).FirstOrDefault();
                        if (filter == null) {
                            filter = filters.Where((f) => f.Position == s.FilterType.Position).FirstOrDefault();
                            if (filter == null) {
                                Notification.ShowWarning(string.Format(Locale.Loc.Instance["LblFilterNotFoundForPosition"], (s.FilterType.Position + 1)));
                            }
                        }
                        s.FilterType = filter;
                    }
                }
                if (l.ActiveSequence == null && l.Count > 0) {
                    l.ActiveSequence = l.Items.SkipWhile(x => x.TotalExposureCount - x.ProgressExposureCount == 0).FirstOrDefault();
                }
                l.DSO?.SetDateAndPosition(SkyAtlasVM.GetReferenceDate(DateTime.Now), latitude, longitude);
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(Locale.Loc.Instance["LblLoadSequenceFailed"] + Environment.NewLine + ex.Message);
            }
            return l;
        }

        public CaptureSequenceList(CaptureSequence seq) : this() {
            Add(seq);
        }

        public void SetSequenceTarget(DeepSkyObject dso) {
            TargetName = dso.Name;
            Coordinates = dso.Coordinates;
            Rotation = dso.Rotation;
            this.DSO = dso;
        }

        private string _targetName;

        [XmlAttribute(nameof(TargetName))]
        public string TargetName {
            get {
                return _targetName;
            }
            set {
                _targetName = value;
                RaisePropertyChanged();
            }
        }

        private SequenceMode _mode;

        [XmlAttribute(nameof(Mode))]
        public SequenceMode Mode {
            get {
                return _mode;
            }
            set {
                _mode = value;
                RaisePropertyChanged();
            }
        }

        private bool _isRunning;

        [XmlIgnore]
        public bool IsRunning {
            get {
                return _isRunning;
            }
            set {
                _isRunning = value;
                RaisePropertyChanged();
            }
        }

        private bool _isFinished;

        [XmlIgnore]
        public bool IsFinished {
            get {
                return _isFinished;
            }
            set {
                _isFinished = value;
                RaisePropertyChanged();
            }
        }

        public CaptureSequence GetNextSequenceItem(CaptureSequence currentItem) {
            if (Items.Count == 0) { return null; }

            CaptureSequence seq = currentItem;

            if (Mode == SequenceMode.STANDARD) {
                if (seq?.ProgressExposureCount == seq?.TotalExposureCount) {
                    //No exposures remaining. Get next Sequence
                    var idx = Items.IndexOf(seq) + 1;
                    seq = Items.Skip(idx).Where(i => i.Enabled).FirstOrDefault();
                    if (seq != null) {
                        return GetNextSequenceItem(seq);
                    }
                }
            } else if (Mode == SequenceMode.ROTATE) {
                //Check if all sequences are done
                if (Items.Where(x => x.Enabled).Count() == Items.Where(x => x.ProgressExposureCount == x.TotalExposureCount && x.Enabled).Count()) {
                    //All sequences done
                    return null;
                }

                if (seq == Items.FirstOrDefault(i => i.Enabled) && seq?.ProgressExposureCount == 0 && seq?.TotalExposureCount > 0) {
                    //first sequence active
                    seq = Items.First(i => i.Enabled);
                } else {
                    do {
                        var idx = (Items.IndexOf(seq) + 1) % Items.Count;
                        seq = Items[idx];
                    } while (!seq.Enabled);
                }

                if (seq.ProgressExposureCount == seq.TotalExposureCount) {
                    return this.GetNextSequenceItem(seq); //Search for next sequence where exposurecount > 0
                }
            }

            return seq;
        }

        public CaptureSequence Next() {
            if (Items.Count == 0) { return null; }

            ActiveSequence = GetNextSequenceItem(ActiveSequence);
            if (ActiveSequence != null) {
                ActiveSequence.ProgressExposureCount++;
            }
            return ActiveSequence;
        }

        private Coordinates _coordinates;

        [XmlElement(nameof(Coordinates))]
        public Coordinates Coordinates {
            get {
                return _coordinates;
            }
            set {
                _coordinates = value;
                RaiseCoordinatesChanged();
            }
        }

        [XmlAttribute(nameof(RAHours))]
        public int RAHours {
            get {
                return (int)Math.Truncate(_coordinates.RA);
            }
            set {
                if (value >= 0) {
                    _coordinates.RA = _coordinates.RA - RAHours + value;
                    RaiseCoordinatesChanged();
                }
            }
        }

        [XmlAttribute(nameof(RAMinutes))]
        public int RAMinutes {
            get {
                return (int)(Math.Floor(_coordinates.RA * 60.0d) % 60);
            }
            set {
                if (value >= 0) {
                    _coordinates.RA = _coordinates.RA - RAMinutes / 60.0d + value / 60.0d;
                    RaiseCoordinatesChanged();
                }
            }
        }

        [XmlAttribute(nameof(RASeconds))]
        public int RASeconds {
            get {
                return (int)(Math.Floor(_coordinates.RA * 60.0d * 60.0d) % 60);
            }
            set {
                if (value >= 0) {
                    _coordinates.RA = _coordinates.RA - RASeconds / (60.0d * 60.0d) + value / (60.0d * 60.0d);
                    RaiseCoordinatesChanged();
                }
            }
        }

        [XmlAttribute(nameof(DecDegrees))]
        public int DecDegrees {
            get {
                return (int)Math.Truncate(_coordinates.Dec);
            }
            set {
                if (value < 0) {
                    _coordinates.Dec = value - DecMinutes / 60.0d - DecSeconds / (60.0d * 60.0d);
                } else {
                    _coordinates.Dec = value + DecMinutes / 60.0d + DecSeconds / (60.0d * 60.0d);
                }
                RaiseCoordinatesChanged();
            }
        }

        [XmlAttribute(nameof(DecMinutes))]
        public int DecMinutes {
            get {
                return (int)Math.Floor((Math.Abs(_coordinates.Dec * 60.0d) % 60));
            }
            set {
                if (_coordinates.Dec < 0) {
                    _coordinates.Dec = _coordinates.Dec + DecMinutes / 60.0d - value / 60.0d;
                } else {
                    _coordinates.Dec = _coordinates.Dec - DecMinutes / 60.0d + value / 60.0d;
                }

                RaiseCoordinatesChanged();
            }
        }

        [XmlAttribute(nameof(DecSeconds))]
        public int DecSeconds {
            get {
                return (int)Math.Round((Math.Abs(_coordinates.Dec * 60.0d * 60.0d) % 60));
            }
            set {
                if (_coordinates.Dec < 0) {
                    _coordinates.Dec = _coordinates.Dec + DecSeconds / (60.0d * 60.0d) - value / (60.0d * 60.0d);
                } else {
                    _coordinates.Dec = _coordinates.Dec - DecSeconds / (60.0d * 60.0d) + value / (60.0d * 60.0d);
                }

                RaiseCoordinatesChanged();
            }
        }

        private double rotation;

        [XmlAttribute(nameof(Rotation))]
        public double Rotation {
            get {
                return rotation;
            }
            set {
                rotation = value;
                RaiseCoordinatesChanged();
            }
        }

        private void RaiseCoordinatesChanged() {
            RaisePropertyChanged(nameof(Coordinates));
            RaisePropertyChanged(nameof(RAHours));
            RaisePropertyChanged(nameof(RAMinutes));
            RaisePropertyChanged(nameof(RASeconds));
            RaisePropertyChanged(nameof(DecDegrees));
            RaisePropertyChanged(nameof(DecMinutes));
            RaisePropertyChanged(nameof(DecSeconds));
            AltitudeVisible = true;
            DSO.Name = this.TargetName;
            DSO.Coordinates = Coordinates;
            DSO.Rotation = Rotation;
        }

        private DeepSkyObject _dso;

        [XmlIgnore]
        public DeepSkyObject DSO {
            get {
                if (_dso == null) {
                    _dso = new DeepSkyObject(string.Empty, Coordinates, string.Empty);
                }
                return _dso;
            }
            private set {
                _dso = value;
                /*_dso.SetDateAndPosition(
                    SkyAtlasVM.GetReferenceDate(DateTime.Now),
                    profileService.ActiveProfile.AstrometrySettings.Latitude,
                    profileService.ActiveProfile.AstrometrySettings.Longitude
                );*/
                RaisePropertyChanged();
            }
        }

        private object lockobj = new object();
        private CaptureSequence _activeSequence;

        [XmlIgnore]
        public CaptureSequence ActiveSequence {
            get {
                lock (lockobj) {
                    if (_activeSequence != null && !_activeSequence.Enabled) {
                        _activeSequence = Items.FirstOrDefault(i => i.Enabled);
                        RaisePropertyChanged(nameof(ActiveSequence));
                        RaisePropertyChanged(nameof(ActiveSequenceIndex));
                    }
                    return _activeSequence;
                }
            }
            private set {
                lock (lockobj) {
                    _activeSequence = value;
                    RaisePropertyChanged(nameof(ActiveSequence));
                    RaisePropertyChanged(nameof(ActiveSequenceIndex));
                }
            }
        }

        public int ActiveSequenceIndex {
            get {
                return Items.IndexOf(_activeSequence) == -1 ? -1 : Items.IndexOf(_activeSequence) + 1;
            }
        }

        private int _delay;

        [XmlAttribute(nameof(Delay))]
        public int Delay {
            get {
                return _delay;
            }
            set {
                _delay = value;
                RaisePropertyChanged();
            }
        }

        private bool _slewToTarget;

        [XmlAttribute(nameof(SlewToTarget))]
        public bool SlewToTarget {
            get {
                return _slewToTarget;
            }
            set {
                _slewToTarget = value;
                if (!_slewToTarget) { CenterTarget = _slewToTarget; }
                RaisePropertyChanged();
            }
        }

        private bool _autoFocusOnStart;

        [XmlAttribute(nameof(AutoFocusOnStart))]
        public bool AutoFocusOnStart {
            get {
                return _autoFocusOnStart;
            }
            set {
                _autoFocusOnStart = value;
                RaisePropertyChanged();
            }
        }

        private bool _centerTarget;

        [XmlAttribute(nameof(CenterTarget))]
        public bool CenterTarget {
            get {
                return _centerTarget;
            }
            set {
                _centerTarget = value;
                if (_centerTarget) { SlewToTarget = _centerTarget; }
                RaisePropertyChanged();
            }
        }

        private bool _startGuiding;

        [XmlAttribute(nameof(StartGuiding))]
        public bool StartGuiding {
            get {
                return _startGuiding;
            }
            set {
                _startGuiding = value;
                RaisePropertyChanged();
            }
        }

        private bool _autoFocusOnFilterChange;

        [XmlAttribute(nameof(AutoFocusOnFilterChange))]
        public bool AutoFocusOnFilterChange {
            get {
                return _autoFocusOnFilterChange;
            }
            set {
                _autoFocusOnFilterChange = value;
                RaisePropertyChanged();
            }
        }

        private bool _autoFocusAfterSetTime;

        [XmlAttribute(nameof(AutoFocusAfterSetTime))]
        public bool AutoFocusAfterSetTime {
            get {
                return _autoFocusAfterSetTime;
            }
            set {
                _autoFocusAfterSetTime = value;
                RaisePropertyChanged();
            }
        }

        private double _autoFocusSetTime = 30;

        [XmlAttribute(nameof(AutoFocusSetTime))]
        public double AutoFocusSetTime {
            get {
                return _autoFocusSetTime;
            }
            set {
                _autoFocusSetTime = value;
                RaisePropertyChanged();
            }
        }

        private bool _autoFocusAfterSetExposures;

        [XmlAttribute(nameof(AutoFocusAfterSetExposures))]
        public bool AutoFocusAfterSetExposures {
            get {
                return _autoFocusAfterSetExposures;
            }
            set {
                _autoFocusAfterSetExposures = value;
                RaisePropertyChanged();
            }
        }

        private double _autoFocusSetExposures = 10;

        [XmlAttribute(nameof(AutoFocusSetExposures))]
        public double AutoFocusSetExposures {
            get {
                return _autoFocusSetExposures;
            }
            set {
                _autoFocusSetExposures = value;
                RaisePropertyChanged();
            }
        }

        private bool _autoFocusAfterTemperatureChange = false;

        [XmlAttribute(nameof(AutoFocusAfterTemperatureChange))]
        public bool AutoFocusAfterTemperatureChange {
            get {
                return _autoFocusAfterTemperatureChange;
            }
            set {
                _autoFocusAfterTemperatureChange = value;
                RaisePropertyChanged();
            }
        }

        private double _autoFocusAfterTemperatureChangeAmount = 5;

        [XmlAttribute(nameof(AutoFocusAfterTemperatureChangeAmount))]
        public double AutoFocusAfterTemperatureChangeAmount {
            get {
                return _autoFocusAfterTemperatureChangeAmount;
            }
            set {
                _autoFocusAfterTemperatureChangeAmount = value;
                RaisePropertyChanged();
            }
        }

        private bool _altitudeVisisble;

        [XmlIgnore]
        public bool AltitudeVisible {
            get {
                return _altitudeVisisble;
            }
            private set {
                _altitudeVisisble = value;
                RaisePropertyChanged();
            }
        }
    }
}