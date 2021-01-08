namespace DaSerialization
{
    public abstract class AFullSerializer<T> : ADeserializer<T>, ISerializer<T>
    {
        public abstract void WriteObject(T obj, BinaryStreamWriter writer);
        public void WriteObjectTypeless(object obj, BinaryStreamWriter writer)
        {
            T typedObj;
            if (obj is T typed)
                typedObj = typed;
            else
                typedObj = default;
            WriteObject(typedObj, writer);
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
