using System.Collections.Generic;
using System.IO;

namespace DaSerialization
{
    public abstract class AListSerializer<T> : AFullSerializer<List<T>, BinaryStream>
    {
        public override int Version => 1;
        public sealed override void ReadDataToObject(ref List<T> list, BinaryStream stream, AContainer<BinaryStream> container)
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
                list.EnsureCapacity(len);
            var reader = stream.GetReader();
            for (int i = 0, count = list.Count; i < len & i < count; i++)
            {
                var v = list[i];
                ReadElement(ref v, reader, container);
                list[i] = v;
            }
            for (int i = list.Count; i < len; i++)
            {
                var v = default(T);
                ReadElement(ref v, reader, container);
                list.Add(v);
            }
            if (list.Count > len)
                list.RemoveRange(len, list.Count - len);
            EndReading();
        }

        public sealed override void WriteObject(List<T> list, BinaryStream stream, AContainer<BinaryStream> container)
        {
            if (list == null)
            {
                stream.WriteInt(Metadata.CollectionSize, -1);
                return;
            }
            var writer = stream.GetWriter();
            stream.WriteInt(Metadata.CollectionSize, list.Count);
            for (int i = 0, max = list.Count; i < max; i++)
                WriteElement(list[i], writer, container);
            EndWriting(writer);
        }

        protected abstract void ReadElement(ref T e, BinaryReader reader, AContainer<BinaryStream> container);
        protected abstract void WriteElement(T e, BinaryWriter writer, AContainer<BinaryStream> container);
        protected virtual void EndReading() { }
        protected virtual void EndWriting(BinaryWriter writer) { }
    }

    public abstract class AListDeserializer<T> : ADeserializer<List<T>, BinaryStream>
    {
        public sealed override void ReadDataToObject(ref List<T> list, BinaryStream stream, AContainer<BinaryStream> container)
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
                list.EnsureCapacity(len);
            var reader = stream.GetReader();
            for (int i = 0, count = list.Count; i < len & i < count; i++)
            {
                var v = list[i];
                ReadElement(ref v, reader, container);
                list[i] = v;
            }
            for (int i = list.Count; i < len; i++)
            {
                var v = default(T);
                ReadElement(ref v, reader, container);
                list.Add(v);
            }
            if (list.Count > len)
                list.RemoveRange(len, list.Count - len);
            EndReading();
        }

        protected abstract void ReadElement(ref T e, BinaryReader reader, AContainer<BinaryStream> container);
        protected virtual void EndReading() { }
    }

}
