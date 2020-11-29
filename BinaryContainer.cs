using System.Collections.Generic;
using System.IO;
using System;
using DaSerialization.Internal;

namespace DaSerialization
{
    [TypeId(2004)]
    public class BinaryContainer : AContainer<BinaryStream>
    {
        private const int TABLE_INFO_TOKEN = 80028104;
        private const int TABLE_HEADER_TOKEN = -114819714;

        // int (4 bytes) - table info token (to ensure the table info section is correct)
        // int (4 bytes) - table start position
        // int (4 bytes) - entries count
        // int (4 bytes) - header token (to ensure content table section is correct)
        private const int TABLE_INFO_SIZE = 16; // in bytes

        // int (4 bytes) - objectId
        // int (4 bytes) - typeId
        // int (4 bytes) - position
        // int (4 bytes) - length
        private const int ENTRY_SIZE = 16;


        public BinaryContainer(int size, SerializerStorage<BinaryStream> storage)
            : base(new BinaryStream(size == 0 ? new MemoryStream() : new MemoryStream(size), true), storage)
        { }

        public BinaryContainer(BinaryStream stream, SerializerStorage<BinaryStream> storage)
            : base(stream, storage)
        { }

        public override long GetContentTableSize(List<SerializedObjectInfo> contentTable)
        {
            return contentTable.Count * ENTRY_SIZE + TABLE_INFO_SIZE;
        }

        protected override List<SerializedObjectInfo> ReadContentTable(BinaryStream stream)
        {
            int entriesCount = 0;
            var reader = stream.GetReader();
            if (stream.Length >= TABLE_INFO_SIZE)
            {
                long infoSectionPosition = stream.Length - 12;
                stream.Seek(infoSectionPosition);
                int infoToken = reader.ReadInt32();
                if (infoToken != TABLE_INFO_TOKEN)
                    throw new Exception($"{typeof(BinaryStream).PrettyTypeName()} doesn't have a valid table info section, token {nameof(infoToken)} read, {TABLE_INFO_TOKEN} expected");
                int tablePosition = reader.ReadInt32();
                entriesCount = reader.ReadInt32();
                stream.Seek(tablePosition);
                int headerToken = reader.ReadInt32();
                if (headerToken != TABLE_HEADER_TOKEN)
                    throw new Exception($"{typeof(BinaryStream).PrettyTypeName()} doesn't have a valid content table, token {headerToken} read, {TABLE_HEADER_TOKEN} expected");

                int expectedSize = entriesCount * ENTRY_SIZE;
                var entriesSectionSize = infoSectionPosition - stream.Position;
                if (entriesSectionSize != expectedSize)
                    throw new Exception($"{typeof(BinaryStream).PrettyTypeName()} doesn't have a valid content table, table size is {entriesSectionSize}, expected {expectedSize}");
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
            var tablePosition = stream.Position;

            var writer = stream.GetWriter();
            writer.Write(TABLE_HEADER_TOKEN);
            for (int i = 0; i < contentTable.Count; i++)
            {
                var entry = contentTable[i];
                writer.Write(entry.ObjectId.EnsureInt32());
                writer.Write(entry.TypeId.EnsureInt32());
                writer.Write(entry.Position.ToInt32());
                writer.Write(entry.Length.EnsureInt32());
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