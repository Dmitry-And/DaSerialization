namespace DaSerialization
{
    public abstract class AFullSerializer<T> : ADeserializer<T>, ISerializer<T>
    {
        public abstract void WriteObject(T obj, BinaryStream stream);
        public void WriteObjectTypeless(object obj, BinaryStream stream)
        {
            T typedObj;
            if (obj is T typed)
                typedObj = typed;
            else
                typedObj = default;
            WriteObject(typedObj, stream);
        }
    }

    public abstract class ADeserializer<T> : IDeserializer<T>
    {
        public abstract int Version { get; }
        public abstract void ReadDataToObject(ref T obj, BinaryStreamReader reader);
        public void ReadDataToTypelessObject(ref object obj, BinaryStreamReader reader)
        {
            T typedObj;
            if (obj is T typed)
                typedObj = typed;
            else
                typedObj = default;
            ReadDataToObject(ref typedObj, reader);
            obj = typedObj;
        }
    }
}
