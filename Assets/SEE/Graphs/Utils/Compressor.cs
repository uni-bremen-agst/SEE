using System;
using System.IO;
using Joveler.Compression.XZ;

namespace SEE.Graphs.Utils
{
    /// <summary>
    /// Allows to compress and uncompress data.
    ///
    /// Compression relies on the native library liblzma. Where that library is
    /// located depends on the host application, which is why this class does not
    /// look it up itself. The host must call <see cref="Initialize(string)"/>
    /// before any data is compressed or uncompressed.
    /// </summary>
    public static class Compressor
    {
        /// <summary>
        /// Initializes the native liblzma library located at <paramref name="liblzmaPath"/>.
        /// Calling this method more than once is harmless; only the first call has an effect.
        /// </summary>
        /// <param name="liblzmaPath">Path to the native liblzma library.</param>
        public static void Initialize(string liblzmaPath)
        {
            try
            {
                XZInit.GlobalInit(liblzmaPath);
            }
            catch (InvalidOperationException e) when (e.Message.Contains(" is already initialized"))
            {
                // Already loaded. We can ignore this.
            }
        }

        /// <summary>
        /// Returns true if <paramref name="filename"/> has a file extension indicating
        /// compression.
        /// </summary>
        /// <param name="filename">Filename to be tested.</param>
        /// <returns>True if <paramref name="filename"/> has a file extension
        /// <see cref="Filenames.CompressedExtension"/>.</returns>
        public static bool IsCompressed(string filename)
        {
            return filename.ToLower().EndsWith(Filenames.CompressedExtension);
        }

        /// <summary>
        /// Opens the file with given <paramref name="filename"/> and returns it as a <see cref="Stream"/>.
        /// If <paramref name="filename"/> has the filename extension
        /// <see cref="Filenames.CompressedExtension"/>, the stream will be the
        /// uncompressed content of the open file; otherwise it will be the content
        /// of the file as is.
        /// </summary>
        /// <param name="filename">Name of the file to be opened.</param>
        /// <returns>Stream of the (possibly uncompressed) content of the opened file.</returns>
        public static Stream Uncompress(string filename)
        {
            FileStream stream = File.OpenRead(filename);
            if (IsCompressed(filename))
            {
                return Uncompress(stream);
            }
            else
            {
                return stream;
            }
        }

        /// <summary>
        /// Returns the uncompressed content of <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">Stream containing compressed data.</param>
        /// <returns>Uncompressed content.</returns>
        public static Stream Uncompress(Stream stream)
        {
            // Handle compressed LZMA2 file.
            XZDecompressOptions options = new()
            {
                LeaveOpen = false
            };
            return new XZStream(stream, options);
        }

        /// <summary>
        /// Saves content of <paramref name="source"/> to a new file named
        /// <paramref name="filename"/>. If <paramref name="filename"/> has
        /// a file extension indicating compression, the file will be
        /// compressed. Otherwise it will be saved without compression.
        /// </summary>
        /// <param name="filename">The name (and path) of the file to create.</param>
        /// <param name="source">The source stream whose content will be written to the file.</param>
        public static void Save(string filename, Stream source)
        {
            Stream fileStream = new FileStream(filename, FileMode.Create);
            if (IsCompressed(filename))
            {
                // Compress to XZ, if necessary.
                XZCompressOptions options = new()
                {
                    LeaveOpen = false
                };
                fileStream = new XZStream(fileStream, options);
            }
            source.CopyTo(fileStream);
            fileStream.Close();
        }

        /// <summary>
        /// Saves the content of <paramref name="sourceFile"/> (possibly compressed) in
        /// <paramref name="targetFile"/> based on the files' extensions.
        ///
        /// Wrapper method for <see cref="Compressor.Save(string, Stream)"/>.
        /// </summary>
        /// <param name="sourceFile">The file which should be saved.</param>
        /// <param name="targetFile">The file to write the content of <paramref name="sourceFile"/> to.</param>
        public static void Save(string sourceFile, string targetFile)
            => Save(targetFile, new MemoryStream(File.ReadAllBytes(sourceFile)));
    }
}
