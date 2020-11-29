# DaSerialization v0.1
Fast, compact, binary, manual de/serialization framework for C# with optional Unity3d integration

# Main Features
- Runtime in-memory or file-based binary serialization and deserialization
- Supports serialization/deserialization for:
  - custom classes and structures
  - external (for example Unity) classes and structures
  - polymorphic objects (generic/abstract classes, interfaces)
  - collections (lists, arrays)
- Full control over **how** and **what** get serialized via hand-written serializer(s), which allows to
  - use any conditions/logic during (de)serialization process
  - compress serialized data on the fly
  - object state validation before serialization
- Full control over **how** an object get deserialized via hand-written deserializer(s), which allows to
  - object initialization after deserialization (though no access to anything outside the deserialized object)
- Minimal generated garbage during deserialization
  - including by ref deserialization into existing object
  - no boxing for structs (de)serialization (if generic API used)
- Backward compatibility for serialized files is possible
  - you can have multiple deserializers of different (old) versions
  - easy API to update file serializers

# More Features
- Almost no type/meta information get serialized
- Minimal Reflection usage during (de)serialization
- Pure C#, no external dependencies (including Unity API or ecosystem)
- No code generation
  - but is possible for default (de)serializer generation
- Doesn't rely on any DI, static data, singletons or service locators
- Helpers to avoid (de)serialization errors on serialized type changes
- Unity3d integration (optional, if-defined)
  - content (and metadata) inspection of serialized files
  - references to serialized files with easy API, additional validations and Inspector support
  - update all files format in the project with just one button
  
# Limitations
- Each particular version of a (de)serializer for a particular type must be represented as a separate class
