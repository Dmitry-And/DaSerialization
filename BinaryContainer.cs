using System.Collections.Generic;
using System.IO;
using System;
using DaSerialization.Internal;

namespace DaSerialization
{
    [TypeId(2004)]
    public class BinaryContainer : AContainer<BinaryStream>
    {
        private const int TABLE_INFO_TOKEN = 549437841;
        private const int TABLE_HEADER_TOKEN = -886121582;

        // int (4 bytes) - table info token (to ensure the table info section is correct)
        // int (4 bytes) - table start position
        // int (4 bytes) - entries count
        // int (4 bytes) - header token (to ensure content table section is correct)
        private const int TABLE_INFO_SIZE = 16; // in bytes

        public BinaryContainer(int size, SerializerStorage<BinaryStream> storage)
            : base(new BinaryStream(size == 0 ? new MemoryStream() : new MemoryStream(size), true), storage)
        { }

        public BinaryContainer(BinaryStream stream, SerializerStorage<BinaryStream> storage)
            : base(stream, storage)
        { }

        public override long GetContentTableSize(List<SerializedObjectInfo> contentTable)
        {
            long size = TABLE_INFO_SIZE + 4 * contentTable.Count; // 4 bytes for each entry from typeId int
            foreach (var e in contentTable)
            {
                size += PackingUtils.GetPackedIntSize(e.ObjectId.EnsureInt32());
                // int (4 bytes) - typeId already counted in the first line
                size += PackingUtils.GetPackedUIntSize(e.Position.ToUInt64());
                size += PackingUtils.GetPackedUIntSize(e.Length.EnsureUInt32());
            }
            return size;
        }

        protected override List<SerializedObjectInfo> ReadContentTable(BinaryStream stream)
        {
            int entriesCount = 0;
            var reader = stream.GetReader();
            long infoSectionPosition = stream.Length - 12;
            if (stream.Length >= TABLE_INFO_SIZE)
            {
                stream.Seek(infoSectionPosition);
                int infoToken = reader.ReadInt32();
                if (infoToken != TABLE_INFO_TOKEN)
                    throw new Exception($"{typeof(BinaryStream).PrettyName()} doesn't have a valid table info section, token {infoToken} read, {TABLE_INFO_TOKEN} expected");
                int tablePosition = reader.ReadInt32();
                entriesCount = reader.ReadInt32();
                stream.Seek(tablePosition);
                int headerToken = reader.ReadInt32();
                if (headerToken != TABLE_HEADER_TOKEN)
                    throw new Exception($"{typeof(BinaryStream).PrettyName()} doesn't have a valid content table, token {headerToken} read, {TABLE_HEADER_TOKEN} expected");
            }
            var contentTable = new List<SerializedObjectInfo>(entriesCount);
            for (int i = 0; i < entriesCount; i++)
            {
                var entry = new SerializedObjectInfo
                {
                    ObjectId = reader.ReadIntPacked().ToInt32(),
                    TypeId = reader.ReadInt32(),
                    Position = reader.ReadUIntPacked().ToInt64(),
                    Length = reader.ReadUIntPacked().ToUInt32(),
                    LocalVersion = 0
                };
                contentTable.Add(entry);
            }

            // the entire table should be read now and we should end up in info section
            if (entriesCount > 0 && stream.Position != infoSectionPosition)
                throw new Exception($"{typeof(BinaryStream).PrettyName()} doesn't have a valid content table, table size differs from expected by {stream.Position - infoSectionPosition}");

            return contentTable;
        }

        protected override void WriteContentTable(BinaryStream stream, List<SerializedObjectInfo> contentTable)
        {
            var tablePosition = stream.Position;

            var writer = stream.GetWriter();
            writer.Write(TABLE_HEADER_TOKEN);
            for (int i = 0; i < contentTable.Count; i++)
            {
                var entry = contentTable[i];
                writer.WriteIntPacked(entry.ObjectId.EnsureInt32());
                writer.Write(entry.TypeId.EnsureInt32());
                writer.WriteUIntPacked(entry.Position.ToUInt64());
                writer.WriteUIntPacked(entry.Length.EnsureUInt32());
            }

            writer.Write(TABLE_INFO_TOKEN);
            writer.Write(tablePosition.ToInt32());
            writer.Write(contentTable.Count.EnsureInt32());

            var tableSize = stream.Position - tablePosition;
            var expectedSize = GetContentTableSize(contentTable);
            if (tableSize != expectedSize)
                throw new Exception($"Content table size {tableSize} differs from expected one {expectedSize}");
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
            int len = reader.ReadUIntPacked().ToInt32();
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
            writer.WriteUIntPacked(memStream.Length.ToUInt32());
            writer.Write(c.Writable);
            var oldPos = memStream.Position;
            memStream.Seek(0, SeekOrigin.Begin);
            memStream.CopyTo(stream.GetUnderlyingStream());
            memStream.Seek(oldPos, SeekOrigin.Begin);
        }
    }
}

#endregion