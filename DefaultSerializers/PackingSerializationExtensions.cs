﻿using System.IO;

namespace DaSerialization
{
    public static class PackingSerializationExtensions
    {
        #region bool packing

        public static void WritePacked(this BinaryWriter writer, bool b1, bool b2 = false, bool b3 = false,
            bool b4 = false, bool b5 = false, bool b6 = false, bool b7 = false, bool b8 = false)
        {
            byte p = (byte)((b1 ? 1 : 0)
                | (b2 ? 2 : 0)
                | (b3 ? 4 : 0)
                | (b4 ? 8 : 0)
                | (b5 ? 16 : 0)
                | (b6 ? 32 : 0)
                | (b7 ? 64 : 0)
                | (b8 ? 128 : 0));
            writer.Write(p);
        }

        public static void ReadPacked(this BinaryReader reader, out bool b1)
        {
            int p = reader.ReadByte();
            b1 = (p & 1) > 0;
        }

        public static void ReadPacked(this BinaryReader reader, out bool b1, out bool b2)
        {
            int p = reader.ReadByte();
            b1 = (p & 1) > 0;
            b2 = (p & 2) > 0;
        }

        public static void ReadPacked(this BinaryReader reader, out bool b1, out bool b2, out bool b3)
        {
            int p = reader.ReadByte();
            b1 = (p & 1) > 0;
            b2 = (p & 2) > 0;
            b3 = (p & 4) > 0;
        }

        public static void ReadPacked(this BinaryReader reader, out bool b1, out bool b2, out bool b3,
            out bool b4)
        {
            int p = reader.ReadByte();
            b1 = (p & 1) > 0;
            b2 = (p & 2) > 0;
            b3 = (p & 4) > 0;
            b4 = (p & 8) > 0;
        }

        public static void ReadPacked(this BinaryReader reader, out bool b1, out bool b2, out bool b3,
            out bool b4, out bool b5)
        {
            int p = reader.ReadByte();
            b1 = (p & 1) > 0;
            b2 = (p & 2) > 0;
            b3 = (p & 4) > 0;
            b4 = (p & 8) > 0;
            b5 = (p & 16) > 0;
        }

        public static void ReadPacked(this BinaryReader reader, out bool b1, out bool b2, out bool b3,
            out bool b4, out bool b5, out bool b6)
        {
            int p = reader.ReadByte();
            b1 = (p & 1) > 0;
            b2 = (p & 2) > 0;
            b3 = (p & 4) > 0;
            b4 = (p & 8) > 0;
            b5 = (p & 16) > 0;
            b6 = (p & 32) > 0;
        }

        public static void ReadPacked(this BinaryReader reader, out bool b1, out bool b2, out bool b3,
            out bool b4, out bool b5, out bool b6, out bool b7)
        {
            int p = reader.ReadByte();
            b1 = (p & 1) > 0;
            b2 = (p & 2) > 0;
            b3 = (p & 4) > 0;
            b4 = (p & 8) > 0;
            b5 = (p & 16) > 0;
            b6 = (p & 32) > 0;
            b7 = (p & 64) > 0;
        }

        public static void ReadPacked(this BinaryReader reader, out bool b1, out bool b2, out bool b3,
            out bool b4, out bool b5, out bool b6, out bool b7, out bool b8)
        {
            int p = reader.ReadByte();
            b1 = (p & 1) > 0;
            b2 = (p & 2) > 0;
            b3 = (p & 4) > 0;
            b4 = (p & 8) > 0;
            b5 = (p & 16) > 0;
            b6 = (p & 32) > 0;
            b7 = (p & 64) > 0;
            b8 = (p & 128) > 0;
        }

        #endregion

        #region 3-byte

        public static uint Read3ByteUInt32(this BinaryReader reader)
        {
            uint head = reader.ReadByte();
            uint tail = reader.ReadUInt16();
            return (head << 16) + tail;
        }
        public static void Write3Byte(this BinaryWriter writer, uint u)
        {
            const uint largest = 0xffffff;
            if (u > largest)
                throw new System.ArgumentException($"Trying to write {u} as 3-byte number (largerst is {largest})");
            byte head = (byte)(u >> 16);
            ushort tail = (ushort)u;
            writer.Write(head);
            writer.Write(tail);
        }

        #endregion

        #region packed uint

        public static int CountBytes(this BinaryReader reader, ulong maxValue)
        { return CountBytes(maxValue); }
        public static int CountBytes(this BinaryWriter writer, ulong maxValue)
        { return CountBytes(maxValue); }

        public static int CountBytes(ulong maxValue)
        {
            if (maxValue <= 0xffUL)
                return 1;
            if (maxValue <= 0xffffUL)
                return 2;
            if (maxValue <= 0xffffffUL)
                return 3;
            if (maxValue <= 0xffffffffUL)
                return 4;
            if (maxValue <= 0xff_ffffffffUL)
                return 5;
            if (maxValue <= 0xffff_ffffffffUL)
                return 6;
            if (maxValue <= 0xffffff_ffffffffUL)
                return 7;
            return 8;
        }

        public static ulong ReadUIntPacked(this BinaryReader reader, int bytes)
        {
            switch (bytes)
            {
                case 1: return reader.ReadByte();
                case 2: return reader.ReadUInt16();
                case 3: return reader.ReadUInt16() + ((ulong)reader.ReadByte() << 16);
                case 4: return reader.ReadUInt32();
                case 5: return reader.ReadUInt32() + ((ulong)reader.ReadByte() << 32);
                case 6: return reader.ReadUInt32() + ((ulong)reader.ReadUInt16() << 32);
                case 7: return reader.ReadUInt32() + ((ulong)reader.ReadUInt16() << 32) + ((ulong)reader.ReadByte() << 48);
                case 8: return reader.ReadUInt64();
                default: throw new System.Exception($"Unsupported bytes count {bytes} in {nameof(ReadUIntPacked)}");
            }
        }

        public static void WriteUIntPacked(this BinaryWriter writer, ulong value, int bytes)
        {
            switch (bytes)
            {
                case 1: writer.Write((byte)value); return;
                case 2: writer.Write((ushort)value); return;
                case 3: writer.Write((ushort)value); writer.Write((byte)(value >> 16)); return;
                case 4: writer.Write((uint)value); return;
                case 5: writer.Write((uint)value); writer.Write((byte)(value >> 32)); return;
                case 6: writer.Write((uint)value); writer.Write((ushort)(value >> 32)); return;
                case 7: writer.Write((uint)value); writer.Write((ushort)(value >> 32)); writer.Write((byte)(value >> 48)); return;
                case 8: writer.Write((ulong)value); return;
                default: throw new System.Exception($"Unsupported bytes count {bytes} in {nameof(WriteUIntPacked)}");
            }
        }

        public static ulong ReadUIntPacked(this BinaryReader reader)
        {
            int bytes = reader.ReadByte();
            return reader.ReadUIntPacked(bytes);
        }

        public static void WriteUIntPacked(this BinaryWriter writer, ulong value)
        {
            var bytes = CountBytes(value);
            writer.Write((byte)bytes);
            writer.WriteUIntPacked(value, bytes);
        }


        private static int GetPackedFormat(ulong maxValue)
        {
            if (maxValue <= 0x1fUL)
                return 0;
            if (maxValue <= 0x1fffUL)
                return 1;
            if (maxValue <= 0x1fffffUL)
                return 2;
            if (maxValue <= 0x1fffffffUL)
                return 3;
            if (maxValue <= 0x1f_ffffffffUL)
                return 4;
            if (maxValue <= 0x1fff_ffffffffUL)
                return 5;
            if (maxValue <= 0x1fffff_ffffffffUL)
                return 6;
            return 7;
        }

        public static ulong ReadUIntPacked_2(this BinaryReader reader)
        {
            int formatAndHighBits = reader.ReadByte();
            int format = formatAndHighBits >> 5;
            int bytes = format == 7 ? 8 : format;
            ulong value = 0;
            if (bytes > 0)
                value = reader.ReadUIntPacked(bytes);
            if (bytes < 8)
                value += ((ulong)(formatAndHighBits & 0x1f)) << (8 * bytes);
            return value;
        }

        public static void WriteUIntPacked_2(this BinaryWriter writer, ulong value)
        {
            var format = GetPackedFormat(value);
            int bytes = format == 7 ? 8 : format;
            int formatAndHighBits = format << 5;
            if (bytes < 8)
                formatAndHighBits += (int)(value >> (8 * bytes));
            writer.Write((byte)formatAndHighBits);
            if (bytes > 0)
                writer.WriteUIntPacked(value, bytes);
        }

        #endregion
    }
}