#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using NINA.Equipment.Equipment.MyFocuser;
using NINA.Profile.Interfaces;
using NINA.Core.Utility;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using NINA.Equipment.Interfaces;
using System.Collections.Generic;

namespace NINA.WPF.Base.ViewModel.Equipment.Focuser {

    public abstract class FocuserDecorator : BaseINPC, IFocuser {

        public FocuserDecorator(IProfileService profileService, IFocuser focuser) {
            this.profileService = profileService;
            this.focuser = focuser;
            this.focuser.PropertyChanged += Focuser_PropertyChanged;
        }

        private void Focuser_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            RaisePropertyChanged(e.PropertyName);
        }

        protected IProfileService profileService;
        protected IFocuser focuser;
        protected OvershootDirection lastDirection = OvershootDirection.NONE;

        public IFocuser Focuser => this.focuser;

        public bool IsMoving => this.focuser.IsMoving;

        public int MaxIncrement => this.focuser.MaxIncrement;

        public int MaxStep => this.focuser.MaxStep;

        public virtual int Position => this.focuser.Position;

        public double StepSize => this.focuser.StepSize;

        public bool TempCompAvailable => this.focuser.TempCompAvailable;

        public bool TempComp { get => this.focuser.TempComp; set => this.focuser.TempComp = value; }

        public double Temperature => this.focuser.Temperature;

        public bool HasSetupDialog => this.focuser.HasSetupDialog;

        public string Id => this.focuser.Id;

        public string Name => this.focuser.Name;
        public string DisplayName => this.focuser.DisplayName;

        public string Category => this.focuser.Category;

        public bool Connected => this.focuser.Connected;

        public string Description => this.focuser.Description;

        public string DriverInfo => this.focuser.DriverInfo;

        public string DriverVersion => this.focuser.DriverVersion;

        public IList<string> SupportedActions => this.focuser.SupportedActions;

        public Task<bool> Connect(CancellationToken token) {
            return this.focuser.Connect(token);
        }

        public void Disconnect() {
            this.focuser.Disconnect();
        }

        public void Halt() {
            this.focuser.Halt();
        }

        public virtual Task Move(int targetPosition, CancellationToken ct, int waitInMs = 1000) {
            lastDirection = DetermineMovingDirection(this.Position, targetPosition);
            return this.focuser.Move(targetPosition, ct);
        }

        protected OvershootDirection DetermineMovingDirection(int oldPosition, int newPosition) {
            if (newPosition > oldPosition) {
                return OvershootDirection.OUT;
            } else if (newPosition < oldPosition) {
                return OvershootDirection.IN;
            } else {
                return lastDirection;
            }
        }

        public void SetupDialog() {
            this.focuser.SetupDialog();
        }

        public string Action(string actionName, string actionParameters = "") {
            return this.focuser.Action(actionName, actionParameters);
        }

        public string SendCommandString(string command, bool raw) {
            return this.focuser.SendCommandString(command, raw);
        }

        public bool SendCommandBool(string command, bool raw) {
            return this.focuser.SendCommandBool(command, raw);
        }

        public void SendCommandBlind(string command, bool raw) {
            this.focuser.SendCommandBlind(command, raw);
        }

    }
}