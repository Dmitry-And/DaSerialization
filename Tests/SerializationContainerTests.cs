using Tests;
using UnityEditor;

namespace DaSerialization.Tests
{
    static class SerializationContainerTests
    {
        private const string CONTAINER_PATH = "Assets/Tests/";
        private const string CONTAINER_NAME = "TestContainer";
        private const string FULL_CONTAINER_PATH = CONTAINER_PATH + CONTAINER_NAME;

        [MenuItem("Tools/Tests/Create Test Container", priority = int.MaxValue)]
        private static BinaryContainer CreateContainer()
        {
            var storage = new BinaryContainerStorageOnFiles(null, "");
            var container = storage.CreateContainer();
            storage.SaveContainer(container, FULL_CONTAINER_PATH);
            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(FULL_CONTAINER_PATH + ".bytes");
            return container;
        }

        [Test(-111)]
        private static bool TopLevelObjectSerializationTest()
        {
            var storage = new BinaryContainerStorageOnFiles(null, "");
            var testContainer = storage.LoadContainer(FULL_CONTAINER_PATH, true);
            TopLevelObject testObj = new TopLevelObject();

            if (!testContainer.Serialize(testObj, 0))
                throw new FailedTest($"Failed to serialized {testObj.PrettyTypeName()}");
            storage.SaveContainer(testContainer, FULL_CONTAINER_PATH);
            return true;
        }

        [Test(-112)]
        private static bool TopLevelStructureSerializationTest()
        {
            var storage = new BinaryContainerStorageOnFiles(null, "");
            var testContainer = storage.LoadContainer(FULL_CONTAINER_PATH, true);
            TopLevelStructure testStruct = new TopLevelStructure(-1985351954);

            if (!testContainer.Serialize(testStruct, 1))
                throw new FailedTest($"Failed to serialized {testStruct.PrettyTypeName()}");
            storage.SaveContainer(testContainer, FULL_CONTAINER_PATH);
            return true;
        }
    }
}
