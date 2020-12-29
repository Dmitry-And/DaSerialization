using System.IO;

namespace DaSerialization
{
    public abstract class AContainerStorage : IContainerStorage
    {
        protected SerializerStorage _serializers;

        public AContainerStorage(SerializerStorage serializers)
        { _serializers = serializers ?? SerializerStorage.Default; }

        public abstract BinaryContainer CreateContainer(int size = 0);
        public abstract BinaryContainer LoadContainer(string name, bool writable, bool errorIfNotExist);
        public abstract bool SaveContainer(BinaryContainer container, string name);
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