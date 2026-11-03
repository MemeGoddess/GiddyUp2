#nullable disable
using System.IO;
using System.IO.Compression;
using UnityEngine;

namespace GiddyUp.Editor
{
    public static class Png
    {
        private static uint[] crcTable;

        public static byte[] Encode(Color32[] bottomUp, int width, int height)
        {
            var raw = new byte[(width * 4 + 1) * height];
            int o = 0;
            for (int row = 0; row < height; row++)
            {
                raw[o++] = 0;
                int src = (height - 1 - row) * width;
                for (int x = 0; x < width; x++)
                {
                    var c = bottomUp[src + x];
                    raw[o++] = c.r;
                    raw[o++] = c.g;
                    raw[o++] = c.b;
                    raw[o++] = c.a;
                }
            }
            byte[] packed;
            using (var ms = new MemoryStream())
            {
                using (var z = new ZLibStream(ms, CompressionLevel.Optimal, true))
                {
                    z.Write(raw, 0, raw.Length);
                }
                packed = ms.ToArray();
            }
            using (var outStream = new MemoryStream())
            {
                outStream.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, 0, 8);
                var ihdr = new byte[13];
                Put(ihdr, 0, (uint)width);
                Put(ihdr, 4, (uint)height);
                ihdr[8] = 8;
                ihdr[9] = 6;
                Chunk(outStream, "IHDR", ihdr);
                Chunk(outStream, "IDAT", packed);
                Chunk(outStream, "IEND", new byte[0]);
                return outStream.ToArray();
            }
        }

        private static void Put(byte[] b, int at, uint v)
        {
            b[at] = (byte)(v >> 24);
            b[at + 1] = (byte)(v >> 16);
            b[at + 2] = (byte)(v >> 8);
            b[at + 3] = (byte)v;
        }

        private static void Chunk(Stream s, string type, byte[] data)
        {
            var len = new byte[4];
            Put(len, 0, (uint)data.Length);
            s.Write(len, 0, 4);
            var typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
            s.Write(typeBytes, 0, 4);
            s.Write(data, 0, data.Length);
            uint crc = Crc(typeBytes, 0xFFFFFFFFu);
            crc = Crc(data, crc) ^ 0xFFFFFFFFu;
            var c = new byte[4];
            Put(c, 0, crc);
            s.Write(c, 0, 4);
        }

        private static uint Crc(byte[] data, uint crc)
        {
            if (crcTable == null)
            {
                crcTable = new uint[256];
                for (uint n = 0; n < 256; n++)
                {
                    uint c = n;
                    for (int k = 0; k < 8; k++)
                    {
                        c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
                    }
                    crcTable[n] = c;
                }
            }
            for (int i = 0; i < data.Length; i++)
            {
                crc = crcTable[(crc ^ data[i]) & 0xFF] ^ (crc >> 8);
            }
            return crc;
        }
    }
}
