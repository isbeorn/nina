using NINA.Model;
using NINA.Utility;
using NINA.Utility.Astrometry;
using System.Drawing;
using System.Linq;
using System.Windows;

namespace NINA.ViewModel.FramingAssistant {

    internal class FramingDSO {
        private const int DSO_DEFAULT_SIZE = 30;

        private double arcSecWidth;
        private double arcSecHeight;
        private readonly double sizeWidth;
        private readonly double sizeHeight;
        private Coordinates coordinates;

        /// <summary>
        /// Constructor for a Framing DSO.
        /// It takes a ViewportFoV and a DeepSkyObject and calculates XY values in pixels from the top left edge of the image subtracting half of its size.
        /// Those coordinates can be used to place the DSO including its name and size in any given image.
        /// </summary>
        /// <param name="dso">The DSO including its coordinates</param>
        /// <param name="viewport">The viewport of the offending DSO</param>
        public FramingDSO(DeepSkyObject dso, ViewportFoV viewport) {
            arcSecWidth = viewport.ArcSecWidth;
            arcSecHeight = viewport.ArcSecHeight;

            if (dso.Size != null && dso.Size >= arcSecWidth) {
                sizeWidth = dso.Size.Value;
            } else {
                sizeWidth = DSO_DEFAULT_SIZE;
            }

            if (dso.Size != null && dso.Size >= arcSecHeight) {
                sizeHeight = dso.Size.Value;
            } else {
                sizeHeight = DSO_DEFAULT_SIZE;
            }

            Id = dso.Id;
            Name1 = dso.Name;
            Name2 = dso.AlsoKnownAs.FirstOrDefault(m => m.StartsWith("M "));
            Name3 = dso.AlsoKnownAs.FirstOrDefault(m => m.StartsWith("NGC "));

            if (Name3 != null && Name1 == Name3.Replace(" ", "")) {
                Name1 = null;
            }

            if (Name1 == null && Name2 == null) {
                Name1 = Name3;
                Name3 = null;
            }

            if (Name1 == null && Name2 != null) {
                Name1 = Name2;
                Name2 = Name3;
                Name3 = null;
            }

            coordinates = dso.Coordinates;

            RecalculateTopLeft(viewport);
        }

        public PointF TextPosition { get; private set; }

        public void RecalculateTopLeft(ViewportFoV reference) {
            CenterPoint = coordinates.GnomonicTanProjection(reference);
            arcSecWidth = reference.ArcSecWidth;
            arcSecHeight = reference.ArcSecHeight;
            TextPosition = new PointF((float)CenterPoint.X, (float)(CenterPoint.Y + RadiusHeight + 5));
        }

        public double RadiusWidth => (sizeWidth / arcSecWidth) / 2;

        public double RadiusHeight => (sizeHeight / arcSecHeight) / 2;

        public System.Windows.Point CenterPoint { get; private set; }

        public string Id { get; }
        public string Name1 { get; }
        public string Name2 { get; }
        public string Name3 { get; }

        private static SolidBrush dsoFillColorBrush = new SolidBrush(Color.FromArgb(10, 255, 255, 255));
        private static SolidBrush dsoFontColorBrush = new SolidBrush(Color.FromArgb(255, 255, 255, 255));

        private static Pen dsoStrokePen = new Pen(Color.FromArgb(255, 255, 255, 255));

        private static Font fontdso = new Font("Segoe UI", 10, System.Drawing.FontStyle.Regular);

        public void Draw(System.Drawing.Graphics g) {
            g.FillEllipse(dsoFillColorBrush, (float)(this.CenterPoint.X - this.RadiusWidth), (float)(this.CenterPoint.Y - this.RadiusHeight),
                    (float)(this.RadiusWidth * 2), (float)(this.RadiusHeight * 2));
            g.DrawEllipse(dsoStrokePen, (float)(this.CenterPoint.X - this.RadiusWidth), (float)(this.CenterPoint.Y - this.RadiusHeight),
                (float)(this.RadiusWidth * 2), (float)(this.RadiusHeight * 2));
            var size1 = g.MeasureString(this.Name1, fontdso);
            g.DrawString(this.Name1, fontdso, dsoFontColorBrush, this.TextPosition.X - size1.Width / 2, (float)(this.TextPosition.Y));
            if (this.Name2 != null) {
                var size2 = g.MeasureString(this.Name2, fontdso);
                g.DrawString(this.Name2, fontdso, dsoFontColorBrush, this.TextPosition.X - size2.Width / 2, (float)(this.TextPosition.Y + size1.Height + 2));
                if (this.Name3 != null) {
                    var size3 = g.MeasureString(this.Name3, fontdso);
                    g.DrawString(this.Name3, fontdso, dsoFontColorBrush, this.TextPosition.X - size3.Width / 2, (float)(this.TextPosition.Y + size1.Height + 2 + size2.Height + 2));
                }
            }
        }
    }
}