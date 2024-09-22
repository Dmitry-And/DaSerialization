using System.Text;
using Tests;

namespace DaSerialization.Tests
{
    public class PackedSerializationTests
    {
        [Test(-111)]
        private static bool PackedStringSerialization()
        {
            var ms = new BinaryStream(SerializerStorage.Default);
            var writer = new BinaryStreamWriter(ms);
            var sb = new StringBuilder(128);
            for (int i = 31; i < 127; i++)
            {
                int length = i - 31;
                char symbol = (char)i;
                sb.Clear();
                sb.Append((char)31);
                sb.Append((char)127);
                for (int j = 0; j < length; j++)
                    sb.Append(symbol);
                sb.Append((char)31);
                sb.Append((char)127);
                var s = sb.ToString();

                writer.WriteStringASCIIPacked(s);
            }
            writer.WriteStringASCIIPacked(null);

            ms.Seek(0);
            var reader = new BinaryStreamReader(ms);
            ms.Seek(ms.ZeroPosition);

            for (int i = 31; i < 127; i++)
            {
                int length = i - 31;
                char symbol = (char)i;
                sb.Clear();
                for (int j = 0; j < length; j++)
                    sb.Append(symbol);
                var s = sb.ToString();

                TestUtils.AssertEqual(s, reader.ReadStringASCIIPacked());
            }
            TestUtils.AssertEqual(null, reader.ReadStringASCIIPacked());

            return true;
        }

        [Test(-110)]
        private static bool PackedUIntSerialization()
        {
            var ms = new BinaryStream(SerializerStorage.Default);
            var writer = new BinaryStreamWriter(ms);
            for (int i = 0; i < 63; i++)
            {
                ulong unsigned = 1UL << i;
                writer.WriteUIntPacked(unsigned - 1);
                writer.WriteUIntPacked(unsigned);
                writer.WriteUIntPacked(unsigned + 1);

                var signed = (long)unsigned;
                writer.WriteIntPacked(signed - 1);
                writer.WriteIntPacked(signed);
                writer.WriteIntPacked(signed + 1);
            }

            ms.Seek(0);
            var reader = new BinaryStreamReader(ms);
            ms.Seek(ms.ZeroPosition);
            var oldPos = ms.Position;
            for (int i = 0; i < 63; i++)
            {
                ulong unsigned = 1UL << i;
                TestUtils.AssertEqual(unsigned - 1, reader.ReadUIntPacked());
                TestUtils.Assert(ms.Position - oldPos == PackingUtils.GetPackedUIntSize(unsigned - 1)); oldPos = ms.Position;
                TestUtils.AssertEqual(unsigned, reader.ReadUIntPacked());
                TestUtils.Assert(ms.Position - oldPos == PackingUtils.GetPackedUIntSize(unsigned)); oldPos = ms.Position;
                TestUtils.AssertEqual(unsigned + 1, reader.ReadUIntPacked());
                TestUtils.Assert(ms.Position - oldPos == PackingUtils.GetPackedUIntSize(unsigned + 1)); oldPos = ms.Position;

                var signed = (long)unsigned;
                TestUtils.AssertEqual(signed - 1, reader.ReadIntPacked());
                TestUtils.Assert(ms.Position - oldPos == PackingUtils.GetPackedIntSize(signed - 1)); oldPos = ms.Position;
                TestUtils.AssertEqual(signed, reader.ReadIntPacked());
                TestUtils.Assert(ms.Position - oldPos == PackingUtils.GetPackedIntSize(signed)); oldPos = ms.Position;
                TestUtils.AssertEqual(signed + 1, reader.ReadIntPacked());
                TestUtils.Assert(ms.Position - oldPos == PackingUtils.GetPackedIntSize(signed + 1)); oldPos = ms.Position;
            }
            return true;
        }

    }
}