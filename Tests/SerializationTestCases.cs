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
        public override int Version => 1;

        public override void ReadDataToObject(ref TopLevelObject obj, BinaryStreamReader reader)
        {
            if (obj == null)
                obj = new TopLevelObject();
            obj.intTest = reader.ReadInt32();
            obj.doubleTest = reader.ReadDouble();
            obj.shortTest = reader.ReadInt16();
            obj.stringTest = reader.ReadString();
        }

        public override void WriteObject(TopLevelObject obj, BinaryStreamWriter writer)
        {
            writer.WriteInt32(obj.intTest);
            writer.WriteDouble(obj.doubleTest);
            writer.WriteInt16(obj.shortTest);
            writer.WriteString(obj.stringTest);
        }
    }
}