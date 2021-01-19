#if DEBUG && !DISABLE_DEBUG
#define STATE_CHECK // additional validation that container do not serialize during deserialization process and vice versa
#define INSPECT_DESERIALIZATION // deserialization callback will be fired
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace DaSerialization
{
    public class BinaryStreamReader : IDisposable
    {
        public SerializerStorage SerializerStorage { get; private set; }

        public long Length => _stream == null ? -1 : _stream.Length;
        public long Capacity => _stream.Capacity;
        public long ZeroPosition => BinaryStream.MetaDataSize;

        private BinaryStream _binaryStream;
        private MemoryStream _stream;
        private BinaryReader _reader;
        private BinaryReader _asciiReader; // workaround, look at BinaryStream.StringEncoding

        public MemoryStream GetUnderlyingStream() => _stream;

        public BinaryStreamReader(BinaryStream binaryStream)
        {
            SerializerStorage = binaryStream.SerializerStorage;
            _binaryStream = binaryStream;
            _stream = binaryStream.GetUnderlyingStream();
            _reader = new BinaryReader(_stream, BinaryStream.StringEncoding, true);
            _asciiReader = new BinaryReader(_stream, Encoding.ASCII, true);
        }

        public int ReadMetadata(Metadata meta)
        {
            if (_stream == null)
                throw new InvalidOperationException($"Trying to {nameof(ReadMetadata)} from empty {this.PrettyTypeName()}");
            if (_binaryStream.IsLocked)
                throw new InvalidOperationException($"Trying to {nameof(ReadMetadata)} from {this.PrettyTypeName()} w/o setting position");
            switch (meta)
            {
                case Metadata.Version:
                case Metadata.CollectionSize:
                    return (int)((long)this.ReadUIntPacked() - 1);
                case Metadata.TypeID:
                    return _reader.ReadInt32();
                case Metadata.ObjectID:
                    return (int)this.ReadUIntPacked();
                default: throw new Exception(meta.ToString());
            }
        }

        #region stream read methods

        public bool ReadBool() => _reader.ReadBoolean();

        public byte   ReadByte()  => _reader.ReadByte();
        public short  ReadInt16() => _reader.ReadInt16();
        public int    ReadInt32() => _reader.ReadInt32();
        public long   ReadInt64() => _reader.ReadInt64();
        public sbyte  ReadSByte()  => _reader.ReadSByte();
        public ushort ReadUInt16() => _reader.ReadUInt16();
        public uint   ReadUInt32() => _reader.ReadUInt32();
        public ulong  ReadUInt64() => _reader.ReadUInt64();

        public decimal ReadDecimal() => _reader.ReadDecimal();
        public float   ReadSingle() => _reader.ReadSingle();
        public double  ReadDouble() => _reader.ReadDouble();

        public char   ReadChar() => _reader.ReadChar();
        public char   ReadCharASCII() => _asciiReader.ReadChar();
        public string ReadString() => _reader.ReadString();
        public string ReadStringASCII() => _asciiReader.ReadString();
        public char[] ReadChars(int count) => _reader.ReadChars(count);
        public void   ReadChars(char[] data, int start = 0, int length = -1)
        {
            int end = length < 0 ? end = data.Length : start + length;
            for (int i = start; i < end; i++)
                data[i] = _reader.ReadChar();
        }
        public char[] ReadCharsASCII(int count) => _asciiReader.ReadChars(count);
        public void   ReadCharsASCII(char[] data, int start = 0, int length = -1)
        {
            int end = length < 0 ? end = data.Length : start + length;
            for (int i = start; i < end; i++)
                data[i] = _asciiReader.ReadChar();
        }
        public byte[] ReadBytes(int count) => _reader.ReadBytes(count);
        public void   ReadBytes(byte[] data, int start = 0, int length = -1)
        {
            int end = length < 0 ? end = data.Length : start + length;
            for (int i = start; i < end; i++)
                data[i] = _reader.ReadByte();
        }

        #endregion

        #region inner deserialization

        public T ReadObject<T>()
        {
            T result = default;
            ReadObject(ref result);
            return result;
        }
        public void ReadObject<T>(ref T obj)
        {
            CheckStreamReady();
            OnDeserializeMetaBegin(typeof(T));
            int readTypeId = ReadMetadata(Metadata.TypeID);
            if (readTypeId == -1)
                obj = default;
            var readTypeInfo = SerializerStorage.GetTypeInfo(readTypeId);
            ReadObjectExact(ref obj, readTypeInfo);
        }

        /// <summary>
        /// Polymorphism was not allowed during serialization
        /// </summary>
        public void ReadObjectExact<T>(ref T obj)
        {
            var typeInfo = SerializerStorage.GetTypeInfo(typeof(T));
            OnDeserializeMetaBegin(typeof(T));
            ReadObjectExact(ref obj, typeInfo);
        }
        /// <summary>
        /// Polymorphism was not allowed during serialization
        /// </summary>
        public void ReadObjectExact<T>(ref T obj, int typeId)
        {
            var typeInfo = SerializerStorage.GetTypeInfo(typeId);
            OnDeserializeMetaBegin(typeInfo.Type);
            ReadObjectExact(ref obj, typeInfo);
        }
        /// <summary>
        /// Polymorphism was not allowed during serialization
        /// </summary>
        private void ReadObjectExact<T>(ref T obj, SerializationTypeInfo typeInfo)
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

        public void Deserialize(long streamPos, ref object obj, SerializationTypeInfo typeInfo, int deserializerVersion)
        {
            _binaryStream.Seek(streamPos);
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
            _binaryStream.ClearStreamPosition();
        }

        private IDeserializer<T> ReadDeserializer<T>(SerializationTypeInfo typeInfo)
        {
            if (typeInfo.Id == -1)
                return null;
            int version = ReadMetadata(Metadata.Version);
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
            int version = ReadMetadata(Metadata.Version);
            if (version == 0)
                return null;
            var dd = SerializerStorage.GetDeserializer(typeInfo, version);
            if (dd == null)
                throw new Exception($"Unable to find deserializer for type {typeTypeInfo}, stream '{typeof(BinaryStream).PrettyName()}', version {version}, read type is {typeInfo}");
            return dd;
        }

        #endregion

        #region arrays

        /// <summary>
        /// Use ReadArrayExact for value type arrays to save some space and performance
        /// </summary>
        public T[] ReadArray<T>()
            where T : class
        {
            T[] arr = null;
            ReadArray(ref arr);
            return arr;
        }
        /// <summary>
        /// Use ReadArrayExact for value type arrays to save some space and performance
        /// </summary>
        public void ReadArray<T>(ref T[] arr)
            where T : class
        {
            CheckStreamReady();
            OnDeserializeMetaBegin(typeof(T[]));
            int len = ReadMetadata(Metadata.CollectionSize);
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
                ReadObject(ref v);
                arr[i] = v;
            }
            OnDeserializeEnd();
        }

        /// <summary>
        /// Polymorphism was not allowed for array elements during serialization
        /// </summary>
        public T[] ReadArrayExact<T>()
        {
            T[] arr = null;
            ReadArrayExact(ref arr);
            return arr;
        }
        /// <summary>
        /// Polymorphism was not allowed for array elements during serialization
        /// </summary>
        public void ReadArrayExact<T>(ref T[] arr)
        {
            CheckStreamReady();
            OnDeserializeMetaBegin(typeof(T[]));
            int len = ReadMetadata(Metadata.CollectionSize);
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

        #region lists

        /// <summary>
        /// Use ReadListExact for value type lists to save some space and performance
        /// </summary>
        public List<T> ReadList<T>()
            where T : class
        {
            List<T> list = null;
            ReadList(ref list);
            return list;
        }
        /// <summary>
        /// Use ReadListExact for value type lists to save some space and performance
        /// </summary>
        public void ReadList<T>(ref List<T> list)
            where T : class
        {
            CheckStreamReady();
            OnDeserializeMetaBegin(typeof(List<T>));
            int len = ReadMetadata(Metadata.CollectionSize);
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
                ReadObject(ref v);
                list[i] = v;
            }
            for (int i = list.Count; i < len; i++)
                list.Add(ReadObject<T>());
            if (list.Count > len)
                list.RemoveRange(len, list.Count - len);
            OnDeserializeEnd();
        }

        /// <summary>
        /// Polymorphism was not allowed for list elements during serialization
        /// </summary>
        public List<T> ReadListExact<T>()
        {
            List<T> list = null;
            ReadListExact(ref list);
            return list;
        }
        /// <summary>
        /// Polymorphism was not allowed for list elements during serialization
        /// </summary>
        public void ReadListExact<T>(ref List<T> list)
        {
            CheckStreamReady();
            OnDeserializeMetaBegin(typeof(List<T>));
            int len = ReadMetadata(Metadata.CollectionSize);
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

        #endregion

        #region state validation

        private void CheckStreamReady()
        {
            if (_binaryStream.IsLocked)
                throw new Exception($"Trying to read/write from/to stream w/o setting position");
        }

        [Conditional("STATE_CHECK")]
        private void LockDeserialization()
        {
            if (_binaryStream.SerializationDepth > 0)
                throw new InvalidOperationException("Trying to deserialize during serialization");
            _binaryStream.SerializationDepth--;
        }
        [Conditional("STATE_CHECK")]
        private void UnlockDeserialization()
        {
            _binaryStream.SerializationDepth++;
        }

        #endregion

        #region deserialization inspection

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
                startPosition = _stream.Position;
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
            var pos = _stream.Position;
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
            var pos = _stream.Position;
            ObjectDeserializationFinished?.Invoke(pos);
            _lastMetaInfoStreamPosition = -1;
#endif
        }

        #endregion

        public void Dispose()
        {
            _reader?.Dispose();
            _reader = null;
            _asciiReader?.Dispose();
            _asciiReader = null;
        }
    }
}