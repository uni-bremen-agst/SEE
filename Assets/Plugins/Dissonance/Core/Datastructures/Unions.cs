using System;
using System.Runtime.InteropServices;

namespace Dissonance.Datastructures
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct Union16
    {
        [FieldOffset(0)]private ushort _ushort;
        //[FieldOffset(0)]private short _short;

        [FieldOffset(0)]private byte _byte1;
        [FieldOffset(1)]private byte _byte2;

        public ushort UInt16
        {
            get { return _ushort; }
            set { _ushort = value; }
        }

        //public short Int16
        //{
        //    get { return _short; }
        //    set { _short = value; }
        //}

        public byte LSB
        {
            get
            {
                return BitConverter.IsLittleEndian ? _byte1 : _byte2;
            }
            set
            {
                //ncrunch: no coverage start (Justification we can't run as little endian and big endian in the same run)
                if (BitConverter.IsLittleEndian)
                    _byte1 = value;
                else
                    _byte2 = value;
                //ncrunch: no coverage end
            }
        }

        public byte MSB
        {
            get
            {
                return BitConverter.IsLittleEndian ? _byte2 : _byte1;
            }
            set
            {
                //ncrunch: no coverage start (Justification we can't run as little endian and big endian in the same run)
                if (BitConverter.IsLittleEndian)
                    _byte2 = value;
                else
                    _byte1 = value;
                //ncrunch: no coverage end
            }
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct Union32
    {
        [FieldOffset(0)] private uint _uint;
        //[FieldOffset(0)] private int _int;

        [FieldOffset(0)] private byte _byte1;
        [FieldOffset(1)] private byte _byte2;
        [FieldOffset(2)] private byte _byte3;
        [FieldOffset(3)] private byte _byte4;

        public uint UInt32
        {
            get { return _uint; }
            set { _uint = value; }
        }

        //public int Int32
        //{
        //    get { return _int; }
        //    set { _int = value; }
        //}

        public void SetBytesFromNetworkOrder(byte b1, byte b2, byte b3, byte b4)
        {
            //ncrunch: no coverage start (Justification we can't run as little endian and big endian in the same run)
            if (BitConverter.IsLittleEndian)
            {
                //Host is little endian, which means bytes are in reverse order
                _byte1 = b4;
                _byte2 = b3;
                _byte3 = b2;
                _byte4 = b1;
            }
            else
            {
                //Host is big endian, which means bytes are in the correct order
                _byte1 = b1;
                _byte2 = b2;
                _byte3 = b3;
                _byte4 = b4;
            }
            //ncrunch: no coverage end
        }

        public void GetBytesInNetworkOrder(out byte b1, out byte b2, out byte b3, out byte b4)
        {
            //ncrunch: no coverage start (Justification we can't run as little endian and big endian in the same run)
            if (BitConverter.IsLittleEndian)
            {
                //Host is little endian, which means bytes are in reverse order
                b4 = _byte1;
                b3 = _byte2;
                b2 = _byte3;
                b1 = _byte4;
            }
            else
            {
                //Host is big endian, which means bytes are in the correct order
                b1 = _byte1;
                b2 = _byte2;
                b3 = _byte3;
                b4 = _byte4;
            }
            //ncrunch: no coverage end
        }
    }
}
