using UnityEngine;
using DaSerialization;
using System.IO;
using UnityEditor;

public class ContainerAsset : Object
{
    public byte[] bytes;

    public ContainerAsset(string path)
    {
        bytes = File.ReadAllBytes(path);
    }

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
