using System;
using System.Collections.Generic;
using System.Reflection;
using DaSerialization.Internal;

namespace DaSerialization
{
    public struct SerializationTypeInfo
    {
        public const short NO_SERIALIZER = -2;
        public const short ABSTRACT = -1;

        public int Id;
        public short SerializerIndex;
        public short DeserializerIndex;
        public bool LatestSerializerWritesContainer;
        public bool IsContainer;
        public Type Type;
        public bool IsValid => Id != -1;

        public SerializationTypeInfo(TypeIdAttribute attr)
        {
            Id = attr.Id;
            Type = attr.Type;
            bool shouldHaveSerializer = attr.ShouldHaveSerializers
                && (!attr.Type.IsAbstract & !attr.Type.IsInterface);
            SerializerIndex = shouldHaveSerializer ? NO_SERIALIZER : ABSTRACT;
            DeserializerIndex = SerializerIndex;
            LatestSerializerWritesContainer = false;
            IsContainer = typeof(IContainer).IsAssignableFrom(Type);
        }

        public void InitSerializers(int index, ISerializer serializerHighestVersion)
        {
            SerializerIndex = index.ToInt16();
            LatestSerializerWritesContainer = serializerHighestVersion is ISerializerWritesContainer;
        }
        public void InitDeserializers(int index)
            => DeserializerIndex = index.ToInt16();

        public static SerializationTypeInfo Invalid => new SerializationTypeInfo()
        {
            Id = -1,
            SerializerIndex = NO_SERIALIZER,
            DeserializerIndex = NO_SERIALIZER,
            Type = null,
        };

        public override string ToString()
            => $"{Type.PrettyName()} (id={Id})";
    }

    public class SerializerStorage<TStream> where TStream : class, IStream<TStream>, new()
    {
        private const int MAX_VERSION = int.MaxValue >> 1;

        private struct SerializerInfo
        {
            public int TypeId;
            public int Version;
            public ISerializer<TStream> Serializer;
        }
        private struct DeserializerInfo
        {
            public int TypeId;
            public int Version;
            public IDeserializer<TStream> Deserializer;
        }

        private Dictionary<Type, ushort> _typeToIndex;
        private Dictionary<int, ushort> _idToIndex;
        private List<SerializationTypeInfo> _typeInfos;
        private List<SerializerInfo> _serializerInfos;
        private List<DeserializerInfo> _deserializerInfos;

        private static SerializerStorage<TStream> _default;
        public static SerializerStorage<TStream> Default
            => _default ?? (_default = new SerializerStorage<TStream>());

        public SerializerStorage(Assembly assemblyToSearch = null)
        {
            List<ISerializer> serializers = null;
            List<IDeserializer> deserializers = null;
            FindAllSerializers(assemblyToSearch, ref serializers, ref deserializers);
            List<TypeIdAttribute> typeIds = null;
            FindAllTypeIds(ref typeIds, assemblyToSearch);
            Init(typeIds, serializers, deserializers);
            CheckAllTypeIdsHaveSerializers();
        }

        public SerializerStorage(List<ISerializer> serializers, List<IDeserializer> deserializers,
            Assembly assemblyToSearch = null)
        {
            List<TypeIdAttribute> typeIds = null;
            FindAllTypeIds(ref typeIds, assemblyToSearch);
            Init(typeIds, serializers, deserializers);
            CheckAllTypeIdsHaveSerializers();
        }

        #region initialization

        public static void FindAllSerializers(Assembly assembly, ref List<ISerializer> serializers, ref List<IDeserializer> deserializers)
        {
            serializers = serializers ?? new List<ISerializer>(128);
            deserializers = deserializers ?? new List<IDeserializer>(256);
            Type sInterface = typeof(ISerializer);
            Type dInterface = typeof(IDeserializer);
            assembly = assembly ?? typeof(ISerializer).Assembly;
            foreach (var t in assembly.GetTypes())
            {
                if (t.IsInterface || t.IsAbstract)
                    continue;

                if (sInterface.IsAssignableFrom(t))
                {
                    if (!TypeIsValidSerializer(t))
                        continue;
                    ISerializer s = Activator.CreateInstance(t) as ISerializer;
                    serializers.Add(s);
                }

                if (dInterface.IsAssignableFrom(t))
                {
                    if (!TypeIsValidSerializer(t))
                        continue;
                    IDeserializer d = Activator.CreateInstance(t) as IDeserializer;
                    deserializers.Add(d);
                }
            }
        }

        private static bool TypeIsValidSerializer(Type t)
        {
            if (t.IsValueType)
            {
                SerializationLogger.LogError($"(De)serializer {t.PrettyName()} is a value type, which is not allowed");
                return false;
            }
            var constructor = t.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, Type.EmptyTypes, null);
            if (constructor == null)
            {
                SerializationLogger.LogError($"(De)serializer {t.PrettyName()} doesn't have a parameterless constructor");
                return false;
            }
            return true;
        }

        public static void FindAllTypeIds(ref List<TypeIdAttribute> typeIds, Assembly assembly = null)
        {
            typeIds = typeIds ?? new List<TypeIdAttribute>(64);

            assembly = assembly ?? typeof(ISerializer).Assembly;
            foreach (var t in assembly.GetTypes())
            {
                var typeId = t.GetCustomAttribute<TypeIdAttribute>();
                if (typeId != null)
                {
                    typeId.AttributeOnType = t;
                    typeIds.Add(typeId);
                }
            }
        }

        private void CheckAllTypeIdsHaveSerializers()
        {
            foreach(var tInfo in _typeInfos)
            {
                if (tInfo.SerializerIndex == SerializationTypeInfo.NO_SERIALIZER)
                    throw new Exception($"Type {tInfo.Type.PrettyName()} marked with {nameof(TypeIdAttribute)} but has no serializers");
                if (tInfo.DeserializerIndex == SerializationTypeInfo.NO_SERIALIZER)
                    throw new Exception($"Type {tInfo.Type.PrettyName()} marked with {nameof(TypeIdAttribute)} but has no derializers");
                if (tInfo.SerializerIndex >= 0)
                {
                    var sVersion = _serializerInfos[tInfo.SerializerIndex].Version;
                    if (GetDeserializer(tInfo, sVersion) == null)
                        throw new Exception($"Type {tInfo.Type.PrettyName()} marked with {nameof(TypeIdAttribute)} but has no derializer of version {sVersion} (max found serializer version)");
                }
            }
        }

        private void Init(List<TypeIdAttribute> typeIds, List<ISerializer> serializers, List<IDeserializer> deserializers)
        {
            _typeInfos = new List<SerializationTypeInfo>(typeIds.Count);
            _typeToIndex = new Dictionary<Type, ushort>(typeIds.Count);
            _idToIndex = new Dictionary<int, ushort>(typeIds.Count);
            foreach (var typeId in typeIds)
            {
                var t = typeId.Type;

                // duplicate checks
                if (_typeToIndex.TryGetValue(t, out var conflictIndex))
                {
                    var conflictId = _typeInfos[conflictIndex].Id;
                    throw new Exception($"Type {t.PrettyName()} was marked by {typeof(TypeIdAttribute).PrettyName()} multiple times: id {conflictId} and id {typeId.Id} on type {typeId.AttributeOnType.PrettyName()}");
                }
                if (_idToIndex.TryGetValue(typeId.Id, out conflictIndex))
                {
                    var conflictType = _typeInfos[conflictIndex].Type;
                    throw new Exception($"Types {t.PrettyName()} and {conflictType.PrettyName()} have the same {typeof(TypeIdAttribute).PrettyName()} id value {typeId.Id}");
                }

                ushort index = _typeInfos.Count.ToUInt16();
                _typeInfos.Add(new SerializationTypeInfo(typeId));
                _typeToIndex.Add(t, index);
                _idToIndex.Add(typeId.Id, index);
            }

            _serializerInfos = new List<SerializerInfo>(serializers.Count + 1);
            foreach (var s in serializers)
            {
                var sGenericDefinition = typeof(ISerializer<,>);
                var sInterface = s.GetType().ImplementsGenericInterfaceDefinition(sGenericDefinition);
                if (sInterface == null)
                    throw new Exception($"Serializer {s.GetType().PrettyName()} does not implement interface {sGenericDefinition.PrettyName()}");
                var arguments = sInterface.GetGenericArguments();
                Type oType = arguments[0];

                if (oType.IsAbstract | oType.IsInterface)
                    continue;
                if (!typeof(TStream).IsAssignableFrom(arguments[1]))
                    continue;

                if (!_typeToIndex.TryGetValue(oType, out var sTypeIndex))
                    throw new Exception($"Serializer {s.PrettyTypeName()} works with type {oType.PrettyName()} which is not marked with {typeof(TypeIdAttribute).PrettyName()}");
                if (s.Version <= 0 | s.Version > MAX_VERSION)
                    throw new Exception($"Serializer {s.PrettyTypeName()} has invalid version {s.Version}, should be in range 1..{MAX_VERSION}");

                _serializerInfos.Add(new SerializerInfo()
                {
                    TypeId = _typeInfos[sTypeIndex].Id,
                    Version = s.Version,
                    Serializer = s as ISerializer<TStream>
                });
            }
            _serializerInfos.Sort((x, y) =>
            {
                var comp = x.TypeId.CompareTo(y.TypeId);
                if (comp == 0)
                    comp = y.Version.CompareTo(x.Version);
                return comp;
            });
            for (int i = _serializerInfos.Count - 1; i >= 0; i--)
            {
                var sInfo = _serializerInfos[i];
                bool highestVersion = i == 0 || _serializerInfos[i - 1].TypeId != sInfo.TypeId;
                if (highestVersion)
                {
                    var index = _idToIndex[sInfo.TypeId];
                    var tInfo = _typeInfos[index];
                    tInfo.InitSerializers(i, sInfo.Serializer);
                    _typeInfos[index] = tInfo;

                }
                else
                {
                    var nextInfo = _serializerInfos[i - 1];
                    if (nextInfo.Version == sInfo.Version)
                        throw new Exception($"Serializers {sInfo.Serializer.PrettyTypeName()} and {nextInfo.Serializer.PrettyTypeName()} have the same version {sInfo.Version}");
                }
            }

            _deserializerInfos = new List<DeserializerInfo>(deserializers.Count + 1);
            foreach (var d in deserializers)
            {
                var dGenericDefinition = typeof(IDeserializer<,>);
                var dInterface = d.GetType().ImplementsGenericInterfaceDefinition(dGenericDefinition);
                if (dInterface == null)
                    throw new Exception($"Deserializer {d.PrettyTypeName()} does not implement interface {dGenericDefinition.PrettyName()}");
                var arguments = dInterface.GetGenericArguments();
                Type oType = arguments[0];

                if (oType.IsAbstract | oType.IsInterface)
                    continue;
                if (!typeof(TStream).IsAssignableFrom(arguments[1]))
                    continue;

                if (!_typeToIndex.TryGetValue(oType, out var dTypeIndex))
                    throw new Exception($"Deserializer {d.PrettyTypeName()} works with type {oType.PrettyName()} which is not marked with {typeof(TypeIdAttribute).PrettyName()}");
                if (d.Version <= 0 | d.Version > MAX_VERSION)
                    throw new Exception($"Deserializer {d.PrettyTypeName()} has invalid version {d.Version}, should be in range 1..{MAX_VERSION}");

                _deserializerInfos.Add(new DeserializerInfo()
                {
                    TypeId = _typeInfos[dTypeIndex].Id,
                    Version = d.Version,
                    Deserializer = d as IDeserializer<TStream>
                });
            }
            _deserializerInfos.Sort((x, y) =>
            {
                var comp = x.TypeId.CompareTo(y.TypeId);
                if (comp == 0)
                    comp = y.Version.CompareTo(x.Version);
                return comp;
            });
            for (int i = _deserializerInfos.Count - 1; i >= 0; i--)
            {
                var dInfo = _deserializerInfos[i];
                if (i > 0)
                {
                    var nextInfo = _deserializerInfos[i - 1];
                    if (nextInfo.TypeId == dInfo.TypeId & nextInfo.Version == dInfo.Version)
                        throw new Exception($"Deserializers {dInfo.Deserializer.PrettyTypeName()} and {nextInfo.Deserializer.PrettyTypeName()} have the same version {dInfo.Version}");
                }
                var index = _idToIndex[dInfo.TypeId];
                var tInfo = _typeInfos[index];
                tInfo.InitDeserializers(i);
                _typeInfos[index] = tInfo;
            }

            var report = $"{this.PrettyTypeName()} initialized with {_serializerInfos.Count} serializers, {_deserializerInfos.Count} deserializers and {_typeInfos.Count} types";
            SerializationLogger.Log(report);

            // to reduce conditions in some inner loops in search of required (de)serializers
            _serializerInfos.Add(new SerializerInfo() { TypeId = -1 });
            _deserializerInfos.Add(new DeserializerInfo() { TypeId = -1 });
        }

        #endregion

        public SerializationTypeInfo GetTypeInfo(Type t, bool throwIfNotFound = true)
        {
            if (t == null)
                return SerializationTypeInfo.Invalid;
            if (_typeToIndex.TryGetValue(t, out var indx))
                return _typeInfos[indx];
            if (throwIfNotFound)
            {
                if (t.IsAbstract | t.IsInterface)
                    throw new ArgumentException($"Type {t.PrettyName()} is not marked with {typeof(TypeIdAttribute).PrettyName()}. It doesn't have to have a (de)serializer but it has to be marked with {typeof(TypeIdAttribute).PrettyName()} to be used in {nameof(IContainer.Serialize)} method");
                else
                    throw new ArgumentException($"Type {t.PrettyName()} is not marked with {typeof(TypeIdAttribute).PrettyName()}");
            }
            return SerializationTypeInfo.Invalid;
        }
        public SerializationTypeInfo GetTypeInfo(int typeId, bool throwIfNotFound = true)
        {
            if (typeId == -1)
                return SerializationTypeInfo.Invalid;
            if (_idToIndex.TryGetValue(typeId, out var indx))
                return _typeInfos[indx];
            if (throwIfNotFound)
                throw new ArgumentException($"No type with id={typeId} was found in {this.PrettyTypeName()}");
            return SerializationTypeInfo.Invalid;
        }

        public bool UpdateSerializersForInnerContainers(SerializationTypeInfo typeInfo, ref object obj)
        {
            if (!typeInfo.IsValid | !typeInfo.LatestSerializerWritesContainer)
                return false;
            var serializer = _serializerInfos[typeInfo.SerializerIndex].Serializer as ISerializerWritesContainer;
            return serializer.UpdateSerializersInInnerContainers(ref obj);
        }

        public IDeserializer<TStream> GetDeserializer(SerializationTypeInfo typeInfo, int version)
        {
            int index = typeInfo.DeserializerIndex;
            var typeId = typeInfo.Id;
            var deserializers = _deserializerInfos;
            do
            {
                if (deserializers[index].Version == version)
                    return deserializers[index].Deserializer;
                index++;
            } while (deserializers[index].TypeId == typeId);
            return null;
        }

        public ISerializer<TStream> GetSerializer(SerializationTypeInfo typeInfo, int version = -1)
        {
            // if version specified - search for the exact version match
            // else - take max version
            int index = typeInfo.SerializerIndex;
            var typeId = typeInfo.Id;
            var serializers = _serializerInfos;
            do
            {
                if (version == -1 || serializers[index].Version == version)
                    return serializers[index].Serializer;
                index++;
            } while (serializers[index].TypeId == typeId);
            return null;
        }

    }
}
