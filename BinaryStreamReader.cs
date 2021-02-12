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

        public bool ReadBool(string metaInfo = null)
        {
            OnDeserializePrimitiveBegin(typeof(bool), metaInfo);
            var result = _reader.ReadBoolean();
            OnDeserializePrimitiveEnd();
            return result;
        }

        public byte   ReadByte(string metaInfo = null)
        {
            OnDeserializePrimitiveBegin(typeof(Byte), metaInfo);
            var result = _reader.ReadByte();
            OnDeserializePrimitiveEnd();
            return result;
        }
        public short  ReadInt16(string metaInfo = null)
        {
            OnDeserializePrimitiveBegin(typeof(Int16), metaInfo);
            var result = _reader.ReadInt16();
            OnDeserializePrimitiveEnd();
            return result;
        }
        public int    ReadInt32(string metaInfo = null)
        {
            OnDeserializePrimitiveBegin(typeof(Int32), metaInfo);
            var result = _reader.ReadInt32();
            OnDeserializePrimitiveEnd();
            return result;
        }
        public long   ReadInt64(string metaInfo = null)
        {
            OnDeserializePrimitiveBegin(typeof(Int64), metaInfo);
            var result = _reader.ReadInt64();
            OnDeserializePrimitiveEnd();
            return result;
        }
        public sbyte  ReadSByte(string metaInfo = null)
        {
            OnDeserializePrimitiveBegin(typeof(SByte), metaInfo);
            var result = _reader.ReadSByte();
            OnDeserializePrimitiveEnd();
            return result;
        }
        public ushort ReadUInt16(string metaInfo = null)
        {
            OnDeserializePrimitiveBegin(typeof(UInt16), metaInfo);
            var result = _reader.ReadUInt16();
            OnDeserializePrimitiveEnd();
            return result;
        }
        public uint   ReadUInt32(string metaInfo = null)
        {
            OnDeserializePrimitiveBegin(typeof(UInt32), metaInfo);
            var result = _reader.ReadUInt32();
            OnDeserializePrimitiveEnd();
            return result;
        }
        public ulong  ReadUInt64(string metaInfo = null)
        {
            OnDeserializePrimitiveBegin(typeof(UInt64), metaInfo);
            var result = _reader.ReadUInt64();
            OnDeserializePrimitiveEnd();
            return result;
        }

        public decimal ReadDecimal(string metaInfo = null)
        {
            OnDeserializePrimitiveBegin(typeof(Decimal), metaInfo);
            var result = _reader.ReadDecimal();
            OnDeserializePrimitiveEnd();
            return result;
        }
        public float   ReadSingle(string metaInfo = null)
        {
            OnDeserializePrimitiveBegin(typeof(Single), metaInfo);
            var result = _reader.ReadSingle();
            OnDeserializePrimitiveEnd();
            return result;
        }
        public double  ReadDouble(string metaInfo = null)
        {
            OnDeserializePrimitiveBegin(typeof(Double), metaInfo);
            var result = _reader.ReadDouble();
            OnDeserializePrimitiveEnd();
            return result;
        }

        public char   ReadChar(string metaInfo = null)
        {
            OnDeserializePrimitiveBegin(typeof(Char), metaInfo);
            var result = _reader.ReadChar();
            OnDeserializePrimitiveEnd();
            return result;
        }
        public char   ReadCharASCII(string metaInfo = null)
        {
            OnDeserializePrimitiveBegin(typeof(Char), metaInfo);
            var result = _asciiReader.ReadChar();
            OnDeserializePrimitiveEnd();
            return result;
        }
        public string ReadString(string metaInfo = null)
        {
            OnDeserializePrimitiveBegin(typeof(String), metaInfo);
            var result = _reader.ReadString();
            OnDeserializePrimitiveEnd();
            return result;
        }
        public string ReadStringASCII(string metaInfo = null)
        {
            OnDeserializePrimitiveBegin(typeof(String), metaInfo);
            var result = _asciiReader.ReadString();
            OnDeserializePrimitiveEnd();
            return result;
        }
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

        public T ReadObject<T>(string metaInfo = null)
        {
            T result = default;
            ReadObject(ref result, metaInfo);
            return result;
        }
        public void ReadObject<T>(ref T obj, string metaInfo = null)
        {
            CheckStreamReady();
            OnDeserializeMetaBegin(typeof(T));
            int readTypeId = ReadMetadata(Metadata.TypeID);
            if (readTypeId == -1)
                obj = default;
            var readTypeInfo = SerializerStorage.GetTypeInfo(readTypeId);
            ReadObjectExact(ref obj, readTypeInfo, metaInfo);
        }

        /// <summary>
        /// Polymorphism was not allowed during serialization
        /// </summary>
        public void ReadObjectExact<T>(ref T obj, string metaInfo = null)
        {
            var typeInfo = SerializerStorage.GetTypeInfo(typeof(T));
            OnDeserializeMetaBegin(typeof(T));
            ReadObjectExact(ref obj, typeInfo, metaInfo);
        }
        /// <summary>
        /// Polymorphism was not allowed during serialization
        /// </summary>
        public void ReadObjectExact<T>(ref T obj, int typeId, string metaInfo = null)
        {
            var typeInfo = SerializerStorage.GetTypeInfo(typeId);
            OnDeserializeMetaBegin(typeInfo.Type);
            ReadObjectExact(ref obj, typeInfo, metaInfo);
        }
        /// <summary>
        /// Polymorphism was not allowed during serialization
        /// </summary>
        private void ReadObjectExact<T>(ref T obj, SerializationTypeInfo typeInfo, string metaInfo)
        {
            CheckStreamReady();
            var deserializerTypeless = ReadDeserializer<T>(typeInfo, out bool deserializerIsOfDerivedType);
            if (deserializerTypeless == null)
            {
                obj = default;
                OnDeserializeObjectBegin(metaInfo);
                OnDeserializeObjectEnd();
                return;
            }
            LockDeserialization();
            OnDeserializeObjectBegin(typeInfo, deserializerTypeless, metaInfo);
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
            OnDeserializeObjectEnd();
            UnlockDeserialization();
        }

        public void Deserialize(long streamPos, ref object obj, SerializationTypeInfo typeInfo, int deserializerVersion, string metaInfo = null)
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
                OnDeserializeObjectBegin(metaInfo);
                OnDeserializeObjectEnd();
                return;
            }
            LockDeserialization();
            OnDeserializeObjectBegin(typeInfo, deserializerTypeless, metaInfo);
            deserializerTypeless.ReadDataToTypelessObject(ref obj, this);
            OnDeserializeObjectEnd();
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
        public T[] ReadArray<T>(string metaInfo = null)
            where T : class
        {
            T[] arr = null;
            ReadArray(ref arr, metaInfo);
            return arr;
        }
        /// <summary>
        /// Use ReadArrayExact for value type arrays to save some space and performance
        /// </summary>
        public void ReadArray<T>(ref T[] arr, string metaInfo = null)
            where T : class
        {
            CheckStreamReady();
            OnDeserializeMetaBegin(typeof(T[]));
            int len = ReadMetadata(Metadata.CollectionSize);
            if (len == -1)
            {
                arr = null;
                OnDeserializeObjectBegin(metaInfo);
                OnDeserializeObjectEnd();
                return;
            }
            if (arr == null || arr.Length != len)
                arr = new T[len];
            OnDeserializeObjectBegin(new SerializationTypeInfo(typeof(T[])), null, metaInfo);
            for (int i = 0; i < len; i++)
            {
                var v = arr[i];
                ReadObject(ref v, IndexMetaInfo(i));
                arr[i] = v;
            }
            OnDeserializeObjectEnd();
        }

        /// <summary>
        /// Polymorphism was not allowed for array elements during serialization
        /// </summary>
        public T[] ReadArrayExact<T>(string metaInfo = null)
        {
            T[] arr = null;
            ReadArrayExact(ref arr, metaInfo);
            return arr;
        }
        /// <summary>
        /// Polymorphism was not allowed for array elements during serialization
        /// </summary>
        public void ReadArrayExact<T>(ref T[] arr, string metaInfo = null)
        {
            CheckStreamReady();
            OnDeserializeMetaBegin(typeof(T[]));
            int len = ReadMetadata(Metadata.CollectionSize);
            if (len < 0)
            {
                arr = null;
                OnDeserializeObjectBegin(metaInfo);
                OnDeserializeObjectEnd();
                return;
            }

            if (arr == null || arr.Length != len)
                arr = new T[len];

            var type = typeof(T);
            var typeInfo = SerializerStorage.GetTypeInfo(type);
            var deserializer = ReadDeserializer<T>(typeInfo);

            LockDeserialization();
            OnDeserializeObjectBegin(new SerializationTypeInfo(typeof(T[])), null, metaInfo);
            for (int i = 0; i < len; i++)
            {
                var v = arr[i];
                OnDeserializeMetaBegin(typeof(T));
                OnDeserializeObjectBegin(typeInfo, deserializer, IndexMetaInfo(i));
                deserializer.ReadDataToObject(ref v, this);
                OnDeserializeObjectEnd();
                arr[i] = v;
            }
            OnDeserializeObjectEnd();
            UnlockDeserialization();
        }

        #endregion

        #region lists

        /// <summary>
        /// Use ReadListExact for value type lists to save some space and performance
        /// </summary>
        public List<T> ReadList<T>(string metaInfo = null)
            where T : class
        {
            List<T> list = null;
            ReadList(ref list, metaInfo);
            return list;
        }
        /// <summary>
        /// Use ReadListExact for value type lists to save some space and performance
        /// </summary>
        public void ReadList<T>(ref List<T> list, string metaInfo = null)
            where T : class
        {
            CheckStreamReady();
            OnDeserializeMetaBegin(typeof(List<T>));
            int len = ReadMetadata(Metadata.CollectionSize);
            if (len < 0)
            {
                list = null;
                OnDeserializeObjectBegin(metaInfo);
                OnDeserializeObjectEnd();
                return;
            }

            if (list == null)
                list = new List<T>(len > 4 ? len : 4);
            else
            {
                if (list.Capacity < len)
                    list.Capacity = len;
            }
            OnDeserializeObjectBegin(new SerializationTypeInfo(typeof(List<T>)), null, metaInfo);
            for (int i = 0, count = list.Count; i < len & i < count; i++)
            {
                var v = list[i];
                ReadObject(ref v, IndexMetaInfo(i));
                list[i] = v;
            }
            for (int i = list.Count; i < len; i++)
                list.Add(ReadObject<T>());
            if (list.Count > len)
                list.RemoveRange(len, list.Count - len);
            OnDeserializeObjectEnd();
        }

        /// <summary>
        /// Polymorphism was not allowed for list elements during serialization
        /// </summary>
        public List<T> ReadListExact<T>(string metaInfo = null)
        {
            List<T> list = null;
            ReadListExact(ref list, metaInfo);
            return list;
        }
        /// <summary>
        /// Polymorphism was not allowed for list elements during serialization
        /// </summary>
        public void ReadListExact<T>(ref List<T> list, string metaInfo = null)
        {
            CheckStreamReady();
            OnDeserializeMetaBegin(typeof(List<T>));
            int len = ReadMetadata(Metadata.CollectionSize);
            if (len < 0)
            {
                list = null;
                OnDeserializeObjectBegin(metaInfo);
                OnDeserializeObjectEnd();
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
            OnDeserializeObjectBegin(new SerializationTypeInfo(typeof(List<T>)), null, metaInfo);
            for (int i = 0, count = list.Count; i < len & i < count; i++)
            {
                var v = list[i];
                OnDeserializeMetaBegin(typeof(T));
                OnDeserializeObjectBegin(typeInfo, deserializer, IndexMetaInfo(i));
                deserializer.ReadDataToObject(ref v, this);
                OnDeserializeObjectEnd();
                list[i] = v;
            }
            for (int i = list.Count; i < len; i++)
            {
                T v = default;
                OnDeserializeMetaBegin(typeof(T));
                OnDeserializeObjectBegin(typeInfo, deserializer, IndexMetaInfo(i));
                deserializer.ReadDataToObject(ref v, this);
                OnDeserializeObjectEnd();
                list.Add(v);
            }
            OnDeserializeObjectEnd();
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
        public delegate void ObjectDeserializationStart(Type refType, SerializationTypeInfo typeInfo, long streamPos, uint metaInfoLen, int version, string name);
        public event ObjectDeserializationStart ObjectDeserializationStarted;
        public delegate void PrimitiveDeserializationStart(Type refType, long streamPos, string name);
        public event PrimitiveDeserializationStart PrimitiveDeserializationStarted;
        public delegate void DeserializationEnd(long streamPos);
        public event DeserializationEnd DeserializationEnded;
        public event DeserializationEnd PrimitiveDeserializationEnded;
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
        [Conditional("INSPECT_DESERIALIZATION")]
        private void OnDeserializeObjectBegin(string name)
            => OnDeserializeObjectBegin(SerializationTypeInfo.Invalid, null, name);
        [Conditional("INSPECT_DESERIALIZATION")]
        private void OnDeserializeObjectBegin(SerializationTypeInfo typeInfo, IDeserializer deserializer, string name)
        {
            if (!EnableDeserializationInspection)
                return;
            var pos = _stream.Position;
            uint metaLen = _lastMetaInfoStreamPosition < 0 ? 0 : (pos - _lastMetaInfoStreamPosition).ToUInt32();
            ObjectDeserializationStarted?.Invoke(_lastRefType, typeInfo, pos, metaLen, deserializer == null ? -1 : deserializer.Version, name);
            _lastMetaInfoStreamPosition = -1;
        }
        [Conditional("INSPECT_DESERIALIZATION")]
        private void OnDeserializeObjectEnd()
        {
            if (!EnableDeserializationInspection)
                return;
            var pos = _stream.Position;
            DeserializationEnded?.Invoke(pos);
            _lastMetaInfoStreamPosition = -1;
        }

        [Conditional("INSPECT_DESERIALIZATION")]
        private void OnDeserializePrimitiveBegin(Type type, string name)
        {
            if (!EnableDeserializationInspection)
                return;
            if (_lastMetaInfoStreamPosition != -1)
                return; // when we deserialize primitives during meta deserialization
            var pos = _stream.Position;
            PrimitiveDeserializationStarted?.Invoke(type, pos, name);
        }
        [Conditional("INSPECT_DESERIALIZATION")]
        private void OnDeserializePrimitiveEnd()
        {
            if (!EnableDeserializationInspection)
                return;
            if (_lastMetaInfoStreamPosition != -1)
                return; // when we deserialize primitives during meta deserialization
            var pos = _stream.Position;
            PrimitiveDeserializationEnded?.Invoke(pos);
        }

        private string IndexMetaInfo(int index)
        {
            return
#if INSPECT_DESERIALIZATION
                index.ToStringFast();
#else
                null;
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