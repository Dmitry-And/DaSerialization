namespace DaSerialization
{
    public interface IDeserializer
    {
        int Version { get; }
        // TODO: through interfase it's slow
        void ReadDataToTypelessObject(ref object obj, BinaryStreamReader reader);
    }
    public interface IDeserializer<T> : IDeserializer
    {
        // TODO: through interfase it's slow
        void ReadDataToObject(ref T obj, BinaryStreamReader reader);
    }

    public interface ISerializer
    {
        int Version { get; }
        // TODO: through interfase it's slow
        void WriteObjectTypeless(object obj, BinaryStreamWriter writer);
    }
    public interface ISerializer<T> : ISerializer
    {
        // TODO: through interfase it's slow
        void WriteObject(T obj, BinaryStreamWriter writer);
    }

    public interface IContainerStorage
    {
        BinaryContainer CreateContainer(int size = 0);
        // TODO: async container loading
        BinaryContainer LoadContainer(string name, bool writable = false, bool errorIfNotExist = true);
        bool SaveContainer(BinaryContainer container, string name);
        bool DeleteContainer(string name);
    }

    public interface ISerializerWritesContainer
    {
        bool UpdateSerializersInInnerContainers(ref object obj);
    }
}
