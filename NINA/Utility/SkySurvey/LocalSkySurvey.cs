using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Utility.Astrometry;

namespace NINA.Utility.SkySurvey {

    internal class LocalSkySurvey : ISkySurvey {

        public Task<SkySurveyImage> GetImage(Coordinates coordinates, double fieldOfView, CancellationToken ct, IProgress<int> progress) {
            throw new NotImplementedException();
        }
    }
}