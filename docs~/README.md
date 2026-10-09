# DaSerialization architecture and development

Paths are relative to this repository. Static source review: 2026-10-08; no builds, tests, Unity imports or conversion tools run. Existing [README](../README.md) describes capabilities/limitations; this guide records implementation entry points. It does not depend on a parent checkout.

## Public API and layers

| Layer | Entry points | Responsibility |
| --- | --- | --- |
| Type identity | `TypeIdAttribute.cs` | Stable integer identity for persisted types, including externally attributed types |
| Serializer contracts | `SerializationInterfaces.cs`, `AFullSerializer.cs` | `ISerializer<T>`, `IDeserializer<T>`, versioned read/write; by-ref construction/reuse |
| Registry | `SerializerStorage.cs` | Discover and validate type IDs and serializers; select versions |
| Stream format | `BinaryStream.cs`, `BinaryStreamReader.cs`, `BinaryStreamWriter.cs` | Binary format, primitive/object read/write, format validation |
| Containers | `BinaryContainer.cs` | Object-ID-addressed serialization/deserialization, content management and cleanup |
| Storage | `ContainerStorage.cs`, `BinaryContainerStorageOnMemory.cs`, `BinaryContainerStorageOnFiles.cs` | Create/load/save/delete container abstraction and backends |
| Unity adapter | `BinaryContainerStorageOnUnity.cs`, `UnityIntegration/` | Resources/persistent storage, references, inspectors and asset tooling |
| Built-ins | `DefaultSerializers/` | Arrays/lists/object and packing support |
| Copy/hash helpers | `SerializationUtils.cs` | Serialization-based `DeepCopy`, `DeepCopyTo`, serialized hash |
| Diagnostics/tests | `Helpers/`, `Tests/` | Logging/type/stream helpers and serialization fixtures |

## Call and data flow

```text
consumer assigns stable type IDs and provides versioned serializers
  -> SerializerStorage discovers/validates registrations
storage.CreateContainer()
  -> container.Serialize(object, objectId)
  -> registry selects serializer -> BinaryStreamWriter -> bytes
storage.SaveContainer(container, name)
storage.LoadContainer(name, writable)
  -> validate stream -> BinaryContainer
  -> container.Deserialize<T>(objectId) / Deserialize(ref target, objectId)
  -> registry selects deserializer -> BinaryStreamReader -> construct/populate target
```

Consumers own schemas: serializers explicitly choose fields, ordering and construction. `AFullSerializer<T>` combines `ADeserializer<T>` with writing; readers can populate existing objects by reference. Polymorphic contracts are supported through type IDs rather than serialized type names. Numeric object IDs identify entries within a container and are distinct from type IDs and serializer versions.

## Registry and compatibility

`SerializerStorage.Default` lazily creates a registry. Default discovery scans `typeof(ISerializer).Assembly`; callers can supply an assembly or explicit serializer/deserializer lists through constructors. Splitting libraries into separate assemblies requires auditing registration scope. Discovery invokes serializer constructors via reflection; they need compatible construction/visibility. Unity consumers commonly use `[Preserve]` for reflected classes.

Keep existing `[TypeId]` values stable and unique. Read/write ordering is the schema; changing a field declaration alone does not change persistence. A particular serializer/deserializer version is represented by a separate class. Retain supported old deserializers intentionally and select a new version when the wire contract changes. Review nested-container serializers through `ISerializerWritesContainer` as well.

## Storage and Unity integration

`IContainerStorage` exposes `CreateContainer`, `LoadContainer(name, writable, errorIfNotExist)`, `SaveContainer`, and `DeleteContainer`. Containers are disposable; consumers normally use `using` blocks. Read/write access and cleanup behavior matter when loading or rewriting existing data.

The Unity adapter is conditional on `UNITY_2018_1_OR_NEWER`. It loads `Resources/Storage/` assets first, then persistent files; resource data is read-only. Editor writes target `Assets/EditorStorage/*.bytes`; player writes target `Application.persistentDataPath/Storage/*.bytes`. `UnityStorage.Instance` supplies a default adapter. `UnityIntegration/ContainerRef.cs` offers serialized references, while `UnityIntegration/Editor/` provides container inspection/conversion tools. Running conversion tools can rewrite data; inspection is not permission to convert.

## Copying, concurrency and style

`SerializationUtils.DeepCopyTo` serializes object ID 1 into a reused memory container and deserializes into the destination, so copied state follows the registered schema. Its static container stack supports nested copy calls; it is not evidence of concurrent safety. The library README states that a particular container must be used on one thread. Do not introduce concurrent container operations or treat serialization-based copying as a fieldwise clone.

Observed style: four-space indentation, Allman braces, block namespaces, PascalCase APIs, generic abstract serializer bases, explicit loops/reused buffers, and mixed BOM/line-ending conventions. Avoid formatting churn. Preserve validation/error behavior and reference/null/polymorphic handling when changing readers/writers.

## Tests and follow-up scope

`Tests/PackedSerializationTests.cs`, `SerializationTestObjects.cs`, and `TestContainerCreator*.cs` are navigation points for packing, manual serializers and fixture creation. Some test support uses the companion custom test infrastructure; a standalone build/test host was not established in this review. No passing-test claim is made. Future authorized changes should exercise meaningful round trips and supported old versions for the affected format.

The recursive fields and nested `BinaryContainer` fields in `SerializationTestObjects.cs` are marked `[NonSerialized]` to exclude them from Unity's field serializer. Their manual DaSerialization serializers still read and write them explicitly; type IDs, versions and binary field ordering are unchanged. This distinction avoids Unity's serialization cycle and unsupported-type analyzer warnings without removing the binary test fixtures.

Keep dependency/host-specific schemas in their owning repositories. Do not assume any particular game, Unity parent directory, or solution exists. Preserve existing README/LICENSE; this guide supplements them.
