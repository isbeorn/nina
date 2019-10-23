namespace NINA.Utility.FlatDeviceSDKs.AlnitakSDK {

    public abstract class Command {
        public string CommandString { get; protected set; }
    }

    public class PingCommand : Command {

        public PingCommand() {
            CommandString = ">POOO\r";
        }
    }

    public class OpenCommand : Command {

        public OpenCommand() {
            CommandString = ">OOOO\r";
        }
    }

    public class CloseCommand : Command {

        public CloseCommand() {
            CommandString = ">COOO\r";
        }
    }

    public class LightOnCommand : Command {

        public LightOnCommand() {
            CommandString = ">LOOO\r";
        }
    }

    public class LightOffCommand : Command {

        public LightOffCommand() {
            CommandString = ">DOOO\r";
        }
    }

    public class SetBrightnessCommand : Command {

        public SetBrightnessCommand(int brightness) {
            CommandString = $">B{brightness:000}\r";
        }
    }

    public class GetBrightnessCommand : Command {

        public GetBrightnessCommand() {
            CommandString = ">JOOO\r";
        }
    }

    public class StateCommand : Command {

        public StateCommand() {
            CommandString = ">SOOO\r";
        }
    }

    public class FirmwareVersionCommand : Command {

        public FirmwareVersionCommand() {
            CommandString = ">VOOO\r";
        }
    }
}