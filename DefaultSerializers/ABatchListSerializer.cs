using System;
using System.Collections.Generic;
using System.IO;

namespace DaSerialization
{
    public abstract class ABatchListSerializer<T> : AFullSerializer<List<T>>
        where T : struct, IEquatable<T>
    {
        public override int Version => 1;
        public sealed override void ReadDataToObject(ref List<T> list, BinaryStream stream)
        {
            int len = stream.ReadInt(Metadata.CollectionSize);
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

            var reader = stream.GetReader();
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

        public sealed override void WriteObject(List<T> list, BinaryStream stream)
        {
            if (list == null)
            {
                stream.WriteInt(Metadata.CollectionSize, -1);
                return;
            }
            stream.WriteInt(Metadata.CollectionSize, list.Count);
            var writer = stream.GetWriter();
            int similar = 0;
            var last = list[0];
            WriteElement(last, writer);
            for (int i = 1, max = list.Count; i < max; i++)
            {
                var curr = list[i];
                if (!curr.Equals(last) | similar == byte.MaxValue)
                {
                    writer.Write(similar.ToByte());
                    last = curr;
                    WriteElement(last, writer);
                    similar = 0;
                }
                else
                    similar++;
            }
            writer.Write(similar.ToByte());
            EndWriting(writer);
        }

        protected abstract void ReadElement(ref T e, BinaryStreamReader reader);
        protected abstract void WriteElement(T e, BinaryWriter writer);
        protected virtual void EndReading() { }
        protected virtual void EndWriting(BinaryWriter writer) { }
    }

    public abstract class ABatchListDeserializer<T> : ADeserializer<List<T>>
        where T : struct, IEquatable<T>
    {
        public override int Version => 1;
        public sealed override void ReadDataToObject(ref List<T> list, BinaryStream stream)
        {
            int len = stream.ReadInt(Metadata.CollectionSize);
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

            var reader = stream.GetReader();
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
