using NINA.Astrometry;
using NINA.Equipment.Interfaces;
using NINA.Profile.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.WPF.Base.ViewModel.Equipment.Telescope {
    internal interface ITelescopeSlewCoordinates {
        Coordinates Coordinates { get; }
        TopocentricCoordinates TopocentricCoordinates { get; }
        Epoch EquatorialSystem { get; }
        Task<bool> Slew(CancellationToken ct);
    }

    internal class EquatorialSlewCoordinates : ITelescopeSlewCoordinates {
        private readonly ITelescope telescope;
        public EquatorialSlewCoordinates(ITelescope telescope, IProfileService profileService, Coordinates coordinates, Epoch equatorialSystem) {
            this.telescope = telescope;
            this.Coordinates = coordinates.Transform(EquatorialSystem);
            this.TopocentricCoordinates = coordinates.Transform(Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Latitude), Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Longitude), profileService.ActiveProfile.AstrometrySettings.Elevation);
            this.EquatorialSystem = equatorialSystem;
        }

        public Coordinates Coordinates { get; private set; }

        public TopocentricCoordinates TopocentricCoordinates { get; private set; }

        public Epoch EquatorialSystem { get; private set; }

        public Task<bool> Slew(CancellationToken ct) {
            return telescope.SlewToCoordinates(Coordinates, ct);
        }
    }

    internal class TopocentricSlewCoordinates : ITelescopeSlewCoordinates {
        private readonly ITelescope telescope;
        public TopocentricSlewCoordinates(ITelescope telescope, TopocentricCoordinates coordinates, Epoch equatorialSystem) {
            this.telescope = telescope;
            this.TopocentricCoordinates = coordinates;
            this.Coordinates = coordinates.Transform(equatorialSystem);
            this.EquatorialSystem = equatorialSystem;
        }

        public TopocentricCoordinates TopocentricCoordinates { get; private set; }

        public Coordinates Coordinates { get; private set; }

        public Epoch EquatorialSystem { get; private set; }

        public Task<bool> Slew(CancellationToken ct) {
            return telescope.SlewToAltAz(TopocentricCoordinates, ct);
        }

    }
}
