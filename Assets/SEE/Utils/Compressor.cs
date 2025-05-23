using System;
using System.IO;
using System.Runtime.InteropServices;
using Joveler.Compression.XZ;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// Allows to compress and uncompress data.
    /// </summary>
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    internal static class Compressor
    {
        /// <summary>
        /// This static constructor is used to initialize the liblzma library.
        /// It needn't be called explicitly, Unity does this automatically once via the <c>InitializeOnLoad</c>
        /// attribute assigned to this class.
        /// </summary>
        static Compressor()
        {
            try
            {
                XZInit.GlobalInit(GetLiblzmaPath());
            }
            catch (InvalidOperationException e) when (e.Message.Contains(" is already initialized"))
            {
                // Already loaded. We can ignore this.
            }
        }

        /// <summary>
        /// Returns the platform-dependent path to the liblzma native library.
        /// </summary>
        /// <returns>Path to the liblzma library</returns>
        /// <exception cref="PlatformNotSupportedException">If the system platform is not supported</exception>
        private static string GetLiblzmaPath()
        {
            // The library liblzma.dll is located in
            // Assets/Packages/Joveler.Compression.XZ.5.0.2/runtimes/<arch>/native/liblzma.dll
            // where <arch> specifies the operating system the Unity editor is currently running on
            // and the hardware architecture (e.g., win-x64).
            //
            // If SEE is started from the Unity editor, the library will be looked up
            // under this path.
            // In a built application of SEE (i.e., an executable running independently
            // from the Unity editor), the library is located in
            // SEE_Data/Plugins/<arch>/liblzma.dll instead, where <arch> specifies
            // the hardware architecture (e.g., x86_64; see also
            // https://docs.unity3d.com/Manual/PluginInspector.html).

            // IMPORTANT NOTE: We need to adjust to Joveler.Compression.XZ whenever the version changes.
            string libDir = Application.isEditor ?
                    Path.Combine(Path.GetFullPath(Application.dataPath), "Packages", "Joveler.Compression.XZ.5.0.2", "runtimes")
                  : Path.Combine(Path.GetFullPath(Application.dataPath), "Plugins");

            if (Application.isEditor)
            {
                if (!File.Exists(libDir))
                {
                    throw new Exception($"Unable to find liblzma path [{libDir}].");
                }
                // In the editor, the <arch> specifier is a combination of the OS and the process
                // architecture. We will first handle the OS.
                OSPlatform platform = GetOSPlatform();
                if (platform == OSPlatform.Windows)
                {
                    libDir = Path.Combine(libDir, "win");
                }
                else if (platform == OSPlatform.Linux)
                {
                    libDir = Path.Combine(libDir, "linux");
                }
                else if (platform == OSPlatform.OSX)
                {
                    libDir = Path.Combine(libDir, "osx");
                }

                // Now follows the process architecture.
                switch (RuntimeInformation.ProcessArchitecture)
                {
                    case Architecture.X86:
                        libDir += "-x86";
                        break;
                    case Architecture.X64:
                        libDir += "-x64";
                        break;
                    case Architecture.Arm when platform == OSPlatform.Windows:
                        libDir += "10-arm";
                        break;
                    case Architecture.Arm64 when platform == OSPlatform.Windows:
                        libDir += "10-arm64";
                        break;
                    case Architecture.Arm:
                        libDir += "-arm";
                        break;
                    case Architecture.Arm64:
                        libDir += "-arm64";
                        break;
                    default: throw new PlatformNotSupportedException($"Unknown architecture {RuntimeInformation.ProcessArchitecture}");
                }

                libDir = Path.Combine(libDir, "native");
            }
            else
            {
                // In a deployed application, only the process architecture matters.
                string arch = RuntimeInformation.ProcessArchitecture switch
                {
                    Architecture.X86 or Architecture.Arm => "x86",
                    Architecture.X64 or Architecture.Arm64 => "x86_64",
                    _ => throw new PlatformNotSupportedException($"Unknown architecture {RuntimeInformation.ProcessArchitecture}"),
                };
                libDir = Path.Combine(libDir, arch);
            }

            string libPath = null;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                libPath = Path.Combine(libDir, "liblzma.dll");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                if (Application.isEditor)
                {
                    libPath = Path.Combine(libDir, "liblzma.so");
                }
                // Under Linux native plugins aren't stored inside a architecture subdir (e.g. x86_64).
                // They are stored directly in the Plugins dir.
                // So under Linux when constructing the path, it is necessary to omit this subdirectory specifically for Linux builds.
                else
                {
                    libPath = Path.Combine(Path.Combine(Path.GetFullPath(Application.dataPath), "Plugins"), "liblzma.so");
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                libPath = Path.Combine(libDir, "liblzma.dylib");
            }

            if (libPath == null)
            {
                throw new PlatformNotSupportedException("Unable to find native library.");
            }

            if (!File.Exists(libPath))
            {
                throw new PlatformNotSupportedException($"Unable to find native library [{libPath}].");
            }

            return libPath;

            // Returns the type of operating system. If other than Windows, Linux,
            // or OSX, an exception is thrown.
            static OSPlatform GetOSPlatform()
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return OSPlatform.Windows;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return OSPlatform.Linux;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return OSPlatform.OSX;
                }
                else
                {
                    throw new PlatformNotSupportedException
                        ("Only Windows, Linux, and OSX are supported operating systems.");
                }
            }
        }

        /// <summary>
        /// Returns true if <paramref name="filename"/> has a file extension indicating
        /// compression.
        /// </summary>
        /// <param name="filename">filename to be tested</param>
        /// <returns>true if <paramref name="filename"/> has a file extension
        /// <see cref="Filenames.CompressedExtension"/></returns>
        public static bool IsCompressed(string filename)
        {
            return filename.ToLower().EndsWith(Filenames.CompressedExtension);
        }

        /// <summary>
        /// Opens the file with given <paramref name="filename"/> and returns it as a <see cref="Stream"/>.
        /// If <paramref name="filename"/> has the filename extension
        /// <see cref="CompressedExtension"/>, the stream will be the
        /// uncompressed content of the open file; otherwise it will be the content
        /// of the file as is.
        /// </summary>
        /// <param name="filename">name of the file to be opened</param>
        /// <returns>stream of the (possibly uncompressed) content of the opened file</returns>
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
        /// <param name="stream">stream containing compressed data</param>
        /// <returns>uncompressed content</returns>
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
        /// <param name="filename"></param>
        /// <param name="source"></param>
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
    }
}
