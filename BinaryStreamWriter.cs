#if DEBUG && !DISABLE_DEBUG
#define STATE_CHECK // additional validation that container do not serialize during deserialization process and vice versa
#define SERIALIZE_POLYMORPHIC_CHECK
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using DaSerialization.Internal;

namespace DaSerialization
{
    public class BinaryStreamWriter : IDisposable
    {
        public SerializerStorage SerializerStorage { get; private set; }

        public long Length => _stream == null ? -1 : _stream.Length;
        public long Capacity => _stream.Capacity;
        public long ZeroPosition => BinaryStream.MetaDataSize;

        private BinaryStream _binaryStream;
        private MemoryStream _stream;
        private BinaryWriter _writer;
        public BinaryWriter GetWriter() => _writer;

        public MemoryStream GetUnderlyingStream() => _stream;

        public BinaryStreamWriter(BinaryStream binaryStream)
        {
            SerializerStorage = binaryStream.SerializerStorage;
            _binaryStream = binaryStream;
            _stream = binaryStream.GetUnderlyingStream();
            _writer = new BinaryWriter(_stream, BinaryStream.DefaultStringEncoding, true);
        }

        public void WriteInt(Metadata meta, int value)
        {
            if (_stream == null)
                throw new InvalidOperationException($"Trying to {nameof(WriteInt)} to empty {this.PrettyTypeName()}");
            if (_binaryStream.IsLocked)
                throw new InvalidOperationException($"Trying to {nameof(WriteInt)} to {this.PrettyTypeName()} w/o setting position");
            if (!_binaryStream.Writable) // TODO: remove?
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

        #region stream read methods

        public void Write(bool value) => _writer.Write(value);

        public void Write(byte value)  => _writer.Write(value);
        public void Write(short value) => _writer.Write(value);
        public void Write(int value) => _writer.Write(value);
        public void Write(long value) => _writer.Write(value);
        public void Write(sbyte value)  => _writer.Write(value);
        public void Write(ushort value) => _writer.Write(value);
        public void Write(uint value) => _writer.Write(value);
        public void Write(ulong value) => _writer.Write(value);

        public void Write(float value) => _writer.Write(value);
        public void Write(double value) => _writer.Write(value);

        public void Write(string value) => _writer.Write(value);

        #endregion

        #region Packed (TODO: move to separate file)

        public void Write3ByteInt32(int value) => _writer.Write3ByteInt32(value);
        public void Write3ByteUInt32(uint value) => _writer.Write3ByteUInt32(value);
        public void WriteIntPacked(long value) => _writer.WriteIntPacked(value);
        public void WriteUIntPacked(ulong value) => _writer.WriteUIntPacked(value);
        public void WriteIntPacked(long value, int bytesCount) => _writer.WriteIntPacked(value, bytesCount);
        public void WriteUIntPacked(ulong value, int bytesCount) => _writer.WriteUIntPacked(value, bytesCount);

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
            _binaryStream.CheckWritingAllowed();
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

        public void ResetContainerSerialization()
        {
            _parentSerializingType = null;
            // to allow Containers to be serialized as root objects
            _allowContainerSerialization = true;
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
        private void CheckStreamReady()
        {
            if (_binaryStream.IsLocked)
                throw new Exception($"Trying to write to stream w/o setting position");
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
            _binaryStream.CheckWritingAllowed();
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
            _binaryStream.CheckWritingAllowed();
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
            _binaryStream.CheckWritingAllowed();
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

        #region state validation

        [Conditional("STATE_CHECK")]
        private void LockSerialization()
        {
            if (_binaryStream.SerializationDepth < 0)
                throw new InvalidOperationException("Trying to serialize during deserialization");
            _binaryStream.SerializationDepth++;
        }
        [Conditional("STATE_CHECK")]
        private void UnlockSerialization()
        {
            _binaryStream.SerializationDepth--;
        }

        #endregion

        public void Dispose()
        {
            _writer?.Dispose();
            _writer = null;
        }
    }
}