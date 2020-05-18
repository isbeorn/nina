using NINA.Utility.SerialCommunication;
using System.Collections.ObjectModel;

namespace NINA.Utility.FlatDeviceSDKs.PegasusAstroSDK {

    public interface IPegasusFlatMaster : ISerialSdk {
        ReadOnlyCollection<string> PortNames { get; }

        bool InitializeSerialPort(string portName, object client);
    }
}