#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Altair;
using NINA.Equipment.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment.MyCamera.ToupTekAlike {

    public static class AltairEnumExtensions {

        public static Altaircam.eOPTION ToAltair(this ToupTekAlikeOption option) {
            return (Altaircam.eOPTION)Enum.Parse(typeof(ToupTekAlikeOption), option.ToString());
        }

        public static Altaircam.eAAF ToAltair(this ToupTekAlikeAAF action) {
            return (Altaircam.eAAF)Enum.Parse(typeof(ToupTekAlikeAAF), action.ToString());
        }

        public static ToupTekAlikeEvent ToEvent(this Altaircam.eEVENT info) {
            return (ToupTekAlikeEvent)Enum.Parse(typeof(Altaircam.eEVENT), info.ToString());
        }

        public static ToupTekAlikeFrameInfo ToFrameInfo(this Altaircam.FrameInfoV4 info) {
            var ttInfo = new ToupTekAlikeFrameInfo();
            ttInfo.flag = info.v3.flag;
            ttInfo.height = info.v3.height;
            ttInfo.width = info.v3.width;
            ttInfo.timestamp = info.v3.timestamp;
            ttInfo.seq = info.v3.seq;
            ttInfo.expotime = info.v3.expotime;
            ttInfo.hasgps = (info.v3.flag & (uint)Altaircam.eFRAMEINFO_FLAG.FRAMEINFO_FLAG_GPS) != 0;
            ttInfo.hasexpotime = (info.v3.flag & (uint)Altaircam.eFRAMEINFO_FLAG.FRAMEINFO_FLAG_EXPOTIME) != 0;
            ttInfo.gps.utcstart = info.gps.utcstart;
            ttInfo.gps.utcend = info.gps.utcend;
            ttInfo.gps.longitude = info.gps.longitude;
            ttInfo.gps.latitude = info.gps.latitude;
            ttInfo.gps.altitude = info.gps.altitude;
            ttInfo.gps.satellite = info.gps.satellite;
            return ttInfo;
        }

        public static ToupTekAlikeDeviceInfo ToDeviceInfo(this Altaircam.DeviceV2 info) {
            var ttInfo = new ToupTekAlikeDeviceInfo();
            ttInfo.displayname = info.displayname;
            ttInfo.id = info.id;
            ttInfo.model = info.model.ToModel();

            return ttInfo;
        }

        public static ToupTekAlikeModel ToModel(this Altaircam.ModelV2 modelV2) {
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

    public class AltairSDKWrapper : IToupTekAlikeCameraSDK {
        private readonly Lock lockObj = new();
        private Altaircam sdk;

        public string Category => "Altair";

        public IToupTekAlikeCameraSDK Open(string id) {
            using var _ = lockObj.EnterScope();
            this.sdk = Altaircam.Open(id);
            return this;
        }

        public uint MaxSpeed {
            get {
                using var _ = lockObj.EnterScope();
                return sdk.MaxSpeed;
            }
        }

        public bool MonoMode {
            get {
                using var _ = lockObj.EnterScope();
                return sdk.MonoMode;
            }
        }

        public void Close() {
            using var _ = lockObj.EnterScope();
            sdk.Close();
            sdk = null;
        }

        public bool get_ExpoAGain(out ushort gain) {
            using var _ = lockObj.EnterScope();
            return sdk.get_ExpoAGain(out gain);
        }

        public void get_ExpoAGainRange(out ushort min, out ushort max, out ushort def) {
            using var _ = lockObj.EnterScope();
            sdk.get_ExpoAGainRange(out min, out max, out def);
        }

        public void get_ExpTimeRange(out uint min, out uint max, out uint def) {
            using var _ = lockObj.EnterScope();
            sdk.get_ExpTimeRange(out min, out max, out def);
        }

        public void get_Option(ToupTekAlikeOption option, out int target) {
            using var _ = lockObj.EnterScope();
            sdk.get_Option(option.ToAltair(), out target);
        }

        public bool get_RawFormat(out uint fourCC, out uint bitDepth) {
            using var _ = lockObj.EnterScope();
            return sdk.get_RawFormat(out fourCC, out bitDepth);
        }

        public void get_Size(out int width, out int height) {
            using var _ = lockObj.EnterScope();
            sdk.get_Size(out width, out height);
        }

        public void get_Speed(out ushort speed) {
            using var _ = lockObj.EnterScope();
            sdk.get_Speed(out speed);
        }

        public void get_Temperature(out short temp) {
            using var _ = lockObj.EnterScope();
            sdk.get_Temperature(out temp);
        }

        public bool PullImage(ushort[] data, int bitDepth, out ToupTekAlikeFrameInfo info) {
            using var _ = lockObj.EnterScope();
            Altaircam.FrameInfoV4 altairInfo;
            var result = sdk.PullImage(data, 0, bitDepth, 0, out altairInfo);
            info = altairInfo.ToFrameInfo();
            return result;
        }

        public bool put_ROI(uint x, uint y, uint width, uint height) {
            using var _ = lockObj.EnterScope();
            return sdk.put_Roi(x, y, width, height);
        }

        public bool put_AutoExpoEnable(bool v) {
            using var _ = lockObj.EnterScope();
            return sdk.put_AutoExpoEnable(v);
        }

        public bool put_ExpoAGain(ushort value) {
            using var _ = lockObj.EnterScope();
            return sdk.put_ExpoAGain(value);
        }

        public bool put_ExpoTime(uint usTime) {
            using var _ = lockObj.EnterScope();
            return sdk.put_ExpoTime(usTime);
        }

        public bool put_Option(ToupTekAlikeOption option, int v) {
            using var _ = lockObj.EnterScope();
            return sdk.put_Option(option.ToAltair(), v);
        }

        public bool put_Speed(ushort value) {
            using var _ = lockObj.EnterScope();
            return sdk.put_Speed(value);
        }

        public bool AAF(ToupTekAlikeAAF action, int outVal, out int inVal) {
            return sdk.AAF(action.ToAltair(), outVal, out inVal);
        }

        public bool AAF(ToupTekAlikeAAF action, int outVal) {
            return sdk.AAF(action.ToAltair(), outVal);
        }

        private ToupTekAlikeCallback toupTekAlikeCallback;
        private Altaircam.DelegateEventCallback nativeCallback;

        public bool StartPullModeWithCallback(ToupTekAlikeCallback toupTekAlikeCallback) {
            using var _ = lockObj.EnterScope();
            this.toupTekAlikeCallback = toupTekAlikeCallback;
            nativeCallback ??= new Altaircam.DelegateEventCallback(EventCallback);

            return sdk.StartPullModeWithCallback(nativeCallback);
        }

        private void EventCallback(Altaircam.eEVENT nEvent) {
            ToupTekAlikeCallback cb;
            using (lockObj.EnterScope()) {
                cb = toupTekAlikeCallback;
            }
            cb?.Invoke(nEvent.ToEvent());
        }

        public bool Trigger(ushort v) {
            using var _ = lockObj.EnterScope();
            return sdk.Trigger(v);
        }

        public string Version() {
            using var _ = lockObj.EnterScope();
            return Altaircam.Version();
        }
    }
}
