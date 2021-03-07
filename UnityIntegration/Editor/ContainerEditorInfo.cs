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

        public bool IsValid => _container != null;
        public long Size => _container == null ? 0 : _container.Size;
        public int EntriesCount => _container == null ? 0 : _container.GetContentTable().Count;
        private BinaryContainer _container;
        public BinaryContainer GetContainer() => _container;
        public int CacheVersion { get; private set; } = 1; // get updated on each change

        public ContainerEditorInfo(TextAsset textAsset, bool verbose = false)
        {
            var containerRef = ContainerRef.FromTextAsset(textAsset, verbose);
            _container = containerRef.Container;
        }
        public ContainerEditorInfo(BinaryContainer container)
        {
            _container = container;
        }

        public bool ContainsObjectWithId(int id)
        {
            foreach (var e in _container.GetContentTable())
                if (e.ObjectId == id)
                    return true;
            return false;
        }

        public void MarkDirty() => CacheVersion++;

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
            public RootObjectInfo(Type refType, long streamPos, uint totalSize, string error)
            {
                Data = new InnerObjectInfo(refType, streamPos, null);
                Data.EndInit(streamPos + totalSize, false);
                Error = error;
            }
        }
        public class InnerObjectInfo
        {
            public bool IsExpandable => InnerObjects != null;
            public bool OldVersion => LatestVersion > Version;
            public bool IsNull => HasDeserializer & TypeInfo.Type == null;
            public bool IsSimpleType => IsSupported & Version == 0 & RefType != null;
            public bool IsRealObject => TypeInfo.IsValid;
            public bool IsSection => IsSupported & Version == 0 & RefType == null & RefTypeName != null;
            public bool HasDeserializer => Version > 0;

            public int Id; // for inner object it's an index inside the parent one
            public bool IsSupported { get; private set; } = false;
            public bool HasOldVersions { get; private set; }
            public bool JsonHasErrors;
            public bool JsonCreated;
            public bool IsInternalData { get; private set; }
            public bool IsMetaData => MetadataType != Metadata.None;

            public Type RefType; // represent type the serialized object is referenced by, not neccesseraly serializable type
            public string RefTypeName; // valid for sections only, non-existent type
            public SerializationTypeInfo TypeInfo = SerializationTypeInfo.Invalid;
            public Metadata MetadataType = Metadata.None;

            public int Version = 0; // version may be -1 if it's non-serializable type, for example List<T> in SerializeList<T>() method
            public int LatestVersion = 0;
            public long StreamPosition;
            public uint InternalSize = 0;
            public uint DataSize;
            public uint TotalSize => InternalSize + DataSize;
            public uint SelfSize; // DataSize excluding inner deserializers data
            public string Name;
            public string JsonData;
            public List<InnerObjectInfo> InnerObjects;

            // when we begin to initialize
            public InnerObjectInfo(Type refType, long streamPos, string name, string typeSuffix = null)
            {
                RefType = refType;
                StreamPosition = streamPos;
                Name = name;
                RefTypeName = typeSuffix;
            }
            public void ValidDataFound(SerializationTypeInfo typeInfo, long streamPos, int version, int latestVersion)
            {
                TypeInfo = typeInfo;
                Version = version;
                LatestVersion = latestVersion;
                InternalSize = (streamPos - StreamPosition).ToUInt32();
                StreamPosition = streamPos; // we should point to first non-meta bit for correct deserialization for JSON preview
                IsSupported = true;
            }
            public void PrimitiveDataFound()
            {
                IsSupported = true;
            }
            public void EndInit(long endStreamPos, bool isInternalData)
            {
                DataSize = (endStreamPos - StreamPosition).ToUInt32();
                SelfSize = IsSupported ? DataSize : 0;
                IsInternalData = isInternalData;
                HasOldVersions = false;
                if (InnerObjects != null)
                    foreach (var io in InnerObjects)
                    {
                        if (!io.IsSimpleType & !io.IsInternalData)
                            SelfSize -= io.DataSize;
                        HasOldVersions |= io.OldVersion | io.HasOldVersions;
                    }
            }

            public InnerObjectInfo(Metadata type, long streamPos, string name)
            {
                MetadataType = type;
                StreamPosition = streamPos;
                Name = name;
                IsSupported = true;
            }

            public InnerObjectInfo(string type, long streamPos, string name)
            {
                RefTypeName = type;
                StreamPosition = streamPos;
                Name = name;
                IsSupported = true;
            }

            public void Add(InnerObjectInfo obj)
            {
                if (InnerObjects == null)
                    InnerObjects = new List<InnerObjectInfo>();
                obj.Id = InnerObjects.Count;
                InnerObjects.Add(obj);
            }
        }

        public int InternalDataSize { get; private set; } = -1;
        public bool HasOldVersions { get; private set; }
        public List<RootObjectInfo> RootObjects { get; private set; }

        public void UpdateDetailedInfo(bool force = false)
        {
            if (!force & RootObjects != null)
                return;
            var contentTable = _container.GetContentTable();
            var metaSize = _container.Size;
            RootObjects = new List<RootObjectInfo>(EntriesCount);
            HasOldVersions = false;
            var stream = _container.GetBinaryStream();
            var reader = stream.GetReader();
            reader.EnableDeserializationInspection = true;
            reader.DeserializationStarted += OnDeserializationStarted;
            reader.MetaDeserializationStarted += OnMetaDeserializationStarted;
            reader.DataDeserializationStarted += OnDataDeserializationStarted;
            reader.DeserializationEnded += OnDeserializationEnded;
            reader.MetaDeserializationEnded += OnDeserializationEnded;
            reader.PrimitiveDeserializationStarted += OnPrimitiveDeserializationStarted;
            reader.PrimitiveDeserializationEnded += OnDeserializationEnded;
            reader.SectionDeserializationStarted += OnSectionDeserializationStarted;
            reader.SectionDeserializationEnded += OnDeserializationEnded;
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
                HasOldVersions |= root.Data.HasOldVersions | root.Data.OldVersion;
                metaSize -= root.Data.TotalSize;
                RootObjects.Add(root);
            }
            reader.DeserializationStarted -= OnDeserializationStarted;
            reader.MetaDeserializationStarted -= OnMetaDeserializationStarted;
            reader.DataDeserializationStarted -= OnDataDeserializationStarted;
            reader.PrimitiveDeserializationStarted -= OnPrimitiveDeserializationStarted;
            reader.SectionDeserializationStarted -= OnSectionDeserializationStarted;
            reader.DeserializationEnded -= OnDeserializationEnded;
            reader.MetaDeserializationEnded -= OnDeserializationEnded;
            reader.PrimitiveDeserializationEnded -= OnDeserializationEnded;
            reader.SectionDeserializationEnded -= OnDeserializationEnded;
            reader.EnableDeserializationInspection = false;
            RootObjects.Sort((x, y) => x.Data.Id.CompareTo(y.Data.Id));
            InternalDataSize = metaSize.ToInt32();

            MarkDirty();
        }

        private class SerializationTypeBinder : Newtonsoft.Json.Serialization.ISerializationBinder
        {
            public Type BindToType(string assemblyName, string typeName) => null;
            public void BindToName(Type serializedType, out string assemblyName, out string typeName)
            {
                assemblyName = null;
                typeName = serializedType.PrettyName();
            }
        }
        private static JsonSerializer _jsonSerializer;
        private static List<string> _jsonErrors = new List<string>();
        public void UpdateJsonData(InnerObjectInfo info)
        {
            if (_jsonSerializer == null)
            {
                _jsonSerializer = new JsonSerializer()
                {
                    Culture = System.Globalization.CultureInfo.InvariantCulture,
                    Formatting = Formatting.Indented,
                    DefaultValueHandling = DefaultValueHandling.Include,
                    NullValueHandling = NullValueHandling.Include,
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                    SerializationBinder = new SerializationTypeBinder(),
                };
                _jsonSerializer.Error += (sender, args) =>
                {
                    if (args.ErrorContext.Error is JsonSerializationException jErr)
                    {
                        _jsonErrors.Add(jErr.Message);
                        args.ErrorContext.Handled = true;
                    }
                };
            }
            if (info.JsonData == null & !info.JsonHasErrors)
            {
                try
                {
                    const int indentation = 4;
                    const bool showTypes = true;

                    object obj = null;
                    _container.GetBinaryStream().GetReader().Deserialize(info.StreamPosition, ref obj, info.TypeInfo, info.Version);

                    var stringWriter = new System.IO.StringWriter();
                    using (var writer = new JsonTextWriter(stringWriter))
                    {
                        writer.QuoteName = false;
                        writer.Formatting = Formatting.Indented;
                        writer.Indentation = indentation;
                        writer.IndentChar = ' ';
                        _jsonSerializer.TypeNameHandling = showTypes ? TypeNameHandling.Objects : TypeNameHandling.None;
                        _jsonSerializer.Serialize(writer, obj, info.TypeInfo.Type);
                    }
                    var json = stringWriter.ToString();

                    var sb = new StringBuilder(json.Length);
                    int i = json.IndexOf('\n') + 1; // skip first line with opening bracket
                    if (i >= 0)
                        i = json.IndexOf('\n', i) + 1 + indentation; // skip second line with $type info
                    if (i > 0)
                        for (int max = json.Length; i < max; i++)
                        {
                            var c = json[i];
                            if (c == '\r')
                                continue;
                            if (c == '\n')
                            {
                                if (sb[sb.Length - 1] == ',')
                                    sb.Length--; // remove trailing comma
                                i += indentation; // remove root object indentation
                                if (i >= max)
                                    break;
                            }
                            sb.Append(c);
                        }

                    if (_jsonErrors.Count > 0)
                    {
                        sb.AppendLine("\n\n");
                        sb.Append("===== ERRORS =====");
                        foreach (var err in _jsonErrors)
                        {
                            sb.AppendLine();
                            sb.AppendLine();
                            sb.Append(err);
                        }
                    }
                    info.JsonData = sb.ToString();
                    info.JsonHasErrors = _jsonErrors.Count > 0;
                    info.JsonCreated = true;
                }
                catch (Exception ex)
                {
                    info.JsonCreated = false;
                    info.JsonHasErrors = true;
                    info.JsonData = ex.PrettyTypeName() + ": " + ex.Message + "\n\n" + ex.StackTrace;
                }
                _jsonErrors.Clear();
            }
        }

        private InnerObjectInfo _rootInfo;
        private Stack<InnerObjectInfo> _activeEntries = new Stack<InnerObjectInfo>();

        private void OnDeserializationStarted(Type refType, long streamPos, string name)
        {
            var info = new InnerObjectInfo(refType, streamPos, name);
            _activeEntries.Push(info);
        }

        private void OnMetaDeserializationStarted(Metadata type, long streamPos, string name)
        {
            var info = new InnerObjectInfo(type, streamPos, name);
            _activeEntries.Push(info);
        }

        private void OnDataDeserializationStarted(SerializationTypeInfo typeInfo, long streamPos, int version)
        {
            var lastVersion = typeInfo.IsValid ? _container.SerializerStorage.GetSerializer(typeInfo).Version : -1;
            var info = _activeEntries.Peek();
            info.ValidDataFound(typeInfo, streamPos, version, lastVersion);
        }

        private void OnPrimitiveDeserializationStarted(Type type, long streamPos, string name, string typeSuffix)
        {
            var info = new InnerObjectInfo(type, streamPos, name, typeSuffix);
            info.PrimitiveDataFound();
            _activeEntries.Push(info);
        }

        private void OnSectionDeserializationStarted(string type, long streamPos, string name)
        {
            name = name ?? type;
            var info = new InnerObjectInfo(type, streamPos, name);
            _activeEntries.Push(info);
        }

        private void OnDeserializationEnded(long streamPos)
        {
            var info = _activeEntries.Pop();
            bool isInternalData = false;
            if (_activeEntries.Count > 0)
            {
                // add this object info as inner one for the previous object
                var parent = _activeEntries.Peek();
                parent.Add(info);
                isInternalData = !parent.IsSupported;
            }
            else
            {
                // this is a top-level object
                _rootInfo = info;
            }
            info.EndInit(streamPos, isInternalData);
        }

        #endregion
    }
}

#endif