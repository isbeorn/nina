using NINA.Model;
using NINA.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace NINA.ViewModel {
    class TelescopeVM : BaseVM {
        public TelescopeVM() {
            Name = "Telescope";            
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["TelescopeSVG"];
            Telescope = new TelescopeModel();
            ChooseTelescopeCommand = new RelayCommand(chooseTelescope);
            DisconnectCommand = new RelayCommand(disconnectTelescope);
            StepperMoveRateCommand = new RelayCommand(stepMoveRate);
            ParkCommand = new AsyncCommand<bool>(parkTelescope);
            UnparkCommand = new RelayCommand(unparkTelescope);
            SlewToCoordinatesCommand = new RelayCommand(slewToCoordinates);

            MoveCommand = new RelayCommand(move);
            StopMoveCommand = new RelayCommand(stopMove);
            StopSlewCommand = new RelayCommand(stopSlew);

            _updateTelescope = new DispatcherTimer();
            _updateTelescope.Interval = TimeSpan.FromMilliseconds(300);
            _updateTelescope.Tick += updateTelescope_Tick;
        }

        private void updateTelescope_Tick(object sender, EventArgs e) {            
            if (Telescope.Connected) {
                Telescope.updateValues();
            }            
        }

        private async Task<bool> parkTelescope() {
            return await Task.Run<bool>(() => { Telescope.park(); return true; }); 

        }

        private void unparkTelescope(object o) {
            Telescope.unpark();
        }

        private DispatcherTimer _updateTelescope;

       private TelescopeModel _telescope;
        public TelescopeModel Telescope {
            get {
                return _telescope;
            }
            set {
                _telescope = value;
                RaisePropertyChanged();
            }
        }

        private void chooseTelescope(object obj) {
            _updateTelescope.Stop();
            if (Telescope.connect()) {
                _updateTelescope.Start();                
            }
        }

        private void disconnectTelescope(object obj) {
            System.Windows.MessageBoxResult result = System.Windows.MessageBox.Show("Disconnect Telescope?", "", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxImage.Question, System.Windows.MessageBoxResult.Cancel);
            if (result == System.Windows.MessageBoxResult.OK) {
                _updateTelescope.Stop();
                Telescope.disconnect();
            }
        }

        private void stepMoveRate(object obj) {
            string cmd = obj.ToString();
            if(cmd == "+") {
                Telescope.MovingRate++;
            } else {
                Telescope.MovingRate--;
            }
        }

        private void move(object obj) {
            string cmd = obj.ToString();
            if(cmd == "W") {                                
                Telescope.moveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisPrimary, -Telescope.MovingRate);
            }
            if (cmd == "O") {
                Telescope.moveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisPrimary, Telescope.MovingRate);
            }
            if (cmd == "N") {
                Telescope.moveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisSecondary, Telescope.MovingRate);
            }
            if (cmd == "S") {                
                Telescope.moveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisSecondary, -Telescope.MovingRate);
            }
        }

        private void stopMove(object obj) {
            string cmd = obj.ToString();
            if (cmd == "W") {
                Telescope.moveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisPrimary, 0);
            }
            if (cmd == "O") {
                Telescope.moveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisPrimary, 0);
            }
            if (cmd == "N") {
                Telescope.moveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisSecondary, 0);
            }
            if (cmd == "S") {
                Telescope.moveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisSecondary, 0);
            }
        }

        private void stopSlew(object obj) {
            Telescope.stopSlew();
        }

        private double _targetDeclination;
        private double _targetRightAscencion;
        public double TargetDeclination {
            get {
                return _targetDeclination;
            }

            set {
                _targetDeclination = value;
                RaisePropertyChanged();
            }
        }

        public double TargetRightAscencion {
            get {
                return _targetRightAscencion;
            }

            set {
                _targetRightAscencion = value;
                RaisePropertyChanged();
            }
        }

        private void slewToCoordinates(object obj) {
            Telescope.slewToCoordinates(TargetRightAscencion, TargetDeclination);
        }

        private ICommand _slewToCoordinatesCommand;
        public ICommand SlewToCoordinatesCommand {
            get {
                return _slewToCoordinatesCommand;
            }
            set {
                _slewToCoordinatesCommand = value;
                RaisePropertyChanged();
            }
        }

        private ICommand _chooseTelescopeCommand;
        public ICommand ChooseTelescopeCommand {
            get {
                return _chooseTelescopeCommand;
            }
            set {
                _chooseTelescopeCommand = value;
                RaisePropertyChanged();
            }
        }

        private ICommand _disconnectCommand;
        public ICommand DisconnectCommand {
            get {
                return _disconnectCommand;
            }
            set {
                _disconnectCommand = value;
                RaisePropertyChanged();
            }
        }

        private ICommand _moveCommand;
        public ICommand MoveCommand {
            get {
                return _moveCommand;
            }
            set {
                _moveCommand = value;
                RaisePropertyChanged();
            }
        }

        private ICommand _stopMoveCommand;
        public ICommand StopMoveCommand {
            get {
                return _stopMoveCommand;
            }
            set {
                _stopMoveCommand = value;
                RaisePropertyChanged();
            }
        }

        private AsyncCommand<bool> _parkCommand;
        public AsyncCommand<bool> ParkCommand {
            get {
                return _parkCommand;
            }
            set {
                _parkCommand = value;
                RaisePropertyChanged();
            }
        }

        private ICommand _unparkCommand;
        public ICommand UnparkCommand {
            get {
                return _unparkCommand;
            }
            set {
                _unparkCommand = value;
                RaisePropertyChanged();
            }
        }

        private ICommand _stopSlewCommand;
        public ICommand StopSlewCommand {
            get {
                return _stopSlewCommand;
            }
            set {
                _stopSlewCommand = value;
                RaisePropertyChanged();
            }
        }

        private ICommand _stepperMoveRateCommand;
        public ICommand StepperMoveRateCommand {
            get {
                return _stepperMoveRateCommand;
            }
            set {
                _stepperMoveRateCommand = value;
                RaisePropertyChanged();
            }
        }

        
    }
}
