using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Plugin.Interfaces {
    public interface ISubscriber {
        Task OnMessageReceived(IMessage message);
    }
}
