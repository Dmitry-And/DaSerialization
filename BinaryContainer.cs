using System.Collections.Generic;
using System.IO;
using System;
using DaSerialization.Internal;

namespace DaSerialization
{
    [TypeId(2004)]
    public class BinaryContainer : AContainer<BinaryStream>
    {
        private const int TABLE_INFO_ID = 80028104;
        private const int TABLE_HEADER_ID = -114819714;

        public BinaryContainer(int size, SerializerStorage<BinaryStream> storage)
            : base(new BinaryStream(size == 0 ? new MemoryStream() : new MemoryStream(size), true), storage)
        { }

        public BinaryContainer(BinaryStream stream, SerializerStorage<BinaryStream> storage)
            : base(stream, storage)
        { }

        public override long CountContentTableLenght(List<SerializedObjectInfo> contentTable)
        {
            // one for entire table:
            // int (4 bytes) - header (to ensure we start to read table from right position)
            // int (4 bytes) - table start position
            // int (4 bytes) - entries count

            int entriesCount = contentTable.Count;
            // for each entry:
            // int (4 bytes) - objectId
            // int (4 bytes) - typeId
            // int (4 bytes) - position
            // int (4 bytes) - length

            return entriesCount * 16 + 12;
        }

        protected override List<SerializedObjectInfo> ReadContentTable(BinaryStream stream)
        {
            int entriesCount = 0;
            var reader = stream.GetReader();
            if (stream.Length > 12)
            {
                stream.Seek(stream.Length - 12);
                int infoId = reader.ReadInt32();
                if (infoId != TABLE_INFO_ID)
                    throw new Exception($"Stream does not have valid {nameof(infoId)}");
                int tablePosition = reader.ReadInt32();
                entriesCount = reader.ReadInt32();
                stream.Seek(tablePosition);
                int headerId = reader.ReadInt32();
                if (headerId != TABLE_HEADER_ID)
                    throw new Exception($"Incorrect header {headerId} in stream '{stream}', expected {TABLE_HEADER_ID}");
            }
            var contentTable = new List<SerializedObjectInfo>(entriesCount);
            for (int i = 0; i < entriesCount; i++)
            {
                var entry = new SerializedObjectInfo
                {
                    ObjectId = reader.ReadInt32(),
                    TypeId = reader.ReadInt32(),
                    Position = reader.ReadInt32(),
                    Length = reader.ReadInt32(),
                    LocalVersion = 0
                };
                contentTable.Add(entry);
            }
            return contentTable;
        }

        protected override void WriteContentTable(BinaryStream stream, List<SerializedObjectInfo> contentTable)
        {
            int tablePosition = (int)stream.Position;

            var writer = stream.GetWriter();
            writer.Write(TABLE_HEADER_ID);
            for (int i = 0; i < contentTable.Count; i++)
            {
                var entry = contentTable[i];
                writer.Write(entry.ObjectId.EnsureInt32());
                writer.Write(entry.TypeId.EnsureInt32());
                writer.Write(entry.Position.ToInt32());
                writer.Write(entry.Length.EnsureInt32());
            }

            writer.Write(TABLE_INFO_ID);
            writer.Write(tablePosition);
            writer.Write(contentTable.Count);
        }
    }
}

#region serialization

namespace DaSerialization.Serialization
{
    public class BinaryContainerSerializer_v1 : AFullSerializer<BinaryContainer, BinaryStream>
    {
        public override int Version => 1;

        public override void ReadDataToObject(ref BinaryContainer c, BinaryStream stream, AContainer<BinaryStream> container)
        {
            var reader = stream.GetReader();
            int len = reader.ReadUIntPacked_2().ToInt32();
            bool writable = reader.ReadBoolean();

            var memStream = c?.GetUnderlyingStream() as MemoryStream;
            if (memStream == null || !memStream.CanWrite)
                memStream = new MemoryStream(len);
            var copiedLen = stream.GetUnderlyingStream().CopyPartiallyTo(memStream, len);
            if (copiedLen != len)
                throw new Exception($"Failed to read {c.PrettyTypeName()}: {copiedLen} bytes read instead of {len} expected");
            var binStream = new BinaryStream(memStream, writable);
            var storage = (c ?? container).SerializerStorage;
            // here we re-create container to avoid messing up with internal
            // state (dirty, size, etc.)
            c = new BinaryContainer(binStream, storage);
        }

        public override void WriteObject(BinaryContainer c, BinaryStream stream, AContainer<BinaryStream> container)
        {
            c.CleanUp();
            var memStream = c.GetUnderlyingStream() as MemoryStream;
            var writer = stream.GetWriter();
            writer.WriteUIntPacked_2(memStream.Length.ToUInt32());
            writer.Write(c.Writable);
            var oldPos = memStream.Position;
            memStream.Seek(0, SeekOrigin.Begin);
            memStream.CopyTo(stream.GetUnderlyingStream());
            memStream.Seek(oldPos, SeekOrigin.Begin);
        }
    }
}

#endregion