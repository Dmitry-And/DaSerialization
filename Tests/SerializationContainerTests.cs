using Tests;
using UnityEditor;

namespace DaSerialization.Tests
{
    static class SerializationContainerTests
    {
        private const string CONTAINER_PATH = "Assets/Tests/";
        private const string CONTAINER_NAME = "TestContainer";
        private const string FULL_CONTAINER_PATH = CONTAINER_PATH + CONTAINER_NAME;

        [MenuItem("Tools/Tests/Load or Create Test Container", priority = int.MaxValue)]
        private static BinaryContainer LoadOrCreateContainer()
        {
            var storage = new BinaryContainerStorageOnFiles(null, "");
            var loadedContainer = storage.LoadContainer(FULL_CONTAINER_PATH, true);

            if (loadedContainer != null)
            {
                Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(FULL_CONTAINER_PATH + ".bytes");
                return loadedContainer;
            }
            else
            {
                var container = storage.CreateContainer();
                storage.SaveContainer(container, FULL_CONTAINER_PATH);
                Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(FULL_CONTAINER_PATH + ".bytes");
                return container;
            }
        }

        [Test(-111)]
        private static bool TopLevelObjectSerialization()
        {
            var storage = new BinaryContainerStorageOnFiles(null, "");
            var testContainer = storage.LoadContainer(FULL_CONTAINER_PATH, true);
            TopLevelObject testObj = new TopLevelObject();

            if (!testContainer.Serialize(testObj, 0))
                throw new FailedTest($"Failed to serialized {testObj.PrettyTypeName()}");
            storage.SaveContainer(testContainer, FULL_CONTAINER_PATH);
            return true;
        }

    }
}
