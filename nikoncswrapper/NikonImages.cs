//
// This work is licensed under a Creative Commons Attribution 3.0 Unported License.
//
// Thomas Dideriksen (thomas@dideriksen.com)
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nikon;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Nikon
{
    public class NikonLiveViewImage
    {
        byte[] _headerBuffer;
        byte[] _jpegBuffer;

        public byte[] JpegBuffer
        {
            get { return _jpegBuffer; }
        }

        public byte[] HeaderBuffer
        {
            get { return _headerBuffer; }
        }

        internal NikonLiveViewImage(byte[] buffer, int headerSize)
        {
            NikonBufferStream stream = new NikonBufferStream(buffer);

            _headerBuffer = new byte[headerSize];
            stream.Read(_headerBuffer, _headerBuffer.Length);

            _jpegBuffer = new byte[buffer.Length - headerSize];
            stream.Read(_jpegBuffer, _jpegBuffer.Length);
        }
    }

    public enum NikonImageType
    {
        Raw = 1,
        Jpeg = 5
    }

    public class NikonImage
    {
        byte[] _buffer;
        NikonImageType _type;
        int _number;
        bool _isFragmentOfRawPlusJpeg;

        internal NikonImage(int size, NikonImageType type, int number, bool isFragmentOfRawPlusJpeg)
        {
            _buffer = new byte[size];
            _type = type;
            _number = number;
            _isFragmentOfRawPlusJpeg = isFragmentOfRawPlusJpeg;
        }

        internal void CopyFrom(IntPtr data, int offset, int length)
        {
            Marshal.Copy(data, _buffer, offset, length);
        }

        public byte[] Buffer
        {
            get { return _buffer; }
        }

        public NikonImageType Type
        {
            get { return _type; }
        }

        public int Number
        {
            get { return _number; }
        }

        public bool IsFragmentOfRawPlusJpeg
        {
            get { return _isFragmentOfRawPlusJpeg; }
        }
    }

    public enum NikonOrientation
    {
        None = 0,
        CounterClockwise = 1,
        Clockwise = 2
    }

    public enum NikonPreviewQuality
    {
        RawPlusJpegFine = 0,
        RawPlusJpegNormal = 1,
        RawPlusJpegBasic = 2,
        Raw = 3,
        Tiff = 4,
        JpegFine = 5,
        JpegNormal = 6,
        JpegBasic = 7
    }

    public enum NikonPreviewCropMode
    {
        Fx = 0,
        Dx = 2,
        FiveFour = 3
    }

    public enum NikonPreviewAFType
    {
        PhaseDetection = 0,
        Contrast = 1
    }

    public enum NikonPreviewFocusInformation
    {
        OutOfFocus = 0,
        InFocus = 1
    }

    public class NikonPreview
    {
        int _width;
        int _height;
        int _focusPoint;
        NikonOrientation _orientation;
        NikonPreviewQuality _quality;
        NikonPreviewCropMode _cropMode;
        NikonPreviewAFType _AFType;
        byte[] _focusControlAreaInfo;
        NikonPreviewFocusInformation _focusInfo;
        int _AFAreaWidth;
        int _AFAreaHeight;
        int _contrastAFPosX;
        int _contrastAFPosY;
        int _constrastAFAreaX;
        int _constrastAFAreaY;
        byte[] _jpegBuffer;

        internal NikonPreview(byte[] buffer)
        {
            Debug.Assert(buffer.Length > 32);

            NikonBufferStream stream = new NikonBufferStream(buffer);

            _width = stream.Read2();
            _height = stream.Read2();
            _focusPoint = stream.Read1();
            _orientation = (NikonOrientation)stream.Read1();
            _quality = (NikonPreviewQuality)stream.Read1();
            _cropMode = (NikonPreviewCropMode)stream.Read1();
            _AFType = (NikonPreviewAFType)stream.Read1();
            _focusControlAreaInfo = new byte[8];
            stream.Read(_focusControlAreaInfo, _focusControlAreaInfo.Length);
            _focusInfo = (NikonPreviewFocusInformation)stream.Read1();
            _AFAreaWidth = stream.Read2();
            _AFAreaHeight = stream.Read2();
            _contrastAFPosX = stream.Read2();
            _contrastAFPosY = stream.Read2();
            _constrastAFAreaX = stream.Read2();
            _constrastAFAreaY = stream.Read2();
            stream.Skip(2); // Skip 2 reserved bytes

            Debug.Assert(stream.Position == 32);

            _jpegBuffer = new byte[buffer.Length - stream.Position];
            stream.Read(_jpegBuffer, _jpegBuffer.Length);
        }

        public int Width
        {
            get { return _width; }
        }

        public int Height
        {
            get { return _height; }
        }

        public int FocusPoint
        {
            get { return _focusPoint; }
        }

        public NikonOrientation Orienation
        {
            get { return _orientation; }
        }

        public NikonPreviewQuality Quality
        {
            get { return _quality; }
        }

        public NikonPreviewCropMode CropMode
        {
            get { return _cropMode; }
        }

        public NikonPreviewAFType AFType
        {
            get { return _AFType; }
        }

        public byte[] FocusControlAreaInformation
        {
            get { return _focusControlAreaInfo; }
        }

        public NikonPreviewFocusInformation FocusInfomration
        {
            get { return _focusInfo; }
        }

        public int AFAreaWidth
        {
            get { return _AFAreaWidth; }
        }

        public int AFAreaHeight
        {
            get { return _AFAreaHeight; }
        }

        public int ContrastAFPositionX
        {
            get { return _contrastAFPosX; }
        }

        public int ContrastAFPositionY
        {
            get { return _contrastAFPosY; }
        }

        public int ConstrastAFAreaX
        {
            get { return _constrastAFAreaX; }
        }

        public int ConstrastAFAreaY
        {
            get { return _constrastAFAreaY; }
        }

        public byte[] JpegBuffer
        {
            get { return _jpegBuffer; }
        }
    }

    public class NikonThumbnail
    {
        byte[] _pixels;
        int _stride;
        int _width;
        int _height;
        eNkMAIDColorSpace _colorSpace;

        internal NikonThumbnail(NkMAIDImageInfo imageInfo, IntPtr data)
        {
            _stride = (int)imageInfo.ulRowBytes;
            _width = (int)imageInfo.szTotalPixels.w;
            _height = (int)imageInfo.szTotalPixels.h;
            _colorSpace = imageInfo.ulColorSpace;
            _pixels = new byte[_stride * _height];

            Marshal.Copy(data, _pixels, 0, _pixels.Length);
        }

        public byte[] Pixels
        {
            get { return _pixels; }
        }

        public int Stride
        {
            get { return _stride; }
        }

        public int Width
        {
            get { return _width; }
        }

        public int Height
        {
            get { return _height; }
        }

        public eNkMAIDColorSpace ColorSpace
        {
            get { return _colorSpace; }
        }
    }

    public class NikonVideoFragment
    {
        string _filename;
        uint _totalSize;
        uint _offset;
        byte[] _buffer;
        uint _videoWidth;
        uint _videoHeight;

        internal NikonVideoFragment(
            string filename,
            uint totalSize,
            uint fragmentOffset,
            byte[] fragmentBuffer,
            uint videoWidth,
            uint videoHeight)
        {
            _filename = filename;
            _totalSize = totalSize;
            _offset = fragmentOffset;
            _buffer = fragmentBuffer;
            _videoWidth = videoWidth;
            _videoHeight = videoHeight;
        }

        public bool IsFirst
        {
            get { return (_offset == 0); }
        }

        public bool IsLast
        {
            get { return (_offset + Size == _totalSize); }
        }

        public double PercentComplete
        {
            get { return ((double)(_offset + Size) / (double)(_totalSize)) * 100.0; }
        }

        public string Filename
        {
            get { return _filename; }
        }

        public uint TotalSize
        {
            get { return _totalSize; }
        }

        public uint Offset
        {
            get { return _offset; }
        }

        public uint Size
        {
            get { return (uint)_buffer.Length; }
        }

        public byte[] Buffer
        {
            get { return _buffer; }
        }

        public uint VideoWidth
        {
            get { return _videoWidth; }
        }

        public uint VideoHeight
        {
            get { return _videoHeight; }
        }
    }

    internal class NikonBufferStream
    {
        byte[] _buffer;
        int _pos;

        public NikonBufferStream(byte[] buffer)
        {
            _buffer = buffer;
            _pos = 0;
        }

        public int Read1()
        {
            int result = (int)_buffer[_pos];
            _pos++;
            return result;
        }

        public int Read2()
        {
            byte[] temp = new byte[2]
            {
                _buffer[_pos + 1],
                _buffer[_pos + 0]
            };

            int result = (int)BitConverter.ToUInt16(temp, 0);
            _pos += 2;
            return result;
        }

        public int Read4()
        {
            byte[] temp = new byte[4]
            {
                _buffer[_pos + 3],
                _buffer[_pos + 2],
                _buffer[_pos + 1],
                _buffer[_pos + 0]
            };

            int result = (int)BitConverter.ToUInt32(temp, 0);
            _pos += 4;
            return result;
        }

        public void Read(byte[] dst, int size)
        {
            MemoryStream stream = new MemoryStream(dst);
            stream.Write(_buffer, _pos, size);
            stream.Close();
            _pos += size;
        }

        public void Skip(int size)
        {
            _pos += size;
        }

        public int Position
        {
            get { return _pos; }
        }
    }
}
