using SiliconStudio.Core.Serialization;
using System;

namespace Clockwork
{
    public static class SerializationStreamExtensions
    {
        public static int ReadPackedInt(this SerializationStream stream)
        {
            // Read out an Int32 7 bits at a time.  The high bit 
            // of the byte when on means to continue reading more bytes.
            int count = 0;
            int shift = 0;
            byte b;
            do
            {
                // Check for a corrupted stream.  Read a max of 5 bytes. 
                // In a future version, add a DataFormatException. 
                if (shift == 5 * 7)  // 5 bytes max per Int32, shift += 7
                    throw new FormatException("Bad string length. 7bit Int32 format");

                // ReadByte handles end of stream cases for us.
                b = stream.ReadByte();
                count |= (b & 0x7F) << shift;
                shift += 7;
            }
            while ((b & 0x80) != 0);

            return count;
        }

        public static void WritePackedInt(this SerializationStream stream, int value)
        {
            // Write out an int 7 bits at a time.  The high bit of the byte, 
            // when on, tells reader to continue reading more bytes. 
            uint v = (uint)value;   // support negative numbers
            while (v >= 0x80)
            {
                stream.Write((byte)(v | 0x80));
                v >>= 7;
            }
            stream.Write((byte)v);
        }
    }
}
