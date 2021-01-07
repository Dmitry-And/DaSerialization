// #define CONTENT_TABLE_INTEGRITY_CHECK

using System.Collections.Generic;
using System.IO;
using System;
using DaSerialization.Internal;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace DaSerialization
{
    public struct SerializedObjectInfo
    {
        public int ObjectId;
        public int TypeId;
        public int LocalVersion;
        public uint Length;
        public long Position;

        public bool Fits(int objectId, int typeId) { return TypeId == typeId & ObjectId == objectId; }
        public bool Fits(int objectId, int typeId, int version) { return TypeId == typeId & ObjectId == objectId & LocalVersion == version; }

        public override string ToString()
            => $"Type {TypeId} Obj {ObjectId} v{LocalVersion} Pos {Position}-{Position + Length}";
    }

    [TypeId(2004)]
    public class BinaryContainer : IDisposable
    {
        private const int TABLE_INFO_TOKEN = 549437841;
        private const int TABLE_HEADER_TOKEN = -886121582;

        // int (4 bytes) - table info token (to ensure the table info section is correct)
        // int (4 bytes) - table start position
        // int (4 bytes) - entries count
        // int (4 bytes) - header token (to ensure content table section is correct)
        private const int TABLE_INFO_SIZE = 16; // in bytes

        public bool Writable { get => _stream.Writable; }
        public bool IsDirty { get; private set; }
        public long Size { get; private set; }

        private BinaryStream _stream;
        public SerializerStorage SerializerStorage { get; private set; }
        private List<SerializedObjectInfo> _contentTable;
        public List<SerializedObjectInfo> GetContentTable() => _contentTable;

        public BinaryContainer(int size, SerializerStorage storage)
            : this(new BinaryStream(size == 0 ? new MemoryStream() : new MemoryStream(size), storage, true))
        { }

        public BinaryContainer(BinaryStream stream)
        {
            SerializerStorage = stream.SerializerStorage;
            _stream = stream;
            _contentTable = ReadContentTable(_stream);
            Size = _stream.Length;
            _stream.ClearStreamPosition();
            CheckContentTableIntegrity();
        }

        public BinaryStream GetBinaryStream() => _stream;

        public long GetContentTableSize(List<SerializedObjectInfo> contentTable)
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

        protected List<SerializedObjectInfo> ReadContentTable(BinaryStream stream)
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

        protected void WriteContentTable(BinaryStream stream, List<SerializedObjectInfo> contentTable)
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

        public bool Has<T>(int objectId)
            => FindContentEntry(objectId, typeof(T)) >= 0;

        public long GetSize<T>(int objectId)
        {
            var entryIndex = FindContentEntry(objectId, typeof(T));
            return entryIndex >= 0 ? _contentTable[entryIndex].Length : -1L;
        }

        #region root deserialization

        public T Deserialize<T>(int objectId)
        {
            T result = default;
            Deserialize(ref result, objectId, SerializerStorage.GetTypeInfo(typeof(T)).Id);
            return result;
        }
        public bool Deserialize<T>(ref T obj, int objectId)
            => Deserialize(ref obj, objectId, SerializerStorage.GetTypeInfo(typeof(T)).Id);
        public bool Deserialize(ref object obj, int objectId, Type type)
            => Deserialize<object>(ref obj, objectId, SerializerStorage.GetTypeInfo(type).Id);
        public bool Deserialize(ref object obj, int objectId, int typeId)
            => Deserialize<object>(ref obj, objectId, typeId);

        // returns false if object was not found
        private bool Deserialize<T>(ref T obj, int objectId, int typeId)
        {
            var entryIndex = FindContentEntry(objectId, typeId);
            if (entryIndex < 0)
            {
                obj = default;
                return false;
            }
            _stream.GetReader().OnDeserializeMetaBegin(SerializerStorage.GetTypeInfo(typeId).Type, _contentTable[entryIndex].Position);
            var endPos = SetStreamPositionAndGetEndPosition(entryIndex);
            _stream.Deserialize(ref obj);
            return ValidateAndClearStreamPosition(entryIndex, endPos);
        }

        #endregion

        #region root serialization

        public bool Serialize<T>(T obj, int objectId)
        {
            var typeTypeId = SerializerStorage.GetTypeInfo(typeof(T)).Id;
            var objTypeInfo = SerializerStorage.GetTypeInfo(obj?.GetType());
            return Serialize(obj, objectId, objTypeInfo, typeTypeId);
        }
        public bool Serialize(object obj, int objectId, Type type)
        {
            var typeTypeId = SerializerStorage.GetTypeInfo(type).Id;
            var objTypeInfo = SerializerStorage.GetTypeInfo(obj?.GetType());
            return Serialize(obj, objectId, objTypeInfo, typeTypeId);
        }
        public bool Serialize(object obj, int objectId, int typeTypeId)
        {
            var objTypeInfo = SerializerStorage.GetTypeInfo(obj?.GetType());
            return Serialize(obj, objectId, objTypeInfo, typeTypeId, true);
        }

        /// <summary>
        /// forcePolymorphic is only for performance reasons, result will be the same
        /// </summary>
        private bool Serialize<T>(T obj, int objectId, SerializationTypeInfo objTypeInfo, int typeTypeId, bool forcePolymorphic = false)
        {
            _stream.CheckWritingAllowed();
            IsValidObjectId(objectId, true);
            int localVersion = GetLastWrittenVersion(objectId, typeTypeId, out _);
            localVersion = localVersion < 0 ? 1 : localVersion + 1;
            long position = _stream.Length;
            _stream.Seek(position);

            _stream.WriteInt(Metadata.ObjectID, objectId);
            _stream.WriteInt(Metadata.TypeID, objTypeInfo.Id);
            bool result = objTypeInfo.Id == -1
                || _stream.SerializeInner(obj, objTypeInfo, objTypeInfo.Id != typeTypeId | forcePolymorphic);
            uint length = (_stream.Position - position).ToUInt32();
            _stream.ClearStreamPosition();
            if (!result)
            {
                Size = _stream.Length;
                return false;
            }

            _contentTable.Add(new SerializedObjectInfo()
            {
                ObjectId = objectId,
                TypeId = typeTypeId,
                Position = position,
                Length = length,
                LocalVersion = localVersion
            });
            CheckContentTableIntegrity();
            IsDirty = true;
            Size = _stream.Length;
            return true;
        }

        #endregion

        #region clear/remove

        public void Clear()
        {
            _contentTable.Clear();
            IsDirty = true;
            _stream.Clear();
            Size = _stream.Length;
        }

        /// <summary>
        /// Removes all not-latest versions of all objects and cleans up stream memory from them
        /// performing defragmentation. After that writes content table to the stream.
        /// This operation preserves stream capacity and does NOT deallocate any memory.
        /// Only CleanUp-ed containers can be saved (the content table serialized only here)
        /// </summary>
        public void CleanUp(bool preserveCapacity = false)
        {
            if (!IsDirty)
                return;
            RemoveOldVersions();

            long writePos = _stream.ZeroPosition;
            for (int i = 0; i < _contentTable.Count; i++)
            {
                var entry = _contentTable[i];
                if (entry.Position != writePos)
                {
                    _stream.Seek(entry.Position);
                    _stream.CopyTo(_stream, writePos, entry.Length);
                    entry.Position = writePos;
                }
                entry.LocalVersion = 0;
                _contentTable[i] = entry;

                writePos += entry.Length;
            }
            _stream.Seek(writePos);
            WriteContentTable(_stream, _contentTable);
            _stream.SetLength(_stream.Position);
            Size = _stream.Length;
            IsDirty = false;
            _stream.ClearStreamPosition();
        }

        public bool Remove<T>(int objectId)
            => Remove(objectId, typeof(T));
        public bool Remove(int objectId, Type type)
        {
            int firstIndex = _contentTable.Count;
            var typeId = SerializerStorage.GetTypeInfo(type).Id;
            for (int i = _contentTable.Count - 1; i >= 0; i--)
                if (_contentTable[i].Fits(objectId, typeId))
                {
                    _contentTable[i] = new SerializedObjectInfo() { LocalVersion = -1 };
                    firstIndex = i;
                }
            return RemoveRemoved(firstIndex) > 0;
        }
        public int RemoveAllWithId(int objectId)
        {
            int firstIndex = _contentTable.Count;
            for (int i = _contentTable.Count - 1; i >= 0; i--)
                if (_contentTable[i].ObjectId == objectId)
                {
                    _contentTable[i] = new SerializedObjectInfo() { LocalVersion = -1 };
                    firstIndex = i;
                }
            return RemoveRemoved(firstIndex);
        }

        public virtual void Dispose()
        {
            _stream?.Dispose();
            _stream = null;
            SerializerStorage = null;
            _contentTable = null;
        }

        #endregion

        #region hashing and other API

        public int GetContentHash(bool withRequestTypes)
        {
            const int p1 = 1537784579;
            const int p2 = 1533554101;
            const int p3 = 988644479;
            int hash = 1745732987;

            var oldPos = _stream.Position;
            var stream = _stream.GetUnderlyingStream();
            var table = _contentTable;
            for (int eIndx = 0, max = table.Count; eIndx < max; eIndx++)
            {
                var entry = table[eIndx];

                if (IsDirty)
                {
                    // we need to check if this entry is latest version
                    bool old = false;
                    for (int otherIndx = max - 1; otherIndx >= 0; otherIndx--)
                        if (table[otherIndx].Fits(entry.ObjectId, entry.TypeId)
                            & table[otherIndx].LocalVersion > entry.LocalVersion)
                        {
                            old = true;
                            break;
                        }
                    if (old)
                        continue;
                }

                int h = unchecked(entry.ObjectId * p3);
                if (withRequestTypes)
                    h = unchecked(h * p3 ^ entry.TypeId * p1);
                _stream.Seek(entry.Position);
                for (long i = entry.Length; i > 0; i--)
                {
                    int b = stream.ReadByte();
                    h = unchecked(h * p2 ^ b * p1);
                }

                // entries are order-independent
                hash = hash ^ h;
            }
            _stream.Seek(oldPos);

            return hash;
        }

        // returns false if object was not found in other container
        public bool CopyObjectFrom<T>(BinaryContainer other, int objectIdInOther, int myObjectId = -1, bool updateSerializers = false)
        {
            _stream.CheckWritingAllowed();
            if (myObjectId == -1)
                myObjectId = objectIdInOther;
            if (other == null)
                throw new ArgumentNullException(nameof(other));
            var otherEntryIndex = other.FindContentEntry(objectIdInOther, typeof(T));
            if (otherEntryIndex < 0)
                return false;

            var otherEndPos = other.SetStreamPositionAndGetEndPosition(otherEntryIndex);
            var oTypeId = other._stream.ReadInt(Metadata.TypeID);
            if (oTypeId != -1 & updateSerializers)
            {
                // we need to update serializer, so fallback to regular
                // deserialization-serialization routine
                other._stream.ClearStreamPosition();
                T o = other.Deserialize<T>(objectIdInOther);
                Serialize(o, myObjectId);
                return true;
            }
            // now we are sure the serializer version is the latest
            // so we need to bit-wise copy the content of the oter stream
            // to our stream and add a content entry
            if (other == this)
                return true;

            var tTypeInfo = SerializerStorage.GetTypeInfo(typeof(T));
            int localVersion = GetLastWrittenVersion(myObjectId, tTypeInfo.Id, out _);
            localVersion = localVersion < 0 ? 1 : localVersion + 1;
            long position = _stream.Length;
            _stream.Seek(position);

            _stream.WriteInt(Metadata.ObjectID, myObjectId);
            _stream.WriteInt(Metadata.TypeID, oTypeId);
            if (oTypeId != -1)
            {
                var contentLength = otherEndPos - other._stream.Position;
                other._stream.CopyTo(_stream, contentLength);
            }
            other.ValidateAndClearStreamPosition(otherEntryIndex, otherEndPos);

            uint length = (_stream.Position - position).ToUInt32();
            _stream.ClearStreamPosition();

            _contentTable.Add(new SerializedObjectInfo()
            {
                ObjectId = myObjectId,
                TypeId = tTypeInfo.Id,
                Position = position,
                Length = length,
                LocalVersion = localVersion
            });
            IsDirty = true;
            Size = _stream.Length;
            CheckContentTableIntegrity();
            return true;
        }

        /// <summary>
        /// returns false if nothing was changed
        /// </summary>
        public bool UpdateSerializers(bool andCleanUp = true)
        {
            bool wasUpdated = false;
            var table = _contentTable;

            // here there may be a temptation to early-out in case all serializers
            // are of the latest versions in the container content table
            // BUT! for now we don't know full list of all serialized objects
            // within container w/o deserializing all of them because of cases
            // of inner objects which were serialized by stream.Serialize(obj)
            // within root object serializers.
            // SO we need to perform full deserialization-serialization process for
            // all root objects.

            for (int eIndx = 0, max = table.Count; eIndx < max; eIndx++)
            {
                var entry = table[eIndx];
                object obj = null;
                Deserialize(ref obj, entry.ObjectId, entry.TypeId);
                if (obj == null)
                    continue;

                var typeInfo = SerializerStorage.GetTypeInfo(obj.GetType());
                // also here we should check if the object will serialize any inner
                // data as a container. If so - we need to run UpdateSerializers()
                // on all inner containers.
                wasUpdated |= SerializerStorage.UpdateSerializersForInnerContainers(typeInfo, ref obj);

                Serialize(obj, entry.ObjectId, entry.TypeId);
                wasUpdated = true;
            }
            if (andCleanUp)
                CleanUp();
            return wasUpdated;
        }

        public SerializationTypeInfo GetTypeInfo(Type t, bool throwIfNotFound = true)
            => SerializerStorage.GetTypeInfo(t, throwIfNotFound);
        public SerializationTypeInfo GetTypeInfo(int typeId, bool throwIfNotFound = true)
            => SerializerStorage.GetTypeInfo(typeId, throwIfNotFound);
        public MemoryStream GetUnderlyingStream()
            => _stream.GetUnderlyingStream();

        #endregion

        #region inner helpers

        private int RemoveRemoved(int firstIndex)
        {
            if (firstIndex >= _contentTable.Count)
                return 0;
            int lastIndex = firstIndex;
            for (int j = firstIndex, max = _contentTable.Count; j < max; j++)
                if (_contentTable[j].LocalVersion >= 0)
                    _contentTable[lastIndex++] = _contentTable[j];
            int toRemove = _contentTable.Count - lastIndex;
            if (toRemove == 0)
                return 0;
            _contentTable.RemoveRange(lastIndex, toRemove);
            IsDirty = true;
            CheckContentTableIntegrity();
            return toRemove;
        }

        private void RemoveOldVersions()
        {
            for (int i = 0; i < _contentTable.Count; i++)
            {
                int objId = _contentTable[i].ObjectId;
                int typeId = _contentTable[i].TypeId;
                int version = _contentTable[i].LocalVersion;
                for (int j = i + 1; j < _contentTable.Count; j++)
                {
                    if (_contentTable[j].Fits(objId, typeId)
                        & _contentTable[j].LocalVersion > version)
                    {
                        _contentTable[i] = new SerializedObjectInfo() { LocalVersion = -1 };
                        break;
                    }
                }
            }
            int lastIndex = 0;
            for (int j = 0; j < _contentTable.Count; j++)
            {
                if (_contentTable[j].LocalVersion >= 0)
                    _contentTable[lastIndex++] = _contentTable[j];
            }
            _contentTable.RemoveRange(lastIndex, _contentTable.Count - lastIndex);
            CheckContentTableIntegrity();
        }

        private long CountTotalDataLength()
        {
            long length = 0;
            for (int i = _contentTable.Count - 1; i >= 0; i--)
                length += _contentTable[i].Length;
            return length;
        }

        private int GetLastWrittenVersion(int objectId, int typeId, out int index)
        {
            index = -1;
            // it's guarateed the local versions are written in ascending order
            for (int i = _contentTable.Count - 1; i >= 0; i--)
                if (_contentTable[i].Fits(objectId, typeId))
                {
                    index = i;
                    return _contentTable[i].LocalVersion;
                }
            return -1;
        }

        public IEnumerable<int> GetObjectIds<T>()
        {
            var typeId = SerializerStorage.GetTypeInfo(typeof(T)).Id;
            for (int i = 0; i < _contentTable.Count; i++)
                if (_contentTable[i].TypeId == typeId)
                {
                    var objectId = _contentTable[i].ObjectId;
                    bool alreadyReturned = false;
                    for (int j = 0; j < i; j++)
                        alreadyReturned |= _contentTable[j].Fits(objectId, typeId);
                    if (!alreadyReturned)
                        yield return objectId;
                }
        }

        private int FindContentEntry(int objectId, Type myType)
            => FindContentEntry(objectId, SerializerStorage.GetTypeInfo(myType).Id);
        private int FindContentEntry(int objectId, int typeId)
        {
            GetLastWrittenVersion(objectId, typeId, out var foundIndx);
            return foundIndx;
        }

        private long SetStreamPositionAndGetEndPosition(int entryIndex)
        {
            var entry = _contentTable[entryIndex];
            long position = entry.Position;
            _stream.Seek(position);

            var readId = _stream.ReadInt(Metadata.ObjectID);
            if (readId != entry.ObjectId)
                throw new Exception($"Read ObjectID ({readId}) doesn't fit the requested id ({entry.ObjectId}) in stream '{_stream.PrettyTypeName()}'");
            return position + _contentTable[entryIndex].Length;
        }

        private bool ValidateAndClearStreamPosition(int entryIndex, long endPos)
        {
            var realPos = _stream.Position;
            _stream.ClearStreamPosition();
            if (realPos == endPos)
                return true;
            var entry = _contentTable[entryIndex];
            var refType = SerializerStorage.GetTypeInfo(entry.TypeId);
            SetStreamPositionAndGetEndPosition(entryIndex);
            var readTypeId = _stream.ReadInt(Metadata.TypeID);
            _stream.ClearStreamPosition();
            var readType = SerializerStorage.GetTypeInfo(readTypeId, false);
            throw new Exception($"Stream position after object id={entry.ObjectId} {readType}{(readTypeId == entry.TypeId ? "" : $" ref type {refType}")} differs from expected one: {realPos} != {endPos}");
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidObjectId(int id, bool throwIfInvalid = false)
        {
            if (id != -1)
                return true;
            if (throwIfInvalid)
                throw new Exception("Invalid object id " + id);
            return false;
        }

        [Conditional("CONTENT_TABLE_INTEGRITY_CHECK")]
        private void CheckContentTableIntegrity()
        {
            // all operations with content table should guarantee
            // the latest versions are written closer to the table end

            for (int i = _contentTable.Count - 1; i > 0; i--)
                for (int j = i - 1; j >= 0; j--)
                {
                    var next = _contentTable[i];
                    var previous = _contentTable[j];
                    if (!next.Fits(previous.ObjectId, previous.TypeId))
                        continue;
                    if (next.LocalVersion <= previous.LocalVersion)
                        throw new Exception($"Content table entry #{i} ({next}) has smaller VERSION than #{j} ({previous})");
                    if (next.Position <= previous.Position)
                        throw new Exception($"Content table entry #{i} ({next}) has earlier POSITION than #{j} ({previous})");
                }
        }

    }
}

#region serialization

namespace DaSerialization.Serialization
{
    public class BinaryContainerSerializer_v1 : AFullSerializer<BinaryContainer>
    {
        public override int Version => 1;

        public override void ReadDataToObject(ref BinaryContainer c, BinaryStream stream)
        {
            var reader = stream.GetReader();
            int len = reader.ReadUIntPacked().ToInt32();
            bool writable = reader.ReadBoolean();

            var memStream = c?.GetUnderlyingStream();
            if (memStream == null || !memStream.CanWrite)
                memStream = new MemoryStream(len);
            var copiedLen = stream.GetUnderlyingStream().CopyPartiallyTo(memStream, len);
            if (copiedLen != len)
                throw new Exception($"Failed to read {c.PrettyTypeName()}: {copiedLen} bytes read instead of {len} expected");
            var storage = c?.SerializerStorage ?? SerializerStorage.Default;
            var binStream = new BinaryStream(memStream, storage, writable);
            // here we re-create container to avoid messing up with internal
            // state (dirty, size, etc.)
            c = new BinaryContainer(binStream);
        }

        public override void WriteObject(BinaryContainer c, BinaryStream stream)
        {
            c.CleanUp();
            var memStream = c.GetUnderlyingStream();
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