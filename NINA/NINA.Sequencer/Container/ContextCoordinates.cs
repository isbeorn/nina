using NINA.Astrometry;

namespace NINA.Sequencer.Container {
    public class ContextCoordinates {
        public ContextCoordinates(Coordinates coordinates, double positionAngle, SiderealShiftTrackingRate shiftTrackingRate) {
            this.Coordinates = coordinates;
            this.PositionAngle = positionAngle;
            this.ShiftTrackingRate = shiftTrackingRate;
        }

        public Coordinates Coordinates { get; private set; }
        public double PositionAngle { get; private set; }
        public SiderealShiftTrackingRate ShiftTrackingRate { get; private set; }
    }
}
