using System.Collections.Generic;

namespace DaSerialization.Tests
{
    public static partial class TestContainerCreator
    {
        public static void CreateContainer(string containerPath)
        {
            var storage = new BinaryContainerStorageOnFiles(null, "");
            storage.StorageFileExtension = ".container";
            var container = storage.CreateContainer();

            var testObject2 = new TestObject();
            var testObject1 = new TestObject();
            var testStruct = TopLevelStructure.Default;

            // test object serialization
            testObject1.TestObj = testObject2;
            testObject1.TopLevelStruct.TestObj = testObject2;
            testObject1.TestObjectsArray = new TestObject[] { testObject2, null, testObject2, testObject2, null };
            testObject1.TopLevelStructsArray = new TopLevelStructure[] { testStruct, testStruct, testStruct };
            testObject1.TestObjectsList = new List<TestObject>() { null, null, testObject2, testObject2, null };
            testObject1.TopLevelStructsList = new List<TopLevelStructure>() { testStruct, testStruct, testStruct };
            testObject1.TestInterface = testObject2;
            testObject1.TestInterfacesArray = new ITestInterface[] { null, testObject2, BottomLevelStructure.Default, null };
            testObject1.TestInterfacesList = new List<ITestInterface>() { testObject2, null, BottomLevelStructure.Default };

            // top level structure serialization
            testStruct.TestObj = testObject2;
            testStruct.TestObjectsArray = new TestObject[] { null, testObject2, null, testObject2, testObject2 };
            testStruct.TopLevelStructsArray = new TopLevelStructure[] { testStruct, testStruct, testStruct };
            testStruct.TestObjectsList = new List<TestObject>() { testObject2, null, testObject2, null };
            testStruct.TopLevelStructsList = new List<TopLevelStructure>() { testStruct, testStruct, testStruct };
            testStruct.TestInterface = testObject2;
            testStruct.TestInterfacesArray = new ITestInterface[] { null, null, BottomLevelStructure.Default, testObject2 };
            testStruct.TestInterfacesList = new List<ITestInterface>() { BottomLevelStructure.Default, null, testObject2, null };

            var topContainer = storage.CreateContainer();
            var innerContainer = storage.CreateContainer();
            var supportContainer = storage.CreateContainer();

            topContainer.Serialize(testObject1, 0);
            topContainer.Serialize(TopLevelStructure.Default, 1);
            innerContainer.Serialize(testObject1, 0);
            innerContainer.Serialize(TopLevelStructure.Default, 1);
            topContainer.Serialize(innerContainer, 2);
            supportContainer.Serialize(topContainer, 0);

            testObject1.TestContainer = topContainer;
            testStruct.TestContainer = topContainer;
            container.Serialize(testObject1, 0);
            container.Serialize(testStruct, 1);
            container.Serialize(supportContainer, 2);
            storage.SaveContainer(container, containerPath);
        }
    }
}
