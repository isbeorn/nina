using ASCOM.DeviceInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyFocuser {
    public interface IFocuserV3Ex : IFocuserV3 {
        Task MoveAsync(int position, CancellationToken ct);
    }
}
