using System.IO;

namespace DaSerialization
{
    public class BinaryContainerStorageOnFiles : AContainerStorage<BinaryStream>
    {
        public string StoragePathPrefix;
        public string StorageFileExtension = ".bytes";

        public BinaryContainerStorageOnFiles(SerializerStorage<BinaryStream> serializers = null, string prefix = "../")
            : base(serializers)
        {
            StoragePathPrefix = prefix;
        }

        public override AContainer<BinaryStream> CreateContainer(int size = 0)
        {
            var memStream = new MemoryStream(size);
            var binStream = new BinaryStream(memStream, true);
            var container = new BinaryContainer(binStream, _serializers);
            return container;
        }

        public override AContainer<BinaryStream> LoadContainer(string name, bool writable = false, bool errorIfNotExist = true)
        {
            // all files are considered as writable
            string filePath = GetFilePath(name);
            if (!File.Exists(filePath))
            {
                if (errorIfNotExist)
                    C.LogError($"File '{filePath}' does not exist");
                return null;
            }
            var data = File.ReadAllBytes(filePath);
            var memStream = CreateMemoryStream(data, writable);
            var binStream = new BinaryStream(memStream, writable);
            var container = new BinaryContainer(binStream, _serializers);
            return container;
        }

        public override bool SaveContainer(AContainer<BinaryStream> container, string name)
        {
            string filePath = GetFilePath(name);
            bool fileExists = File.Exists(filePath);
            if (!fileExists)
                fileExists = CreateFile(filePath);
            if (!fileExists)
                return false;
            container.CleanUp();

            using (var file = File.Open(filePath, FileMode.Create))
            {
                var memStream = container.GetUnderlyingStream();
                memStream.Seek(0, SeekOrigin.Begin);
                memStream.CopyTo(file);
                file.Close();
            }

            return true;
        }

        public override bool DeleteContainer(string name)
        {
            string filePath = GetFilePath(name);
            if (!File.Exists(filePath))
                return false;
            File.Delete(filePath);
            return true;
        }

        private string GetFilePath(string containerName)
        {
            return StoragePathPrefix + containerName + StorageFileExtension;
        }

        public static bool CreateFile(string filePath)
        {
            var directory = Directory.GetParent(filePath).FullName;
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            if (!Directory.Exists(directory))
            {
                C.LogError($"Unable to create directory '{directory}'");
                return false;
            }
            File.WriteAllText(filePath, "");
            if (!File.Exists(filePath))
            {
                C.LogError($"Unable to create file '{filePath}'");
                return false;
            }
            return true;
        }

    }
}
