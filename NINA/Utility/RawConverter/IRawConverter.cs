using NINA.Model.MyCamera;
using NINA.Utility.Enum;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Utility.RawConverter {

    internal class RawConverter {

        public static IRawConverter CreateInstance(RawConverterEnum converter) {
            switch (converter) {
                case Enum.RawConverterEnum.DCRAW:
                    return new DCRaw();

                case Enum.RawConverterEnum.FREEIMAGE:
                    return new FreeImageConverter();

                default:
                    return new DCRaw();
            }
        }
    }

    internal interface IRawConverter {

        Task<ImageArray> ConvertToImageArray(MemoryStream s, CancellationToken token, bool calculateStatistics, int histogramResolution);
    }
}