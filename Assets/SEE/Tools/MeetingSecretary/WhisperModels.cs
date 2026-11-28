using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SEE.Tools.MeetingSecretary
{
    /// <summary>
    /// Knowledge about the Whisper model files: where to download them and
    /// how to verify them.
    /// </summary>
    public static class WhisperModels
    {
        /// <summary>
        /// The URL of the folder containing the model files: the official
        /// Hugging Face repository of whisper.cpp.
        /// </summary>
        private const string baseUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/";

        /// <summary>
        /// The SHA-256 checksums (lower-case hexadecimal) of the model files we know.
        /// </summary>
        /// <remarks>
        /// Source: the Git LFS object IDs of the files in the Hugging Face repository
        /// ggerganov/whisper.cpp at commit 5359861c739e955e79d9a303bcbc70fb988958b1,
        /// as reported by https://huggingface.co/api/models/ggerganov/whisper.cpp/tree/main
        /// on 2026-11-27. They are also shown on the page of each file on huggingface.co.
        /// </remarks>
        private static readonly Dictionary<string, string> checksums = new()
        {
            { "ggml-base-q5_1.bin", "422f1ae452ade6f30a004d7e5c6a43195e4433bc370bf23fac9cc591f01a8898" },
            { "ggml-small-q5_1.bin", "ae85e4a935d7a567bd102fe55afc16bb595bdb618e11b2fc7591bc08120411bb" },
            { "ggml-large-v3-turbo-q5_0.bin", "394221709cd5ad1f40c46e6031ca61bce88931e6e088c188294c6d5a55ffa7e2" },
        };

        /// <summary>
        /// True if <paramref name="fileName"/> is a plain file name. Names that could
        /// point outside of the folder for the models are rejected, because the name
        /// comes from a configuration file the user can edit.
        /// </summary>
        /// <param name="fileName">The file name to be checked.</param>
        /// <returns>True if <paramref name="fileName"/> is neither empty nor contains
        /// a path separator, a colon, or "..", and is not a path.</returns>
        public static bool IsValidFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return false;
            }
            if (fileName.Contains("/") || fileName.Contains("\\") || fileName.Contains(".."))
            {
                return false;
            }
            // A colon may introduce a drive on Windows, e.g., "C:model.bin".
            if (fileName.Contains(":"))
            {
                return false;
            }
            if (Path.IsPathRooted(fileName))
            {
                return false;
            }
            if (Path.GetFileName(fileName) != fileName)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Returns the URL from which the model file <paramref name="fileName"/> can be downloaded.
        /// </summary>
        /// <param name="fileName">The file name of the model.</param>
        /// <returns>The download URL.</returns>
        public static string GetDownloadUrl(string fileName)
        {
            return baseUrl + Uri.EscapeDataString(fileName);
        }

        /// <summary>
        /// Yields the expected checksum of the model file <paramref name="fileName"/>.
        /// </summary>
        /// <param name="fileName">The file name of the model.</param>
        /// <param name="checksum">The expected SHA-256 checksum; null if unknown.</param>
        /// <returns>True if the checksum of <paramref name="fileName"/> is known.</returns>
        public static bool TryGetChecksum(string fileName, out string checksum)
        {
            return checksums.TryGetValue(fileName, out checksum);
        }

        /// <summary>
        /// True if the file at <paramref name="path"/> has the SHA-256 checksum <paramref name="expected"/>.
        /// </summary>
        /// <param name="path">The path of the file to be verified.</param>
        /// <param name="expected">The expected checksum in hexadecimal notation.</param>
        /// <returns>True if the checksums are equal.</returns>
        public static bool HasChecksum(string path, string expected)
        {
            return string.Equals(ComputeChecksum(path), expected, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns the SHA-256 checksum of the file at <paramref name="path"/>.
        /// The file is read piece by piece, so that large files do not fill the memory.
        /// </summary>
        /// <param name="path">The path of the file.</param>
        /// <returns>The checksum in lower-case hexadecimal notation.</returns>
        public static string ComputeChecksum(string path)
        {
            using FileStream stream = File.OpenRead(path);
            using SHA256 sha256 = SHA256.Create();
            byte[] hash = sha256.ComputeHash(stream);

            StringBuilder result = new();
            foreach (byte b in hash)
            {
                result.Append(b.ToString("x2"));
            }
            return result.ToString();
        }
    }
}
