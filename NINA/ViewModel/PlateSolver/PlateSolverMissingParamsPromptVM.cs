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

using NINA.PlateSolving;
using NINA.Utility;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace NINA.ViewModel.PlateSolver {

    internal class PlateSolverMissingParamsPromptVM : BaseINPC {

        public class ParameterDetails {
            public string Property { get; private set; }
            public double? Value { get; private set; }

            public ParameterDetails(string property, double? value) {
                this.Property = property;
                this.Value = value;
            }
        }

        public class ObservableParameterDetails : BaseINPC {

            public ObservableParameterDetails(ParameterDetails parameterDetails) {
                this.Property = parameterDetails.Property;
                this.Label = Locale.Loc.Instance[PlateSolveParameter.GetLabelForOptionalProperty(this.Property)];
                this.Value = parameterDetails.Value;
            }

            public string Property { get; private set; }
            public string Label { get; private set; }

            private double? _value;

            public double? Value {
                get {
                    return _value;
                }
                set {
                    _value = value;
                    RaisePropertyChanged();
                }
            }
        }

        public PlateSolverMissingParamsPromptVM(IReadOnlyCollection<string> properties) {
            var parametersBuilder = ImmutableList.CreateBuilder<ObservableParameterDetails>();
            foreach (var property in properties) {
                var parameterDetails = new ParameterDetails(property: property, value: null);
                var observableParameter = new ObservableParameterDetails(parameterDetails);
                parametersBuilder.Add(observableParameter);
                observableParameter.PropertyChanged += ObservableParameter_PropertyChanged;
            }

            this.Parameters = parametersBuilder.ToImmutable();
            this.ContinueCommand = new RelayCommand(ContinuePlateSolver);
            this.CancelCommand = new RelayCommand(CancelPlateSolver);
        }

        public ImmutableList<ObservableParameterDetails> Parameters { get; }
        public bool Continue { get; private set; } = false;
        public RelayCommand ContinueCommand { get; }
        public RelayCommand CancelCommand { get; }

        private void ObservableParameter_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            this.ChildChanged(sender, e);
        }

        private void CancelPlateSolver(object obj) {
            Continue = false;
        }

        private void ContinuePlateSolver(object obj) {
            Continue = true;
        }
    }
}