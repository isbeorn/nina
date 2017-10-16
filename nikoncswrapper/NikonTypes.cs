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
using System.Diagnostics;

namespace Nikon
{
    //
    // NikonRange - wrapper for NkMAIDRange
    //
    public class NikonRange
    {
        NkMAIDRange _range;

        internal NikonRange(NkMAIDRange range)
        {
            _range = range;
        }

        public NkMAIDRange Range
        {
            get { return _range; }
        }

        public double Min
        {
            get { return _range.lfLower; }
        }

        public double Max
        {
            get { return _range.lfUpper; }
        }

        double Delta
        {
            get
            {
                Debug.Assert(_range.ulSteps > 1);
                return (Max - Min) / ((double)(_range.ulSteps - 1));
            }
        }

        double ValueFromIndex(uint index)
        {
            return Min + index * Delta;
        }

        uint IndexFromValue(double value)
        {
            if (value < Min || value > Max)
            {
                throw new NikonException("Value out of range");
            }

            return (uint)Math.Floor((value - Min) / Delta);
        }

        public double DefaultValue
        {
            get
            {
                if (_range.ulSteps == 0)
                {
                    return _range.lfDefault;
                }
                else
                {
                    return ValueFromIndex(_range.ulDefaultIndex);
                }
            }
        }

        public double Value
        {
            get
            {
                if (_range.ulSteps == 0)
                {
                    return _range.lfValue;
                }
                else
                {
                    return ValueFromIndex(_range.ulValueIndex);
                }
            }

            set
            {
                if (_range.ulSteps == 0)
                {
                    _range.lfValue = value;
                }
                else
                {
                    _range.ulValueIndex = IndexFromValue(value);
                }
            }
        }
    }

    //
    // NikonArray - wrapper for NkMAIDArray
    //
    public class NikonArray
    {
        NkMAIDArray _array;
        byte[] _buffer;

        internal NikonArray(NkMAIDArray a, byte[] buffer)
        {
            _array = a;
            _array.pData = IntPtr.Zero;
            _buffer = buffer;
        }

        public NkMAIDArray Array
        {
            get { return _array; }
        }

        public byte[] Buffer
        {
            get { return _buffer; }
        }
    }

    //
    // NikonEnum - wrapper for NkMAIDEnum
    //
    public class NikonEnum
    {
        object[] _list;
        NkMAIDEnum _enum;

        internal NikonEnum(NkMAIDEnum e, byte[] buffer)
        {
            _enum = e;
            _enum.pData = IntPtr.Zero;

            switch (e.ulType)
            {
                case eNkMAIDArrayType.kNkMAIDArrayType_PackedString:
                    _list = GetPackedStringArray(buffer);
                    break;

                case eNkMAIDArrayType.kNkMAIDArrayType_Unsigned:
                    _list = GetUint32Array(buffer, e.ulElements);
                    break;

                default:
                    throw new NikonException("Enum of type " + e.ulType.ToString() + " cannot be parsed. Not implemented.");
            }
        }

        private object[] GetUint32Array(byte[] data, uint length)
        {
            Debug.Assert(data.Length / 4 >= length);

            List<object> result = new List<object>();

            for (int i = 0; i < length; i++)
            {
                UInt32 val = BitConverter.ToUInt32(data, i * 4);
                result.Add(val);
            }

            return result.ToArray();
        }

        private string[] GetPackedStringArray(byte[] data)
        {
            List<string> result = new List<string>();
            List<byte> item = new List<byte>();

            foreach (byte character in data)
            {
                if (character == 0)
                {
                    string itemString = ASCIIEncoding.ASCII.GetString(item.ToArray());
                    result.Add(itemString);
                    item.Clear();
                }
                else
                {
                    item.Add(character);
                }
            }

            return result.ToArray();
        }

        public NkMAIDEnum Enum
        {
            get { return _enum; }
        }

        public object this[int index]
        {
            get { return _list[index]; }
        }

        public object GetEnumValueByIndex(int index)
        {
            return _list[index];
        }

        public int Length
        {
            get { return _list.Length; }
        }

        public int DefaultIndex
        {
            get { return (int)_enum.ulDefault; }
        }

        public int Index
        {
            get { return (int)_enum.ulValue; }
            set { _enum.ulValue = (uint)value; }
        }

        public object DefaultValue
        {
            get
            {
                if (_enum.ulDefault < _list.Length)
                {
                    return _list[_enum.ulDefault];
                }
                else
                {
                    return null;
                }
            }
        }

        public object Value
        {
            get
            {
                if (_enum.ulValue < _list.Length)
                {
                    return _list[_enum.ulValue];
                }
                else
                {
                    return null;
                }
            }
        }

        public override string ToString()
        {
            return (Value != null) ? Value.ToString() : null;
        }
    }
}
