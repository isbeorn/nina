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
using NINA.Profile;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.ViewModel {

    public class DockManagerVM : BaseVM {

        public DockManagerVM(IProfileService profileService/*IEnumerable<DockableVM> dockWindowViewModels*/) : base(profileService) {
            LoadAvalonDockLayoutCommand = new RelayCommand(LoadAvalonDockLayout);
            ResetDockLayoutCommand = new RelayCommand(ResetDockLayout, (object o) => _dockmanager != null);
        }

        private void ResetDockLayout(object arg) {
            if (MyMessageBox.MyMessageBox.Show(Locale.Loc.Instance["LblResetDockLayoutConfirmation"], Locale.Loc.Instance["LblResetDockLayout"], System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxResult.No) == System.Windows.MessageBoxResult.Yes) {
                lock (lockObj) {
                    _dockloaded = false;

                    var serializer = new Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer(_dockmanager);
                    serializer.LayoutSerializationCallback += (s, args) => {
                        var d = (DockableVM)args.Content;
                        d.IsVisible = true;
                        args.Content = d;
                    };

                    LoadDefaultLayout(serializer);
                }
            }
        }

        private ObservableCollection<DockableVM> _anchorables;

        public ObservableCollection<DockableVM> Anchorables {
            get {
                if (_anchorables == null) {
                    _anchorables = new ObservableCollection<DockableVM>();
                }
                return _anchorables;
            }
            private set {
                _anchorables = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<DockableVM> _anchorableTools;

        public ObservableCollection<DockableVM> AnchorableTools {
            get {
                if (_anchorableTools == null) {
                    _anchorableTools = new ObservableCollection<DockableVM>();
                }
                return _anchorableTools;
            }
            private set {
                _anchorableTools = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<DockableVM> _anchorableInfoPanels;

        public ObservableCollection<DockableVM> AnchorableInfoPanels {
            get {
                if (_anchorableInfoPanels == null) {
                    _anchorableInfoPanels = new ObservableCollection<DockableVM>();
                }
                return _anchorableInfoPanels;
            }
            private set {
                _anchorableInfoPanels = value;
                RaisePropertyChanged();
            }
        }

        private Xceed.Wpf.AvalonDock.DockingManager _dockmanager;
        private bool _dockloaded = false;
        private object lockObj = new object();

        public void LoadAvalonDockLayout(object o) {
            lock (lockObj) {
                if (!_dockloaded) {
                    _dockmanager = (Xceed.Wpf.AvalonDock.DockingManager)o;

                    var serializer = new Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer(_dockmanager);
                    serializer.LayoutSerializationCallback += (s, args) => {
                        args.Content = args.Content;
                    };

                    if (System.IO.File.Exists(Utility.AvalonDock.LayoutInitializer.LAYOUTFILEPATH)) {
                        try {
                            serializer.Deserialize(Utility.AvalonDock.LayoutInitializer.LAYOUTFILEPATH);
                            _dockloaded = true;
                        } catch (Exception ex) {
                            Logger.Error("Failed to load AvalonDock Layout. Loading default Layout!", ex);
                            using (var stream = new StringReader(Properties.Resources.avalondock)) {
                                serializer.Deserialize(stream);
                            }
                        }
                    } else {
                        LoadDefaultLayout(serializer);
                    }
                }
            }
        }

        private void LoadDefaultLayout(Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer serializer) {
            lock (lockObj) {
                using (var stream = new StringReader(Properties.Resources.avalondock)) {
                    serializer.Deserialize(stream);
                    _dockloaded = true;
                }
            }
        }

        public void SaveAvalonDockLayout() {
            lock (lockObj) {
                if (_dockloaded) {
                    var serializer = new Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer(_dockmanager);
                    serializer.Serialize(Utility.AvalonDock.LayoutInitializer.LAYOUTFILEPATH);
                }
            }
        }

        public ICommand LoadAvalonDockLayoutCommand { get; private set; }
        public ICommand ResetDockLayoutCommand { get; }
    }
}