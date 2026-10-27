using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;

namespace B25BEvidenceStorage
{
    // Storage tooling only; no product, test, Roslyn or Worker dependency.
    public static class ArchiveCodec
    {
        private static readonly byte[] Header = { 31, 139, 8, 0, 0, 0, 0, 0, 2, 255 };
        private static readonly uint[] CrcTable = MakeTable();
        private static uint[] MakeTable()
        {
            uint[] table = new uint[256];
            for (uint i = 0; i < 256; i++)
            {
                uint c = i;
                for (int bit = 0; bit < 8; bit++) c = (c & 1) != 0 ? 0xedb88320U ^ (c >> 1) : c >> 1;
                table[i] = c;
            }
            return table;
        }
        private static uint Update(uint crc, byte[] bytes, int count)
        {
            for (int i = 0; i < count; i++) crc = CrcTable[(crc ^ bytes[i]) & 255] ^ (crc >> 8);
            return crc;
        }
        private static void UInt32(Stream output, uint value)
        {
            for (int i = 0; i < 4; i++) output.WriteByte((byte)(value >> (i * 8)));
        }
        private static uint ReadUInt32(Stream input)
        {
            uint result = 0;
            for (int i = 0; i < 4; i++) { int b = input.ReadByte(); if (b < 0) throw new InvalidDataException("Missing gzip trailer"); result |= (uint)b << (i * 8); }
            return result;
        }
        public static void Compress(string source, string archive)
        {
            using (FileStream input = File.OpenRead(source))
            using (FileStream output = new FileStream(archive, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                output.Write(Header, 0, Header.Length);
                uint crc = 0xffffffffU; long size = 0; byte[] buffer = new byte[65536]; int n;
                using (DeflateStream deflate = new DeflateStream(output, CompressionLevel.Optimal, true))
                    while ((n = input.Read(buffer, 0, buffer.Length)) != 0) { deflate.Write(buffer, 0, n); crc = Update(crc, buffer, n); size += n; }
                UInt32(output, ~crc); UInt32(output, unchecked((uint)size)); output.Flush(true);
            }
        }
        // Independently decode the RFC1952 stream, verify explicit CRC/ISIZE and RAW SHA256.
        // No raw 90-MiB file is materialized just to verify it.
        public static void Verify(string archive, string expectedSha256, long expectedSize)
        {
            uint expectedCrc, expectedModuloSize;
            using (FileStream input = File.OpenRead(archive))
            {
                if (input.Length < 18) throw new InvalidDataException("Short gzip");
                for (int i = 0; i < Header.Length; i++) if (input.ReadByte() != Header[i]) throw new InvalidDataException("Noncanonical gzip header");
                input.Position = input.Length - 8; expectedCrc = ReadUInt32(input); expectedModuloSize = ReadUInt32(input);
            }
            using (FileStream input = File.OpenRead(archive))
            using (GZipStream gzip = new GZipStream(input, CompressionMode.Decompress))
            using (SHA256 sha = SHA256.Create())
            {
                uint crc = 0xffffffffU; long size = 0; byte[] buffer = new byte[65536]; int n;
                while ((n = gzip.Read(buffer, 0, buffer.Length)) != 0)
                { sha.TransformBlock(buffer, 0, n, buffer, 0); crc = Update(crc, buffer, n); size += n; }
                sha.TransformFinalBlock(new byte[0], 0, 0);
                string actual = BitConverter.ToString(sha.Hash).Replace("-", "");
                if (size != expectedSize || unchecked((uint)size) != expectedModuloSize || ~crc != expectedCrc || !String.Equals(actual, expectedSha256, StringComparison.Ordinal))
                    throw new InvalidDataException("Raw size, CRC32 or SHA256 mismatch");
            }
        }
        public static void Expand(string archive, string output, string expectedSha256, long expectedSize)
        {
            Verify(archive, expectedSha256, expectedSize);
            using (FileStream input = File.OpenRead(archive))
            using (GZipStream gzip = new GZipStream(input, CompressionMode.Decompress))
            using (FileStream target = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                gzip.CopyTo(target);
        }
    }
}
