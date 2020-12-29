﻿using System.Collections.Generic;
using System.IO;
using DaSerialization.Internal;

namespace DaSerialization
{
    public class BinaryContainerStorageOnMemory : AContainerStorage
    {
        private MemoryStream _storage = new MemoryStream();
        private struct PosLen { public long Position; public long Length; }
        private Dictionary<string, PosLen> _containerPositions = new Dictionary<string, PosLen>(20);

        public BinaryContainerStorageOnMemory(SerializerStorage serializers = null)
            : base(serializers) { }

        public BinaryContainer CreateBinaryContainer(int size = 0)
            => new BinaryContainer(size, _serializers);
        public override BinaryContainer CreateContainer(int size = 0)
            => CreateBinaryContainer(size);

        public override BinaryContainer LoadContainer(string name, bool writable, bool errorIfNotExist = true)
        {
            // all saved in memory containers are considered as writable
            BinaryContainer container = null;
            if (_containerPositions.TryGetValue(name, out PosLen containerPosLen))
            {
                _storage.Seek(containerPosLen.Position, SeekOrigin.Begin);
                var data = new byte[containerPosLen.Length];
                _storage.Read(data, 0, data.Length);
                var memStream = CreateMemoryStream(data, writable);
                var binStream = new BinaryStream(memStream, _serializers, writable);
                container = new BinaryContainer(binStream);
            }
            else if (errorIfNotExist)
                SerializationLogger.LogError($"Container '{name}' does not exist in {this.PrettyTypeName()}");
            return container;
        }

        public override bool SaveContainer(BinaryContainer container, string name)
        {
            _storage.Seek(0, SeekOrigin.End);
            PosLen containerPosLen = new PosLen()
            {
                Position = _storage.Position
            };

            container.CleanUp();
            var memStream = container.GetUnderlyingStream();
            memStream.Seek(0, SeekOrigin.Begin);
            memStream.CopyTo(_storage);
            containerPosLen.Length = _storage.Position - containerPosLen.Position;
            _containerPositions[name] = containerPosLen;
            return true;
        }

        public override bool DeleteContainer(string name)
        {
            return _containerPositions.Remove(name);
        }
    }
}
