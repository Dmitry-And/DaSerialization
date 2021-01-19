namespace DaSerialization
{
    public static class ArraySerializationExtensions
    {
        public static void ReadArray(this BinaryStreamReader reader, ref long[] array, int arrayLength = -1)
        {
            int count = PrepareToRead(reader, ref array, arrayLength);
            for (int i = 0; i < count; i++)
                array[i] = reader.ReadInt64();
        }
        public static void ReadArray(this BinaryStreamReader reader, ref ulong[] array, int arrayLength = -1)
        {
            int count = PrepareToRead(reader, ref array, arrayLength);
            for (int i = 0; i < count; i++)
                array[i] = reader.ReadUInt64();
        }
        public static void ReadArray(this BinaryStreamReader reader, ref int[] array, int arrayLength = -1)
        {
            int count = PrepareToRead(reader, ref array, arrayLength);
            for (int i = 0; i < count; i++)
                array[i] = reader.ReadInt32();
        }
        public static void ReadArray(this BinaryStreamReader reader, ref uint[] array, int arrayLength = -1)
        {
            int count = PrepareToRead(reader, ref array, arrayLength);
            for (int i = 0; i < count; i++)
                array[i] = reader.ReadUInt32();
        }
        public static void ReadArray(this BinaryStreamReader reader, ref short[] array, int arrayLength = -1)
        {
            int count = PrepareToRead(reader, ref array, arrayLength);
            for (int i = 0; i < count; i++)
                array[i] = reader.ReadInt16();
        }
        public static void ReadArray(this BinaryStreamReader reader, ref ushort[] array, int arrayLength = -1)
        {
            int count = PrepareToRead(reader, ref array, arrayLength);
            for (int i = 0; i < count; i++)
                array[i] = reader.ReadUInt16();
        }
        public static void ReadArray(this BinaryStreamReader reader, ref sbyte[] array, int arrayLength = -1)
        {
            int count = PrepareToRead(reader, ref array, arrayLength);
            for (int i = 0; i < count; i++)
                array[i] = reader.ReadSByte();
        }
        public static void ReadArray(this BinaryStreamReader reader, ref byte[] array, int arrayLength = -1)
        {
            int count = PrepareToRead(reader, ref array, arrayLength);
            for (int i = 0; i < count; i++)
                array[i] = reader.ReadByte();
        }

        private static int PrepareToRead<T>(BinaryStreamReader reader, ref T[] array, int arrayLength)
        {
            int count = reader.ReadMetadata(Metadata.CollectionSize);

            if (array != null & arrayLength < 0)
                arrayLength = array.Length;
            arrayLength = arrayLength < count ? count : arrayLength;

            if (array == null || array.Length != arrayLength)
                array = new T[arrayLength];
            return count;
        }

        public static void WriteArray(this BinaryStreamWriter writer, long[] array, int count = -1)
        {
            count = PrepareToWrite(writer, array, count);
            for (int i = 0; i < count; i++)
                writer.WriteInt64(array[i]);
        }
        public static void WriteArray(this BinaryStreamWriter writer, ulong[] array, int count = -1)
        {
            count = PrepareToWrite(writer, array, count);
            for (int i = 0; i < count; i++)
                writer.WriteUInt64(array[i]);
        }
        public static void WriteArray(this BinaryStreamWriter writer, int[] array, int count = -1)
        {
            count = PrepareToWrite(writer, array, count);
            for (int i = 0; i < count; i++)
                writer.WriteInt32(array[i]);
        }
        public static void WriteArray(this BinaryStreamWriter writer, uint[] array, int count = -1)
        {
            count = PrepareToWrite(writer, array, count);
            for (int i = 0; i < count; i++)
                writer.WriteUInt32(array[i]);
        }
        public static void WriteArray(this BinaryStreamWriter writer, short[] array, int count = -1)
        {
            count = PrepareToWrite(writer, array, count);
            for (int i = 0; i < count; i++)
                writer.WriteInt16(array[i]);
        }
        public static void WriteArray(this BinaryStreamWriter writer, ushort[] array, int count = -1)
        {
            count = PrepareToWrite(writer, array, count);
            for (int i = 0; i < count; i++)
                writer.WriteUInt16(array[i]);
        }
        public static void WriteArray(this BinaryStreamWriter writer, sbyte[] array, int count = -1)
        {
            count = PrepareToWrite(writer, array, count);
            for (int i = 0; i < count; i++)
                writer.WriteSByte(array[i]);
        }
        public static void WriteArray(this BinaryStreamWriter writer, byte[] array, int count = -1)
        {
            count = PrepareToWrite(writer, array, count);
            for (int i = 0; i < count; i++)
                writer.WriteByte(array[i]);
        }

        private static int PrepareToWrite<T>(BinaryStreamWriter writer, T[] array, int count)
        {
            if (count < 0)
                count = array.Length;
            writer.WriteMetadata(Metadata.CollectionSize, count);
            return count;
        }
    }
}