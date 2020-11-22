using System;
using System.IO;
using System.Text;

namespace DaSerialization
{
    public class BinaryStream : IStream<BinaryStream>
    {
        // first 4 bytes written to the stream to identify it as a valid BinaryStream
        public const int MagicNumber = 0x35_2A_31_BB; // 891957691
        public const int MetaDataSize = sizeof(int);
        private readonly Encoding DefaultStringEncoding = Encoding.UTF8;

        private MemoryStream _stream;
        private BinaryReader _reader;
        private BinaryWriter _writer;
        private bool _locked = true;

        public long Position
        {
            get { return _stream == null | _locked ? -1 : _stream.Position; }
            private set
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
        public bool Writable { get; private set; }

        public BinaryStream()
        {
            Writable = true;
        }
        public BinaryStream(MemoryStream stream, bool writable = false)
        {
            _stream = stream;
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
            _stream = new MemoryStream((int)length + System.Runtime.InteropServices.Marshal.SizeOf(MagicNumber));
            CreateReaderAndWriter();
            WriteMagicNumber();
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
            if (_stream == null)
                throw new InvalidOperationException($"Trying to {nameof(CopyTo)} from empty {this.PrettyTypeName()}");
            if (_locked)
                throw new InvalidOperationException($"Trying to {nameof(CopyTo)} from {this.PrettyTypeName()} w/o setting position");
            if (destination == null)
                throw new ArgumentException("Other stream is null");
            if (!destination.Writable)
                throw new InvalidOperationException($"Trying to {nameof(CopyTo)} to {destination.PrettyTypeName()} which is not writable");
            if (_locked)
                throw new InvalidOperationException($"Trying to {nameof(CopyTo)} to {destination.PrettyTypeName()} w/o setting position");
            if (Position + length > Length)
                throw new IndexOutOfRangeException($"Trying to {nameof(CopyTo)} from {this.PrettyTypeName()} more bytes than it has: position {Position}, length {Length}, copying {length}");

            var writer = destination._writer;
            for (long i = 0; i < length; i++)
                writer.Write(_reader.ReadByte());
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
                    return (int)((long)_reader.ReadUIntPacked_2() - 1);
                case Metadata.TypeID:
                    return _reader.ReadInt32();
                case Metadata.ObjectID:
                    return (int)_reader.ReadUIntPacked_2();
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
                    _writer.WriteUIntPacked_2((value + 1).ToUInt64());
                    return;
                case Metadata.TypeID:
                    _writer.Write(value);
                    return;
                case Metadata.ObjectID:
                    _writer.WriteUIntPacked_2((ulong)value);
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

        public Stream GetUnderlyingStream() => _stream;
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
    }

}
