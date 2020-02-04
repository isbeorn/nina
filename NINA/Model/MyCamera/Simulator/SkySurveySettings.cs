#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

namespace NINA.Model.MyCamera.Simulator {

    public class SkySurveySettings : BaseINPC {

        public SkySurveySettings() {
            WidthAndHeight = 1000;
            FieldOfView = 1;
            RAError = 0;
            DecError = 0;
        }

        private object lockObj = new object();
        private int decError;

        public int DecError {
            get => decError;
            set {
                lock (lockObj) {
                    decError = value;
                }
                RaisePropertyChanged();
            }
        }

        private int raError;

        public int RAError {
            get => raError;
            set {
                lock (lockObj) {
                    raError = value;
                }
                RaisePropertyChanged();
            }
        }

        private int widthAndHeight;

        public int WidthAndHeight {
            get => widthAndHeight;
            set {
                lock (lockObj) {
                    widthAndHeight = value;
                }
                RaisePropertyChanged();
            }
        }

        private double fieldOfView;

        public double FieldOfView {
            get => fieldOfView;
            set {
                lock (lockObj) {
                    fieldOfView = value;
                }
                RaisePropertyChanged();
            }
        }
    }
}