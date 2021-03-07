#if DEBUG && !DISABLE_DEBUG
#define STATE_CHECK // additional validation that container do not serialize during deserialization process and vice versa
#define SERIALIZE_POLYMORPHIC_CHECK
#endif

using System;
using System.IO;
using System.Text;

namespace DaSerialization
{
    public enum Metadata
    {
        ObjectID,
        TypeID, // 0 if object is null
        Version, // 0 if object is null
        CollectionSize,

        None = -1,
    }

    public class BinaryStream
    {
        // first 4 bytes written to the stream to identify it as a valid BinaryStream
        public const int MagicNumber = 0x35_2A_31_BB; // 891957691
        public const int MetaDataSize = sizeof(int);
        // the only encoding supported for now. a lot of changes required
        // to provide generic Read/WriteString(Encoding) methods w/o allocations
        // https://stackoverflow.com/a/29436218
        public static readonly Encoding StringEncoding = Encoding.UTF8;

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
            _stream = new MemoryStream();
            SerializerStorage = storage;
            Writable = true;
            CreateReaderAndWriter();
            WriteMagicNumber();
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
            _writer.WriteInt32(MagicNumber);
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
                    writer.WriteByte(reader.ReadByte());
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
                    writer.WriteByte(data);
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
    }

}
