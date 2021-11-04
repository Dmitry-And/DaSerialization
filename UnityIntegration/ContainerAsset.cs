using UnityEngine;
using DaSerialization;

public class ContainerAsset : Object
{
    public byte[] bytes;

    public bool TryGetContainer(byte[] data, out BinaryContainer container, bool verbose = false)
    {
        container = UnityStorage.Instance.GetContainerFromData(data, !Application.isPlaying);

        if (container != null)
            return true;
        else
        {
            if (verbose)
                Debug.LogError("error");
            return false;
        }
    }
}
