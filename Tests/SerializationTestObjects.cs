using System;

namespace DaSerialization.Tests
{
    [Serializable, TypeId(54876)]
    public class TestObject
    {
        public int intTest;
        public double doubleTest;
        public short shortTest;
        public string stringTest;

        public TestObject() 
        {
            intTest = 2047483647;
            doubleTest = 2.457692;
            shortTest = 30567;
            stringTest = "dflkajefciv,;eiq";
        }
    }

    public class TopLevelObjectSerializer : AFullSerializer<TestObject>
    {
        public override int Version => 1;        

        public override void ReadDataToObject(ref TestObject obj, BinaryStreamReader reader)
        {
            if (obj == null)
                obj = new TestObject();
            obj.intTest = reader.ReadInt32("N_Int32");
            obj.doubleTest = reader.ReadDouble("N_Double");
            obj.shortTest = reader.ReadInt16("N_Int16");
            obj.stringTest = reader.ReadString("N_String");
        }

        public override void WriteObject(TestObject obj, BinaryStreamWriter writer)
        {
            writer.WriteInt32(obj.intTest);
            writer.WriteDouble(obj.doubleTest);
            writer.WriteInt16(obj.shortTest);
            writer.WriteString(obj.stringTest);
        }
    }

    [Serializable, TypeId(1546853)]
    public struct TopLevelStructure
    {
        public int intTest;
        public string stringTest;
        public char charTest;
        public byte byteTest;

        public TopLevelStructure(int intVal)
        {
            intTest = intVal;
            stringTest = "   qeri.kjnvj2DFJE";
            charTest = '-';
            byteTest = 247;
        }
    }

    public class TopLevelStructureSerializer : AFullSerializer<TopLevelStructure>
    {
        public override int Version => 1;

        public override void ReadDataToObject(ref TopLevelStructure obj, BinaryStreamReader reader)
        {
            obj.intTest = reader.ReadInt32("N_Int32");
            obj.stringTest = reader.ReadString("N_String");
            obj.charTest = reader.ReadChar("N_Char");
            obj.byteTest = reader.ReadByte("N_Byte");
        }

        public override void WriteObject(TopLevelStructure obj, BinaryStreamWriter writer)
        {
            writer.WriteInt32(obj.intTest);
            writer.WriteString(obj.stringTest);
            writer.WriteChar(obj.charTest);
            writer.WriteByte(obj.byteTest);
        }
    }
}