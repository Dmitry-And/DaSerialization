namespace DaSerialization
{
    public static class PackingUtils
    {
        #region bool packing

        public static void WritePacked(this BinaryStreamWriter writer, bool b1, bool b2 = false, bool b3 = false,
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
            writer.WriteByte(p);
        }

        public static void ReadPacked(this BinaryStreamReader reader, out bool b1)
        {
            int p = reader.ReadByte("Packed B1");
            b1 = (p & 1) > 0;
        }

        public static void ReadPacked(this BinaryStreamReader reader, out bool b1, out bool b2)
        {
            int p = reader.ReadByte("Packed B2");
            b1 = (p & 1) > 0;
            b2 = (p & 2) > 0;
        }

        public static void ReadPacked(this BinaryStreamReader reader, out bool b1, out bool b2, out bool b3)
        {
            int p = reader.ReadByte("Packed B3");
            b1 = (p & 1) > 0;
            b2 = (p & 2) > 0;
            b3 = (p & 4) > 0;
        }

        public static void ReadPacked(this BinaryStreamReader reader, out bool b1, out bool b2, out bool b3,
            out bool b4)
        {
            int p = reader.ReadByte("Packed B4");
            b1 = (p & 1) > 0;
            b2 = (p & 2) > 0;
            b3 = (p & 4) > 0;
            b4 = (p & 8) > 0;
        }

        public static void ReadPacked(this BinaryStreamReader reader, out bool b1, out bool b2, out bool b3,
            out bool b4, out bool b5)
        {
            int p = reader.ReadByte("Packed B5");
            b1 = (p & 1) > 0;
            b2 = (p & 2) > 0;
            b3 = (p & 4) > 0;
            b4 = (p & 8) > 0;
            b5 = (p & 16) > 0;
        }

        public static void ReadPacked(this BinaryStreamReader reader, out bool b1, out bool b2, out bool b3,
            out bool b4, out bool b5, out bool b6)
        {
            int p = reader.ReadByte("Packed B6");
            b1 = (p & 1) > 0;
            b2 = (p & 2) > 0;
            b3 = (p & 4) > 0;
            b4 = (p & 8) > 0;
            b5 = (p & 16) > 0;
            b6 = (p & 32) > 0;
        }

        public static void ReadPacked(this BinaryStreamReader reader, out bool b1, out bool b2, out bool b3,
            out bool b4, out bool b5, out bool b6, out bool b7)
        {
            int p = reader.ReadByte("Packed B7");
            b1 = (p & 1) > 0;
            b2 = (p & 2) > 0;
            b3 = (p & 4) > 0;
            b4 = (p & 8) > 0;
            b5 = (p & 16) > 0;
            b6 = (p & 32) > 0;
            b7 = (p & 64) > 0;
        }

        public static void ReadPacked(this BinaryStreamReader reader, out bool b1, out bool b2, out bool b3,
            out bool b4, out bool b5, out bool b6, out bool b7, out bool b8)
        {
            int p = reader.ReadByte("Packed B8");
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

        public static int Read3ByteInt32(this BinaryStreamReader reader)
            => UIntToInt(Read3ByteUInt32(reader)).ToInt32();
        public static uint Read3ByteUInt32(this BinaryStreamReader reader)
        {
            uint head = reader.ReadByte();
            uint tail = reader.ReadUInt16();
            return (head << 16) + tail;
        }

        public static void Write3ByteInt32(this BinaryStreamWriter writer, int u)
            => Write3ByteUInt32(writer, IntToUInt(u).ToUInt32());
        public static void Write3ByteUInt32(this BinaryStreamWriter writer, uint u)
        {
            const uint largest = 0xffffff;
            if (u > largest)
                throw new System.ArgumentException($"Trying to write {u} as 3-byte number (largerst is {largest})");
            byte head = (byte)(u >> 16);
            ushort tail = (ushort)u;
            writer.WriteByte(head);
            writer.WriteUInt16(tail);
        }

        #endregion

        #region packed uint

        public static ulong IntToUInt(long i)
            => i >= 0 ? 2UL * ((ulong)i) : ((ulong)(-1L - i) * 2UL + 1UL);
        public static long UIntToInt(ulong i)
            => (i & 1UL) == 0UL ? (long)(i >> 1) : -1L - (long)(i >> 1);

        public static int CountUIntBytes(this BinaryStreamReader reader, ulong maxValue)
            => CountUIntBytes(maxValue);
        public static int CountUIntBytes(this BinaryStreamWriter writer, ulong maxValue)
            => CountUIntBytes(maxValue);
        public static int CountIntBytes(this BinaryStreamReader reader, long maxValue)
            => CountIntBytes(maxValue);
        public static int CountIntBytes(this BinaryStreamWriter writer, long maxValue)
            => CountIntBytes(maxValue);

        public static int CountIntBytes(long maxValue)
            => CountUIntBytes(IntToUInt(maxValue));
        public static int CountUIntBytes(ulong maxValue)
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

        public static long ReadIntPacked(this BinaryStreamReader reader, int bytesCount)
            => UIntToInt(ReadUIntPacked(reader, bytesCount));
        public static ulong ReadUIntPacked(this BinaryStreamReader reader, int bytesCount)
        {
            switch (bytesCount)
            {
                case 1: return reader.ReadByte("Packed 0-7");
                case 2: return reader.ReadUInt16("Packed 0-15");
                case 3: return reader.ReadUInt16("Packed 0-15") + ((ulong)reader.ReadByte("Packed 16-23") << 16);
                case 4: return reader.ReadUInt32("Packed 0-31");
                case 5: return reader.ReadUInt32("Packed 0-31") + ((ulong)reader.ReadByte("Packed 32-39") << 32);
                case 6: return reader.ReadUInt32("Packed 0-31") + ((ulong)reader.ReadUInt16("Packed 32-47") << 32);
                case 7: return reader.ReadUInt32("Packed 0-31") + ((ulong)reader.ReadUInt16("Packed 32-47") << 32) + ((ulong)reader.ReadByte("Packed 48-55") << 48);
                case 8: return reader.ReadUInt64("Packed 0-63");
                default: throw new System.Exception($"Unsupported bytes count {bytesCount} in {nameof(ReadUIntPacked)}");
            }
        }

        public static void WriteIntPacked(this BinaryStreamWriter writer, long value, int bytesCount)
            => WriteUIntPacked(writer, IntToUInt(value), bytesCount);
        public static void WriteUIntPacked(this BinaryStreamWriter writer, ulong value, int bytesCount)
        {
            switch (bytesCount)
            {
                case 1: writer.WriteByte((byte)value); return;
                case 2: writer.WriteUInt16((ushort)value); return;
                case 3: writer.WriteUInt16((ushort)value); writer.WriteByte((byte)(value >> 16)); return;
                case 4: writer.WriteUInt32((uint)value); return;
                case 5: writer.WriteUInt32((uint)value); writer.WriteByte((byte)(value >> 32)); return;
                case 6: writer.WriteUInt32((uint)value); writer.WriteUInt16((ushort)(value >> 32)); return;
                case 7: writer.WriteUInt32((uint)value); writer.WriteUInt16((ushort)(value >> 32)); writer.WriteByte((byte)(value >> 48)); return;
                case 8: writer.WriteUInt64((ulong)value); return;
                default: throw new System.Exception($"Unsupported bytes count {bytesCount} in {nameof(WriteUIntPacked)}");
            }
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

        public static int GetPackedIntSize(long value)
            => GetPackedUIntSize(IntToUInt(value));
        public static int GetPackedUIntSize(ulong value)
        {
            var format = GetPackedFormat(value);
            return format == 7 ? 9 : format + 1;
        }

        public static long ReadIntPacked(this BinaryStreamReader reader)
            => UIntToInt(ReadUIntPacked(reader));
        public static ulong ReadUIntPacked(this BinaryStreamReader reader)
        {
            int formatAndHighBits = reader.ReadByte("Format And MSB");
            int format = formatAndHighBits >> 5;
            int bytes = format == 7 ? 8 : format;
            ulong value = 0;
            if (bytes > 0)
                value = reader.ReadUIntPacked(bytes);
            if (bytes < 8)
                value += ((ulong)(formatAndHighBits & 0x1f)) << (8 * bytes);
            return value;
        }

        public static void WriteIntPacked(this BinaryStreamWriter writer, long value)
            => WriteUIntPacked(writer, IntToUInt(value));
        public static void WriteUIntPacked(this BinaryStreamWriter writer, ulong value)
        {
            var format = GetPackedFormat(value);
            int bytes = format == 7 ? 8 : format;
            int formatAndHighBits = format << 5;
            if (bytes < 8)
                formatAndHighBits += (int)(value >> (8 * bytes));
            writer.WriteByte((byte)formatAndHighBits);
            if (bytes > 0)
                writer.WriteUIntPacked(value, bytes);
        }

        #endregion
    }
}