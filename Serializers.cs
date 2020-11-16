using System;

[TypeId(100011287, typeof(object), false)] // to make possible DeepCopy for non-generic calls (also for value types with boxing)
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Struct
    | AttributeTargets.Interface // to mark collections of interfaces as serializable
    | AttributeTargets.Enum,     // to mark collections of enums as serializable
    Inherited = false,
    AllowMultiple = true)]       // it's possible the same class is mark with different type ids for the class itself and for a collection of this class
public class TypeIdAttribute : Attribute
{
    // negative Ids are reserved for special (project-specific) cases
    public int Id { get; private set; }
    public Type Type { get; private set; }
    private Type _attributeOnType;
    public Type AttributeOnType
    {
        get => _attributeOnType;
        set
        {
            Type = Type ?? value;
            _attributeOnType = value;
        }
    }
    public bool ShouldHaveSerializers { get; private set; }

    public TypeIdAttribute(int id, Type t = null, bool shouldHaveSerializers = true)
    {
        if (id == -1 | id == 0)
            throw new Exception($"{nameof(TypeId)} for type {t.PrettyName()} is {id}. This value is not allowed");
        Id = id;
        Type = t;
        ShouldHaveSerializers = shouldHaveSerializers;
    }
}

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

    public abstract class ADeserializer<T, TStream> : IDeserializer<T, TStream>
        where TStream : class, IStream<TStream>, new()
    {
        public abstract int Version { get; }
        public abstract void ReadDataToObject(ref T obj, TStream stream, AContainer<TStream> container);
        public void ReadDataToTypelessObject(ref object obj, TStream stream, AContainer<TStream> container)
        {
            var typedObj = (T)Convert.ChangeType(obj, typeof(T));
            ReadDataToObject(ref typedObj, stream, container);
            obj = typedObj;
        }
    }

    public abstract class AFullSerializer<T, TStream>
        : ISerializer<T, TStream>, IDeserializer<T, TStream>
        where TStream : class, IStream<TStream>, new()
    {
        public abstract int Version { get; }
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

    public abstract class AEmptyClassSerializer<T, TStream>
        : ISerializer<T, TStream>, IDeserializer<T, TStream>
        where TStream : class, IStream<TStream>, new()
        where T : class, new()
    {
        public abstract int Version { get; }
        public void WriteObject(T obj, TStream stream, AContainer<TStream> container) { }
        public void WriteObjectTypeless(object obj, TStream stream, AContainer<TStream> container) { }
        public void ReadDataToObject(ref T obj, TStream stream, AContainer<TStream> container)
        { if (obj == null) obj = new T(); }
        public void ReadDataToTypelessObject(ref object obj, TStream stream, AContainer<TStream> container)
        { if (obj == null) obj = new T(); }
    }
    public abstract class AEmptyClassDeserializer<T, TStream>
        : IDeserializer<T, TStream>
        where TStream : class, IStream<TStream>, new()
        where T : class, new()
    {
        public abstract int Version { get; }
        public void ReadDataToObject(ref T obj, TStream stream, AContainer<TStream> container)
        { if (obj == null) obj = new T(); }
        public void ReadDataToTypelessObject(ref object obj, TStream stream, AContainer<TStream> container)
        { if (obj == null) obj = new T(); }
    }

    public static class ISerializerHelper
    {
        public static Exception OldVersion<T, TStream>(this ISerializer<T, TStream> serializer)
            where TStream : class, IStream<TStream>, new()
        {
            return new Exception($"Trying to serialize {typeof(T).PrettyName()} with old version ({serializer.Version}) of {serializer.PrettyTypeName()}");
        }
    }
}