#if UNITY_2018_1_OR_NEWER

using System;
using DaSerialization;
using UnityEngine;
using UnityEngine.Serialization;
using System.Runtime.CompilerServices;
using DaSerialization.Internal;

#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public struct ContainerRefWithId
{
    public static string TextAssetFieldName = nameof(_textAsset);

    [FormerlySerializedAs("Container")]
    [SerializeField] private TextAsset _textAsset;
    public TextAsset TextAsset
    {
        get => _textAsset;
        set
        {
            if (_textAsset == value)
                return;
            _textAsset = value;
            _initialized = false;
            _container = null;
        }
    }
    public int Id;

    private bool _initialized;
    private IContainer _container;
    public IContainer Container { get { Init(); return _container; } }
    public bool IsValid => Init();

    public bool Init()
        => ContainerAssetUtils.Init(ref _initialized, _textAsset, ref _container);

#if UNITY_EDITOR
    public bool UpdateSerializers()
    {
        if (!IsValid)
            return false;

        var container = Container;
        if (container.UpdateSerializers())
        {
            WriteToTextAsset();
            return true;
        }
        return false;
    }

    public void Save<T>(T obj, bool andWriteToAsset)
    { Init(); ContainerAssetUtils.Save(_textAsset, ref _container, obj, Id, andWriteToAsset); }

    public void Remove<T>(bool andWriteToAsset)
        => Remove(typeof(T), andWriteToAsset);
    public void Remove(Type typeToDelete, bool andWriteToAsset)
        => Remove(Id, typeToDelete, andWriteToAsset);
    public void Remove(int idToDelete, Type typeToDelete, bool andWriteToAsset)
        => ContainerAssetUtils.Remove(_textAsset, Container, idToDelete, typeToDelete, andWriteToAsset);

    public void WriteToTextAsset()
        => ContainerAssetUtils.WriteToTextAsset(Container, _textAsset);
#endif

    public T Load<T>(bool objectExpected = true)
    {
        T result = default;
        Load(ref result, objectExpected);
        return result;
    }

    public bool Load<T>(ref T obj, bool objectExpected = true)
        => ContainerAssetUtils.Load(_textAsset, Container, ref obj, Id, objectExpected);
}

[Serializable]
public struct ContainerRef
{
    public static string TextAssetFieldName = nameof(_textAsset);

    [FormerlySerializedAs("Container")]
    [SerializeField] private TextAsset _textAsset;
    public TextAsset TextAsset
    {
        get => _textAsset;
        set
        {
            if (_textAsset == value)
                return;
            _textAsset = value;
            _initialized = false;
            _container = null;
        }
    }

    private bool _initialized;
    private IContainer _container;
    public IContainer Container { get { Init(); return _container; } }
    public bool IsValid => Init();

    public static ContainerRef FromTextAsset(TextAsset textAsset)
        => new ContainerRef() { _textAsset = textAsset };
    //public static ContainerRef FromResourcesAsset(string path)
    //public static ContainerRef FromPersistentAsset(string path)

    public bool Init()
        => ContainerAssetUtils.Init(ref _initialized, _textAsset, ref _container);

#if UNITY_EDITOR
    public bool UpdateSerializers()
    {
        if (!IsValid)
            return false;

        var container = Container;
        if (container.UpdateSerializers())
        {
            WriteToTextAsset();
            return true;
        }
        return false;
    }

    public void Save<T>(T obj, int id, bool andWriteToAsset)
    { Init(); ContainerAssetUtils.Save(_textAsset, ref _container, obj, id, andWriteToAsset); }

    public void Remove<T>(int idToDelete, bool andWriteToAsset)
        => Remove(idToDelete, typeof(T), andWriteToAsset);
    public void Remove(int idToDelete, Type typeToDelete, bool andWriteToAsset)
        => ContainerAssetUtils.Remove(_textAsset, Container, idToDelete, typeToDelete, andWriteToAsset);

    public void WriteToTextAsset()
        => ContainerAssetUtils.WriteToTextAsset(Container, _textAsset);
#endif

    public T Load<T>(int id, bool objectExpected = true)
    {
        T result = default;
        Load(ref result, id, objectExpected);
        return result;
    }

    public bool Load<T>(ref T obj, int id, bool objectExpected = true)
        => ContainerAssetUtils.Load(_textAsset, Container, ref obj, id, objectExpected);
}

namespace DaSerialization.Internal
{
    public static class ContainerAssetUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Init(ref bool initialized, TextAsset textAsset, ref IContainer container)
        {
            if (!initialized)
            {
                initialized = true;
                if (textAsset == null)
                    return false;
                var data = textAsset.bytes;
                container = UnityStorage.Instance.GetContainerFromData(data, !Application.isPlaying);
                if (container == null)
                    Debug.LogError($"Asset {textAsset.name} contains data which is not a valid {nameof(BinaryContainer)}", textAsset);
            }
            return container != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Load<T>(TextAsset textAsset, IContainer container, ref T obj, int id, bool objectExpected)
        {
            BinaryContainer.IsValidObjectId(id, true);
            if (container == null)
            {
                if (objectExpected)
                {
                    if (textAsset == null)
                        Debug.LogError("Trying to load from Null container");
                    else
                        Debug.LogError("Trying to load from invalid container " + textAsset.name, textAsset);
                }
                return false;
            }

            bool found = container.Deserialize(ref obj, id);
            if (!found & objectExpected)
                Debug.LogError($"No object of type {typeof(T).PrettyName()} with id {id} found in container {textAsset.name}\n", textAsset);
            return found;
        }

#if UNITY_EDITOR
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Save<T>(TextAsset textAsset, ref IContainer container, T obj, int id, bool andWriteToAsset)
        {
            BinaryContainer.IsValidObjectId(id, true);
            if (textAsset == null)
                throw new NullReferenceException("Trying to save to Null container");

            container = container ?? UnityStorage.Instance.CreateContainer();
            container.Serialize(obj, id);
            if (andWriteToAsset)
                WriteToTextAsset(container, textAsset);
        }

        public static void Remove(TextAsset textAsset, IContainer container, int idToDelete, Type typeToDelete, bool andWriteToAsset)
        {
            BinaryContainer.IsValidObjectId(idToDelete, true);
            if (textAsset == null)
                throw new NullReferenceException("Trying to remove from Null container");
            if (container == null) // the same as !IsValid
                throw new NullReferenceException("Trying to remove from invalid container");

            container.Remove(idToDelete, typeToDelete);
            if (andWriteToAsset)
                WriteToTextAsset(container, textAsset);
        }

        public static void WriteToTextAsset(IContainer container, TextAsset textAsset)
        {
            if (container == null) // same as !IsValid
            {
                Debug.LogError("Trying to write invalid container");
                return;
            }
            var path = AssetDatabase.GetAssetPath(textAsset);
            UnityStorage.Instance.SaveContainerAtPath(container, path);
            AssetDatabase.Refresh();
        }
#endif
    }
}

#endif