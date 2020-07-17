#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FTD2XX_NET;
using NINA.MGEN.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.MGEN.Commands.AppMode {

    public class QueryCalibrationCommand : AutoGuidingCommand<CalibrationStatusResult> {
        public override byte SubCommandCode { get; } = 0x29;

        protected override CalibrationStatusResult ExecuteSubCommand(IFTDI device) {
            Write(device, SubCommandCode);
            var data = Read(device, 3);
            if (data[0] == 0x00) {
                var state = data[1];
                var errorByte = data[2];
                var error = string.Empty;
                var calibrationStatus = CalibrationStatus.NotStarted;
                switch (state) {
                    case 0x00:
                        //CalibrationStatus = "Not yet started";
                        calibrationStatus = CalibrationStatus.NotStarted;
                        break;

                    case 0x01:
                        //CalibrationStatus = "Measuring start position";
                        calibrationStatus = CalibrationStatus.MeasuringStartPosition;
                        break;

                    case 0x02:
                        //CalibrationStatus = "Moving DEC, eliminating backlash.";
                        calibrationStatus = CalibrationStatus.MovingDecEliminatingBacklash;
                        break;

                    case 0x03:
                        //CalibrationStatus = "Measuring / moving DEC.";
                        calibrationStatus = CalibrationStatus.MeasuringDec;
                        break;

                    case 0x04:
                        //CalibrationStatus = "Measuring / moving RA.";
                        calibrationStatus = CalibrationStatus.MeasuringRA;
                        break;

                    case 0x05:
                        //CalibrationStatus = "Almost done, moving DEC back to original pos";
                        calibrationStatus = CalibrationStatus.AlmostDone;
                        break;

                    case 0xff:

                        switch (errorByte) {
                            case 0x00:
                                calibrationStatus = CalibrationStatus.Done;
                                break;

                            case 0x01:
                                calibrationStatus = CalibrationStatus.Error;
                                error = "The user has canceled the calibration";
                                break;

                            case 0x02:
                                calibrationStatus = CalibrationStatus.Error;
                                error = "Star has been lost (or wasn't present)";
                                break;

                            case 0x04:
                                calibrationStatus = CalibrationStatus.Error;
                                error = "Fatal position error detected";
                                break;

                            case 0x05:
                                calibrationStatus = CalibrationStatus.Error;
                                error = "Orientation error detected";
                                break;
                        }
                        break;
                }
                return new CalibrationStatusResult(calibrationStatus, error);
            } else {
                throw new UnexpectedReturnCodeException();
            }
        }
    }

    public class CalibrationStatusResult : MGENResult {

        public CalibrationStatusResult(CalibrationStatus status) : base(true) {
            CalibrationStatus = status;
        }

        public CalibrationStatusResult(CalibrationStatus status, string error) : base(true) {
            CalibrationStatus = status;
            Error = error;
        }

        public CalibrationStatus CalibrationStatus { get; private set; }
        public string Error { get; private set; } = string.Empty;
    }
}