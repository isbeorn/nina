#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Astrometry;
using NINA.Core.Utility;
using System.Windows.Input;

namespace NINA.ViewModel.FramingAssistant {
    public class FramingPlateSolveParameter {
        public FramingPlateSolveParameter(Coordinates referenceCoordinates, double focalLength, double pixelSize, int binX) {
            Coordinates = new InputCoordinates(referenceCoordinates);
            FocalLength = focalLength;
            PixelSize = pixelSize;
            Binning = binX;
                        
            BlindSolveCommand = new RelayCommand((o) => DoBlindSolve = true);
            SolveCommand = new RelayCommand((o) => DoBlindSolve = false);

        }

        public InputCoordinates Coordinates { get; set; }
        public double FocalLength { get; set; }
        public double PixelSize { get; set; }
        public int Binning { get; set; }
        public bool? DoBlindSolve { get; private set; }
        public ICommand BlindSolveCommand { get; }
        public ICommand SolveCommand { get; }
    }
}