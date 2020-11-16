using System.IO;

namespace DaSerialization
{
    public abstract class AArraySerializer<T> : AFullSerializer<T[], BinaryStream>
    {
        public override int Version => 1;
        public sealed override void ReadDataToObject(ref T[] arr, BinaryStream stream, AContainer<BinaryStream> container)
        {
            var reader = stream.GetReader();
            int length = reader.ReadInt32();
            if (length < 0)
            {
                arr = null;
                return;
            }
            ArrayUtils.EnsureSizeOf(ref arr, length);
            for (int i = 0; i < length; i++)
                ReadElement(ref arr[i], reader, container);
        }

        public sealed override void WriteObject(T[] arr, BinaryStream stream, AContainer<BinaryStream> container)
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
                WriteElement(arr[i], writer, container);
        }

        protected abstract void ReadElement(ref T e, BinaryReader reader, AContainer<BinaryStream> container);
        protected abstract void WriteElement(T e, BinaryWriter writer, AContainer<BinaryStream> container);
    }

    public abstract class AArrayDeserializer<T> : ADeserializer<T[], BinaryStream>
    {
        public override int Version => 1;
        public sealed override void ReadDataToObject(ref T[] arr, BinaryStream stream, AContainer<BinaryStream> container)
        {
            var reader = stream.GetReader();
            int length = reader.ReadInt32();
            if (length < 0)
            {
                arr = null;
                return;
            }
            ArrayUtils.EnsureSizeOf(ref arr, length);
            for (int i = 0; i < length; i++)
                ReadElement(ref arr[i], reader, container);
        }

        protected abstract void ReadElement(ref T e, BinaryReader reader, AContainer<BinaryStream> container);
    }

}
