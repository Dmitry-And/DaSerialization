using System;

// not in DaSerializer namespace to avoid excessive 'using' directives

[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Struct
    | AttributeTargets.Interface // to mark collections of interfaces as serializable
    | AttributeTargets.Enum,     // to mark collections of enums as serializable
    Inherited = false,
    AllowMultiple = true)]       // if used for the class itself and for a collection(s) of this class
public class TypeIdAttribute : Attribute
{
    // negative Ids are reserved for special (project-specific) cases
    public int Id { get; private set; }
    public Type Type { get; private set; }
    private Type _attributeOnType;
    public Type AttributeOnType
    {
        get => _attributeOnType;
        set
        {
            Type = Type ?? value;
            _attributeOnType = value;
        }
    }
    public bool ShouldHaveSerializers { get; private set; }

    public TypeIdAttribute(int id, Type t = null, bool shouldHaveSerializers = true)
    {
        if (id == -1 | id == 0)
            throw new Exception($"{nameof(TypeId)} for type {t.PrettyName()} is {id}. This value is not allowed");
        Id = id;
        Type = t;
        ShouldHaveSerializers = shouldHaveSerializers;
    }
}