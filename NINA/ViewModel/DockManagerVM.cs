using NINA.Utility;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace NINA.ViewModel {

    public class DockManagerVM : BaseVM {

        public DockManagerVM(/*IEnumerable<DockableVM> dockWindowViewModels*/) {
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