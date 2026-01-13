using System;
using System.IO;
using System.Text;

namespace XMLDocNormalizer.IO
{
    /// <summary>
    /// Provides text file I/O helpers that preserve the original file encoding and BOM.
    /// </summary>
    internal static class FileText
    {
        /// <summary>
        /// Reads a text file and returns its content along with the detected encoding and BOM flag.
        /// </summary>
        /// <param name="path">The file path to read.</param>
        /// <param name="encoding">The detected encoding.</param>
        /// <param name="hasBom">True if the file started with a BOM; otherwise false.</param>
        /// <returns>The file content.</returns>
        public static string ReadAllTextPreserveEncoding(string path, out Encoding encoding, out bool hasBom)
        {
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
            {
                string text = reader.ReadToEnd();
                encoding = reader.CurrentEncoding;
                hasBom = StartsWithBom(encoding, stream);
                return text;
            }
        }

        /// <summary>
        /// Writes text to a file using the specified encoding and BOM behavior.
        /// </summary>
        /// <param name="path">The file path to write.</param>
        /// <param name="text">The text to write.</param>
        /// <param name="encoding">The encoding to use.</param>
        /// <param name="hasBom">True to write a BOM if the encoding supports it.</param>
        public static void WriteAllTextPreserveEncoding(string path, string text, Encoding encoding, bool hasBom)
        {
            Encoding finalEncoding = GetEncodingWithBomBehavior(encoding, hasBom);

            using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            using (StreamWriter writer = new StreamWriter(stream, finalEncoding))
            {
                writer.Write(text);
            }
        }

        /// <summary>
        /// Adjusts encodings that have configurable BOM emission (notably UTF-8).
        /// </summary>
        /// <param name="encoding">The original encoding.</param>
        /// <param name="hasBom">True to emit a BOM; otherwise false.</param>
        /// <returns>An encoding instance that matches the desired BOM behavior.</returns>
        private static Encoding GetEncodingWithBomBehavior(Encoding encoding, bool hasBom)
        {
            if (encoding is UTF8Encoding)
            {
                return new UTF8Encoding(encoderShouldEmitUTF8Identifier: hasBom);
            }

            // For UTF-16/UTF-32 encodings, BOM emission is typically inherent to the encoding type.
            // For other encodings, BOM is not applicable.
            return encoding;
        }

        /// <summary>
        /// Checks whether the file begins with the BOM/preamble of the given encoding.
        /// </summary>
        /// <param name="encoding">The detected encoding.</param>
        /// <param name="stream">The file stream used for reading.</param>
        /// <returns>True if the file begins with the encoding preamble; otherwise false.</returns>
        private static bool StartsWithBom(Encoding encoding, FileStream stream)
        {
            // We can only reliably test the original bytes if we look at the stream start.
            // StreamReader will have advanced the position, so we must re-check at beginning.
            long oldPos = stream.Position;
            stream.Position = 0;

            byte[] preamble = encoding.GetPreamble();
            bool has = false;

            if (preamble.Length > 0)
            {
                byte[] buffer = new byte[preamble.Length];
                int read = stream.Read(buffer, 0, buffer.Length);

                if (read == preamble.Length)
                {
                    has = true;
                    for (int i = 0; i < preamble.Length; i++)
                    {
                        if (buffer[i] != preamble[i])
                        {
                            has = false;
                            break;
                        }
                    }
                }
            }

            stream.Position = oldPos;
            return has;
        }
    }
}
