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

            // top level container serialization
            var testContainer = storage.CreateContainer();
            int data1 = 1576521201;
            byte data2 = 250;
            string data3 = "4257-=fefj2";
            testContainer.Serialize(data1, 0);
            testContainer.Serialize(data2, 1);
            testContainer.Serialize(data3, 2);
            container.Serialize(testContainer, 2);

            storage.SaveContainer(container, FULL_CONTAINER_PATH);
            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(FULL_CONTAINER_PATH + ".bytes");
        }
    }
}
