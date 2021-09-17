using DaSerialization;

namespace Tests
{
    static class SerializationContainerTests
    {
        private const string CONTAINER_PATH = "Tests";
        private const string CONTAINER_NAME = "/test_container";

        private static BinaryContainer LoadOrCreateContainer(string containerName)
        {
            var storage = new BinaryContainerStorageOnUnity();

            if (storage.LoadContainer(CONTAINER_PATH + containerName) != null) 
                return storage.LoadContainer(CONTAINER_PATH + containerName);
            else
            {
                var container = storage.CreateBinaryContainer();
                storage.SaveContainer(container, CONTAINER_PATH + containerName);
                return container;
            }
        }

        [Test(-111)]
        private static bool ContainerExistance()
        {
            var testContainer = LoadOrCreateContainer(CONTAINER_NAME);
            if (testContainer == null) 
                throw new FailedTest("Container does not exist.");
            return true;
        }

        [Test(-112)]
        private static bool TopLevelObjectSerialization()
        {
            var storage = new BinaryContainerStorageOnUnity();
            var testContainer = LoadOrCreateContainer(CONTAINER_NAME);
            TestClass testClass = new TestClass(testContainer);

            testContainer.Serialize(testClass, 0);
            storage.SaveContainer(testContainer, CONTAINER_PATH + CONTAINER_NAME);
            return true;
        }
    }
}
