#if UNITY_2018_1_OR_NEWER

using System;
using System.Collections.Generic;
using DaSerialization;
using UnityEditor;
using UnityEngine;

public class ContainerInAssetWindow : EditorWindow
{
    [Serializable]
    public class ContainerInfo : ContainerInfo.IEntriesHolder
    {
        [Serializable]
        public class Entry : IEntriesHolder
        {
            public string TypeName;
            public Type Type;
            public object Reference;
            public int Id;
            public int Size;
            public int SizeSelf;
            public List<Entry> InnerEntries;
            public bool IsNull;
            public Entry(Type type, object obj, long size, int metaInfoSize)
            {
                Id = -1;
                IsNull = obj == null;
                Type = type;
                TypeName = IsNull ? type.PrettyName() : obj.PrettyTypeName();
                Reference = obj;
                Size = (int)size;
                SizeSelf = Size - metaInfoSize;
            }
            public void AddInner(Entry entry)
            {
                InnerEntries = InnerEntries ?? new List<Entry>();
                InnerEntries.Add(entry);
                SizeSelf -= entry.Size;
            }
        }
        public interface IEntriesHolder
        {
            void AddInner(Entry entry);
        }

        public long Size;
        public long ContentTableLength;
        public List<Entry> Entries;
        public TextAsset Asset;

        public ContainerInfo(BinaryContainer container)
        {
            Size = container.GetUnderlyingStream().Length;
            var contentTable = container.GetContentTable();
            ContentTableLength = container.CountContentTableLenght(contentTable);
            Entries = new List<Entry>(contentTable.Count);
            container.EnableDeserializationInspection = true;
            container.ObjectDeserializationFinished += OnObjectDeserializationFinished;
            _activeEntries.Clear();
            _activeEntries.Push(new List<IEntriesHolder> { this });
            foreach (var e in contentTable)
            {
                object o = null;
                container.Deserialize(ref o, e.ObjectId, e.TypeId);
                OnObjectDeserializationFinished(null, null, 0, 0, -1);
                if (Entries.Count > 0)
                    Entries[Entries.Count - 1].Id = e.ObjectId;
            }
            container.EnableDeserializationInspection = false;
            _activeEntries.Clear();
            Sort();
        }
        public void AddInner(Entry entry)
        {
            Entries.Add(entry);
        }
        public void Save()
        {
            var path = AssetDatabase.GetAssetPath(Asset);
            var container = UnityStorage.Instance.CreateContainer();
            foreach (var entry in Entries)
                if (entry.Id != -1)
                    container.Serialize(entry.Reference, entry.Id, entry.Type);
            UnityStorage.Instance.SaveContainerAtPath(container, path);
            AssetDatabase.Refresh();
        }

        public void Sort()
        {
            Entries.Sort(
                (x, y) =>
                {
                    int c = x.Id.CompareTo(y.Id);
                    if (c == 0)
                        c = string.CompareOrdinal(x.TypeName, y.TypeName);
                    return c;
                });
        }

        private Stack<List<IEntriesHolder>> _activeEntries = new Stack<List<IEntriesHolder>>();
        private void OnObjectDeserializationFinished(Type type, object obj, long dataSize, int metaSize, int nestedLevel)
        {
            var newEntry = new Entry(type, obj, dataSize + metaSize, metaSize);
            bool added = false;
            int index = nestedLevel + 2;
            while (_activeEntries.Count > index)
            {
                var addedEntries = _activeEntries.Pop();
                IEntriesHolder lastParentEntry;
                if (_activeEntries.Count == index)
                {
                    if (index == 1)
                        lastParentEntry = _activeEntries.Peek()[_activeEntries.Peek().Count - 1];
                    else
                    {
                        _activeEntries.Peek().Add(newEntry);
                        lastParentEntry = newEntry;
                    }
                    added = true;
                }
                else
                    throw new Exception();
                foreach (var e in addedEntries)
                    lastParentEntry.AddInner(e as Entry);
                addedEntries.Clear();
            }
            while (_activeEntries.Count < index)
                _activeEntries.Push(new List<IEntriesHolder>());
            if (!added)
                _activeEntries.Peek().Add(newEntry);
        }
    }
    public TextAsset Target;
    [NonSerialized] private ContainerInfo _info;
    [NonSerialized] private Cached<TextAsset> __target = new Cached<TextAsset>();
    private Vector2 _scrollPos;

    [MenuItem("Window/Container Viewer")]
    public static void Init()
    {
        ContainerInAssetWindow window = (ContainerInAssetWindow)GetWindow(typeof(ContainerInAssetWindow));
        window.name = "Container Viewer";
        window.Show();
    }

    void OnGUI()
    {
        Target = (TextAsset)EditorGUILayout.ObjectField("Target", Target, typeof(TextAsset), false);
        if (__target.Update(Target))
            _info = GetContainerInfo(Target);
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
        if (Target != null & _info == null)
            EditorGUILayout.HelpBox("This is not a container", MessageType.Error);
        else
            DrawContainerContent(_info);
        EditorGUILayout.EndScrollView();
    }

    public static ContainerInfo GetContainerInfo(TextAsset asset)
    {
        if (asset == null)
            return null;
        var container = UnityStorage.Instance.GetContainerFromData(asset.bytes, false);
        if (container == null)
            return null;
        var info = new ContainerInfo(container);
        info.Asset = asset;
        return info;
    }
    public static void DrawContainerContent(ContainerInfo containerInfo)
    {
        if (containerInfo == null)
        {
            EditorGUILayout.HelpBox("No container", MessageType.Info);
            return;
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Container", EditorStyles.boldLabel);
        GUI.contentColor = Color.white;
        GUILayout.Label(containerInfo.Size + " bytes", EditorStyles.whiteLabel, sizeWidth);
        GUI.contentColor = Color.grey;
        GUILayout.Label(containerInfo.ContentTableLength + " tbl", EditorStyles.whiteLabel, sizeWidth);
        GUI.contentColor = Color.white;
        if (containerInfo.Asset != null && GUILayout.Button("Save", GUILayout.Width(40f)))
            containerInfo.Save();
        EditorGUILayout.EndHorizontal();

        foreach (var e in containerInfo.Entries)
            DrawEntry(e, 0, true);
    }

    private static GUILayoutOption idWidth = GUILayout.Width(70f);
    private static GUILayoutOption sizeWidth = GUILayout.Width(80f);
    private static void DrawEntry(ContainerInfo.Entry e, int indent, bool expanded)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("", EditorStyles.whiteLabel, GUILayout.Width(25f * indent));
        GUI.contentColor = Color.red;
        EditorGUILayout.LabelField(e.Id != -1 ? e.Id.ToString() : "", EditorStyles.whiteLabel, idWidth);
        GUI.contentColor = e.IsNull ? Color.grey : Color.black;
        GUILayout.Label(e.TypeName, EditorStyles.whiteLabel);

        // TODO: draw Edit button;

        GUI.contentColor = Color.white;
        GUILayout.Label(e.Size + " bytes", EditorStyles.whiteLabel, sizeWidth);
        GUI.contentColor = Color.grey;
        GUILayout.Label(e.SizeSelf == 0 ? "-" : (e.SizeSelf + " self"), EditorStyles.whiteLabel, sizeWidth);
        GUI.contentColor = Color.white;
        EditorGUILayout.EndHorizontal();
        if (expanded && e.InnerEntries != null)
            foreach (var inner in e.InnerEntries)
                DrawEntry(inner, indent + 1, true); // todo: expanded
    }
}

#endif
