#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.Equipment.MySwitch;
using NINA.Equipment.Equipment.MyDome;
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Equipment.Equipment.MyFocuser;
using NINA.Equipment.Equipment.MyGuider;
using NINA.Equipment.Equipment.MyRotator;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Equipment.MyCamera;
using NINA.WPF.Base.Interfaces.Utility;
using NINA.Core.Utility;

namespace NINA.WPF.Base.Utility {

    public class AllDeviceConsumer : BaseINPC, IAllDeviceConsumer {
        private readonly ICameraMediator cameraMediator;
        private readonly IFocuserMediator focuserMediator;
        private readonly IRotatorMediator rotatorMediator;
        private readonly ITelescopeMediator telescopeMediator;
        private readonly IDomeMediator domeMediator;
        private readonly IFilterWheelMediator filterWheelMediator;
        private readonly IGuiderMediator guiderMediator;
        private readonly ISwitchMediator switchMediator;

        public AllDeviceConsumer(ICameraMediator cameraMediator, IFocuserMediator focuserMediator, IRotatorMediator rotatorMediator, ITelescopeMediator telescopeMediator,
            IDomeMediator domeMediator, IFilterWheelMediator filterWheelMediator, IGuiderMediator guiderMediator, ISwitchMediator switchMediator) {
            this.cameraMediator = cameraMediator;
            this.focuserMediator = focuserMediator;
            this.rotatorMediator = rotatorMediator;
            this.telescopeMediator = telescopeMediator;
            this.domeMediator = domeMediator;
            this.filterWheelMediator = filterWheelMediator;
            this.guiderMediator = guiderMediator;
            this.switchMediator = switchMediator;

            cameraMediator.RegisterConsumer(this);
            focuserMediator.RegisterConsumer(this);
            rotatorMediator.RegisterConsumer(this);
            telescopeMediator.RegisterConsumer(this);
            domeMediator.RegisterConsumer(this);
            filterWheelMediator.RegisterConsumer(this);
            guiderMediator.RegisterConsumer(this);
            switchMediator.RegisterConsumer(this);
        }

        public void Dispose() {
            cameraMediator.RemoveConsumer(this);
            focuserMediator.RemoveConsumer(this);
            rotatorMediator.RemoveConsumer(this);
            telescopeMediator.RemoveConsumer(this);
            domeMediator.RemoveConsumer(this);
            filterWheelMediator.RemoveConsumer(this);
            guiderMediator.RemoveConsumer(this);
            switchMediator.RemoveConsumer(this);
        }

        private CameraInfo cameraInfo;

        public CameraInfo CameraInfo {
            get => cameraInfo;
            private set {
                cameraInfo = value;
                RaisePropertyChanged();
            }
        }

        public void UpdateDeviceInfo(CameraInfo deviceInfo) {
            CameraInfo = deviceInfo;
        }

        private FocuserInfo focuserInfo;

        public FocuserInfo FocuserInfo {
            get => focuserInfo;
            private set {
                focuserInfo = value;
                RaisePropertyChanged();
            }
        }

        public void UpdateDeviceInfo(FocuserInfo deviceInfo) {
            FocuserInfo = deviceInfo;
        }

        private RotatorInfo rotatorInfo;

        public RotatorInfo RotatorInfo {
            get => rotatorInfo;
            private set {
                rotatorInfo = value;
                RaisePropertyChanged();
            }
        }

        public void UpdateDeviceInfo(RotatorInfo deviceInfo) {
            RotatorInfo = deviceInfo;
        }

        private TelescopeInfo telescopeInfo;

        public TelescopeInfo TelescopeInfo {
            get => telescopeInfo;
            private set {
                telescopeInfo = value;
                RaisePropertyChanged();
            }
        }

        public void UpdateDeviceInfo(TelescopeInfo deviceInfo) {
            TelescopeInfo = deviceInfo;
        }

        private DomeInfo domeInfo;

        public DomeInfo DomeInfo {
            get => domeInfo;
            private set {
                domeInfo = value;
                RaisePropertyChanged();
            }
        }

        public void UpdateDeviceInfo(DomeInfo deviceInfo) {
            DomeInfo = deviceInfo;
        }

        private FilterWheelInfo filterWheelInfo;

        public FilterWheelInfo FilterWheelInfo {
            get => filterWheelInfo;
            private set {
                filterWheelInfo = value;
                RaisePropertyChanged();
            }
        }

        public void UpdateDeviceInfo(FilterWheelInfo deviceInfo) {
            FilterWheelInfo = deviceInfo;
        }

        private GuiderInfo guiderInfo;

        public GuiderInfo GuiderInfo {
            get => guiderInfo;
            private set {
                guiderInfo = value;
                RaisePropertyChanged();
            }
        }

        public void UpdateDeviceInfo(GuiderInfo deviceInfo) {
            GuiderInfo = deviceInfo;
        }

        private SwitchInfo switchInfo;

        public SwitchInfo SwitchInfo {
            get => switchInfo;
            private set {
                switchInfo = value;
                RaisePropertyChanged();
            }
        }

        public void UpdateDeviceInfo(SwitchInfo deviceInfo) {
            SwitchInfo = deviceInfo;
        }

        public void UpdateEndAutoFocusRun(AutoFocusInfo info) {
            ;
        }

        public void UpdateUserFocused(FocuserInfo info) {
            ;
        }
    }
}