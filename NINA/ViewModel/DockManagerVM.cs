using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.ViewModel {
    public class DockManagerVM : BaseVM {

        public DockManagerVM(IEnumerable<DockableVM> dockWindowViewModels) {
            this.Documents = new ObservableCollection<DockableVM>();
            this.Anchorables = new ObservableCollection<object>();

            foreach (var document in dockWindowViewModels) {
                document.PropertyChanged += DockWindowViewModel_PropertyChanged;
                if (!document.IsClosed)
                    this.Documents.Add(document);
            }
        }

        /// <summary>Gets a collection of all visible documents</summary>
        private ObservableCollection<DockableVM> _documents;
        public ObservableCollection<DockableVM> Documents {
            get {
                if(_documents == null) {
                    _documents = new ObservableCollection<DockableVM>();
                }
                return _documents;
            }
            private set {
                _documents = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<object> _anchorables;
        public ObservableCollection<object> Anchorables {
            get {
                if (_anchorables == null) {
                    _anchorables = new ObservableCollection<object>();
                }
                return _anchorables;
            }
            private set {
                _anchorables = value;
                RaisePropertyChanged();
            }
        }

        private void DockWindowViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            DockableVM document = sender as DockableVM;

            if (e.PropertyName == nameof(DockableVM.IsClosed)) {
                if (!document.IsClosed)
                    this.Documents.Add(document);
                else
                    this.Documents.Remove(document);
            }
        }

    }
}
