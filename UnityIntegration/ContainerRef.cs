#if UNITY_2018_1_OR_NEWER

using System;
using System.IO;
using DaSerialization;
using UnityEngine;
using UnityEngine.Serialization;
using System.Runtime.CompilerServices;
using DaSerialization.Internal;

#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public struct ContainerRefWithId : IEquatable<ContainerRefWithId>
{
    public static string TextAssetFieldName = nameof(_textAsset);

    [FormerlySerializedAs("Container")]
    [SerializeField] private TextAsset _textAsset;
    [SerializeField] private DefaultAsset _defaultAsset;

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
            MuteInvalidAssetErrors = false; // conservative strategy
        }
    }

    public DefaultAsset DefaultAsset
    {
        get => _defaultAsset;
        set
        {
            if (_defaultAsset == value)
                return;
            _defaultAsset = value;
            _initialized = false;
            _container = null;
            MuteInvalidAssetErrors = false; // conservative strategy
        }
    }

    public int Id;
    public bool MuteInvalidAssetErrors; // set true to avoid errors 'asset is not a containner'

    private bool _initialized;
    private BinaryContainer _container;
    //public BinaryContainer Container { get { Init(); return _container; } }

    public BinaryContainer Container { get { InitDefaultAsset(); return _container; } }
    //public bool IsValid => Init();

    public bool IsValid => InitDefaultAsset();

    public bool Equals(ContainerRefWithId other) => other.Id == Id && other._textAsset == _textAsset;

    public bool Init()
        => ContainerAssetUtils.Init(ref _initialized, _textAsset, ref _container, !MuteInvalidAssetErrors);

    public bool InitDefaultAsset()
        => ContainerAssetUtils.InitDefaultAsset(ref _initialized, _defaultAsset, ref _container, !MuteInvalidAssetErrors);

#if UNITY_EDITOR
    public bool UpdateSerializers()
    {
        if (!IsValid)
            return false;

        var container = Container;
        if (container.UpdateSerializers())
        {
            //WriteToTextAsset();
            WriteToDefaultAsset();
            return true;
        }
        return false;
    }

    public void Save<T>(T obj, bool andWriteToAsset)
    { Init(); ContainerAssetUtils.Save(_textAsset, ref _container, obj, Id, andWriteToAsset); }

    public void SaveDefaultAsset<T>(T obj, bool andWriteToAsset)
    { InitDefaultAsset(); ContainerAssetUtils.SaveDefaultAsset(_defaultAsset, ref _container, obj, Id, andWriteToAsset); }

    public void Remove<T>(bool andWriteToAsset)
        => Remove(typeof(T), andWriteToAsset);
    public void Remove(Type typeToDelete, bool andWriteToAsset)
        => Remove(Id, typeToDelete, andWriteToAsset);
    public void Remove(int idToDelete, Type typeToDelete, bool andWriteToAsset)
        => ContainerAssetUtils.Remove(_textAsset, Container, idToDelete, typeToDelete, andWriteToAsset);

    public void RemoveDefaultAsset<T>(bool andWriteToAsset)
        => RemoveDefaultAsset(typeof(T), andWriteToAsset);
    public void RemoveDefaultAsset(Type typeToDelete, bool andWriteToAsset)
    => RemoveDefaultAsset(Id, typeToDelete, andWriteToAsset);
    public void RemoveDefaultAsset(int idToDelete, Type typeToDelete, bool andWriteToAsset)
    => ContainerAssetUtils.RemoveDefaultAsset(_defaultAsset, Container, idToDelete, typeToDelete, andWriteToAsset);

    public void WriteToTextAsset()
        => ContainerAssetUtils.WriteToTextAsset(Container, _textAsset);

    public void WriteToDefaultAsset()
        => ContainerAssetUtils.WriteToDefaultAsset(Container, _defaultAsset);
#endif

    public T Load<T>(bool objectExpected = true)
    {
        T result = default;
        //Load(ref result, objectExpected);
        LoadDefaultAsset(ref result, objectExpected);
        return result;
    }

    public bool Load<T>(ref T obj, bool objectExpected = true)
        => ContainerAssetUtils.Load(_textAsset, Container, ref obj, Id, objectExpected);

    public bool LoadDefaultAsset<T>(ref T obj, bool objectExpected = true)
        => ContainerAssetUtils.LoadDefaultAsset(_defaultAsset, Container, ref obj, Id, objectExpected);
}

[Serializable]
public struct ContainerRef : IEquatable<ContainerRef>
{
    public static string TextAssetFieldName = nameof(_textAsset);

    [FormerlySerializedAs("Container")]
    [SerializeField] private TextAsset _textAsset;
    [SerializeField] private DefaultAsset _defaultAsset;
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
            MuteInvalidAssetErrors = false; // conservative strategy
        }
    }

    public DefaultAsset DefaultAsset
    {
        get => _defaultAsset;
        set
        {
            if (_defaultAsset == value)
                return;
            _defaultAsset = value;
            _initialized = false;
            _container = null;
            MuteInvalidAssetErrors = false; // conservative strategy
        }
    }

    public bool MuteInvalidAssetErrors; // set true to avoid errors 'asset is not a containner'

    private bool _initialized;
    private BinaryContainer _container;
   // public BinaryContainer Container { get { Init(); return _container; } }
    public BinaryContainer Container { get { InitDefaultAsset(); return _container; } }

    //public bool IsValid => Init();
    public bool IsValid => InitDefaultAsset();

    public static ContainerRef FromTextAsset(TextAsset textAsset, bool verbose = true)
        => new ContainerRef() { _textAsset = textAsset, MuteInvalidAssetErrors = !verbose };

    public static ContainerRef FromDefaultAsset(DefaultAsset defaultAsset, bool verbose = true)
        => new ContainerRef() { _defaultAsset = defaultAsset, MuteInvalidAssetErrors = !verbose };

    public bool Equals(ContainerRef other) => other._textAsset == _textAsset;

    public bool Init()
        => ContainerAssetUtils.Init(ref _initialized, _textAsset, ref _container, !MuteInvalidAssetErrors);

    public bool InitDefaultAsset()
        => ContainerAssetUtils.InitDefaultAsset(ref _initialized, _defaultAsset, ref _container, !MuteInvalidAssetErrors);

#if UNITY_EDITOR
    public bool UpdateSerializers()
    {
        if (!IsValid)
            return false;

        if (Container.UpdateSerializers())
        {
            //WriteToTextAsset();
            WriteToDefaultAsset();
            return true;
        }
        return false;
    }

    public void Save<T>(T obj, int id, bool andWriteToAsset)
    { Init(); ContainerAssetUtils.Save(_textAsset, ref _container, obj, id, andWriteToAsset); }

    public void SaveDefaultAsset<T>(T obj, int id, bool andWriteToAsset)
    { InitDefaultAsset(); ContainerAssetUtils.SaveDefaultAsset(_defaultAsset, ref _container, obj, id, andWriteToAsset); }

    public void Remove<T>(int idToDelete, bool andWriteToAsset)
        => Remove(idToDelete, typeof(T), andWriteToAsset);
    public void Remove(int idToDelete, Type typeToDelete, bool andWriteToAsset)
        => ContainerAssetUtils.Remove(_textAsset, Container, idToDelete, typeToDelete, andWriteToAsset);

    public void RemoveDefaultAsset<T>(int idToDelete, bool andWriteToAsset)
        => RemoveDefaultAsset(idToDelete, typeof(T), andWriteToAsset);
    public void RemoveDefaultAsset(int idToDelete, Type typeToDelete, bool andWriteToAsset)
        => ContainerAssetUtils.RemoveDefaultAsset(_defaultAsset, Container, idToDelete, typeToDelete, andWriteToAsset);

    public void WriteToTextAsset()
        => ContainerAssetUtils.WriteToTextAsset(Container, _textAsset);

    public void WriteToDefaultAsset()
        => ContainerAssetUtils.WriteToDefaultAsset(Container, _defaultAsset);
#endif

    public T Load<T>(int id, bool objectExpected = true)
    {
        T result = default;
        Load(ref result, id, objectExpected);
        return result;
    }

    public bool Load<T>(ref T obj, int id, bool objectExpected = true)
        => ContainerAssetUtils.Load(_textAsset, Container, ref obj, id, objectExpected);

    public bool LoadDefaultAsset<T>(ref T obj, int id, bool objectExpected = true)
        => ContainerAssetUtils.LoadDefaultAsset(_defaultAsset, Container, ref obj, id, objectExpected);
}

namespace DaSerialization.Internal
{
    public static class ContainerAssetUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Init(ref bool initialized, TextAsset textAsset, ref BinaryContainer container, bool verbose)
        {
            if (!initialized)
            {
                initialized = true;
                if (textAsset == null)
                    return false;
                var data = textAsset.bytes;
                try
                {
                    container = UnityStorage.Instance.GetContainerFromData(data, !Application.isPlaying);
                }
                catch (Exception e)
                {
                    if (verbose)
                        Debug.LogException(e, textAsset);
                    container = null;
                }
                if (container == null & verbose)
                    Debug.LogError($"Asset {textAsset.name} contains data which is not a valid {nameof(BinaryContainer)}", textAsset);
            }
            return container != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool InitDefaultAsset(ref bool initialized, DefaultAsset defaultAsset, ref BinaryContainer container, bool verbose)
        {
            if (!initialized)
            {
                initialized = true;
                if (defaultAsset == null)
                    return false;
                var data = File.ReadAllBytes(AssetDatabase.GetAssetPath(defaultAsset));
                try
                {
                    container = UnityStorage.Instance.GetContainerFromData(data, !Application.isPlaying);
                }
                catch (Exception e)
                {
                    if (verbose)
                        Debug.LogException(e, defaultAsset);
                    container = null;
                }
                if (container == null & verbose)
                    Debug.LogError($"Asset {defaultAsset.name} contains data which is not a valid {nameof(BinaryContainer)}", defaultAsset);
            }
            return container != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Load<T>(TextAsset textAsset, BinaryContainer container, ref T obj, int id, bool objectExpected)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LoadDefaultAsset<T>(DefaultAsset defaultAsset, BinaryContainer container, ref T obj, int id, bool objectExpected)
        {
            BinaryContainer.IsValidObjectId(id, true);
            if (container == null)
            {
                if (objectExpected)
                {
                    if (defaultAsset == null)
                        Debug.LogError("Trying to load from Null container");
                    else
                        Debug.LogError("Trying to load from invalid container " + defaultAsset.name, defaultAsset);
                }
                return false;
            }

            bool found = container.Deserialize(ref obj, id);
            if (!found & objectExpected)
                Debug.LogError($"No object of type {typeof(T).PrettyName()} with id {id} found in container {defaultAsset.name}\n", defaultAsset);
            return found;
        }


#if UNITY_EDITOR
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Save<T>(TextAsset textAsset, ref BinaryContainer container, T obj, int id, bool andWriteToAsset)
        {
            BinaryContainer.IsValidObjectId(id, true);
            if (textAsset == null)
                throw new NullReferenceException("Trying to save to Null container");

            container = container ?? UnityStorage.Instance.CreateContainer();
            container.Serialize(obj, id);
            if (andWriteToAsset)
                WriteToTextAsset(container, textAsset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SaveDefaultAsset<T>(DefaultAsset defaultAsset, ref BinaryContainer container, T obj, int id, bool andWriteToAsset)
        {
            BinaryContainer.IsValidObjectId(id, true);
            if (defaultAsset == null)
                throw new NullReferenceException("Trying to save to Null container");

            container = container ?? UnityStorage.Instance.CreateContainer();
            container.Serialize(obj, id);
            if (andWriteToAsset)
                WriteToDefaultAsset(container, defaultAsset);
        }

        public static void Remove(TextAsset textAsset, BinaryContainer container, int idToDelete, Type typeToDelete, bool andWriteToAsset)
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

        public static void RemoveDefaultAsset(DefaultAsset defaultAsset, BinaryContainer container, int idToDelete, Type typeToDelete, bool andWriteToAsset)
        {
            BinaryContainer.IsValidObjectId(idToDelete, true);
            if (defaultAsset == null)
                throw new NullReferenceException("Trying to remove from Null container");
            if (container == null) // the same as !IsValid
                throw new NullReferenceException("Trying to remove from invalid container");

            container.Remove(idToDelete, typeToDelete);
            if (andWriteToAsset)
                WriteToDefaultAsset(container, defaultAsset);
        }

        public static void WriteToTextAsset(BinaryContainer container, TextAsset textAsset)
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

        public static void WriteToDefaultAsset(BinaryContainer container, DefaultAsset defaultAsset)
        {
            if (container == null) // same as !IsValid
            {
                Debug.LogError("Trying to write invalid container");
                return;
            }
            var path = AssetDatabase.GetAssetPath(defaultAsset);
            UnityStorage.Instance.SaveContainerAtPath(container, path);
            AssetDatabase.Refresh();
        }
#endif
    }
}

#endif