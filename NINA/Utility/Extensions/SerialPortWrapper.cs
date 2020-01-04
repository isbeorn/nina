//below is for testing purposes only

using System;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;

namespace NINA.Utility.Extensions {

    public class SerialPortProvider : ISerialPortProvider {

        public ISerialPort GetSerialPort(string portName) {
            return new SerialPortWrapper {
                PortName = portName,
                BaudRate = 9600,
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                DtrEnable = false,
                NewLine = "\n",
                ReadTimeout = 500,
                WriteTimeout = 500
            };
        }

        public ReadOnlyCollection<string> GetPortNames() {
            return new ReadOnlyCollection<string>(SerialPort.GetPortNames().OrderBy(s => s).ToList());
        }
    }

    public interface ISerialPortProvider {

        ISerialPort GetSerialPort(string portName);

        ReadOnlyCollection<string> GetPortNames();
    }

    public interface ISerialPort : IDisposable {
        string PortName { get; set; }
        bool DtrEnable { get; set; }

        void Write(string value);

        string ReadLine();

        void Open();

        void Close();
    }

    public sealed class SerialPortWrapper : ISerialPort {
        private readonly SerialPort _serialPort;

        public SerialPortWrapper() {
            _serialPort = new SerialPort();
        }

        public string PortName { get => _serialPort.PortName; set => _serialPort.PortName = value; }
        public int BaudRate { get => _serialPort.BaudRate; set => _serialPort.BaudRate = value; }
        public Parity Parity { get => _serialPort.Parity; set => _serialPort.Parity = value; }
        public int DataBits { get => _serialPort.DataBits; set => _serialPort.DataBits = value; }
        public StopBits StopBits { get => _serialPort.StopBits; set => _serialPort.StopBits = value; }
        public Handshake Handshake { get => _serialPort.Handshake; set => _serialPort.Handshake = value; }
        public bool DtrEnable { get => _serialPort.DtrEnable; set => _serialPort.DtrEnable = value; }
        public string NewLine { get => _serialPort.NewLine; set => _serialPort.NewLine = value; }
        public int ReadTimeout { get => _serialPort.ReadTimeout; set => _serialPort.ReadTimeout = value; }
        public int WriteTimeout { get => _serialPort.WriteTimeout; set => _serialPort.WriteTimeout = value; }

        public void Close() => _serialPort.Close();

        public void Dispose() => _serialPort.Dispose();

        public void Open() => _serialPort.Open();

        public string ReadLine() => _serialPort.ReadLine();

        public void Write(string value) => _serialPort.Write(value);
    }
}