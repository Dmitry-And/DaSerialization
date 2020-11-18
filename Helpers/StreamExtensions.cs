using System.IO;

namespace DaSerialization.Internal
{
    public static class StreamExtensions
    {
        private const int CopyBufferSize = 4096;

        private static byte[] _buffer = new byte[CopyBufferSize];
        public static int CopyPartiallyTo(this Stream s, Stream destination, int length)
        {
            int readTotal = 0;
            do
            {
                int toRead = CopyBufferSize < length ? CopyBufferSize : length;
                int read = s.Read(_buffer, 0, toRead);
                if (read <= 0)
                    return readTotal;
                readTotal += read;
                length -= read;
                destination.Write(_buffer, 0, read);
            } while (length > 0);
            return readTotal;
        }
    }
}