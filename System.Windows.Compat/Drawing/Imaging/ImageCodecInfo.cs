#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

namespace System.Drawing.Imaging {

    public class ImageCodecInfo {
        public System.Guid Clsid { get; set; }
        public string CodecName { get; set; }
        public string DllName { get; set; }
        public string FilenameExtension { get; set; }
        public string FormatDescription { get; set; }
        public System.Guid FormatID { get; set; }
        public string MimeType { get; set; }

        public static ImageCodecInfo[] GetImageEncoders() {
            // Return stub encoders
            return new ImageCodecInfo[] {
                new ImageCodecInfo { 
                    FormatID = ImageFormat.Jpeg.Guid,
                    MimeType = "image/jpeg"
                },
                new ImageCodecInfo {
                    FormatID = ImageFormat.Png.Guid,
                    MimeType = "image/png"
                }
            };
        }
    }

    public class EncoderParameters : System.IDisposable {
        public EncoderParameter[] Param { get; set; }

        public EncoderParameters(int count) {
            Param = new EncoderParameter[count];
        }

        public void Dispose() { }
    }

    public class EncoderParameter : System.IDisposable {
        public Encoder Encoder { get; set; }
        public long Value { get; set; }

        public EncoderParameter(Encoder encoder, long value) {
            Encoder = encoder;
            Value = value;
        }

        public void Dispose() { }
    }

    public class Encoder {
        public System.Guid Guid { get; set; }

        public static readonly Encoder Quality = new Encoder { Guid = new System.Guid("1d5be4b5-fa4a-452d-9cdd-5db35105e7eb") };
        public static readonly Encoder Compression = new Encoder { Guid = new System.Guid("e09d739d-ccd4-44ee-8eba-3fbf8be4fc58") };
    }
}
