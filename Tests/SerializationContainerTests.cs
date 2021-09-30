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
            var testContainer = storage.CreateContainer();

            var testObject2 = new TestObject();
            var testObject1 = new TestObject();
            var testStruct = TopLevelStructure.Default;

            // test object serialization
            testObject1.TestObj = testObject2;
            testObject1.TopLevelStruct.TestObj = testObject2;
            testObject1.TestObjects = new TestObject[] { testObject2, null, testObject2, testObject2, null };
            testObject1.TestStructs = new TopLevelStructure[] { testStruct, testStruct, testStruct };
            testContainer.Serialize(testObject1, 0);

            // top level structure serialization
            testStruct.TestObj = testObject2;
            testStruct.TestObjects = new TestObject[] { null, testObject2, null, testObject2, testObject2 };
            testStruct.TestStructs = new TopLevelStructure[] { testStruct, testStruct, testStruct };
            testContainer.Serialize(testStruct, 1);

            // top level container serialization
            var container = storage.CreateContainer();
            container.Serialize(testObject1, 0);
            container.Serialize(TopLevelStructure.Default, 1);
            testContainer.Serialize(container, 2);

            storage.SaveContainer(testContainer, FULL_CONTAINER_PATH);
            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(FULL_CONTAINER_PATH + ".bytes");
        }
    }
}
