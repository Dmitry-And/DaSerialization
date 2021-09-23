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
        private static void CreateContainer()
        {
            var storage = new BinaryContainerStorageOnFiles(null, "");
            var container = storage.CreateContainer();

            // test object serialization
            TestObject testObj = new TestObject();
            container.Serialize(testObj, 0);

            // top level structure serialization
            TopLevelStructure testStruct = new TopLevelStructure(-1985351954);
            container.Serialize(testStruct, 1);

            storage.SaveContainer(container, FULL_CONTAINER_PATH);
            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(FULL_CONTAINER_PATH + ".bytes");
        }
    }
}
