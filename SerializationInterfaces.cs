using System;
using System.IO;

namespace DaSerialization
{
    public interface IStream<TStream> : IDisposable where TStream : IStream<TStream>
    {
        int ReadInt(Metadata meta);
        void WriteInt(Metadata meta, int value);
        long ZeroPosition { get; }
        long Position { get; }
        long Length { get; }
        long Capacity { get; }
        void Seek(long position);
        void CopyTo(TStream stream, long length); // CopyTo itself illegal
        void CopyTo(TStream stream, long position, long length); // CopyTo itself allowed
        bool Writable { get; }
        void Allocate(long length);
        void SetLength(long length);
        Stream GetUnderlyingStream();
        void Clear();
        int GetMetaDataSize();
    }

    public interface IContainer : IDisposable
    {
        bool Writable { get; }
        bool IsDirty { get; }
        void CleanUp(bool preserveCapacity = false);
        bool Has<T>(int objectId);
        T Deserialize<T>(int objectId);
        bool Deserialize<T>(ref T obj, int objectId);
        bool Deserialize(ref object obj, int objectId, int typeId);
        T Deserialize<T>();
        void Deserialize<T>(ref T obj);
        void DeserializeStatic<T>(ref T obj);
        void DeserializeStatic<T>(ref T obj, int typeId);
        bool Serialize<T>(T obj, int objectId);
        bool Serialize(object obj, int objectId, int typeId);
        bool Serialize<T>(T obj);
        bool SerializeStatic<T>(T obj);
        bool SerializeStatic<T>(T obj, int typeId, bool inherited);
        bool Remove<T>(int objectId);
        bool Remove(int objectId, Type type);
        int RemoveAllWithId(int objectId);
        Stream GetUnderlyingStream();
        void Clear();
        long Size { get; }
        int GetContentHash(bool hashTypes);
        bool UpdateSerializers(bool andCleanUp = true);
    }

    public interface IContainerStorage
    {
        IContainer CreateContainer(int size = 0);
        // TODO: async container loading
        IContainer LoadContainer(string name, bool writable = false, bool errorIfNotExist = true);
        bool SaveContainer(IContainer container, string name);
        bool DeleteContainer(string name);
    }

    public interface ISerializerWritesContainer
    {
        bool UpdateSerializersInInnerContainers(ref object obj);
    }
}
