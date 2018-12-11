#region "copyright"

/*
    Copyright © 2016 - 2018 Stefan Berg <isbeorn86+NINA@googlemail.com>

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
using NINA.Utility.Profile;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace NINA.ViewModel {

    public class DockManagerVM : BaseVM {

        public DockManagerVM(IProfileService profileService/*IEnumerable<DockableVM> dockWindowViewModels*/) : base(profileService) {
            LoadAvalonDockLayoutCommand = new RelayCommand(LoadAvalonDockLayout);

            /*foreach (var document in dockWindowViewModels) {
                document.PropertyChanged += DockWindowViewModel_PropertyChanged;
                if (!document.IsClosed)
                    this.Documents.Add(document);
            }*/
        }

        /// <summary>
        /// Gets a collection of all visible documents
        /// </summary>
        private ObservableCollection<DockableVM> _documents;

        public ObservableCollection<DockableVM> Documents {
            get {
                if (_documents == null) {
                    _documents = new ObservableCollection<DockableVM>();
                }
                return _documents;
            }
            private set {
                _documents = value;
                RaisePropertyChanged();
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

        private Xceed.Wpf.AvalonDock.DockingManager _dockmanager;
        private bool _dockloaded = false;

        public void LoadAvalonDockLayout(object o) {
            if (!_dockloaded) {
                _dockmanager = (Xceed.Wpf.AvalonDock.DockingManager)o;
                var serializer = new Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer(_dockmanager);
                serializer.LayoutSerializationCallback += (s, args) => {
                    args.Content = args.Content;
                };

                if (System.IO.File.Exists(Utility.AvalonDock.LayoutInitializer.LAYOUTFILEPATH)) {
                    serializer.Deserialize(Utility.AvalonDock.LayoutInitializer.LAYOUTFILEPATH);
                }
                _dockloaded = true;
            }
        }

        public void SaveAvalonDockLayout() {
            var serializer = new Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer(_dockmanager);
            serializer.Serialize(Utility.AvalonDock.LayoutInitializer.LAYOUTFILEPATH);
        }

        public ICommand LoadAvalonDockLayoutCommand { get; private set; }

        /*private void DockWindowViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            DockableVM document = sender as DockableVM;

            if (e.PropertyName == nameof(DockableVM.IsClosed)) {
                if (!document.IsClosed)
                    this.Documents.Add(document);
                else
                    this.Documents.Remove(document);
            }
        }*/
    }
}