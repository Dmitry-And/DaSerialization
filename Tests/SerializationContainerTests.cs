using DaSerialization;

namespace Tests
{
    static class SerializationContainerTests
    {
        private const string CONTAINER_PATH = "Assets/Tests/";
        private const string CONTAINER_NAME = "TestContainer";
        private const string FULL_CONTAINER_PATH = CONTAINER_PATH + CONTAINER_NAME;

        private static BinaryContainer LoadOrCreateContainer()
        {
            var storage = new BinaryContainerStorageOnFiles(null, "");
            var loadedContainer = storage.LoadContainer(FULL_CONTAINER_PATH, true);

            if (loadedContainer != null) 
                return loadedContainer;
            else
            {
                var container = storage.CreateContainer();
                storage.SaveContainer(container, FULL_CONTAINER_PATH);
                return container;
            }
        }

        [Test(-111)]
        private static bool ContainerExistanceTest()
        {
            var testContainer = LoadOrCreateContainer();
            if (testContainer == null) 
                throw new FailedTest("Container does not exist.");
            return true;
        }

  //      [Test(-112)]
        private static bool TopLevelObjectSerialization()
        {
            var storage = new BinaryContainerStorageOnUnity();
            var testContainer = LoadOrCreateContainer();
            TestClass testClass = new TestClass(testContainer);

            testContainer.Serialize(testClass, 0);
            storage.SaveContainer(testContainer, FULL_CONTAINER_PATH);
            return true;
        }
    }
}
