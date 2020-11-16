using System.IO;

namespace DaSerialization
{
    public abstract class AContainerStorage<TStream>
        : IContainerStorage where TStream : class, IStream<TStream>, new()
    {
        protected SerializerStorage<TStream> _serializers;

        public AContainerStorage(SerializerStorage<TStream> serializers)
        { _serializers = serializers ?? SerializerStorage<TStream>.Default; }

        IContainer IContainerStorage.CreateContainer(int size)
        { return CreateContainer(size); }

        IContainer IContainerStorage.LoadContainer(string name, bool writable, bool errorIfNotExist)
        { return LoadContainer(name, writable, errorIfNotExist); }

        bool IContainerStorage.SaveContainer(IContainer container, string name)
        {
            var typedContainer = container as AContainer<TStream>;
            if (typedContainer == null)
                throw new System.ArgumentException($"{nameof(SaveContainer)} called with argument {container.PrettyTypeName()} but {typeof(AContainer<TStream>).PrettyName()} expected", nameof(container));
            return SaveContainer(typedContainer, name);
        }

        public abstract AContainer<TStream> CreateContainer(int size = 0);
        public abstract AContainer<TStream> LoadContainer(string name, bool writable, bool errorIfNotExist);
        public abstract bool SaveContainer(AContainer<TStream> container, string name);
        public abstract bool DeleteContainer(string name);

        protected static MemoryStream CreateMemoryStream(byte[] data, bool writable)
        {
            if (!writable)
                return new MemoryStream(data, false);

            // though other MemoryStream constructors can use provided array
            // as a buffer BUT they limit the capacity of the stream to
            // the size of the provided array, so we need to create
            // a new MemoryStream with internal buffer and copy content of the array
            // to it if we want to write to the MemoryStream later
            var ms = new MemoryStream(data.Length + 1024);
            ms.Write(data, 0, data.Length);
            return ms;
        }
    }
}