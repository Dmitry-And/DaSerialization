using Tests;

namespace DaSerialization
{
    static class SerializationTestContainer
    {
        const string containerName = "/test_container";
        const string containerPath = "Tests";

        private static BinaryContainer LoadOrCreateContainer(string containerName)
        {
            var storage = new BinaryContainerStorageOnUnity();

            if (storage.LoadContainer(containerPath + containerName) != null) return storage.LoadContainer(containerPath + containerName);
            else
            {
                var container = storage.CreateBinaryContainer();
                storage.SaveContainer(container, containerPath + containerName);
                return container;
            }
        }

        [Test(-111)]
        private static bool ContainerExistance()
        {
            var testContainer = LoadOrCreateContainer(containerName);
            if (testContainer == null) throw new FailedTest("Container does not exist.");
            return true;
        }
    }
}
