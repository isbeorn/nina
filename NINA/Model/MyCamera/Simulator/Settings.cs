#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Utility;

namespace NINA.Model.MyCamera.Simulator {

    public class Settings : BaseINPC {
        private CameraType type = CameraType.RANDOM;

        public CameraType Type {
            get => type;
            set {
                type = value;

                RaisePropertyChanged();
            }
        }

        public RandomSettings RandomSettings { get; set; } = new RandomSettings();
        public ImageSettings ImageSettings { get; set; } = new ImageSettings();
        public SkySurveySettings SkySurveySettings { get; set; } = new SkySurveySettings();
    }
}