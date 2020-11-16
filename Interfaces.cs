namespace DaSerialization
{
    public interface IDeserializer
    {
        int Version { get; }
    }
    public interface IDeserializer<TStream> : IDeserializer where TStream : class, IStream<TStream>, new()
    {
        void ReadDataToTypelessObject(ref object obj, TStream stream, AContainer<TStream> container);
    }
    public interface IDeserializer<T, TStream> : IDeserializer<TStream> where TStream : class, IStream<TStream>, new()
    {
        void ReadDataToObject(ref T obj, TStream stream, AContainer<TStream> container);
    }

    public interface ISerializer
    {
        int Version { get; }
    }
    public interface ISerializer<TStream> : ISerializer where TStream : class, IStream<TStream>, new()
    {
        void WriteObjectTypeless(object obj, TStream stream, AContainer<TStream> container);
    }
    public interface ISerializer<T, TStream> : ISerializer<TStream> where TStream : class, IStream<TStream>, new()
    {
        void WriteObject(T obj, TStream stream, AContainer<TStream> container);
    }

}