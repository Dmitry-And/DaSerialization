#if UNITY_2018_1_OR_NEWER
using System.IO;
using UnityEngine;

namespace DaSerialization
{
    public class BinaryContainerStorageOnUnity : AContainerStorage<BinaryStream>
    {
        public static string ResourcesPathPrefix = "Storage/";
        public static string PersistentEditorPathPrefix = "Assets/EditorStorage/";
        private static string _persistentRuntimePathPrefix;
        public static string PersistentRuntimePathPrefix =>
            string.IsNullOrEmpty(_persistentRuntimePathPrefix)
            ? _persistentRuntimePathPrefix = Application.persistentDataPath + "/Storage/"
            : _persistentRuntimePathPrefix;
        public static string PersistentFileExtension = ".bytes";

        public BinaryContainerStorageOnUnity(SerializerStorage<BinaryStream> serializers = null)
            : base(serializers) { }

        public BinaryContainer CreateBinaryContainer(int size = 0)
            => new BinaryContainer(size, _serializers);
        public override AContainer<BinaryStream> CreateContainer(int size = 0)
            => CreateBinaryContainer(size);

        public override AContainer<BinaryStream> LoadContainer(string name, bool writable = false, bool errorIfNotExist = true)
        {
            var assetName = GetResourcesAssetPath(name);
            var textAsset = Resources.Load<TextAsset>(assetName);
            byte[] data = textAsset == null ? null : textAsset.bytes;
            if (data != null)
            {
                if (writable)
                    Debug.LogError($"Trying to load writable container from {nameof(TextAsset)}. Illegal in runtime");
                writable = false;
            }
            else
            {
                // try load from persistent datapath
                var filePath = GetPersistentFilePath(name);
                if (!File.Exists(filePath))
                {
                    if (errorIfNotExist)
                        Debug.LogWarning($"File '{filePath}' does not exist");
                    return null;
                }
                data = File.ReadAllBytes(filePath);
            }
            return GetContainerFromData(data, writable);
        }

        public BinaryContainer GetContainerFromData(byte[] data, bool writable)
        {
            if (!BinaryStream.IsValidData(data))
                return null;
            var memStream = CreateMemoryStream(data, writable);
            var binStream = new BinaryStream(memStream, writable);
            if (!binStream.CheckIsValidStream()) // just in case but can be removed
                return null;
            var container = new BinaryContainer(binStream, _serializers);
            return container;
        }

        public override bool SaveContainer(AContainer<BinaryStream> container, string name)
        {
            string filePath = GetPersistentFilePath(name);
            return SaveContainerAtPath(container, filePath);
        }

        public bool SaveContainerAtPath(IContainer container, string filePath)
        {
            bool fileExists = File.Exists(filePath);
            if (!fileExists)
                fileExists = BinaryContainerStorageOnFiles.CreateFile(filePath);
            if (!fileExists)
                return false;
            if (!filePath.EndsWith(".bytes", System.StringComparison.OrdinalIgnoreCase))
                Debug.LogWarning($"Saving {container.PrettyTypeName()} to '{filePath}' with non-'.bytes' extension may lead to data change after Unity asset import");
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
            var assetName = GetResourcesAssetPath(name);
            var textAsset = Resources.Load<TextAsset>(assetName);
            if (textAsset != null)
            {
                Debug.LogError($"Unable to delete Resources asset '{assetName}'");
                return false;
            }
            var filePath = GetPersistentFilePath(name);
            if (!File.Exists(filePath))
                return false;
            File.Delete(filePath);
            if (Application.isEditor)
            {
                var metaPath = filePath + ".meta";
                if (File.Exists(metaPath))
                    File.Delete(metaPath);
            }
            return true;
        }

        public string GetResourcesAssetPath(string containerName)
        {
            return ResourcesPathPrefix + containerName;
        }

        public string GetPersistentFilePath(string containerName)
        {
            return (Application.isEditor ? PersistentEditorPathPrefix : PersistentRuntimePathPrefix)
                + containerName + PersistentFileExtension;
        }
        public string GetPersistentDirectoryPath(string dirName)
        {
            return (Application.isEditor ? PersistentEditorPathPrefix : PersistentRuntimePathPrefix)
                + dirName;
        }

    }
}
#endif