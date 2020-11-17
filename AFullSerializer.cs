namespace DaSerialization
{
    public abstract class AFullSerializer<T, TStream>
        : ADeserializer<T, TStream>, ISerializer<T, TStream>
        where TStream : class, IStream<TStream>, new()
    {
        public abstract void WriteObject(T obj, TStream stream, AContainer<TStream> container);
        public void WriteObjectTypeless(object obj, TStream stream, AContainer<TStream> container)
        {
            T typedObj;
            if (obj is T typed)
                typedObj = typed;
            else
                typedObj = default;
            WriteObject(typedObj, stream, container);
        }
    }

    public abstract class ADeserializer<T, TStream> : IDeserializer<T, TStream>
        where TStream : class, IStream<TStream>, new()
    {
        public abstract int Version { get; }
        public abstract void ReadDataToObject(ref T obj, TStream stream, AContainer<TStream> container);
        public void ReadDataToTypelessObject(ref object obj, TStream stream, AContainer<TStream> container)
        {
            T typedObj;
            if (obj is T typed)
                typedObj = typed;
            else
                typedObj = default;
            ReadDataToObject(ref typedObj, stream, container);
            obj = typedObj;
        }
    }
}
