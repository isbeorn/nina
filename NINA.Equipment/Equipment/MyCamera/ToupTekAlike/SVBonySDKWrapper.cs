using Altair;
using NINA.Equipment.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment.MyCamera.ToupTekAlike {
    public static class SVBonyEnumExtensions {

        public static Svbonycam.eOPTION ToSVBony(this ToupTekAlikeOption option) {
            return (Svbonycam.eOPTION)Enum.Parse(typeof(ToupTekAlikeOption), option.ToString());
        }

        public static ToupTekAlikeEvent ToEvent(this Svbonycam.eEVENT info) {
            return (ToupTekAlikeEvent)Enum.Parse(typeof(Svbonycam.eEVENT), info.ToString());
        }

        public static ToupTekAlikeFrameInfo ToFrameInfo(this Svbonycam.FrameInfoV4 info) {
            var ttInfo = new ToupTekAlikeFrameInfo();
            ttInfo.flag = info.v3.flag;
            ttInfo.height = info.v3.height;
            ttInfo.width = info.v3.width;
            ttInfo.timestamp = info.v3.timestamp;
            ttInfo.seq = info.v3.seq;
            ttInfo.expotime = info.v3.expotime;
            ttInfo.hasgps = (info.v3.flag & (uint)Svbonycam.eFRAMEINFO_FLAG.FRAMEINFO_FLAG_GPS) != 0;
            ttInfo.hasexpotime = (info.v3.flag & (uint)Svbonycam.eFRAMEINFO_FLAG.FRAMEINFO_FLAG_EXPOTIME) != 0;
            ttInfo.gps.utcstart = info.gps.utcstart;
            ttInfo.gps.utcend = info.gps.utcend;
            ttInfo.gps.longitude = info.gps.longitude;
            ttInfo.gps.latitude = info.gps.latitude;
            ttInfo.gps.altitude = info.gps.altitude;
            ttInfo.gps.satellite = info.gps.satellite;
            return ttInfo;
        }

        public static ToupTekAlikeDeviceInfo ToDeviceInfo(this Svbonycam.DeviceV2 info) {
            var ttInfo = new ToupTekAlikeDeviceInfo();
            ttInfo.displayname = info.displayname;
            ttInfo.id = info.id;
            ttInfo.model = info.model.ToModel();

            return ttInfo;
        }

        public static ToupTekAlikeModel ToModel(this Svbonycam.ModelV2 modelV2) {
            var ttModel = new ToupTekAlikeModel();
            ttModel.flag = modelV2.flag;
            ttModel.ioctrol = modelV2.ioctrol;
            ttModel.maxfanspeed = modelV2.maxfanspeed;
            ttModel.maxspeed = modelV2.maxspeed;
            ttModel.name = modelV2.name;
            ttModel.preview = modelV2.preview;
            ttModel.still = modelV2.still;
            ttModel.xpixsz = modelV2.xpixsz;
            ttModel.ypixsz = modelV2.ypixsz;
            ttModel.res = new ToupTekAlikeResolution[modelV2.res.Length];
            for (var i = 0; i < modelV2.res.Length; i++) {
                ttModel.res[i] = new ToupTekAlikeResolution() { height = modelV2.res[i].height, width = modelV2.res[i].width };
            }
            return ttModel;
        }
    }
    public class SVBonySDKWrapper : IToupTekAlikeCameraSDK {
        private readonly object lockObj = new();
        private Svbonycam sdk;

        public string Category => "SVBony";

        public IToupTekAlikeCameraSDK Open(string id) {
            lock (lockObj) {
                this.sdk = Svbonycam.Open(id);
                return this;
            }
        }

        public uint MaxSpeed {
            get {
                lock (lockObj) {
                    return sdk.MaxSpeed;
                }
            }
        }

        public bool MonoMode {
            get {
                lock (lockObj) {
                    return sdk.MonoMode;
                }
            }
        }

        public void Close() {
            lock (lockObj) {
                Volatile.Write(ref toupTekAlikeCallback, null);
                sdk?.Close();
                sdk = null;
            }
        }

        public bool get_ExpoAGain(out ushort gain) {
            lock (lockObj) {
                return sdk.get_ExpoAGain(out gain);
            }
        }

        public void get_ExpoAGainRange(out ushort min, out ushort max, out ushort def) {
            lock (lockObj) {
                sdk.get_ExpoAGainRange(out min, out max, out def);
            }
        }

        public void get_ExpTimeRange(out uint min, out uint max, out uint def) {
            lock (lockObj) {
                sdk.get_ExpTimeRange(out min, out max, out def);
            }
        }

        public void get_Option(ToupTekAlikeOption option, out int target) {
            lock (lockObj) {
                sdk.get_Option(option.ToSVBony(), out target);
            }
        }

        public bool get_RawFormat(out uint fourCC, out uint bitDepth) {
            lock (lockObj) {
                return sdk.get_RawFormat(out fourCC, out bitDepth);
            }
        }

        public void get_Size(out int width, out int height) {
            lock (lockObj) {
                sdk.get_Size(out width, out height);
            }
        }

        public void get_Speed(out ushort speed) {
            lock (lockObj) {
                sdk.get_Speed(out speed);
            }
        }

        public void get_Temperature(out short temp) {
            lock (lockObj) {
                sdk.get_Temperature(out temp);
            }
        }

        public bool PullImage(ushort[] data, int bitDepth, out ToupTekAlikeFrameInfo info) {
            lock (lockObj) {
                Svbonycam.FrameInfoV4 svbonyInfo;
                var result = sdk.PullImage(data, 0, bitDepth, 0, out svbonyInfo);
                info = svbonyInfo.ToFrameInfo();
                return result;
            }
        }

        public bool put_ROI(uint x, uint y, uint width, uint height) {
            lock (lockObj) {
                return sdk.put_Roi(x, y, width, height);
            }
        }

        public bool put_AutoExpoEnable(bool v) {
            lock (lockObj) {
                return sdk.put_AutoExpoEnable(v);
            }
        }

        public bool put_ExpoAGain(ushort value) {
            lock (lockObj) {
                return sdk.put_ExpoAGain(value);
            }
        }

        public bool put_ExpoTime(uint usTime) {
            lock (lockObj) {
                return sdk.put_ExpoTime(usTime);
            }
        }

        public bool put_Option(ToupTekAlikeOption option, int v) {
            lock (lockObj) {
                return sdk.put_Option(option.ToSVBony(), v);
            }
        }

        public bool put_Speed(ushort value) {
            lock (lockObj) {
                return sdk.put_Speed(value);
            }
        }

        private ToupTekAlikeCallback toupTekAlikeCallback;
        private Svbonycam.DelegateEventCallback nativeCallback;

        public bool StartPullModeWithCallback(ToupTekAlikeCallback toupTekAlikeCallback) {
            lock (lockObj) {
                Volatile.Write(ref this.toupTekAlikeCallback, toupTekAlikeCallback);
                nativeCallback ??= new Svbonycam.DelegateEventCallback(EventCallback);

                return sdk.StartPullModeWithCallback(nativeCallback);
            }
        }

        private void EventCallback(Svbonycam.eEVENT nEvent) {
            // A native Close may wait for this callback. Never acquire the SDK gate here.
            var cb = Volatile.Read(ref toupTekAlikeCallback);
            cb?.Invoke(nEvent.ToEvent());
        }

        public bool Trigger(ushort v) {
            lock (lockObj) {
                return sdk.Trigger(v);
            }
        }

        public string Version() {
            lock (lockObj) {
                return Svbonycam.Version();
            }
        }
    }
}
