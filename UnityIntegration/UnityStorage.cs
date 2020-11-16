#if UNITY_2018_1_OR_NEWER

using DaSerialization;

public static class UnityStorage
{
    public static BinaryContainerStorageOnUnity Instance = new BinaryContainerStorageOnUnity();
}

#endif