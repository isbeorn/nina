using NINA.Utility.Astrometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.PlateSolving {

    internal class PlateSolveParameter {
        public double FocalLength { get; set; }
        public double PixelSize { get; set; }
        public double ImageWidth { get; set; }
        public double ImageHeight { get; set; }
        public double SearchRadius { get; set; }
        public double Regions { get; set; }
        public int DownSampleFactor { get; set; }
        public int MaxObjects { get; set; }
        public Coordinates Coordinates { get; set; }
        public Stream Image { get; set; }

        public double ArcSecPerPixel {
            get {
                return Astrometry.ArcsecPerPixel(PixelSize, FocalLength);
            }
        }

        public double FoVH {
            get {
                return Astrometry.ArcminToDegree(Astrometry.FieldOfView(ArcSecPerPixel, ImageHeight));
            }
        }

        public double FoVW {
            get {
                return Astrometry.ArcminToDegree(Astrometry.FieldOfView(ArcSecPerPixel, ImageWidth));
            }
        }

        public override string ToString() {
            var formatCoordinates = Coordinates != null ? $"Reference Coordinates RA: {Coordinates.RAString} Dec: {Coordinates.DecString} Epoch: {Coordinates.Epoch}" : "";
            return $"FocalLength: {FocalLength}" + Environment.NewLine +
                $"PixelSize: {PixelSize}" + Environment.NewLine +
                $"ImageWidth: {ImageWidth}" + Environment.NewLine +
                $"ImageHeight: {ImageHeight}" + Environment.NewLine +
                $"SearchRadius: {SearchRadius}" + Environment.NewLine +
                $"Regions: {Regions}" + Environment.NewLine +
                $"DownSampleFactor: {DownSampleFactor}" + Environment.NewLine +
                $"MaxObjects: {MaxObjects}" + Environment.NewLine +
                $"{formatCoordinates}";
        }
    }
}