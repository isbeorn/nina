using NINA.Model;
using NINA.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.ViewModel{
    class SequenceVM : BaseINPC {

        public SequenceVM() {
            AddSequenceCommand = new RelayCommand(AddSequence);
        }
        

        private ObservableCollection<SequenceModel> _sequence;
        public ObservableCollection<SequenceModel> Sequence {
            get {
                if(_sequence == null) {
                    _sequence = new ObservableCollection<SequenceModel>();
                    _sequence.Add(new SequenceModel());
                }
                return _sequence;
            }
            set {
                _sequence = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<string> _imageTypes;
        public ObservableCollection<string> ImageTypes {
            get {
                if(_imageTypes == null) {
                    _imageTypes = new ObservableCollection<string>();
                    _imageTypes.Add(Model.SequenceModel.ImageTypes.LIGHT);
                    _imageTypes.Add(Model.SequenceModel.ImageTypes.DARK);
                    _imageTypes.Add(Model.SequenceModel.ImageTypes.FLAT);
                    _imageTypes.Add(Model.SequenceModel.ImageTypes.BIAS);
                }
                return _imageTypes;
            }
            set {
                _imageTypes = value;
                RaisePropertyChanged();
            }
        }

        public void AddSequence(object o) {
            Sequence.Add(new SequenceModel());
        }

        private ICommand _addSequenceCommand;
        public ICommand AddSequenceCommand {
            get { return _addSequenceCommand; }
            set { _addSequenceCommand = value;  RaisePropertyChanged(); }
        }
    }
}
