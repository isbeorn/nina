using NINA.Model;
using NINA.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.ViewModel{
    class SequenceVM : DockableVM {

        public SequenceVM() {
            Title = "LblSequence";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["SequenceSVG"];
            
            ContentId = nameof(SequenceVM);
            AddSequenceCommand = new RelayCommand(AddSequence);
            RemoveSequenceCommand = new RelayCommand(RemoveSequence);
            StartSequenceCommand = new AsyncCommand<bool>(() => StartSequence(new Progress<string>(p => Status = p)));
            CancelSequenceCommand = new RelayCommand(CancelSequence);

            RegisterMediatorMessages();
        }

        private void CancelSequence(object obj) {
            _canceltoken?.Cancel();
        }

        private CancellationTokenSource _canceltoken;

        private string _status;
        public string Status {
            get {
                return _status;
            }
            set {
                _status = value;
                RaisePropertyChanged();

                Mediator.Instance.Notify(MediatorMessages.StatusUpdate, _status);
            }
        }

        private async Task<bool> StartSequence(IProgress<string> progress) {
            _canceltoken = new CancellationTokenSource();
            await Mediator.Instance.NotifyAsync(AsyncMediatorMessages.StartSequence, new object[] { this.Sequence, true, _canceltoken, progress });
            return true;
        }

        private void RegisterMediatorMessages() {
            Mediator.Instance.Register((object o) => {
                var args = (object[])o;
                if (args.Length == 1) {
                    var dso = (DeepSkyObject)args[0];
                    Sequence.SetSequenceTarget(dso);
                }
            },MediatorMessages.SetSequenceCoordinates);
        }

        private CaptureSequenceList _sequence;
        public CaptureSequenceList Sequence {
            get {
                if(_sequence == null) {
                    var seq = new CaptureSequence();
                    _sequence = new CaptureSequenceList(seq);                    
                    SelectedSequenceIdx = _sequence.Count - 1;                    
                }
                return _sequence;
            }
            set {
                _sequence = value;
                RaisePropertyChanged();
            }
        }

        private int _selectedSequenceIdx;
        public int SelectedSequenceIdx {
            get {
                return _selectedSequenceIdx;
            }
            set {
                _selectedSequenceIdx = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<string> _imageTypes;
        public ObservableCollection<string> ImageTypes {
            get {
                if(_imageTypes == null) {
                    _imageTypes = new ObservableCollection<string>();

                    Type type = typeof(Model.CaptureSequence.ImageTypes); 
                    foreach (var p in type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)) {
                        var v = p.GetValue(null);
                        _imageTypes.Add(v.ToString());
                    }
                }
                return _imageTypes;
            }
            set {
                _imageTypes = value;
                RaisePropertyChanged();
            }
        }

        public void AddSequence(object o) {
            Sequence.Add(new CaptureSequence());
            SelectedSequenceIdx = Sequence.Count - 1;
        }

        private void RemoveSequence(object obj) {
            var idx = SelectedSequenceIdx;
            if(idx > -1) { 
                Sequence.RemoveAt(idx);            
                if(idx < Sequence.Count - 1) {
                    SelectedSequenceIdx = idx;
                } else {
                    SelectedSequenceIdx = Sequence.Count - 1;
                }
            }
        }
                
        public ICommand AddSequenceCommand { get; private set; }
        
        public ICommand RemoveSequenceCommand { get; private set; }
        
        public IAsyncCommand StartSequenceCommand { get; private set; }
        
        public ICommand CancelSequenceCommand { get; private set; }
    }
}
