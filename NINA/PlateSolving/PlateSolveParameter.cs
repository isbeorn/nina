using NINA.Utility.Astrometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.PlateSolving {

    internal class PlateSolveParameter {
        public int FocalLength { get; set; }
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
    }
}