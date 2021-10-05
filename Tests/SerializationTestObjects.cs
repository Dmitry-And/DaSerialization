using System;
using System.Collections.Generic;

namespace DaSerialization.Tests
{
    public interface ITestInterface { }

    [Serializable, TypeId(54876)]
    public class TestObject : ITestInterface
    {
        public bool BoolTest;
        public byte ByteTest;
        public short ShortTest;
        public int IntTest;
        public long LongTest;
        public sbyte SByteTest;
        public uint UIntTest;
        public ulong ULongTest;
        public decimal DecimalTest;
        public float FloatTest;
        public double DoubleTest;
        public char CharTest;
        public char CharASCIITest;
        public string StringTest;
        public string StringASCIITest;
        public char[] CharsTest;
        public char[] CharsASCIITest;
        public byte[] BytesTest;

        public bool[] BoolsTestArray;
        public short[] ShortsTestArray;
        public int[] IntsTestArray;
        public long[] LongsTestArray;
        public sbyte[] SBytesTestArray;
        public uint[] UIntsTestArray;
        public ulong[] ULongsTestArray;
        public decimal[] DecimalsTestArray;
        public float[] FloatsTestArray;
        public double[] DoublesTestArray;
        public string[] StringsTestArray;
        public string[] StringsASCIITestArray;

        public List<bool> BoolsTestList;

        public TestObject TestObj;
        public TopLevelStructure TopLevelStruct;

        public TestObject[] TestObjectsArray;
        public TopLevelStructure[] TopLevelStructsArray;
        public List<TestObject> TestObjectsList;
        public List<TopLevelStructure> TopLevelStructsList;

        public ITestInterface TestInterface;
        public ITestInterface[] TestInterfacesArray;

        public TestObject() 
        {
            BoolTest = true;
            ByteTest = 253;
            ShortTest = 30567;
            IntTest = 2047483647;
            LongTest = 8112261925743664796;
            SByteTest = -125;
            UIntTest = 4021563158;
            ULongTest = 17446744073709551615;
            DecimalTest = 5.012654201m;
            FloatTest = 1.2575646f;
            DoubleTest = 2.457692;
            CharTest = '*';
            CharASCIITest = '#';
            StringTest = "dflkajefciv,;eiq";
            StringASCIITest = "ieqrjmz. 923 ; j3";
            CharsTest = new char[] { 'f', '2', '=' };
            CharsASCIITest = new char[] { '/', '8', 'b' };
            BytesTest = new byte[] { 16, 145, 249 };
            TopLevelStruct = TopLevelStructure.Default;
            //BoolsTestArray = new bool[] { true, true, false };
            //ShortsTestArray = new short[] { 30567, 18457, 29456 };
            //IntsTestArray = new int[] { 2047483647, 1947483647, 1847483647 };
            //LongsTestArray = new long[] { 8112261925743664796, 7912261925743664796, 7712261925743664796 };
            //SBytesTestArray = new sbyte[] { -125, 125, -120 };
            //UIntsTestArray = new uint[] { 4021563158, 3521563158, 2521563158 };
            //ULongsTestArray = new ulong[] { 17446744073709551615, 10446744073709551615, 14446744073709551615 };
            //DecimalsTestArray = new decimal[] { 5.012654201m, 4.112654201m, 2.312654201m };
            //FloatsTestArray = new float[] { 1.2575646f, 0.2575646f, 1.5575646f };
            //DoublesTestArray = new double[] { 2.457692, 1.657692, 0.557692 };
            //StringsTestArray = new string[] { "dflkajefciv,;eiq", "f562", "fjei2mn" };
            //StringsASCIITestArray = new string[] { "ieqrjmz. 923 ; j3", " dfj;efj3", "5425fvadv" };
            BoolsTestList = new List<bool>() { true, false, false };
        }
    }

    public class TopLevelObjectSerializer : AFullSerializer<TestObject>
    {
        public override int Version => 1;        

        public override void ReadDataToObject(ref TestObject obj, BinaryStreamReader reader)
        {
            if (obj == null)
                obj = new TestObject();
            obj.BoolTest = reader.ReadBool("N_Bool");
            obj.ByteTest = reader.ReadByte("N_Byte");
            obj.ShortTest = reader.ReadInt16("N_Int16");
            obj.IntTest = reader.ReadInt32("N_Int32");
            obj.LongTest = reader.ReadInt64("N_Int64");
            obj.SByteTest = reader.ReadSByte("N_SByte");
            obj.UIntTest = reader.ReadUInt32("N_UInt32");
            obj.ULongTest = reader.ReadUInt64("N_UInt64");
            obj.DecimalTest = reader.ReadDecimal("N_Decimal");
            obj.FloatTest = reader.ReadSingle("N_Single");
            obj.DoubleTest = reader.ReadDouble("N_Double");
            obj.CharTest = reader.ReadChar("N_Char");
            obj.CharASCIITest = reader.ReadCharASCII("N_CharASCII");
            obj.StringTest = reader.ReadString("N_String");
            obj.StringASCIITest = reader.ReadStringASCII("N_StringASCII");
            obj.CharsTest = reader.ReadChars(3, "N_Chars");
            obj.CharsASCIITest = reader.ReadCharsASCII(3, "N_CharsASCII");
            obj.BytesTest = reader.ReadBytes(3, "N_Bytes");
            obj.TestObj = reader.ReadObject<TestObject>("N_TestObject");
            obj.TopLevelStruct = reader.ReadObject<TopLevelStructure>("N_TopLevelStructure");
            obj.TestObjectsArray = reader.ReadArray<TestObject>("N_TestObject[]");
            obj.TopLevelStructsArray = reader.ReadArrayExact<TopLevelStructure>("N_TopLevelStructure[]");
            obj.TestObjectsList = reader.ReadList<TestObject>("N_List<TestObject>");
            obj.TopLevelStructsList = reader.ReadListExact<TopLevelStructure>("N_List<TopLevelStructure");
            //obj.BoolsTestArray = reader.ReadArrayExact<bool>("N_Bool[]");
            //obj.ShortsTestArray = reader.ReadArrayExact<Int16>("N_Int16[]");
            //obj.IntsTestArray = reader.ReadArrayExact<Int32>("N_Int32[]");
            //obj.LongsTestArray = reader.ReadArrayExact<Int64>("N_Int64[]");
            //obj.SBytesTestArray = reader.ReadArrayExact<sbyte>("N_SByte[]");
            //obj.UIntsTestArray = reader.ReadArrayExact<UInt32>("N_UInt32[]");
            //obj.ULongsTestArray = reader.ReadArrayExact<UInt64>("N_UInt64[]");
            //obj.DecimalsTestArray = reader.ReadArrayExact<decimal>("N_Decimal[]");
            //obj.FloatsTestArray = reader.ReadArrayExact<float>("N_Float[]");
            //obj.DoublesTestArray = reader.ReadArrayExact<double>("N_Double[]");
            //obj.StringsTestArray = reader.ReadArray<string>("N_String[]");
            //obj.StringsASCIITestArray = reader.ReadArray<string>("N_StringASCII[]");

            obj.TestInterface = reader.ReadObject<ITestInterface>("N_ITestInterface");
            obj.TestInterfacesArray = reader.ReadArray<ITestInterface>("N_ITestInterface[]");
        }

        public override void WriteObject(TestObject obj, BinaryStreamWriter writer)
        {
            writer.WriteBool(obj.BoolTest);
            writer.WriteByte(obj.ByteTest);
            writer.WriteInt16(obj.ShortTest);
            writer.WriteInt32(obj.IntTest);
            writer.WriteInt64(obj.LongTest);
            writer.WriteSByte(obj.SByteTest);
            writer.WriteUInt32(obj.UIntTest);
            writer.WriteUInt64(obj.ULongTest);
            writer.WriteDecimal(obj.DecimalTest);
            writer.WriteSingle(obj.FloatTest);
            writer.WriteDouble(obj.DoubleTest);
            writer.WriteChar(obj.CharTest);
            writer.WriteCharASCII(obj.CharASCIITest);
            writer.WriteString(obj.StringTest);
            writer.WriteStringASCII(obj.StringASCIITest);
            writer.WriteChars(obj.CharsTest);
            writer.WriteCharsASCII(obj.CharsASCIITest);
            writer.WriteBytes(obj.BytesTest);
            writer.WriteObject(obj.TestObj);
            writer.WriteObject(obj.TopLevelStruct);
            writer.WriteArray(obj.TestObjectsArray);
            writer.WriteArrayExact(obj.TopLevelStructsArray);
            writer.WriteList(obj.TestObjectsList);
            writer.WriteListExact(obj.TopLevelStructsList);
            //writer.WriteArrayExact(obj.BoolsTestArray);
            //writer.WriteArrayExact(obj.ShortsTestArray);
            //writer.WriteArrayExact(obj.IntsTestArray);
            //writer.WriteArrayExact(obj.LongsTestArray);
            //writer.WriteArrayExact(obj.SBytesTestArray);
            //writer.WriteArrayExact(obj.UIntsTestArray);
            //writer.WriteArrayExact(obj.ULongsTestArray);
            //writer.WriteArrayExact(obj.DecimalsTestArray);
            //writer.WriteArrayExact(obj.FloatsTestArray);
            //writer.WriteArrayExact(obj.DoublesTestArray);
            //writer.WriteArray(obj.StringsTestArray);
            //writer.WriteArray(obj.StringsASCIITestArray);

            writer.WriteObject(obj.TestInterface);
            writer.WriteArray(obj.TestInterfacesArray);
        }
    }

    [Serializable, TypeId(1546853)]
    public struct TopLevelStructure
    {
        public bool BoolTest;
        public byte ByteTest;
        public short ShortTest;
        public int IntTest;
        public long LongTest;
        public sbyte SByteTest;
        public uint UIntTest;
        public ulong ULongTest;
        public decimal DecimalTest;
        public float FloatTest;
        public double DoubleTest;
        public char CharTest;
        public char CharASCIITest;
        public string StringTest;
        public string StringASCIITest;
        public char[] CharsTest;
        public char[] CharsASCIITest;
        public byte[] BytesTest;

        public BottomLevelStructure BottomLevelStruct;
        public TestObject TestObj;

        public TestObject[] TestObjectsArray;
        public TopLevelStructure[] TopLevelStructsArray;
        public List<TestObject> TestObjectsList;
        public List<TopLevelStructure> TopLevelStructsList;

        public static TopLevelStructure Default
            => new TopLevelStructure() 
            {
            BoolTest = true,
            ByteTest = 251,
            ShortTest = 29678,
            IntTest = 1947483647,
            LongTest = 8002261925743664756,
            SByteTest = -123,
            UIntTest = 3921563219,
            ULongTest = 17576821435769000024,
            DecimalTest = 4.1222541012m,
            FloatTest = 2.75557986f,
            DoubleTest = 3.5484573,
            CharTest = '+',
            CharASCIITest = '!',
            StringTest = "qericmajb@@1jkb.  9;kjn",
            StringASCIITest = "]['lwqhfaje2 32",
            CharsTest = new char[] { '-', '5', 'f' },
            CharsASCIITest = new char[] { '3', 'e', '+' },
            BytesTest = new byte[] { 2, 99, 254 },
            BottomLevelStruct = BottomLevelStructure.Default,
            };    
    }

    public class TopLevelStructureSerializer : AFullSerializer<TopLevelStructure>
    {
        public override int Version => 1;

        public override void ReadDataToObject(ref TopLevelStructure obj, BinaryStreamReader reader)
        {
            obj.BoolTest = reader.ReadBool("N_Bool");
            obj.ByteTest = reader.ReadByte("N_Byte");
            obj.ShortTest = reader.ReadInt16("N_Int16");
            obj.IntTest = reader.ReadInt32("N_Int32");
            obj.LongTest = reader.ReadInt64("N_Int64");
            obj.SByteTest = reader.ReadSByte("N_SByte");
            obj.UIntTest = reader.ReadUInt32("N_UInt32");
            obj.ULongTest = reader.ReadUInt64("N_UInt64");
            obj.DecimalTest = reader.ReadDecimal("N_Decimal");
            obj.FloatTest = reader.ReadSingle("N_Single");
            obj.DoubleTest = reader.ReadDouble("N_Double");
            obj.CharTest = reader.ReadChar("N_Char");
            obj.CharASCIITest = reader.ReadCharASCII("N_CharASCII");
            obj.StringTest = reader.ReadString("N_String");
            obj.StringASCIITest = reader.ReadStringASCII("N_StringASCII");
            obj.CharsTest = reader.ReadChars(3, "N_Chars");
            obj.CharsASCIITest = reader.ReadCharsASCII(3, "N_CharsASCII");
            obj.BytesTest = reader.ReadBytes(3, "N_Bytes");
            obj.BottomLevelStruct = reader.ReadObject<BottomLevelStructure>("N_BottomLevelStructure");
            obj.TestObj = reader.ReadObject<TestObject>("N_TestObject");
            obj.TestObjectsArray = reader.ReadArray<TestObject>("N_TestObject[]");
            obj.TopLevelStructsArray = reader.ReadArrayExact<TopLevelStructure>("N_TopLevelStructure[]");
            obj.TestObjectsList = reader.ReadList<TestObject>("N_List<TestObject>");
            obj.TopLevelStructsList = reader.ReadListExact<TopLevelStructure>("N_List<TopLevelStructure");
        }

        public override void WriteObject(TopLevelStructure obj, BinaryStreamWriter writer)
        {
            writer.WriteBool(obj.BoolTest);
            writer.WriteByte(obj.ByteTest);
            writer.WriteInt16(obj.ShortTest);
            writer.WriteInt32(obj.IntTest);
            writer.WriteInt64(obj.LongTest);
            writer.WriteSByte(obj.SByteTest);
            writer.WriteUInt32(obj.UIntTest);
            writer.WriteUInt64(obj.ULongTest);
            writer.WriteDecimal(obj.DecimalTest);
            writer.WriteSingle(obj.FloatTest);
            writer.WriteDouble(obj.DoubleTest);
            writer.WriteChar(obj.CharTest);
            writer.WriteCharASCII(obj.CharASCIITest);
            writer.WriteString(obj.StringTest);
            writer.WriteStringASCII(obj.StringASCIITest);
            writer.WriteChars(obj.CharsTest);
            writer.WriteCharsASCII(obj.CharsASCIITest);
            writer.WriteBytes(obj.BytesTest);
            writer.WriteObject(obj.BottomLevelStruct);
            writer.WriteObject(obj.TestObj);
            writer.WriteArray(obj.TestObjectsArray);
            writer.WriteArrayExact(obj.TopLevelStructsArray);
            writer.WriteList(obj.TestObjectsList);
            writer.WriteListExact(obj.TopLevelStructsList);
        }
    }

    [Serializable, TypeId(5763879)]
    public struct BottomLevelStructure : ITestInterface
    {
        public bool BoolTest;
        public byte ByteTest;
        public short ShortTest;
        public int IntTest;
        public long LongTest;
        public sbyte SByteTest;
        public uint UIntTest;
        public ulong ULongTest;
        public decimal DecimalTest;
        public float FloatTest;
        public double DoubleTest;
        public char CharTest;
        public char CharASCIITest;
        public string StringTest;
        public string StringASCIITest;
        public char[] CharsTest;
        public char[] CharsASCIITest;
        public byte[] BytesTest;

        public bool[] BoolsTestArray;
        public short[] ShortsTestArray;
        public int[] IntsTestArray;
        public long[] LongsTestArray;
        public sbyte[] SBytesTestArray;
        public uint[] UIntsTestArray;
        public ulong[] ULongsTestArray;
        public decimal[] DecimalsTestArray;
        public float[] FloatsTestArray;
        public double[] DoublesTestArray;
        public string[] StringsTestArray;
        public string[] StringsASCIITestArray;

        public static BottomLevelStructure Default
            => new BottomLevelStructure()
            {
                BoolTest = true,
                ByteTest = 252,
                ShortTest = 29611,
                IntTest = 1947483603,
                LongTest = 8002261925743664776,
                SByteTest = -120,
                UIntTest = 3921563275,
                ULongTest = 17576821435769000076,
                DecimalTest = 4.1222541087m,
                FloatTest = 2.75557999f,
                DoubleTest = 3.5484532,
                CharTest = '-',
                CharASCIITest = '&',
                StringTest = "jqpi38vnm//7856",
                StringASCIITest = "fj2mnz/123/*-",
                CharsTest = new char[] { '*', '0', 'q' },
                CharsASCIITest = new char[] { '9', 's', '>' },
                BytesTest = new byte[] { 7, 100, 252 },
                //BoolsTestArray = new bool[] { true, true, false },
                //ShortsTestArray = new short[] { 30567, 18457, 29456 },
                //IntsTestArray = new int[] { 2047483647, 1947483647, 1847483647 },
                //LongsTestArray = new long[] { 8112261925743664796, 7912261925743664796, 7712261925743664796 },
                //SBytesTestArray = new sbyte[] { -125, 125, -120 },
                //UIntsTestArray = new uint[] { 4021563158, 3521563158, 2521563158 },
                //ULongsTestArray = new ulong[] { 17446744073709551615, 10446744073709551615, 14446744073709551615 },
                //DecimalsTestArray = new decimal[] { 5.012654201m, 4.112654201m, 2.312654201m },
                //FloatsTestArray = new float[] { 1.2575646f, 0.2575646f, 1.5575646f },
                //DoublesTestArray = new double[] { 2.457692, 1.657692, 0.557692 },
                //StringsTestArray = new string[] { "dflkajefciv,;eiq", "f562", "fjei2mn" },
                //StringsASCIITestArray = new string[] { "ieqrjmz. 923 ; j3", " dfj;efj3", "5425fvadv" },
            };
    }

    public class BottomLevelStructureSerializer : AFullSerializer<BottomLevelStructure>
    {
        public override int Version => 1;

        public override void ReadDataToObject(ref BottomLevelStructure obj, BinaryStreamReader reader)
        {
            obj.BoolTest = reader.ReadBool("N_Bool");
            obj.ByteTest = reader.ReadByte("N_Byte");
            obj.ShortTest = reader.ReadInt16("N_Int16");
            obj.IntTest = reader.ReadInt32("N_Int32");
            obj.LongTest = reader.ReadInt64("N_Int64");
            obj.SByteTest = reader.ReadSByte("N_SByte");
            obj.UIntTest = reader.ReadUInt32("N_UInt32");
            obj.ULongTest = reader.ReadUInt64("N_UInt64");
            obj.DecimalTest = reader.ReadDecimal("N_Decimal");
            obj.FloatTest = reader.ReadSingle("N_Single");
            obj.DoubleTest = reader.ReadDouble("N_Double");
            obj.CharTest = reader.ReadChar("N_Char");
            obj.CharASCIITest = reader.ReadCharASCII("N_CharASCII");
            obj.StringTest = reader.ReadString("N_String");
            obj.StringASCIITest = reader.ReadStringASCII("N_StringASCII");
            obj.CharsTest = reader.ReadChars(3, "N_Chars");
            obj.CharsASCIITest = reader.ReadCharsASCII(3, "N_CharsASCII");
            obj.BytesTest = reader.ReadBytes(3, "N_Bytes");
            //obj.BoolsTestArray = reader.ReadArrayExact<bool>("N_Bool[]");
            //obj.ShortsTestArray = reader.ReadArrayExact<Int16>("N_Int16[]");
            //obj.IntsTestArray = reader.ReadArrayExact<Int32>("N_Int32[]");
            //obj.LongsTestArray = reader.ReadArrayExact<Int64>("N_Int64[]");
            //obj.SBytesTestArray = reader.ReadArrayExact<sbyte>("N_SByte[]");
            //obj.UIntsTestArray = reader.ReadArrayExact<UInt32>("N_UInt32[]");
            //obj.ULongsTestArray = reader.ReadArrayExact<UInt64>("N_UInt64[]");
            //obj.DecimalsTestArray = reader.ReadArrayExact<decimal>("N_Decimal[]");
            //obj.FloatsTestArray = reader.ReadArrayExact<float>("N_Float[]");
            //obj.DoublesTestArray = reader.ReadArrayExact<double>("N_Double[]");
            //obj.StringsTestArray = reader.ReadArray<string>("N_String[]");
            //obj.StringsASCIITestArray = reader.ReadArray<string>("N_StringASCII[]");
        }

        public override void WriteObject(BottomLevelStructure obj, BinaryStreamWriter writer)
        {
            writer.WriteBool(obj.BoolTest);
            writer.WriteByte(obj.ByteTest);
            writer.WriteInt16(obj.ShortTest);
            writer.WriteInt32(obj.IntTest);
            writer.WriteInt64(obj.LongTest);
            writer.WriteSByte(obj.SByteTest);
            writer.WriteUInt32(obj.UIntTest);
            writer.WriteUInt64(obj.ULongTest);
            writer.WriteDecimal(obj.DecimalTest);
            writer.WriteSingle(obj.FloatTest);
            writer.WriteDouble(obj.DoubleTest);
            writer.WriteChar(obj.CharTest);
            writer.WriteCharASCII(obj.CharASCIITest);
            writer.WriteString(obj.StringTest);
            writer.WriteStringASCII(obj.StringASCIITest);
            writer.WriteChars(obj.CharsTest);
            writer.WriteCharsASCII(obj.CharsASCIITest);
            writer.WriteBytes(obj.BytesTest);
            //writer.WriteArrayExact(obj.BoolsTestArray);
            //writer.WriteArrayExact(obj.ShortsTestArray);
            //writer.WriteArrayExact(obj.IntsTestArray);
            //writer.WriteArrayExact(obj.LongsTestArray);
            //writer.WriteArrayExact(obj.SBytesTestArray);
            //writer.WriteArrayExact(obj.UIntsTestArray);
            //writer.WriteArrayExact(obj.ULongsTestArray);
            //writer.WriteArrayExact(obj.DecimalsTestArray);
            //writer.WriteArrayExact(obj.FloatsTestArray);
            //writer.WriteArrayExact(obj.DoublesTestArray);
            //writer.WriteArray(obj.StringsTestArray);
            //writer.WriteArray(obj.StringsASCIITestArray);
        }
    }
}