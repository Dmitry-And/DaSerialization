#if UNITY_2018_1_OR_NEWER

using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace DaSerialization.Editor
{
    public class ContainerEditorInfo
    {
        #region basic info

        public TextAsset Asset { get; private set; }
        public bool IsValid => _container != null;
        public long Size => _container == null ? 0 : _container.Size;
        public int EntriesCount => _container == null ? 0 : _container.GetContentTable().Count;
        private BinaryContainer _container;

        public ContainerEditorInfo(TextAsset textAsset)
        {
            Asset = textAsset;
            var containerRef = ContainerRef.FromTextAsset(Asset);
            _container = containerRef.Container as BinaryContainer;
        }

        #endregion

        #region detailed info

        public class RootObjectInfo
        {
            public bool IsSupported => Data.IsSupported; // set to false if no deserializer found
            public InnerObjectInfo Data;
            public string Error;

            public RootObjectInfo(InnerObjectInfo data, int id)
            {
                Data = data;
                Data.Id = id;
            }
            public RootObjectInfo(Type refType, long streamPos, int totalSize, string error)
            {
                Data = new InnerObjectInfo(refType, streamPos, totalSize);
                Error = error;
            }
        }
        public class InnerObjectInfo
        {
            public bool IsSupported => SelfSize >= 0;
            public bool IsExpandable => InnerObjects != null;
            public bool OldVersion => LatestVersion > Version;
            public bool IsNull => TypeInfo.Type == null;
            public bool IsRealObject => TypeInfo.IsValid;

            public int Id; // for inner object it's an index inside the parent one
            public bool HasOldVersions;
            public bool JsonHasErrors;
            public Type RefType;
            public SerializationTypeInfo TypeInfo;
            public int Version; // version may be -1 if it's non-serializable type, for example List<T> in SerializeList<T>
            public int LatestVersion;
            public long StreamPosition;
            public int MetaSize;
            public int DataSize;
            public int TotalSize => MetaSize + DataSize;
            public int SelfSize; // DataSize excluding all inner objects' total size
            public string Caption;
            public string JsonData;
            public List<InnerObjectInfo> InnerObjects;

            public InnerObjectInfo(Type refType, long streamPos, int totalSize)
            {
                RefType = refType;
                StreamPosition = streamPos;
                MetaSize = 0;
                DataSize = totalSize;
                SelfSize = -1;
                Caption = refType != null
                    ? $"{RefType.PrettyName()} : [Error]"
                    : "[Error]";
                HasOldVersions = false;
            }
            public InnerObjectInfo(Type refType, SerializationTypeInfo typeInfo, long streamPos, int metaSize, int version, int latestVersion)
            {
                RefType = refType;
                TypeInfo = typeInfo;
                Version = version;
                LatestVersion = latestVersion;
                StreamPosition = streamPos;
                MetaSize = metaSize;
                Caption = RefType == TypeInfo.Type
                    ? RefType.PrettyName()
                    : $"{RefType.PrettyName()} : {TypeInfo.Type.PrettyName()}";
            }
            public void EndInit(long endStreamPos)
            {
                DataSize = (endStreamPos - StreamPosition).ToInt32();
                SelfSize = DataSize;
                HasOldVersions = false;
                if (InnerObjects != null)
                    foreach (var io in InnerObjects)
                    {
                        SelfSize -= io.TotalSize;
                        HasOldVersions |= io.OldVersion | io.HasOldVersions;
                    }
            }

            public void Add(InnerObjectInfo obj)
            {
                if (InnerObjects == null)
                    InnerObjects = new List<InnerObjectInfo>();
                obj.Id = InnerObjects.Count;
                InnerObjects.Add(obj);
            }
        }

        public int TableSize { get; private set; } = -1;
        public List<RootObjectInfo> RootObjects { get; private set; }

        public void UpdateDetailedInfo(bool force = false)
        {
            if (!force & RootObjects != null)
                return;
            var contentTable = _container.GetContentTable();
            TableSize = _container.CountContentTableLenght(contentTable).ToInt32();
            RootObjects = new List<RootObjectInfo>(EntriesCount);
            _container.EnableDeserializationInspection = true;
            _container.ObjectDeserializationStarted += OnObjectDeserializationStarted;
            _container.ObjectDeserializationFinished += OnObjectDeserializationFinished;
            foreach (var e in contentTable)
            {
                object o = null;
                try
                {
                    _container.Deserialize(ref o, e.ObjectId, e.TypeId);
                }
                catch (Exception ex)
                {
                    var refType = _container.GetTypeInfo(e.TypeId, false).Type;
                    var invalidRoot = new RootObjectInfo(refType, e.Position, e.Length, ex.Message);
                    RootObjects.Add(invalidRoot);
                    continue;
                }
                if (_activeEntries.Count != 0)
                    throw new Exception("Deserialization start calls count != end calls count");
                var root = new RootObjectInfo(_rootInfo, e.ObjectId);
                RootObjects.Add(root);
            }
            _container.ObjectDeserializationStarted -= OnObjectDeserializationStarted;
            _container.ObjectDeserializationFinished -= OnObjectDeserializationFinished;
            _container.EnableDeserializationInspection = false;
            RootObjects.Sort((x, y) => x.Data.Id.CompareTo(y.Data.Id));
        }

        private static JsonSerializerSettings _jsonSettings;
        private static List<string> _jsonErrors = new List<string>();
        public void UpdateJsonData(InnerObjectInfo info)
        {
            if (_jsonSettings == null)
            {
                _jsonSettings = new JsonSerializerSettings()
                {
                    Culture = System.Globalization.CultureInfo.InvariantCulture,
                    Formatting = Formatting.Indented,
                    DefaultValueHandling = DefaultValueHandling.Include,
                    NullValueHandling = NullValueHandling.Include,
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    Error = delegate (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
                    {
                        if (args.ErrorContext.Error is JsonSerializationException)
                        {
                            _jsonErrors.Add(args.ErrorContext.Error.Message);
                            args.ErrorContext.Handled = true;
                        }
                    },
                };
            }
            if (info.JsonData == null & !info.JsonHasErrors)
            {
                try
                {
                    object obj = null;
                    var container = _container as IContainerInternals;
                    container.Deserialize(info.StreamPosition, ref obj, info.TypeInfo, info.Version);
                    info.JsonData = JsonConvert.SerializeObject(obj, _jsonSettings);
                    info.JsonHasErrors = _jsonErrors.Count > 0;
                    if (_jsonErrors.Count > 0)
                    {
                        var sb = new StringBuilder(info.JsonData);
                        sb.AppendLine("\n\n");
                        sb.AppendLine("=== ERRORS ===");
                        foreach (var err in _jsonErrors)
                            sb.AppendLine(err);
                        info.JsonData = sb.ToString();
                    }
                }
                catch (Exception ex)
                {
                    info.JsonHasErrors = true;
                    info.JsonData = ex.Message;
                }
                _jsonErrors.Clear();
            }
        }

        private InnerObjectInfo _rootInfo;
        private Stack<InnerObjectInfo> _activeEntries = new Stack<InnerObjectInfo>();
        private void OnObjectDeserializationStarted(Type refType, SerializationTypeInfo typeInfo, long streamPos, int metaInfoLen, int version)
        {
            var lastVersion = typeInfo.IsValid ? _container.SerializerStorage.GetSerializer(typeInfo).Version : -1;
            var info = new InnerObjectInfo(refType, typeInfo, streamPos, metaInfoLen, version, lastVersion);
            _activeEntries.Push(info);
        }
        private void OnObjectDeserializationFinished(long streamPos)
        {
            var info = _activeEntries.Pop();
            info.EndInit(streamPos);
            if (_activeEntries.Count > 0)
            {
                // add this object info as inner one for the previous object
                var parent = _activeEntries.Peek();
                parent.Add(info);
            }
            else
            {
                // this is a top-level object
                _rootInfo = info;
            }
        }

        #endregion
    }
}

#endif