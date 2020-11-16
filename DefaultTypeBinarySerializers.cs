using System;
using System.Collections.Generic;
using System.IO;

namespace DaSerialization
{

    #region array serializers

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

    #endregion

    #region list serializers

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

    #endregion

    #region list batch serializers

    public abstract class ABatchListSerializer<T> : AFullSerializer<List<T>, BinaryStream>
        where T : struct, IEquatable<T>
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
                list.ClearAndEnsureCapacity(len);

            var reader = stream.GetReader();
            T curr = default;
            while (len > 0)
            {
                ReadElement(ref curr, reader, container);
                int similar = reader.ReadByte() + 1;
                for (int i = 0; i < similar; i++)
                    list.Add(curr);
                len -= similar;
            }
            EndReading();
        }

        public sealed override void WriteObject(List<T> list, BinaryStream stream, AContainer<BinaryStream> container)
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
            WriteElement(last, writer, container);
            for (int i = 1, max = list.Count; i < max; i++)
            {
                var curr = list[i];
                if (!curr.Equals(last) | similar == byte.MaxValue)
                {
                    writer.Write(similar.ToByte());
                    last = curr;
                    WriteElement(last, writer, container);
                    similar = 0;
                }
                else
                    similar++;
            }
            writer.Write(similar.ToByte());
            EndWriting(writer);
        }

        protected abstract void ReadElement(ref T e, BinaryReader reader, AContainer<BinaryStream> container);
        protected abstract void WriteElement(T e, BinaryWriter writer, AContainer<BinaryStream> container);
        protected virtual void EndReading() { }
        protected virtual void EndWriting(BinaryWriter writer) { }
    }


    public abstract class ABatchListDeserializer<T> : ADeserializer<List<T>, BinaryStream>
        where T : struct, IEquatable<T>
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
                list.ClearAndEnsureCapacity(len);

            var reader = stream.GetReader();
            T curr = default;
            while (len > 0)
            {
                ReadElement(ref curr, reader, container);
                int similar = reader.ReadByte() + 1;
                for (int i = 0; i < similar; i++)
                    list.Add(curr);
                len -= similar;
            }
            EndReading();
        }

        protected abstract void ReadElement(ref T e, BinaryReader reader, AContainer<BinaryStream> container);
        protected virtual void EndReading() { }
    }

    #endregion

    #region default type lists and arrays serializers

    [TypeId(10, typeof(UInt64[]))]
    public class UInt64ArraySerializer : AArraySerializer<UInt64>
    {
        protected override void ReadElement(ref UInt64 e, BinaryReader reader, AContainer<BinaryStream> container) { e = reader.ReadUInt64(); }
        protected override void WriteElement(UInt64 e, BinaryWriter writer, AContainer<BinaryStream> container) { writer.Write(e); }
    }
    [TypeId(11, typeof(Int64[]))]
    public class Int64ArraySerializer : AArraySerializer<Int64>
    {
        protected override void ReadElement(ref Int64 e, BinaryReader reader, AContainer<BinaryStream> container) { e = reader.ReadInt64(); }
        protected override void WriteElement(Int64 e, BinaryWriter writer, AContainer<BinaryStream> container) { writer.Write(e); }
    }
    [TypeId(12, typeof(UInt32[]))]
    public class UInt32ArraySerializer : AArraySerializer<UInt32>
    {
        protected override void ReadElement(ref UInt32 e, BinaryReader reader, AContainer<BinaryStream> container) { e = reader.ReadUInt32(); }
        protected override void WriteElement(UInt32 e, BinaryWriter writer, AContainer<BinaryStream> container) { writer.Write(e); }
    }
    [TypeId(13, typeof(Int32[]))]
    public class Int32ArraySerializer : AArraySerializer<Int32>
    {
        protected override void ReadElement(ref Int32 e, BinaryReader reader, AContainer<BinaryStream> container) { e = reader.ReadInt32(); }
        protected override void WriteElement(Int32 e, BinaryWriter writer, AContainer<BinaryStream> container) { writer.Write(e); }
    }
    [TypeId(14, typeof(UInt16[]))]
    public class UInt16ArraySerializer : AArraySerializer<UInt16>
    {
        protected override void ReadElement(ref UInt16 e, BinaryReader reader, AContainer<BinaryStream> container) { e = reader.ReadUInt16(); }
        protected override void WriteElement(UInt16 e, BinaryWriter writer, AContainer<BinaryStream> container) { writer.Write(e); }
    }
    [TypeId(15, typeof(Int16[]))]
    public class Int16ArraySerializer : AArraySerializer<Int16>
    {
        protected override void ReadElement(ref Int16 e, BinaryReader reader, AContainer<BinaryStream> container) { e = reader.ReadInt16(); }
        protected override void WriteElement(Int16 e, BinaryWriter writer, AContainer<BinaryStream> container) { writer.Write(e); }
    }
    [TypeId(16, typeof(Byte[]))]
    public class ByteArraySerializer : AArraySerializer<Byte>
    {
        protected override void ReadElement(ref Byte e, BinaryReader reader, AContainer<BinaryStream> container) { e = reader.ReadByte(); }
        protected override void WriteElement(Byte e, BinaryWriter writer, AContainer<BinaryStream> container) { writer.Write(e); }
    }
    [TypeId(17, typeof(SByte[]))]
    public class SByteArraySerializer : AArraySerializer<SByte>
    {
        protected override void ReadElement(ref SByte e, BinaryReader reader, AContainer<BinaryStream> container) { e = reader.ReadSByte(); }
        protected override void WriteElement(SByte e, BinaryWriter writer, AContainer<BinaryStream> container) { writer.Write(e); }
    }

    [TypeId(20, typeof(List<UInt64>))]
    public class UInt64ListSerializer : AListSerializer<UInt64>
    {
        protected override void ReadElement(ref UInt64 e, BinaryReader reader, AContainer<BinaryStream> container) { e = reader.ReadUInt64(); }
        protected override void WriteElement(UInt64 e, BinaryWriter writer, AContainer<BinaryStream> container) { writer.Write(e); }
    }
    [TypeId(21, typeof(List<Int64>))]
    public class Int64ListSerializer : AListSerializer<Int64>
    {
        protected override void ReadElement(ref Int64 e, BinaryReader reader, AContainer<BinaryStream> container) { e = reader.ReadInt64(); }
        protected override void WriteElement(Int64 e, BinaryWriter writer, AContainer<BinaryStream> container) { writer.Write(e); }
    }
    [TypeId(22, typeof(List<UInt32>))]
    public class UInt32ListSerializer : AListSerializer<UInt32>
    {
        protected override void ReadElement(ref UInt32 e, BinaryReader reader, AContainer<BinaryStream> container) { e = reader.ReadUInt32(); }
        protected override void WriteElement(UInt32 e, BinaryWriter writer, AContainer<BinaryStream> container) { writer.Write(e); }
    }
    [TypeId(23, typeof(List<Int32>))]
    public class Int32ListSerializer : AListSerializer<Int32>
    {
        protected override void ReadElement(ref Int32 e, BinaryReader reader, AContainer<BinaryStream> container) { e = reader.ReadInt32(); }
        protected override void WriteElement(Int32 e, BinaryWriter writer, AContainer<BinaryStream> container) { writer.Write(e); }
    }
    [TypeId(24, typeof(List<UInt16>))]
    public class UInt16ListSerializer : AListSerializer<UInt16>
    {
        protected override void ReadElement(ref UInt16 e, BinaryReader reader, AContainer<BinaryStream> container) { e = reader.ReadUInt16(); }
        protected override void WriteElement(UInt16 e, BinaryWriter writer, AContainer<BinaryStream> container) { writer.Write(e); }
    }
    [TypeId(25, typeof(List<Int16>))]
    public class Int16ListSerializer : AListSerializer<Int16>
    {
        protected override void ReadElement(ref Int16 e, BinaryReader reader, AContainer<BinaryStream> container) { e = reader.ReadInt16(); }
        protected override void WriteElement(Int16 e, BinaryWriter writer, AContainer<BinaryStream> container) { writer.Write(e); }
    }
    [TypeId(26, typeof(List<Byte>))]
    public class ByteListSerializer : AListSerializer<Byte>
    {
        protected override void ReadElement(ref Byte e, BinaryReader reader, AContainer<BinaryStream> container) { e = reader.ReadByte(); }
        protected override void WriteElement(Byte e, BinaryWriter writer, AContainer<BinaryStream> container) { writer.Write(e); }
    }
    [TypeId(27, typeof(List<SByte>))]
    public class SByteListSerializer : AListSerializer<SByte>
    {
        protected override void ReadElement(ref SByte e, BinaryReader reader, AContainer<BinaryStream> container) { e = reader.ReadSByte(); }
        protected override void WriteElement(SByte e, BinaryWriter writer, AContainer<BinaryStream> container) { writer.Write(e); }
    }

    #endregion
}