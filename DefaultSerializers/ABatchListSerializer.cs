using System;
using System.Collections.Generic;

namespace DaSerialization
{
    public abstract class ABatchListSerializer<T> : AFullSerializer<List<T>>
        where T : struct, IEquatable<T>
    {
        public override int Version => 1;
        public sealed override void ReadDataToObject(ref List<T> list, BinaryStreamReader reader)
        {
            int len = reader.ReadMetadata(Metadata.CollectionSize);
            if (len < 0)
            {
                list = null;
                return;
            }
            if (list == null)
                list = new List<T>(len > 4 ? len : 4);
            else
            {
                list.Clear();
                if (list.Capacity < len)
                    list.Capacity = len;
            }

            T curr = default;
            while (len > 0)
            {
                ReadElement(ref curr, reader);
                int similar = reader.ReadByte() + 1;
                for (int i = 0; i < similar; i++)
                    list.Add(curr);
                len -= similar;
            }
            EndReading();
        }

        public sealed override void WriteObject(List<T> list, BinaryStreamWriter writer)
        {
            if (list == null)
            {
                writer.WriteMetadata(Metadata.CollectionSize, -1);
                return;
            }
            writer.WriteMetadata(Metadata.CollectionSize, list.Count);
            int similar = 0;
            var last = list[0];
            WriteElement(last, writer);
            for (int i = 1, max = list.Count; i < max; i++)
            {
                var curr = list[i];
                if (!curr.Equals(last) | similar == byte.MaxValue)
                {
                    writer.WriteByte(similar.ToByte());
                    last = curr;
                    WriteElement(last, writer);
                    similar = 0;
                }
                else
                    similar++;
            }
            writer.WriteByte(similar.ToByte());
            EndWriting(writer);
        }

        protected abstract void ReadElement(ref T e, BinaryStreamReader reader);
        protected abstract void WriteElement(T e, BinaryStreamWriter writer);
        protected virtual void EndReading() { }
        protected virtual void EndWriting(BinaryStreamWriter writer) { }
    }

    public abstract class ABatchListDeserializer<T> : ADeserializer<List<T>>
        where T : struct, IEquatable<T>
    {
        public override int Version => 1;
        public sealed override void ReadDataToObject(ref List<T> list, BinaryStreamReader reader)
        {
            int len = reader.ReadMetadata(Metadata.CollectionSize);
            if (len < 0)
            {
                list = null;
                return;
            }
            if (list == null)
                list = new List<T>(len > 4 ? len : 4);
            else
            {
                list.Clear();
                if (list.Capacity < len)
                    list.Capacity = len;
            }

            T curr = default;
            while (len > 0)
            {
                ReadElement(ref curr, reader);
                int similar = reader.ReadByte() + 1;
                for (int i = 0; i < similar; i++)
                    list.Add(curr);
                len -= similar;
            }
            EndReading();
        }

        protected abstract void ReadElement(ref T e, BinaryStreamReader reader);
        protected virtual void EndReading() { }
    }

}
