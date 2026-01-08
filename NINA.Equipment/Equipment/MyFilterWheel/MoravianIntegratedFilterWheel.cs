using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment.MyFilterWheel {
    public class MoravianIntegratedFilterWheel : BaseINPC, IFilterWheel {
        private readonly IProfileService profileService;
        private readonly ICameraMediator cameraMediator;
        private MoravianCamera moravianCamera;

        public MoravianIntegratedFilterWheel(
                IProfileService profileService,
                ICameraMediator cameraMediator,
                string id,
                string name,
                string category,
                string driverVersion,
                string firmwareVersion,
                string flashVersion) {
            this.profileService = profileService;
            this.cameraMediator = cameraMediator;
            Id = $"{category}_{id}_IntegratedFW";
            Name = name;
            DisplayName = $"Moravian - {Name} ({(id.Length > 8 ? id[^8..] : id)}) Integrated Filter Wheel"; ;
            Category = category;
            DriverVersion = driverVersion;
        }

        public int[] FocusOffsets => Filters.Select((x) => x.FocusOffset).ToArray();

        public string[] Names => Filters.Select((x) => x.Name).ToArray();

        public short Position {
            get => field;
            set {
                moravianCamera.SetFilter((uint)value);
                field = value;
            }
        }

        public AsyncObservableCollection<FilterInfo> Filters { get; private set { field = value; RaisePropertyChanged(); } }

        public bool HasSetupDialog => false;

        public string Id { get; init; }

        public string Name { get; init; }

        public string DisplayName { get; init; }

        public string Category { get; init; }

        public bool Connected {
            get => field && moravianCamera.Connected;
            set {
                field = value;
                RaisePropertyChanged();
            }
        }

        public string Description => Id;

        public string DriverInfo => $"Native driver implementation for {Category} Integrated Filter Wheels";

        public string DriverVersion { get; init; }

        public IList<string> SupportedActions => new List<string>();


        public string Action(string actionName, string actionParameters) {
            throw new NotImplementedException();
        }

        public async Task<bool> Connect(CancellationToken token) {
            if (!cameraMediator.GetInfo().Connected || cameraMediator.GetDevice() is not MoravianCamera mc) {
                throw new InvalidOperationException($"Cannot connect {Category} integrated filter wheel. Camera must be connected first.");
            }

            if (Id != mc.Id + "_IntegratedFW") {
                // In case the connected camera is a different moravian camera than the one this filter wheel is attached to
                throw new InvalidOperationException($"Cannot connect {Category} integrated filter wheel when not connected to the corresponding camera.");
            }

            // Ensure we have the right instance
            moravianCamera = mc;
            var filters = moravianCamera.GetFilters();
            profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters = filters;
            Filters = filters;
            moravianCamera.ReinitFilterWheel();
            Position = 0;

            Connected = true;
            return true;
        }

        public void Disconnect() {
            moravianCamera = null;
            Connected = false;
        }

        public void SendCommandBlind(string command, bool raw = true) {
            throw new NotImplementedException();
        }

        public bool SendCommandBool(string command, bool raw = true) {
            throw new NotImplementedException();
        }

        public string SendCommandString(string command, bool raw = true) {
            throw new NotImplementedException();
        }

        public void SetupDialog() {
        }
    }
}
