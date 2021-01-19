using System;
using System.Collections.Generic;

namespace DaSerialization.Serialization
{
    [TypeId(10, typeof(UInt64[]))]
    public class UInt64ArraySerializer : AArraySerializer<UInt64>
    {
        protected override void ReadElement(ref UInt64 e, BinaryStreamReader reader) { e = reader.ReadUInt64(); }
        protected override void WriteElement(UInt64 e, BinaryStreamWriter writer) { writer.WriteUInt64(e); }
    }
    [TypeId(11, typeof(Int64[]))]
    public class Int64ArraySerializer : AArraySerializer<Int64>
    {
        protected override void ReadElement(ref Int64 e, BinaryStreamReader reader) { e = reader.ReadInt64(); }
        protected override void WriteElement(Int64 e, BinaryStreamWriter writer) { writer.WriteInt64(e); }
    }
    [TypeId(12, typeof(UInt32[]))]
    public class UInt32ArraySerializer : AArraySerializer<UInt32>
    {
        protected override void ReadElement(ref UInt32 e, BinaryStreamReader reader) { e = reader.ReadUInt32(); }
        protected override void WriteElement(UInt32 e, BinaryStreamWriter writer) { writer.WriteUInt32(e); }
    }
    [TypeId(13, typeof(Int32[]))]
    public class Int32ArraySerializer : AArraySerializer<Int32>
    {
        protected override void ReadElement(ref Int32 e, BinaryStreamReader reader) { e = reader.ReadInt32(); }
        protected override void WriteElement(Int32 e, BinaryStreamWriter writer) { writer.WriteInt32(e); }
    }
    [TypeId(14, typeof(UInt16[]))]
    public class UInt16ArraySerializer : AArraySerializer<UInt16>
    {
        protected override void ReadElement(ref UInt16 e, BinaryStreamReader reader) { e = reader.ReadUInt16(); }
        protected override void WriteElement(UInt16 e, BinaryStreamWriter writer) { writer.WriteUInt16(e); }
    }
    [TypeId(15, typeof(Int16[]))]
    public class Int16ArraySerializer : AArraySerializer<Int16>
    {
        protected override void ReadElement(ref Int16 e, BinaryStreamReader reader) { e = reader.ReadInt16(); }
        protected override void WriteElement(Int16 e, BinaryStreamWriter writer) { writer.WriteInt16(e); }
    }
    [TypeId(16, typeof(Byte[]))]
    public class ByteArraySerializer : AArraySerializer<Byte>
    {
        protected override void ReadElement(ref Byte e, BinaryStreamReader reader) { e = reader.ReadByte(); }
        protected override void WriteElement(Byte e, BinaryStreamWriter writer) { writer.WriteByte(e); }
    }
    [TypeId(17, typeof(SByte[]))]
    public class SByteArraySerializer : AArraySerializer<SByte>
    {
        protected override void ReadElement(ref SByte e, BinaryStreamReader reader) { e = reader.ReadSByte(); }
        protected override void WriteElement(SByte e, BinaryStreamWriter writer) { writer.WriteSByte(e); }
    }

    [TypeId(20, typeof(List<UInt64>))]
    public class UInt64ListSerializer : AListSerializer<UInt64>
    {
        protected override void ReadElement(ref UInt64 e, BinaryStreamReader reader) { e = reader.ReadUInt64(); }
        protected override void WriteElement(UInt64 e, BinaryStreamWriter writer) { writer.WriteUInt64(e); }
    }
    [TypeId(21, typeof(List<Int64>))]
    public class Int64ListSerializer : AListSerializer<Int64>
    {
        protected override void ReadElement(ref Int64 e, BinaryStreamReader reader) { e = reader.ReadInt64(); }
        protected override void WriteElement(Int64 e, BinaryStreamWriter writer) { writer.WriteInt64(e); }
    }
    [TypeId(22, typeof(List<UInt32>))]
    public class UInt32ListSerializer : AListSerializer<UInt32>
    {
        protected override void ReadElement(ref UInt32 e, BinaryStreamReader reader) { e = reader.ReadUInt32(); }
        protected override void WriteElement(UInt32 e, BinaryStreamWriter writer) { writer.WriteUInt32(e); }
    }
    [TypeId(23, typeof(List<Int32>))]
    public class Int32ListSerializer : AListSerializer<Int32>
    {
        protected override void ReadElement(ref Int32 e, BinaryStreamReader reader) { e = reader.ReadInt32(); }
        protected override void WriteElement(Int32 e, BinaryStreamWriter writer) { writer.WriteInt32(e); }
    }
    [TypeId(24, typeof(List<UInt16>))]
    public class UInt16ListSerializer : AListSerializer<UInt16>
    {
        protected override void ReadElement(ref UInt16 e, BinaryStreamReader reader) { e = reader.ReadUInt16(); }
        protected override void WriteElement(UInt16 e, BinaryStreamWriter writer) { writer.WriteUInt16(e); }
    }
    [TypeId(25, typeof(List<Int16>))]
    public class Int16ListSerializer : AListSerializer<Int16>
    {
        protected override void ReadElement(ref Int16 e, BinaryStreamReader reader) { e = reader.ReadInt16(); }
        protected override void WriteElement(Int16 e, BinaryStreamWriter writer) { writer.WriteInt16(e); }
    }
    [TypeId(26, typeof(List<Byte>))]
    public class ByteListSerializer : AListSerializer<Byte>
    {
        protected override void ReadElement(ref Byte e, BinaryStreamReader reader) { e = reader.ReadByte(); }
        protected override void WriteElement(Byte e, BinaryStreamWriter writer) { writer.WriteByte(e); }
    }
    [TypeId(27, typeof(List<SByte>))]
    public class SByteListSerializer : AListSerializer<SByte>
    {
        protected override void ReadElement(ref SByte e, BinaryStreamReader reader) { e = reader.ReadSByte(); }
        protected override void WriteElement(SByte e, BinaryStreamWriter writer) { writer.WriteSByte(e); }
    }
}