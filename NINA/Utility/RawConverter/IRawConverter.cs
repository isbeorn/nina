using NINA.Model.MyCamera;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Utility.RawConverter {
    class RawConverter {
        public static IRawConverter CreateInstance() {
            //todo new config var
            //return new DCRaw();
            return new FreeImageConverter();
        }
    }

    interface IRawConverter {
        Task<ImageArray> ConvertToImageArray(MemoryStream s, CancellationToken token);
    }
}
