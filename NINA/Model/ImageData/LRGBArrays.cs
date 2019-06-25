using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Model.ImageData {
    public class LRGBArrays {
        public ushort[] Lum { get; set; }
        public ushort[] Red { get; set; }
        public ushort[] Green { get; set; }
        public ushort[] Blue { get; set; }

        public LRGBArrays(ushort[] lum, ushort[] red, ushort[] green, ushort[] blue) {
            Lum = lum;
            Red = red;
            Green = green;
            Blue = blue;
        }
    }
}
