#if DEBUG && !DISABLE_DEBUG
#define STATE_CHECK // additional validation that container do not serialize during deserialization process and vice versa
#define INSPECT_DESERIALIZATION // deserialization callback will be fired
#define SERIALIZE_POLYMORPHIC_CHECK
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using DaSerialization.Internal;

namespace DaSerialization
{
    public class BinaryStream : IStreamInternals
    {
        // first 4 bytes written to the stream to identify it as a valid BinaryStream
        public const int MagicNumber = 0x35_2A_31_BB; // 891957691
        public const int MetaDataSize = sizeof(int);
        private readonly Encoding DefaultStringEncoding = Encoding.UTF8;

        public SerializerStorage SerializerStorage { get; private set; }

        private MemoryStream _stream;
        private BinaryReader _reader;
        private BinaryWriter _writer;
        private bool _locked = true;

        public long Position
        {
            get { return _stream == null | _locked ? -1 : _stream.Position; }
            protected set
            {
                if (value < 0 | _stream == null || _stream.Length < value)
                    _locked = true;
                else
                {
                    _stream.Seek(value, SeekOrigin.Begin);
                    _locked = false;
                }
            }
        }

        public long Length => _stream == null ? -1 : _stream.Length;
        public long Capacity => _stream.Capacity;
        public long ZeroPosition => MetaDataSize;
        public bool Writable { get; protected set; }

        public BinaryStream(SerializerStorage storage)
        {
            SerializerStorage = storage;
            Writable = true;
        }
        public BinaryStream(MemoryStream stream, SerializerStorage storage, bool writable = false)
        {
            _stream = stream;
            SerializerStorage = storage;
            Writable = writable;
            CreateReaderAndWriter();
            if (writable & _stream.Length == 0)
                WriteMagicNumber();
        }

        public void Allocate(long length)
        {
            if (!Writable)
                throw new InvalidOperationException($"Trying to {nameof(Allocate)} non-writable {this.PrettyTypeName()}");
            if (_stream != null)
                throw new InvalidOperationException($"Trying to {nameof(Allocate)} a {this.PrettyTypeName()} which is already initialized");
            _stream = new MemoryStream((int)length + MetaDataSize);
            CreateReaderAndWriter();
            WriteMagicNumber();
        }

        public void SetLength(long length)
        {
            _stream.SetLength(length);
        }

        private void CreateReaderAndWriter()
        {
            _reader = new BinaryReader(_stream, DefaultStringEncoding, true);
            if (Writable)
                _writer = new BinaryWriter(_stream, DefaultStringEncoding, true);
        }

        private void WriteMagicNumber()
        {
            _stream.Seek(0, SeekOrigin.Begin);
            _writer.Write(MagicNumber);
        }

        public bool CheckIsValidStream()
        {
            if (_stream == null)
                return true;
            if (_stream.Length < 4)
                return false;
            _stream.Seek(0, SeekOrigin.Begin);
            var number = _reader.ReadInt32();
            return number == MagicNumber;
        }
        public static bool IsValidData(byte[] data)
        {
            if (data == null || data.Length < 4)
                return false;
            int prefix = (int)(((uint)data[0] << 0)
                + ((uint)data[1] << 8)
                + ((uint)data[2] << 16)
                + ((uint)data[3] << 24));
            return prefix == MagicNumber;
        }

        public void CopyTo(BinaryStream destination, long length)
        {
            if (this == destination)
                throw new InvalidOperationException($"Trying to {nameof(CopyTo)} to the {this.PrettyTypeName()} itself");
            CopyTo(destination, destination.Position, length);
        }
        public void CopyTo(BinaryStream destination, long position, long length)
        {
            if (_stream == null)
                throw new InvalidOperationException($"Trying to {nameof(CopyTo)} from empty {this.PrettyTypeName()}");
            if (_locked)
                throw new InvalidOperationException($"Trying to {nameof(CopyTo)} from {this.PrettyTypeName()} w/o setting position");
            if (destination == null)
                throw new ArgumentException("Other stream is null");
            if (!destination.Writable)
                throw new InvalidOperationException($"Trying to {nameof(CopyTo)} to {destination.PrettyTypeName()} which is not writable");
            if (destination._locked)
                throw new InvalidOperationException($"Trying to {nameof(CopyTo)} to {destination.PrettyTypeName()} w/o setting position");
            if (Position + length > Length)
                throw new IndexOutOfRangeException($"Trying to {nameof(CopyTo)} from {this.PrettyTypeName()} more bytes than it has: position {Position}, length {Length}, copying {length}");

            var writer = destination._writer;
            var reader = _reader;
            if (this != destination)
            {
                destination.Seek(position);
                for (long i = 0; i < length; i++)
                    writer.Write(reader.ReadByte());
            }
            else
            {
                var readPos = Position;
                var writePos = position;
                for (long i = 0; i < length; i++)
                {
                    // TODO: performance
                    Seek(readPos++);
                    var data = reader.ReadByte();
                    Seek(writePos++);
                    writer.Write(data);
                }
                Seek(readPos); // convention: this stream is 'read' stream in the first place
            }
        }

        public int ReadInt(Metadata meta)
        {
            if (_stream == null)
                throw new InvalidOperationException($"Trying to {nameof(ReadInt)} from empty {this.PrettyTypeName()}");
            if (_locked)
                throw new InvalidOperationException($"Trying to {nameof(ReadInt)} from {this.PrettyTypeName()} w/o setting position");
            switch (meta)
            {
                case Metadata.Version:
                case Metadata.CollectionSize:
                    return (int)((long)_reader.ReadUIntPacked() - 1);
                case Metadata.TypeID:
                    return _reader.ReadInt32();
                case Metadata.ObjectID:
                    return (int)_reader.ReadUIntPacked();
                default: throw new Exception(meta.ToString());
            }
        }

        public void WriteInt(Metadata meta, int value)
        {
            if (_stream == null)
                throw new InvalidOperationException($"Trying to {nameof(WriteInt)} to empty {this.PrettyTypeName()}");
            if (_locked)
                throw new InvalidOperationException($"Trying to {nameof(WriteInt)} to {this.PrettyTypeName()} w/o setting position");
            if (!Writable)
                throw new InvalidOperationException($"Trying to {nameof(WriteInt)} to non-writable {this.PrettyTypeName()}");
            switch (meta)
            {
                case Metadata.Version:
                case Metadata.CollectionSize:
                    _writer.WriteUIntPacked((value + 1).ToUInt64());
                    return;
                case Metadata.TypeID:
                    _writer.Write(value);
                    return;
                case Metadata.ObjectID:
                    _writer.WriteUIntPacked((ulong)value);
                    return;
                default: throw new Exception(meta.ToString());
            }
        }

        public void Seek(long position)
        {
            if (_stream == null)
                throw new InvalidOperationException($"Trying to {nameof(Seek)} in empty {this.PrettyTypeName()}");
            Position = position;
        }

        public void ClearStreamPosition()
        {
            Seek(-1);
            _parentSerializingType = null;
            // to allow Containers to be serialized as root objects
            _allowContainerSerialization = true;
        }

        public void Dispose()
        {
            _reader?.Dispose();
            _reader = null;
            _writer?.Dispose();
            _writer = null;
            _stream?.Dispose();
            _stream = null;
            _locked = true;
        }

        public MemoryStream GetUnderlyingStream() => _stream;
        public BinaryReader GetReader() => _reader;
        public BinaryWriter GetWriter() => Writable ? _writer : null;
        public int GetMetaDataSize() => MetaDataSize;

        public void Clear()
        {
            if (!Writable)
                throw new InvalidOperationException($"Trying to {nameof(Clear)} non-writable {this.PrettyTypeName()}");
            _stream.SetLength(0);
            WriteMagicNumber();
        }

        #region inner deserialization

        public void DeserializeStatic<T>(ref T obj)
        {
            var typeInfo = SerializerStorage.GetTypeInfo(typeof(T));
            OnDeserializeMetaBegin(typeof(T));
            DeserializeStatic(ref obj, typeInfo);
        }
        public void DeserializeStatic<T>(ref T obj, int typeId)
        {
            var typeInfo = SerializerStorage.GetTypeInfo(typeId);
            OnDeserializeMetaBegin(typeInfo.Type);
            DeserializeStatic(ref obj, typeInfo);
        }
        private void DeserializeStatic<T>(ref T obj, SerializationTypeInfo typeInfo)
        {
            CheckStreamReady();
            var deserializerTypeless = ReadDeserializer<T>(typeInfo, out bool deserializerIsOfDerivedType);
            if (deserializerTypeless == null)
            {
                obj = default;
                OnDeserializeDataBegin();
                OnDeserializeEnd();
                return;
            }
            LockDeserialization();
            OnDeserializeDataBegin(typeInfo, deserializerTypeless);
            if (deserializerIsOfDerivedType)
            {
                var objTypeless = obj as object;
                deserializerTypeless.ReadDataToTypelessObject(ref objTypeless, this);
                obj = (T)objTypeless;
            }
            else
            {
                var deserializer = deserializerTypeless as IDeserializer<T>;
                deserializer.ReadDataToObject(ref obj, this);
            }
            OnDeserializeEnd();
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
            OnDeserializeMetaBegin(typeof(T));
            int readTypeId = ReadInt(Metadata.TypeID);
            if (readTypeId == -1)
                obj = default;
            var readTypeInfo = SerializerStorage.GetTypeInfo(readTypeId);
            DeserializeStatic(ref obj, readTypeInfo);
        }
        void IStreamInternals.Deserialize(long streamPos, ref object obj, SerializationTypeInfo typeInfo, int deserializerVersion)
        {
            Seek(streamPos);
            OnDeserializeMetaBegin(typeInfo.Type);
            CheckStreamReady();
            IDeserializer deserializerTypeless = null;
            if (typeInfo.Id != -1
                & deserializerVersion != 0)
            {
                deserializerTypeless = SerializerStorage.GetDeserializer(typeInfo, deserializerVersion);
                if (deserializerTypeless == null)
                    throw new Exception($"Unable to find deserializer for type {typeInfo}, stream '{typeof(BinaryStream).PrettyName()}', version {deserializerVersion}");
            }
            if (deserializerTypeless == null)
            {
                obj = default;
                OnDeserializeDataBegin();
                OnDeserializeEnd();
                return;
            }
            LockDeserialization();
            OnDeserializeDataBegin(typeInfo, deserializerTypeless);
            deserializerTypeless.ReadDataToTypelessObject(ref obj, this);
            OnDeserializeEnd();
            UnlockDeserialization();
            ClearStreamPosition();
        }

        private IDeserializer<T> ReadDeserializer<T>(SerializationTypeInfo typeInfo)
        {
            if (typeInfo.Id == -1)
                return null;
            int version = ReadInt(Metadata.Version);
            var dd = SerializerStorage.GetDeserializer(typeInfo, version) as IDeserializer<T>;
            if (dd == null)
                throw new Exception($"Unable to find deserializer for type {SerializerStorage.GetTypeInfo(typeof(T), false)}, stream '{typeof(BinaryStream).PrettyName()}', version {version}, read type is {typeInfo}");
            return dd;
        }
        private IDeserializer ReadDeserializer<T>(SerializationTypeInfo typeInfo, out bool deserializerIsOfDerivedType)
        {
            deserializerIsOfDerivedType = false;
            if (typeInfo.Id == -1)
                return null;
            var typeTypeInfo = SerializerStorage.GetTypeInfo(typeof(T), false);
            if (typeTypeInfo.Id != typeInfo.Id)
                deserializerIsOfDerivedType = true;
            int version = ReadInt(Metadata.Version);
            if (version == 0)
                return null;
            var dd = SerializerStorage.GetDeserializer(typeInfo, version);
            if (dd == null)
                throw new Exception($"Unable to find deserializer for type {typeTypeInfo}, stream '{typeof(BinaryStream).PrettyName()}', version {version}, read type is {typeInfo}");
            return dd;
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
                WriteInt(Metadata.TypeID, typeInfo.Id);
                return SerializeInner(obj, typeInfo, false);
            }
            // null
            bool isDefault = EqualityComparer<T>.Default.Equals(obj, default);
            if (isDefault)
            {
                WriteInt(Metadata.TypeID, -1);
                return true;
            }
            // reference, not-null
            {
                var type = obj.GetType();
                var typeInfo = SerializerStorage.GetTypeInfo(type);
                WriteInt(Metadata.TypeID, typeInfo.Id);
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

        bool IStreamInternals.SerializeInner<T>(T obj, SerializationTypeInfo typeInfo, bool inheritance)
            => SerializeInner(obj, typeInfo, inheritance);

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
                var serializer = SerializerStorage.GetSerializer(typeInfo) as ISerializer<T>;
                if (serializer == null)
                    throw new Exception($"Unable to find serializer for type {typeInfo}, stream '{typeof(BinaryStream).PrettyName()}'");
                var version = !isValueType && EqualityComparer<T>.Default.Equals(obj, default)
                    ? 0 : serializer.Version;
                WriteInt(Metadata.Version, version);
                if (version != 0)
                    serializer.WriteObject(obj, this);
            }
            else
            {
                // inheritance/interfaces takes place!
                var serializer = SerializerStorage.GetSerializer(typeInfo);
                if (serializer == null)
                    throw new Exception($"Unable to find serializer for type {typeInfo}, stream '{typeof(BinaryStream).PrettyName()}'");
                var version = EqualityComparer<T>.Default.Equals(obj, default)
                    ? 0 : serializer.Version;
                WriteInt(Metadata.Version, version);
                if (version != 0)
                    serializer.WriteObjectTypeless(obj, this);
            }
            EndWriteCheck(oldValue, oldType);
            UnlockSerialization();
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
                SerializationLogger.LogWarning($"Serializing nested {typeInfo.Type.PrettyName()} within {_parentSerializingType.PrettyName()} type by serializer {serializer.PrettyTypeName()} which doesn't implement {nameof(ISerializerWritesContainer)} interfase.\nThis may lead to incorrect {nameof(BinaryContainer.UpdateSerializers)} effects of not updating serializers within nested containers");
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
        public void CheckWritingAllowed()
        {
            if (!Writable)
                throw new InvalidOperationException($"Trying to write to non-writable stream {this.PrettyTypeName()}");
        }
        private void CheckStreamReady()
        {
            if (Position < 0)
                throw new Exception($"Trying to read/write from/to stream w/o setting position");
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
            WriteInt(Metadata.CollectionSize, count);
            if (count < 0)
                return;
            var type = typeof(T);
            var typeInfo = SerializerStorage.GetTypeInfo(type);
            var serializer = SerializerStorage.GetSerializer(typeInfo) as ISerializer<T>;
            if (serializer == null)
                throw new Exception($"Unable to find serializer for type {typeInfo}, stream '{typeof(BinaryStream).PrettyName()}'");
            WriteInt(Metadata.Version, serializer.Version);

            var isRefType = !type.IsValueType;
            LockSerialization();
            BeginWriteCheck(typeInfo, out var oldValue, out var oldType);
            for (int i = 0; i < count; i++)
            {
                var e = list[i];
                if (isRefType && (e == null || e.GetType() != type))
                    throw new ArgumentException($"Trying to {nameof(SerializeListStatic)} a list with polymorphic elements. Element {i} is {e.PrettyTypeName()} in List<{type.PrettyName()}>");
                serializer.WriteObject(e, this);
            }
            EndWriteCheck(oldValue, oldType);
            UnlockSerialization();
        }

        public void SerializeList<T>(List<T> list)
            where T : class
        {
            CheckWritingAllowed();
            CheckStreamReady();
            int count = list == null ? -1 : list.Count;
            WriteInt(Metadata.CollectionSize, count);
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
            OnDeserializeMetaBegin(typeof(List<T>));
            int len = ReadInt(Metadata.CollectionSize);
            if (len < 0)
            {
                list = null;
                OnDeserializeDataBegin();
                OnDeserializeEnd();
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
            OnDeserializeDataBegin(new SerializationTypeInfo(typeof(List<T>)), null);
            for (int i = 0, count = list.Count; i < len & i < count; i++)
            {
                var v = list[i];
                OnDeserializeMetaBegin(typeof(T));
                OnDeserializeDataBegin(typeInfo, deserializer);
                deserializer.ReadDataToObject(ref v, this);
                OnDeserializeEnd();
                list[i] = v;
            }
            for (int i = list.Count; i < len; i++)
            {
                T v = default;
                OnDeserializeMetaBegin(typeof(T));
                OnDeserializeDataBegin(typeInfo, deserializer);
                deserializer.ReadDataToObject(ref v, this);
                OnDeserializeEnd();
                list.Add(v);
            }
            OnDeserializeEnd();
            UnlockDeserialization();
            if (list.Count > len)
                list.RemoveRange(len, list.Count - len);
        }

        public void DeserializeList<T>(ref List<T> list)
            where T : class
        {
            CheckStreamReady();
            OnDeserializeMetaBegin(typeof(List<T>));
            int len = ReadInt(Metadata.CollectionSize);
            if (len < 0)
            {
                list = null;
                OnDeserializeDataBegin();
                OnDeserializeEnd();
                return;
            }

            if (list == null)
                list = new List<T>(len > 4 ? len : 4);
            else
            {
                if (list.Capacity < len)
                    list.Capacity = len;
            }
            OnDeserializeDataBegin(new SerializationTypeInfo(typeof(List<T>)), null);
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
            OnDeserializeEnd();
        }

        #endregion

        #region arrays

        public void SerializeArrayStatic<T>(T[] arr)
        {
            CheckWritingAllowed();
            CheckStreamReady();
            int count = arr == null ? -1 : arr.Length;
            WriteInt(Metadata.CollectionSize, count);
            if (count < 0)
                return;
            var type = typeof(T);
            var typeInfo = SerializerStorage.GetTypeInfo(type);
            var serializer = SerializerStorage.GetSerializer(typeInfo) as ISerializer<T>;
            if (serializer == null)
                throw new Exception($"Unable to find serializer for type {typeInfo}, stream '{typeof(BinaryStream).PrettyName()}'");
            WriteInt(Metadata.Version, serializer.Version);

            var isRefType = !type.IsValueType;
            LockSerialization();
            BeginWriteCheck(typeInfo, out var oldValue, out var oldType);
            for (int i = 0; i < count; i++)
            {
                var e = arr[i];
                if (isRefType && (e == null || e.GetType() != type))
                    throw new ArgumentException($"Trying to {nameof(SerializeArrayStatic)} an array with polymorphic elements. Element {i} is {e.PrettyTypeName()} in {type.PrettyName()} array");
                serializer.WriteObject(e, this);
            }
            EndWriteCheck(oldValue, oldType);
            UnlockSerialization();
        }

        public void SerializeArray<T>(T[] arr)
            where T : class
        {
            int len = arr == null ? -1 : arr.Length;
            WriteInt(Metadata.CollectionSize, len);
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
            CheckStreamReady();
            OnDeserializeMetaBegin(typeof(T[]));
            int len = ReadInt(Metadata.CollectionSize);
            if (len == -1)
            {
                arr = null;
                OnDeserializeDataBegin();
                OnDeserializeEnd();
                return;
            }
            if (arr == null || arr.Length != len)
                arr = new T[len];
            OnDeserializeDataBegin(new SerializationTypeInfo(typeof(T[])), null);
            for (int i = 0; i < len; i++)
            {
                var v = arr[i];
                Deserialize(ref v);
                arr[i] = v;
            }
            OnDeserializeEnd();
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
            OnDeserializeMetaBegin(typeof(T[]));
            int len = ReadInt(Metadata.CollectionSize);
            if (len < 0)
            {
                arr = null;
                OnDeserializeDataBegin();
                OnDeserializeEnd();
                return;
            }

            if (arr == null || arr.Length != len)
                arr = new T[len];

            var type = typeof(T);
            var typeInfo = SerializerStorage.GetTypeInfo(type);
            var deserializer = ReadDeserializer<T>(typeInfo);

            LockDeserialization();
            OnDeserializeDataBegin(new SerializationTypeInfo(typeof(T[])), null);
            for (int i = 0; i < len; i++)
            {
                var v = arr[i];
                OnDeserializeMetaBegin(typeof(T));
                OnDeserializeDataBegin(typeInfo, deserializer);
                deserializer.ReadDataToObject(ref v, this);
                OnDeserializeEnd();
                arr[i] = v;
            }
            OnDeserializeEnd();
            UnlockDeserialization();
        }

        #endregion

        #region state validations

        public bool EnableDeserializationInspection;
        public delegate void DeserializationStarted(Type refType, SerializationTypeInfo typeInfo, long streamPos, uint metaInfoLen, int version);
        public event DeserializationStarted ObjectDeserializationStarted;
        public delegate void DeserializationFinished(long streamPos);
        public event DeserializationFinished ObjectDeserializationFinished;
        private long _lastMetaInfoStreamPosition = -1;
        private Type _lastRefType;

        public void OnDeserializeMetaBegin(Type refType, long startPosition = long.MaxValue)
        {
#if INSPECT_DESERIALIZATION
            if (!EnableDeserializationInspection)
                return;
            if (_lastMetaInfoStreamPosition != -1)
                return;
            if (startPosition == long.MaxValue)
                startPosition = Position;
            _lastRefType = refType;
            _lastMetaInfoStreamPosition = startPosition;
#endif
        }
        private void OnDeserializeDataBegin()
            => OnDeserializeDataBegin(SerializationTypeInfo.Invalid, null);
        private void OnDeserializeDataBegin(SerializationTypeInfo typeInfo, IDeserializer deserializer)
        {
#if INSPECT_DESERIALIZATION
            if (!EnableDeserializationInspection)
                return;
            var pos = Position;
            uint metaLen = _lastMetaInfoStreamPosition < 0 ? 0 : (pos - _lastMetaInfoStreamPosition).ToUInt32();
            ObjectDeserializationStarted?.Invoke(_lastRefType, typeInfo, pos, metaLen, deserializer == null ? -1 : deserializer.Version);
            _lastMetaInfoStreamPosition = -1;
#endif
        }
        private void OnDeserializeEnd()
        {
#if INSPECT_DESERIALIZATION
            if (!EnableDeserializationInspection)
                return;
            var pos = Position;
            ObjectDeserializationFinished?.Invoke(pos);
            _lastMetaInfoStreamPosition = -1;
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
