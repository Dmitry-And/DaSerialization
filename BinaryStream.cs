#if DEBUG && !DISABLE_DEBUG
#define STATE_CHECK // additional validation that container do not serialize during deserialization process and vice versa
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
    public enum Metadata
    {
        ObjectID,
        TypeID, // 0 if object is null
        Version, // 0 if object is null
        CollectionSize,
    }

    public class BinaryStream
    {
        // first 4 bytes written to the stream to identify it as a valid BinaryStream
        public const int MagicNumber = 0x35_2A_31_BB; // 891957691
        public const int MetaDataSize = sizeof(int);
        public static readonly Encoding DefaultStringEncoding = Encoding.UTF8;

        public SerializerStorage SerializerStorage { get; private set; }

        private MemoryStream _stream;
        private BinaryStreamReader _reader;
        private BinaryWriter _writer;
        private bool _locked = true;
        public bool IsLocked => _locked;

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
            if (_stream.Length != 0)
            {
                if (!CheckIsValidStream())
                    throw new InvalidDataException($"Trying to create {nameof(BinaryStream)} with invalid stream data");
            }
            else if (writable)
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
            _reader = new BinaryStreamReader(this);
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
        public BinaryStreamReader GetReader() => _reader;
        public BinaryWriter GetWriter() => Writable ? _writer : null;
        public int GetMetaDataSize() => MetaDataSize;

        public void Clear()
        {
            if (!Writable)
                throw new InvalidOperationException($"Trying to {nameof(Clear)} non-writable {this.PrettyTypeName()}");
            _stream.SetLength(0);
            WriteMagicNumber();
        }

        #region deserialization

        public int ReadInt(Metadata meta) => _reader.ReadInt(meta);

        public void DeserializeStatic<T>(ref T obj)
            => _reader.DeserializeStatic<T>(ref obj);
        public void DeserializeStatic<T>(ref T obj, int typeId)
            => _reader.DeserializeStatic<T>(ref obj, typeId);
        public T Deserialize<T>()
            => _reader.Deserialize<T>();
        public void Deserialize<T>(ref T obj)
            => _reader.Deserialize<T>(ref obj);
        public void Deserialize(long streamPos, ref object obj, SerializationTypeInfo typeInfo, int deserializerVersion)
            => _reader.Deserialize(streamPos, ref obj, typeInfo, deserializerVersion);

        public T[] DeserializeArray<T>()
            where T : class
            => _reader.DeserializeArray<T>();
        public void DeserializeArray<T>(ref T[] arr)
            where T : class
            => _reader.DeserializeArray<T>(ref arr);
        public T[] DeserializeArrayStatic<T>()
            => _reader.DeserializeArrayStatic<T>();
        public void DeserializeArrayStatic<T>(ref T[] arr)
            => _reader.DeserializeArrayStatic<T>(ref arr);

        public List<T> DeserializeList<T>()
            where T : class
            => _reader.DeserializeList<T>();
        public List<T> DeserializeListStatic<T>()
            => _reader.DeserializeListStatic<T>();
        public void DeserializeListStatic<T>(ref List<T> list)
            => _reader.DeserializeListStatic(ref list);
        public void DeserializeList<T>(ref List<T> list)
            where T : class
            => _reader.DeserializeList(ref list);

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

        /// <summary>
        /// inheritance is only for performance reasons
        /// </summary>
        public bool SerializeInner<T>(T obj, SerializationTypeInfo typeInfo, bool inheritance)
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
            if (IsLocked)
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

        #endregion

        #region state validations

#if STATE_CHECK
        public int SerializationLock { get; private set; } = 0;
#endif

        [Conditional("STATE_CHECK")]
        private void LockSerialization()
        {
#if STATE_CHECK
            SerializationLock++;
            if (_reader.DeserializationLock > 0)
                throw new InvalidOperationException("Trying to serialize during deserialization");
#endif
        }
        [Conditional("STATE_CHECK")]
        private void UnlockSerialization()
        {
#if STATE_CHECK
            SerializationLock--;
#endif
        }

        #endregion

    }

}
