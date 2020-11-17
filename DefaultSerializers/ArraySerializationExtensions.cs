using System.IO;

namespace DaSerialization
{
    public static class ArraySerializationExtensions
    {
        public static void ReadArray(this BinaryReader reader, ref long[] array, int arrayLength = -1)
        {
            int count = PrepareToRead(reader, ref array, arrayLength);
            for (int i = 0; i < count; i++)
                array[i] = reader.ReadInt64();
        }
        public static void ReadArray(this BinaryReader reader, ref ulong[] array, int arrayLength = -1)
        {
            int count = PrepareToRead(reader, ref array, arrayLength);
            for (int i = 0; i < count; i++)
                array[i] = reader.ReadUInt64();
        }
        public static void ReadArray(this BinaryReader reader, ref int[] array, int arrayLength = -1)
        {
            int count = PrepareToRead(reader, ref array, arrayLength);
            for (int i = 0; i < count; i++)
                array[i] = reader.ReadInt32();
        }
        public static void ReadArray(this BinaryReader reader, ref uint[] array, int arrayLength = -1)
        {
            int count = PrepareToRead(reader, ref array, arrayLength);
            for (int i = 0; i < count; i++)
                array[i] = reader.ReadUInt32();
        }
        public static void ReadArray(this BinaryReader reader, ref short[] array, int arrayLength = -1)
        {
            int count = PrepareToRead(reader, ref array, arrayLength);
            for (int i = 0; i < count; i++)
                array[i] = reader.ReadInt16();
        }
        public static void ReadArray(this BinaryReader reader, ref ushort[] array, int arrayLength = -1)
        {
            int count = PrepareToRead(reader, ref array, arrayLength);
            for (int i = 0; i < count; i++)
                array[i] = reader.ReadUInt16();
        }
        public static void ReadArray(this BinaryReader reader, ref sbyte[] array, int arrayLength = -1)
        {
            int count = PrepareToRead(reader, ref array, arrayLength);
            for (int i = 0; i < count; i++)
                array[i] = reader.ReadSByte();
        }
        public static void ReadArray(this BinaryReader reader, ref byte[] array, int arrayLength = -1)
        {
            int count = PrepareToRead(reader, ref array, arrayLength);
            for (int i = 0; i < count; i++)
                array[i] = reader.ReadByte();
        }

        private static int PrepareToRead<T>(BinaryReader reader, ref T[] array, int arrayLength)
        {
            int count = reader.ReadInt32();

            if (array != null & arrayLength < 0)
                arrayLength = array.Length;
            arrayLength = arrayLength < count ? count : arrayLength;

            if (array == null || array.Length != arrayLength)
                array = new T[arrayLength];
            return count;
        }

        public static void WriteArray(this BinaryWriter writer, long[] array, int count = -1)
        {
            count = PrepareToWrite(writer, array, count);
            for (int i = 0; i < count; i++)
                writer.Write(array[i]);
        }
        public static void WriteArray(this BinaryWriter writer, ulong[] array, int count = -1)
        {
            count = PrepareToWrite(writer, array, count);
            for (int i = 0; i < count; i++)
                writer.Write(array[i]);
        }
        public static void WriteArray(this BinaryWriter writer, int[] array, int count = -1)
        {
            count = PrepareToWrite(writer, array, count);
            for (int i = 0; i < count; i++)
                writer.Write(array[i]);
        }
        public static void WriteArray(this BinaryWriter writer, uint[] array, int count = -1)
        {
            count = PrepareToWrite(writer, array, count);
            for (int i = 0; i < count; i++)
                writer.Write(array[i]);
        }
        public static void WriteArray(this BinaryWriter writer, short[] array, int count = -1)
        {
            count = PrepareToWrite(writer, array, count);
            for (int i = 0; i < count; i++)
                writer.Write(array[i]);
        }
        public static void WriteArray(this BinaryWriter writer, ushort[] array, int count = -1)
        {
            count = PrepareToWrite(writer, array, count);
            for (int i = 0; i < count; i++)
                writer.Write(array[i]);
        }
        public static void WriteArray(this BinaryWriter writer, sbyte[] array, int count = -1)
        {
            count = PrepareToWrite(writer, array, count);
            for (int i = 0; i < count; i++)
                writer.Write(array[i]);
        }
        public static void WriteArray(this BinaryWriter writer, byte[] array, int count = -1)
        {
            count = PrepareToWrite(writer, array, count);
            for (int i = 0; i < count; i++)
                writer.Write(array[i]);
        }

        private static int PrepareToWrite<T>(BinaryWriter writer, T[] array, int count)
        {
            if (count < 0)
                count = array.Length;
            writer.Write(count.EnsureInt32());
            return count;
        }
    }
}