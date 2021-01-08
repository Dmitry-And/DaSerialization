using System.IO;

namespace DaSerialization
{
    public abstract class AArraySerializer<T> : AFullSerializer<T[]>
    {
        public override int Version => 1;
        public sealed override void ReadDataToObject(ref T[] arr, BinaryStreamReader reader)
        {
            int length = reader.ReadInt32();
            if (length < 0)
            {
                arr = null;
                return;
            }
            if (arr == null || arr.Length != length)
                arr = new T[length];
            for (int i = 0; i < length; i++)
                ReadElement(ref arr[i], reader);
        }

        public sealed override void WriteObject(T[] arr, BinaryStream stream)
        {
            var writer = stream.GetWriter();
            if (arr == null)
            {
                writer.Write((-1).EnsureInt32());
                return;
            }
            int length = arr.Length;
            writer.Write(length);
            for (int i = 0; i < length; i++)
                WriteElement(arr[i], stream);
        }

        protected abstract void ReadElement(ref T e, BinaryStreamReader reader);
        protected abstract void WriteElement(T e, BinaryStream stream);
    }

    public abstract class AArrayDeserializer<T> : ADeserializer<T[]>
    {
        public override int Version => 1;
        public sealed override void ReadDataToObject(ref T[] arr, BinaryStreamReader reader)
        {
            int length = reader.ReadInt32();
            if (length < 0)
            {
                arr = null;
                return;
            }
            if (arr == null || arr.Length != length)
                arr = new T[length];
            for (int i = 0; i < length; i++)
                ReadElement(ref arr[i], reader);
        }

        protected abstract void ReadElement(ref T e, BinaryStreamReader reader);
    }

}
