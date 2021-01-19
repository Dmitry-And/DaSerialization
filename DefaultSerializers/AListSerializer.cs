using System.Collections.Generic;

namespace DaSerialization
{
    public abstract class AListSerializer<T> : AFullSerializer<List<T>>
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
            else if (list.Capacity < len)
                list.Capacity = len;
            for (int i = 0, count = list.Count; i < len & i < count; i++)
            {
                var v = list[i];
                ReadElement(ref v, reader);
                list[i] = v;
            }
            for (int i = list.Count; i < len; i++)
            {
                var v = default(T);
                ReadElement(ref v, reader);
                list.Add(v);
            }
            if (list.Count > len)
                list.RemoveRange(len, list.Count - len);
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
            for (int i = 0, max = list.Count; i < max; i++)
                WriteElement(list[i], writer);
            EndWriting(writer);
        }

        protected abstract void ReadElement(ref T e, BinaryStreamReader reader);
        protected abstract void WriteElement(T e, BinaryStreamWriter writer);
        protected virtual void EndReading() { }
        protected virtual void EndWriting(BinaryStreamWriter writer) { }
    }

    public abstract class AListDeserializer<T> : ADeserializer<List<T>>
    {
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
            else if (list.Capacity < len)
                list.Capacity = len;
            for (int i = 0, count = list.Count; i < len & i < count; i++)
            {
                var v = list[i];
                ReadElement(ref v, reader);
                list[i] = v;
            }
            for (int i = list.Count; i < len; i++)
            {
                var v = default(T);
                ReadElement(ref v, reader);
                list.Add(v);
            }
            if (list.Count > len)
                list.RemoveRange(len, list.Count - len);
            EndReading();
        }

        protected abstract void ReadElement(ref T e, BinaryStreamReader reader);
        protected virtual void EndReading() { }
    }

}
