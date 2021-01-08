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
        private BinaryStreamWriter _writer;
        private bool _locked = true;
        public bool IsLocked => _locked;
        // 0 - nothing is serializing,
        // positive - something is serializing,
        // negative - something is deserializing
        public int SerializationDepth;

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
            if (!Writable)
                throw new InvalidOperationException($"Trying to {nameof(SetLength)} for non-writable {this.PrettyTypeName()}");
            _stream.SetLength(length);
        }

        private void CreateReaderAndWriter()
        {
            _reader = new BinaryStreamReader(this);
            if (Writable)
                _writer = new BinaryStreamWriter(this);
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


        public void Seek(long position)
        {
            if (_stream == null)
                throw new InvalidOperationException($"Trying to {nameof(Seek)} in empty {this.PrettyTypeName()}");
            Position = position;
        }

        public void ClearStreamPosition()
        {
            Seek(-1);
            _writer?.ResetContainerSerialization();
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
        public BinaryStreamWriter GetWriter() => Writable ? _writer : null;
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

        #region serialization

        public void WriteInt(Metadata meta, int value)
            => _writer.WriteInt(meta, value);

        public bool Serialize<T>(T obj)
            => _writer.Serialize(obj);
        public bool SerializeStatic<T>(T obj)
            => _writer.SerializeStatic(obj);
        public bool SerializeStatic<T>(T obj, int typeId, bool inherited)
            => _writer.SerializeStatic(obj, typeId, inherited);

        public bool SerializeInner<T>(T obj, SerializationTypeInfo typeInfo, bool inheritance)
            => _writer.SerializeInner(obj, typeInfo, inheritance);

        public void SerializeListStatic<T>(List<T> list)
            => _writer.SerializeListStatic(list);
        public void SerializeList<T>(List<T> list) where T : class
            => _writer.SerializeList(list);
        public void SerializeArrayStatic<T>(T[] arr)
            => _writer.SerializeArrayStatic(arr);
        public void SerializeArray<T>(T[] arr) where T : class
            => _writer.SerializeArray(arr);

        // TODO: remove?
        public void CheckWritingAllowed()
        {
            if (!Writable)
                throw new InvalidOperationException($"Trying to write to non-writable stream {this.PrettyTypeName()}");
        }

        #endregion

    }

}
