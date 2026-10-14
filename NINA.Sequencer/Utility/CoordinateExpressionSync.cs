using NINA.Astrometry;
using NINA.Sequencer.Logic;

namespace NINA.Sequencer.Utility {
    internal static class CoordinateExpressionSync {
        internal static void Apply(InputCoordinates input, Expression expression, bool rightAscension,
            ref bool protect, ref double lastRa, ref double lastDec) {
            double evaluatedValue = expression.EvaluatedValue;
            if (expression.Error != null || !double.IsFinite(evaluatedValue) || input?.Coordinates == null) {
                return;
            }

            var current = input.Coordinates;
            if ((rightAscension ? current.RA : current.Dec) == evaluatedValue) {
                lastRa = current.RA;
                lastDec = current.Dec;
                return;
            }

            bool previous = protect;
            protect = true;
            try {
                var value = current.Clone();
                bool negativeZero = input.NegativeDec;
                if (rightAscension) {
                    value.RA = evaluatedValue;
                } else {
                    value.Dec = evaluatedValue;
                }

                input.Coordinates = value;
                if (value.Dec == 0) {
                    input.NegativeDec = negativeZero;
                }
            } finally {
                lastRa = input.Coordinates.RA;
                lastDec = input.Coordinates.Dec;
                protect = previous;
            }
        }
    }
}
