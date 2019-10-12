using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NINA.Model.ImageData {

    public class DebayeredImageData {
        public LRGBArrays Data { get; set; }
        public BitmapSource ImageSource { get; set; }
    }
}
