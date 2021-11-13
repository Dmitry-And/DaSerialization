using UnityEngine;
using DaSerialization;

public class ContainerAsset : ScriptableObject
{
    public byte[] bytes;
    private BinaryContainer _container;

    public bool TryGetContainer(out BinaryContainer container)
    {
        if (_container == null)
            _container = UnityStorage.Instance.GetContainerFromData(bytes, false);

        container = _container;

        if (container != null)
            return true;
        else
        {
            Debug.LogError("error");
            return false;
        }
    }
}
