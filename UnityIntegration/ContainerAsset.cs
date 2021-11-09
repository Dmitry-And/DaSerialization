using UnityEngine;
using DaSerialization;

public class ContainerAsset : Object
{
    public byte[] bytes;

    public bool TryGetContainer(out BinaryContainer container)
    {
        container = UnityStorage.Instance.GetContainerFromData(bytes, false);

        if (container != null)
            return true;
        else
        {
            Debug.LogError("error");
            return false;
        }
    }
}
