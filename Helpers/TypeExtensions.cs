using System;
using System.Text;

public static class TypeExtensions
{
    public static bool ImplementsInterface(this Type t, Type interfaceType)
    {
        if (t == null)
            throw new ArgumentNullException(nameof(t));
        if (interfaceType == null)
            throw new ArgumentNullException(nameof(interfaceType));
        if (!interfaceType.IsInterface)
            throw new ArgumentException($"{interfaceType.PrettyName()} is not an interface");

        if (interfaceType.IsGenericTypeDefinition)
        {
            var interfaces = t.GetInterfaces();
            for (int i = 0; i < interfaces.Length; i++)
            {
                var curr = interfaces[i];
                if (!curr.IsGenericType)
                    continue;
                if (curr.GetGenericTypeDefinition() == interfaceType)
                    return true;
            }
            return false;
        }
        return interfaceType.IsAssignableFrom(t);
    }

    public static Type ImplementsGenericInterfaceDefinition(this Type t, Type genericInterfaceDefenition)
    {
        if (t == null)
            throw new ArgumentNullException(nameof(t));
        if (genericInterfaceDefenition == null)
            throw new ArgumentNullException(nameof(genericInterfaceDefenition));
        if (!genericInterfaceDefenition.IsInterface | !genericInterfaceDefenition.IsGenericTypeDefinition)
            throw new ArgumentException(nameof(genericInterfaceDefenition) + " is not a generic interface definition");

        var interfaces = t.GetInterfaces();
        for (int i = 0; i < interfaces.Length; i++)
        {
            var curr = interfaces[i];
            if (!curr.IsGenericType)
                continue;
            if (curr.IsGenericTypeDefinition)
                continue;
            if (curr.GetGenericTypeDefinition() == genericInterfaceDefenition)
                return curr;
        }
        return null;
    }

    public static Type ImplementsGenericClassDefinition(this Type t, Type genericClassDefenition)
    {
        if (t == null)
            throw new ArgumentNullException(nameof(t));
        if (genericClassDefenition == null)
            throw new ArgumentNullException(nameof(genericClassDefenition));
        if (genericClassDefenition.IsInterface | !genericClassDefenition.IsGenericTypeDefinition)
            throw new ArgumentException(genericClassDefenition.PrettyName() + " is not a generic class definition");

        while(t != null)
        {
            if (t.IsGenericType
                && t.GetGenericTypeDefinition() == genericClassDefenition)
                return t;
            t = t.BaseType;
        }
        return null;
    }

    public static bool CanBeAssignedTo(this Type type, Type super)
    {
        if (super.IsAssignableFrom(type))
            return true;
        if (super.IsGenericType & !super.IsGenericTypeDefinition)
            throw new NotImplementedException($"{nameof(CanBeAssignedTo)} does not support generic types which are not generic type definitions! {nameof(super)}={super.PrettyName()}, {nameof(type)}={type.PrettyName()}");
        if (super.IsGenericTypeDefinition)
        {
            if (super.IsInterface)
                return type.ImplementsGenericInterfaceDefinition(super) != null;
            return type.ImplementsGenericClassDefinition(super) != null;
        }
        return super.IsInterface && type.ImplementsInterface(super);
    }

    public static string PrettyTypeName(this object o)
    {
        if (o == null)
            return "null";
        return o.GetType().PrettyName();
    }

    private static StringBuilder _tempStringBuilder = new StringBuilder(64);
    public static string PrettyName(this Type t)
    {
        if (t == null)
            return "null";
        t.PrettyNameTo(_tempStringBuilder);
        var result = _tempStringBuilder.ToString();
        _tempStringBuilder.Clear();
        return result;
    }

    public static void PrettyNameTo(this Type t, StringBuilder sb)
    {
        if (t == null)
            sb.Append("null");

        if (t.IsArray)
        {
            int dimensions = t.GetArrayRank();
            int initialPos = sb.Length;
            t.GetElementType().PrettyNameTo(sb);
            int genericEndPos = LastIndexOf(sb, '>');
            if (genericEndPos <= initialPos)
                genericEndPos = initialPos;
            int insertPos = IndexOf(sb, '[', genericEndPos);
            if (insertPos < 0)
            {
                sb.Append("[");
                for (; dimensions > 1; dimensions--)
                    sb.Append(",");
                sb.Append("]");
            }
            else
            {
                sb.Insert(insertPos++, "[");
                for (; dimensions > 1; dimensions--)
                    sb.Insert(insertPos++, ",");
                sb.Insert(insertPos++, "]");
            }
            return;
        }
        if (t.IsGenericType)
        {
            var args = t.GetGenericArguments();
            var typeName = t.Name;
            sb.Append(typeName, 0, typeName.Length - 2);
            sb.Append("<");
            args[0].PrettyNameTo(sb);
            for (int i = 1; i < args.Length; i++)
            {
                sb.Append(",");
                args[i].PrettyNameTo(sb);
            }
            sb.Append(">");
            return;
        }
        sb.Append(t.Name);
    }

    private static int IndexOf(StringBuilder sb, char c, int startIndex = 0)
    {
        for (int i = startIndex, max = sb.Length; i < max; i++)
            if (sb[i] == c)
                return i;
        return -1;
    }
    private static int LastIndexOf(StringBuilder sb, char c)
    {
        for (int i = sb.Length - 1; i >= 0; i--)
            if (sb[i] == c)
                return i;
        return -1;
    }
}