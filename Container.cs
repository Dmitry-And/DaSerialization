#if UNITY_EDITOR && !DISABLE_DEBUG
#define STATE_CHECK // additional validation that container do not serialize during deserialization process and vice versa
#define INSPECT_DESERIALIZATION // deserialization callback will be fired
#define SERIALIZE_POLYMORPHIC_CHECK
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using DaSerialization.Internal;

namespace DaSerialization
{
    public enum Metadata
    {
        ObjectID,
        TypeID, // 0 if object is null
        Version, // 0 if object is null
        CollectionSize,
    }

    public struct SerializedObjectInfo
    {
        public int ObjectId;
        public int TypeId;
        public int LocalVersion;
        public int Length;
        public long Position;

        public bool Fits(int objectId, int typeId) { return TypeId == typeId & ObjectId == objectId; }
        public bool Fits(int objectId, int typeId, int version) { return TypeId == typeId & ObjectId == objectId & LocalVersion == version; }
    }

    public abstract class AContainer<TStream> : IContainer
        where TStream : class, IStream<TStream>, new()
    {
        public bool Writable { get => _stream.Writable; }
        public bool IsDirty { get; private set; }
        public long Size { get; private set; }

        private TStream _stream;
        public SerializerStorage<TStream> SerializerStorage { get; private set; }
        private List<SerializedObjectInfo> _contentTable;
        public List<SerializedObjectInfo> GetContentTable() => _contentTable;

        public AContainer(TStream stream, SerializerStorage<TStream> storage)
        {
            SerializerStorage = storage;
            _stream = stream;
            _contentTable = ReadContentTable(_stream);
            Size = _stream.Length;
            ClearStreamPosition();
        }

        protected abstract List<SerializedObjectInfo> ReadContentTable(TStream stream);
        protected abstract void WriteContentTable(TStream stream, List<SerializedObjectInfo> contentTable);
        public abstract long CountContentTableLenght(List<SerializedObjectInfo> contentTable);

        public bool Has<T>(int objectId)
            => FindContentEntry(objectId, typeof(T)) >= 0;

        public int GetSize<T>(int objectId)
        {
            var entryIndex = FindContentEntry(objectId, typeof(T));
            return entryIndex >= 0 ? _contentTable[entryIndex].Length : -1;
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
            var endPos = SetStreamPositionAndGetEndPosition(entryIndex);
            Deserialize(ref obj);
            return ValidateAndClearStreamPosition(entryIndex, endPos);
        }

        #endregion

        #region inner deserialization

        public void DeserializeStatic<T>(ref T obj)
        {
            var typeInfo = SerializerStorage.GetTypeInfo(typeof(T));
            DeserializeStatic(ref obj, typeInfo);
        }
        public void DeserializeStatic<T>(ref T obj, int typeId)
        {
            var typeInfo = SerializerStorage.GetTypeInfo(typeId);
            DeserializeStatic(ref obj, typeInfo);
        }
        private void DeserializeStatic<T>(ref T obj, SerializationTypeInfo typeInfo)
        {
            CheckStreamReady();
            OnDeserializeMetaBegin();
            var deserializerTypeless = ReadDeserializer<T>(typeInfo, out bool deserializerIsOfDerivedType);
            if (deserializerTypeless == null)
            {
                obj = default;
                return;
            }
            LockDeserialization();
            OnDeserializeDataBegin();
            if (deserializerIsOfDerivedType)
            {
                var objTypeless = obj as object;
                deserializerTypeless.ReadDataToTypelessObject(ref objTypeless, _stream, this);
                obj = (T)objTypeless;
            }
            else
            {
                var deserializer = deserializerTypeless as IDeserializer<T, TStream>;
                deserializer.ReadDataToObject(ref obj, _stream, this);
            }
            OnDeserializeEnd(typeInfo.Type, obj);
            UnlockDeserialization();
        }

        public T Deserialize<T>()
        {
            T result = default;
            Deserialize(ref result);
            return result;
        }
        public void Deserialize<T>(ref T obj)
        {
            CheckStreamReady();
            OnDeserializeMetaBegin();
            int readTypeId = _stream.ReadInt(Metadata.TypeID);
            if (readTypeId == -1)
                obj = default;
            var readTypeInfo = SerializerStorage.GetTypeInfo(readTypeId);
            DeserializeStatic(ref obj, readTypeInfo);
        }

        private IDeserializer<T, TStream> ReadDeserializer<T>(SerializationTypeInfo typeInfo)
        {
            if (typeInfo.Id == -1)
                return null;
            int version = _stream.ReadInt(Metadata.Version);
            var dd = SerializerStorage.GetDeserializer(typeInfo, version) as IDeserializer<T, TStream>;
            if (dd == null)
                throw new Exception($"Unable to find deserializer for type {SerializerStorage.GetTypeInfo(typeof(T), false)}, stream '{typeof(TStream).PrettyName()}', version {version}, read type is {typeInfo}");
            return dd;
        }
        private IDeserializer<TStream> ReadDeserializer<T>(SerializationTypeInfo typeInfo, out bool deserializerIsOfDerivedType)
        {
            deserializerIsOfDerivedType = false;
            if (typeInfo.Id == -1)
                return null;
            var typeTypeInfo = SerializerStorage.GetTypeInfo(typeof(T), false);
            if (typeTypeInfo.Id != typeInfo.Id)
                deserializerIsOfDerivedType = true;
            int version = _stream.ReadInt(Metadata.Version);
            if (version == 0)
                return null;
            var dd = SerializerStorage.GetDeserializer(typeInfo, version);
            if (dd == null)
                throw new Exception($"Unable to find deserializer for type {typeTypeInfo}, stream '{typeof(TStream).PrettyName()}', version {version}, read type is {typeInfo}");
            return dd;
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
            CheckWritingAllowed();
            IsValidObjectId(objectId, true);
            int localVersion = GetLastWrittenVersion(objectId, typeTypeId, out _);
            localVersion = localVersion < 0 ? 1 : localVersion + 1;
            long position = _stream.Length;
            _stream.Seek(position);

            _stream.WriteInt(Metadata.ObjectID, objectId);
            _stream.WriteInt(Metadata.TypeID, objTypeInfo.Id);
            bool result = objTypeInfo.Id == -1
                || SerializeInner(obj, objTypeInfo, objTypeInfo.Id != typeTypeId | forcePolymorphic);
            int length = (int)(_stream.Position - position);
            ClearStreamPosition();
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
            IsDirty = true;
            Size = _stream.Length;
            return true;
        }

        #endregion

        #region inner serialization

        public bool Serialize<T>(T obj)
        {
            CheckStreamReady();
            var baseType = typeof(T);
            // value type
            if (baseType.IsValueType)
            {
                var typeInfo = SerializerStorage.GetTypeInfo(baseType);
                _stream.WriteInt(Metadata.TypeID, typeInfo.Id);
                return SerializeInner(obj, typeInfo, false);
            }
            // null
            bool isDefault = EqualityComparer<T>.Default.Equals(obj, default);
            if (isDefault)
            {
                _stream.WriteInt(Metadata.TypeID, -1);
                return true;
            }
            // reference, not-null
            {
                var type = obj.GetType();
                var typeInfo = SerializerStorage.GetTypeInfo(type);
                _stream.WriteInt(Metadata.TypeID, typeInfo.Id);
                return SerializeInner(obj, typeInfo, type != baseType);
            }
        }
        public bool SerializeStatic<T>(T obj)
        {
            var typeInfo = SerializerStorage.GetTypeInfo(typeof(T));
            return SerializeInner(obj, typeInfo, false);
        }
        public bool SerializeStatic<T>(T obj, int typeId, bool inherited)
        {
            var typeInfo = SerializerStorage.GetTypeInfo(typeId);
            return SerializeInner(obj, typeInfo, inherited);
        }

        /// <summary>
        /// inheritance is only for performance reasons
        /// </summary>
        private bool SerializeInner<T>(T obj, SerializationTypeInfo typeInfo, bool inheritance)
        {
            CheckWritingAllowed();
            CheckStreamReady();
            LockSerialization();
            BeginWriteCheck(typeInfo, out var oldValue, out var oldType);
            bool isValueType = typeof(T).IsValueType;
            bool polymorphic = !isValueType & inheritance;
            if (!polymorphic)
            {
                CheckNonPolymorphic(obj);
                var serializer = SerializerStorage.GetSerializer(typeInfo) as ISerializer<T, TStream>;
                if (serializer == null)
                    throw new Exception($"Unable to find serializer for type {typeInfo}, stream '{typeof(TStream).PrettyName()}'");
                var version = !isValueType && EqualityComparer<T>.Default.Equals(obj, default)
                    ? 0 : serializer.Version;
                _stream.WriteInt(Metadata.Version, version);
                if (version != 0)
                    serializer.WriteObject(obj, _stream, this);
            }
            else
            {
                // inheritance/interfaces takes place!
                var serializer = SerializerStorage.GetSerializer(typeInfo);
                if (serializer == null)
                    throw new Exception($"Unable to find serializer for type {typeInfo}, stream '{typeof(TStream).PrettyName()}'");
                var version = EqualityComparer<T>.Default.Equals(obj, default)
                    ? 0 : serializer.Version;
                _stream.WriteInt(Metadata.Version, version);
                if (version != 0)
                    serializer.WriteObjectTypeless(obj, _stream, this);
            }
            EndWriteCheck(oldValue, oldType);
            UnlockSerialization();
            IsDirty = true;
            Size = _stream.Length;
            return true;
        }

        private bool _allowContainerSerialization;
        private Type _parentSerializingType;
        private void BeginWriteCheck(SerializationTypeInfo typeInfo, out bool oldValue, out Type oldType)
        {
            if (typeInfo.IsContainer & !_allowContainerSerialization)
            {
                var parentTypeInfo = SerializerStorage.GetTypeInfo(_parentSerializingType);
                var serializer = SerializerStorage.GetSerializer(parentTypeInfo);
                SerializationLogger.LogWarning($"Serializing nested {typeInfo.Type.PrettyName()} within {_parentSerializingType.PrettyName()} type by serializer {serializer.PrettyTypeName()} which doesn't implement {nameof(ISerializerWritesContainer)} interfase.\nThis may lead to incorrect {nameof(UpdateSerializers)} effects of not updating serializers within nested containers");
            }
            oldValue = _allowContainerSerialization;
            _allowContainerSerialization = typeInfo.LatestSerializerWritesContainer;
            oldType = _parentSerializingType;
            _parentSerializingType = typeInfo.Type;
        }
        private void EndWriteCheck(bool oldValue, Type oldType)
        {
            _allowContainerSerialization = oldValue;
            _parentSerializingType = oldType;
        }
        private void CheckWritingAllowed()
        {
            if (!Writable)
                throw new InvalidOperationException($"Trying to serialize into container with non-writable stream {_stream}");
        }
        private void CheckStreamReady()
        {
            if (_stream.Position < 0)
                throw new Exception($"Trying to serialize to stream w/o setting position");
        }
        [Conditional("SERIALIZE_POLYMORPHIC_CHECK")]
        private void CheckNonPolymorphic<T>(T obj)
        {
            var type = typeof(T);
            if (type.IsValueType)
                return;
            if (EqualityComparer<T>.Default.Equals(obj, default))
                return;
            if (type != obj.GetType())
                throw new Exception($"Object {obj.PrettyTypeName()} is polymorphic but expected of type {type.PrettyName()}");
        }

        #endregion

        #region lists

        public void SerializeListStatic<T>(List<T> list)
        {
            CheckWritingAllowed();
            CheckStreamReady();
            int count = list == null ? -1 : list.Count;
            _stream.WriteInt(Metadata.CollectionSize, count);
            if (count < 0)
                return;
            var type = typeof(T);
            var typeInfo = SerializerStorage.GetTypeInfo(type);
            var serializer = SerializerStorage.GetSerializer(typeInfo) as ISerializer<T, TStream>;
            if (serializer == null)
                throw new Exception($"Unable to find serializer for type {typeInfo}, stream '{typeof(TStream).PrettyName()}'");
            _stream.WriteInt(Metadata.Version, serializer.Version);

            var isRefType = !type.IsValueType;
            LockSerialization();
            BeginWriteCheck(typeInfo, out var oldValue, out var oldType);
            for (int i = 0; i < count; i++)
            {
                var e = list[i];
                if (isRefType && (e == null || e.GetType() != type))
                    throw new ArgumentException($"Trying to {nameof(SerializeListStatic)} a list with polymorphic elements. Element {i} is {e.PrettyTypeName()} in List<{type.PrettyName()}>");
                serializer.WriteObject(e, _stream, this);
            }
            EndWriteCheck(oldValue, oldType);
            UnlockSerialization();
            IsDirty = true;
            Size = _stream.Length;
        }

        public void SerializeList<T>(List<T> list)
            where T : class
        {
            CheckWritingAllowed();
            CheckStreamReady();
            int count = list == null ? -1 : list.Count;
            _stream.WriteInt(Metadata.CollectionSize, count);
            for (int i = 0; i < count; i++)
                Serialize(list[i]);
        }

        public List<T> DeserializeList<T>()
            where T : class
        {
            List<T> list = null;
            DeserializeList(ref list);
            return list;
        }

        public List<T> DeserializeListStatic<T>()
        {
            List<T> list = null;
            DeserializeListStatic(ref list);
            return list;
        }

        public void DeserializeListStatic<T>(ref List<T> list)
        {
            CheckStreamReady();
            OnDeserializeMetaBegin();
            int len = _stream.ReadInt(Metadata.CollectionSize);
            if (len < 0)
            {
                list = null;
                return;
            }

            if (list == null)
                list = new List<T>(len > 4 ? len : 4);
            else
            {
                if (list.Capacity < len)
                    list.Capacity = len;
            }

            var type = typeof(T);
            var typeInfo = SerializerStorage.GetTypeInfo(type);
            var deserializer = ReadDeserializer<T>(typeInfo);

            LockDeserialization();
            OnDeserializeDataBegin();
            for (int i = 0, count = list.Count; i < len & i < count; i++)
            {
                var v = list[i];
                OnDeserializeDataBegin();
                deserializer.ReadDataToObject(ref v, _stream, this);
                OnDeserializeEnd(typeInfo.Type, v);
                list[i] = v;
            }
            for (int i = list.Count; i < len; i++)
            {
                T v = default;
                OnDeserializeDataBegin();
                deserializer.ReadDataToObject(ref v, _stream, this);
                OnDeserializeEnd(typeInfo.Type, v);
                list.Add(v);
            }
            OnDeserializeEnd(typeInfo.Type, list);
            UnlockDeserialization();
            if (list.Count > len)
                list.RemoveRange(len, list.Count - len);
        }

        public void DeserializeList<T>(ref List<T> list)
            where T : class
        {
            CheckStreamReady();
            int len = _stream.ReadInt(Metadata.CollectionSize);
            if (len < 0)
            {
                list = null;
                return;
            }

            if (list == null)
                list = new List<T>(len > 4 ? len : 4);
            else
            {
                if (list.Capacity < len)
                    list.Capacity = len;
            }
            for (int i = 0, count = list.Count; i < len & i < count; i++)
            {
                var v = list[i];
                Deserialize(ref v);
                list[i] = v;
            }
            for (int i = list.Count; i < len; i++)
                list.Add(Deserialize<T>());
            if (list.Count > len)
                list.RemoveRange(len, list.Count - len);
        }

        #endregion

        #region arrays

        public void SerializeArrayStatic<T>(T[] arr)
        {
            CheckWritingAllowed();
            CheckStreamReady();
            int count = arr == null ? -1 : arr.Length;
            _stream.WriteInt(Metadata.CollectionSize, count);
            if (count < 0)
                return;
            var type = typeof(T);
            var typeInfo = SerializerStorage.GetTypeInfo(type);
            var serializer = SerializerStorage.GetSerializer(typeInfo) as ISerializer<T, TStream>;
            if (serializer == null)
                throw new Exception($"Unable to find serializer for type {typeInfo}, stream '{typeof(TStream).PrettyName()}'");
            _stream.WriteInt(Metadata.Version, serializer.Version);

            var isRefType = !type.IsValueType;
            LockSerialization();
            BeginWriteCheck(typeInfo, out var oldValue, out var oldType);
            for (int i = 0; i < count; i++)
            {
                var e = arr[i];
                if (isRefType && (e == null || e.GetType() != type))
                    throw new ArgumentException($"Trying to {nameof(SerializeArrayStatic)} an array with polymorphic elements. Element {i} is {e.PrettyTypeName()} in {type.PrettyName()} array");
                serializer.WriteObject(e, _stream, this);
            }
            EndWriteCheck(oldValue, oldType);
            UnlockSerialization();
            IsDirty = true;
            Size = _stream.Length;
        }

        public void SerializeArray<T>(T[] arr)
            where T : class
        {
            int len = arr == null ? -1 : arr.Length;
            _stream.WriteInt(Metadata.CollectionSize, len);
            for (int i = 0; i < len; i++)
                Serialize(arr[i]);
        }

        public T[] DeserializeArray<T>()
            where T : class
        {
            T[] arr = null;
            DeserializeArray(ref arr);
            return arr;
        }

        public void DeserializeArray<T>(ref T[] arr)
            where T : class
        {
            int len = _stream.ReadInt(Metadata.CollectionSize);
            if (len == -1)
            {
                arr = null;
                return;
            }
            if (arr == null || arr.Length != len)
                arr = new T[len];
            for (int i = 0; i < len; i++)
            {
                var v = arr[i];
                Deserialize(ref v);
                arr[i] = v;
            }
        }

        public T[] DeserializeArrayStatic<T>()
        {
            T[] arr = null;
            DeserializeArrayStatic(ref arr);
            return arr;
        }

        public void DeserializeArrayStatic<T>(ref T[] arr)
        {
            CheckStreamReady();
            OnDeserializeMetaBegin();
            int len = _stream.ReadInt(Metadata.CollectionSize);
            if (len < 0)
            {
                arr = null;
                return;
            }

            if (arr == null || arr.Length != len)
                arr = new T[len];

            var type = typeof(T);
            var typeInfo = SerializerStorage.GetTypeInfo(type);
            var deserializer = ReadDeserializer<T>(typeInfo);

            LockDeserialization();
            OnDeserializeDataBegin();
            for (int i = 0; i < len; i++)
            {
                var v = arr[i];
                OnDeserializeDataBegin();
                deserializer.ReadDataToObject(ref v, _stream, this);
                OnDeserializeEnd(typeInfo.Type, v);
                arr[i] = v;
            }
            OnDeserializeEnd(typeInfo.Type, arr);
            UnlockDeserialization();
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
        /// Create new stream instead of existing one, copy only latest versions of all objects into
        /// the stream, serialize seek table into the stream and mark the container as not dirty.
        /// Only CleanUp-ed containers can be saved (the seek table serialized only here
        /// </summary>
        public void CleanUp(bool preserveCapacity = false)
        {
            // TODO: performance
            if (!IsDirty)
                return;
            RemoveOldVersions();
            long capacity = preserveCapacity ? _stream.Capacity
                : CountTotalDataLength() + CountContentTableLenght(_contentTable);
            var newStream = new TStream();
            newStream.Allocate(capacity);
            newStream.Seek(newStream.Length);
            for (int i = 0; i < _contentTable.Count; i++)
            {
                var entry = _contentTable[i];
                _stream.Seek(entry.Position);
                entry.Position = newStream.Position;
                entry.LocalVersion = 0;
                _stream.CopyTo(newStream, entry.Length);
                _contentTable[i] = entry;
            }
            WriteContentTable(newStream, _contentTable);
            _stream = newStream;
            Size = _stream.Length;
            IsDirty = false;
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
        public bool CopyObjectFrom<T>(AContainer<TStream> other, int objectIdInOther, int myObjectId = -1, bool updateSerializers = false)
        {
            CheckWritingAllowed();
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
                other.ClearStreamPosition();
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

            int length = (int)(_stream.Position - position);
            ClearStreamPosition();

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
            // of inner objects which were serialized by container.Serialize(obj)
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
        public Stream GetUnderlyingStream()
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
            int maxVersion = -1;
            index = -1;
            for (int i = _contentTable.Count - 1; i >= 0; i--)
                if (_contentTable[i].Fits(objectId, typeId)
                    && _contentTable[i].LocalVersion > maxVersion)
                {
                    maxVersion = _contentTable[i].LocalVersion;
                    index = i;
                }
            return maxVersion;
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
            ClearStreamPosition();
            if (realPos == endPos)
                return true;
            var entry = _contentTable[entryIndex];
            var refType = SerializerStorage.GetTypeInfo(entry.TypeId);
            SetStreamPositionAndGetEndPosition(entryIndex);
            var readTypeId = _stream.ReadInt(Metadata.TypeID);
            ClearStreamPosition();
            var readType = SerializerStorage.GetTypeInfo(readTypeId, false);
            throw new Exception($"Stream position after object id={entry.ObjectId} {readType}{(readTypeId == entry.TypeId ? "" : $" ref type {refType}")} differs from expected one: {realPos} != {endPos}");
        }
        private void ClearStreamPosition()
        {
            _stream.Seek(-1);
            _parentSerializingType = null;
            // to allow Containers to be serialized as root objects
            _allowContainerSerialization = true;
        }

        #endregion

        #region state validations

        public bool EnableDeserializationInspection;
        public delegate void DeserializationFinished(Type type, object obj, long dataSize, int metaSize, int nestedLevel);
        public event DeserializationFinished ObjectDeserializationFinished;
#if INSPECT_DESERIALIZATION
        private Stack<long> _streamStartMetaPositions = new Stack<long>(16);
        private Stack<long> _streamStartDataPositions = new Stack<long>(16);
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidObjectId(int id, bool throwIfInvalid = false)
        {
            if (id != -1)
                return true;
            if (throwIfInvalid)
                throw new Exception("Invalid object id " + id);
            return false;
        }

        [Conditional("INSPECT_DESERIALIZATION")]
        private void OnDeserializeMetaBegin()
        {
#if INSPECT_DESERIALIZATION
            if (!EnableDeserializationInspection)
                return;
            if (_streamStartMetaPositions.Count == _streamStartDataPositions.Count)
                _streamStartMetaPositions.Push(_stream.Position);
#endif
        }
        [Conditional("INSPECT_DESERIALIZATION")]
        private void OnDeserializeDataBegin()
        {
#if INSPECT_DESERIALIZATION
            if (!EnableDeserializationInspection)
                return;
            var pos = _stream.Position;
            if (_streamStartMetaPositions.Count == _streamStartDataPositions.Count)
                _streamStartMetaPositions.Push(pos);
            _streamStartDataPositions.Push(pos);
#endif
        }
        [Conditional("INSPECT_DESERIALIZATION")]
        private void OnDeserializeEnd(Type type, object obj)
        {
#if INSPECT_DESERIALIZATION
            if (!EnableDeserializationInspection)
                return;
            var dataStartPos = _streamStartDataPositions.Pop();
            var dataSize = _stream.Position - dataStartPos;
            var metaSize = dataStartPos - _streamStartMetaPositions.Pop();
            var nestedness = _streamStartDataPositions.Count;
            ObjectDeserializationFinished?.Invoke(type, obj, dataSize, (int)metaSize, nestedness);
#endif
        }

#if STATE_CHECK
        private int _serializationLock = 0;
        private int _deserializationLock = 0;
#endif

        [Conditional("STATE_CHECK")]
        private void LockSerialization()
        {
#if STATE_CHECK
            _serializationLock++;
            if (_deserializationLock > 0)
                throw new InvalidOperationException("Trying to serialize during deserialization");
#endif
        }
        [Conditional("STATE_CHECK")]
        private void UnlockSerialization()
        {
#if STATE_CHECK
            _serializationLock--;
#endif
        }

        [Conditional("STATE_CHECK")]
        private void LockDeserialization()
        {
#if STATE_CHECK
            _deserializationLock++;
            if (_serializationLock > 0)
                throw new InvalidOperationException("Trying to deserialize during serialization");
#endif
        }
        [Conditional("STATE_CHECK")]
        private void UnlockDeserialization()
        {
#if STATE_CHECK
            _deserializationLock--;
#endif
        }

        #endregion
    }
}