using System.Collections.Generic;
using DaSerialization;

public static class SerializationUtils
{
    private static BinaryContainerStorageOnMemory _copyStorage = new BinaryContainerStorageOnMemory();
    private static Stack<BinaryContainer> _copyContainers = new Stack<BinaryContainer>();

    public static T DeepCopy<T>(this T obj)
    {
        T result = default;
        DeepCopyTo(obj, ref result);
        return result;
    }

    public static void DeepCopyTo<T>(this T from, ref T to)
    {
        var copyContainer = PopContainer();
        copyContainer.Serialize(from, 1);
        copyContainer.Deserialize(ref to, 1);
        copyContainer.Clear();
        _copyContainers.Push(copyContainer);
    }

    public static int GetSerializedHash<T>(this T obj)
    {
        var copyContainer = PopContainer();
        copyContainer.Clear();
        copyContainer.Serialize(obj, 1);
        int hash = copyContainer.GetContentHash(false);
        copyContainer.Clear();
        _copyContainers.Push(copyContainer);

        return hash;
    }

    private static BinaryContainer PopContainer()
    {
        if (_copyContainers.Count > 0)
            return _copyContainers.Pop();
        return _copyStorage.CreateContainer() as BinaryContainer;
    }
}