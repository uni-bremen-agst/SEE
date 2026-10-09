using System;
using System.IO;
using System.Runtime.InteropServices;
using SEE.Graphs.Utils;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// Locates the native liblzma library within the Unity editor or a built
    /// player of SEE and hands it to <see cref="Compressor"/>, which cannot look
    /// it up itself because it must not depend on Unity.
    /// </summary>
    internal static class LiblzmaLoader
    {
        /// <summary>
        /// Initializes <see cref="Compressor"/> with the liblzma library.
        /// Unity calls this method automatically: in the editor whenever the
        /// scripts are loaded, and in a built player when it starts.
        /// </summary>
#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Load()
        {
            Compressor.Initialize(GetLiblzmaPath());
        }

        /// <summary>
        /// Returns the platform-dependent path to the liblzma native library.
        /// </summary>
        /// <returns>Path to the liblzma library.</returns>
        /// <exception cref="PlatformNotSupportedException">If the system platform is not supported.</exception>
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
                if (!Directory.Exists(libDir))
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
    }
}
