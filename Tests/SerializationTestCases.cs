using System;

namespace DaSerialization.Tests
{
    [Serializable, TypeId(54876)]
    public class TopLevelObject
    {
        public int intTest;
        public double doubleTest;
        public short shortTest;
        public string stringTest;

        public TopLevelObject() 
        {
            intTest = 2047483647;
            doubleTest = 2.457692;
            shortTest = 30567;
            stringTest = "dflkajefciv,;eiq";
        }
    }

    public class TopLevelObjectSerializer : AFullSerializer<TopLevelObject>
    {
        private const string FIELD_META_INFO_PREFIX = "N_";

        public override int Version => 1;        

        public override void ReadDataToObject(ref TopLevelObject obj, BinaryStreamReader reader)
        {
            if (obj == null)
                obj = new TopLevelObject();
            obj.intTest = reader.ReadInt32(FIELD_META_INFO_PREFIX + obj.intTest.PrettyTypeName());
            obj.doubleTest = reader.ReadDouble(FIELD_META_INFO_PREFIX + obj.doubleTest.PrettyTypeName());
            obj.shortTest = reader.ReadInt16(FIELD_META_INFO_PREFIX + obj.shortTest.PrettyTypeName());
            obj.stringTest = reader.ReadString(FIELD_META_INFO_PREFIX + obj.stringTest.PrettyTypeName());
        }

        public override void WriteObject(TopLevelObject obj, BinaryStreamWriter writer)
        {
            writer.WriteInt32(obj.intTest);
            writer.WriteDouble(obj.doubleTest);
            writer.WriteInt16(obj.shortTest);
            writer.WriteString(obj.stringTest);
        }
    }

    [Serializable, TypeId(1546853)]
    public struct TopLevelScructure
    {
        public int intTest;
        public string stringTest;
        public char charTest;
        public byte byteTest;

        public TopLevelScructure(int intVal)
        {
            intTest = intVal;
            stringTest = "   qeri.kjnvj2DFJE";
            charTest = '-';
            byteTest = 247;
        }
    }

    public class TopLevelStructureSerializer : AFullSerializer<TopLevelScructure>
    {
        private const string FIELD_META_INFO_PREFIX = "N_";

        public override int Version => 1;

        public override void ReadDataToObject(ref TopLevelScructure obj, BinaryStreamReader reader)
        {
            obj.intTest = reader.ReadInt32(FIELD_META_INFO_PREFIX + obj.intTest.PrettyTypeName());
            obj.stringTest = reader.ReadString(FIELD_META_INFO_PREFIX + obj.stringTest.PrettyTypeName());
            obj.charTest = reader.ReadChar(FIELD_META_INFO_PREFIX + obj.charTest.PrettyTypeName());
            obj.byteTest = reader.ReadByte(FIELD_META_INFO_PREFIX + obj.byteTest.PrettyTypeName());
        }

        public override void WriteObject(TopLevelScructure obj, BinaryStreamWriter writer)
        {
            writer.WriteInt32(obj.intTest);
            writer.WriteString(obj.stringTest);
            writer.WriteChar(obj.charTest);
            writer.WriteByte(obj.byteTest);
        }
    }
}