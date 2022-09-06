using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace SEE.Utils
{
    /// <summary>
    /// Android does not allow to simply store files in a file system.
    /// This class provides a way to read data from a JAR file
    /// </summary>
    public static class WebRequests
    {
        /// <summary>
        /// Returns a memory stream of the given of the file at the given jar file path.
        /// Because of the file structure in certain builds a Unity web request is needed at this point.
        /// </summary>
        /// <param name="jarPath">The jar file path to the required file</param>
        /// <returns>stream of data contained in <paramref name="jarPath"/></returns>
        public static Stream GetStream(string jarPath)
        {
            string filename = GetFilePath(jarPath);
            UnityWebRequest loadingRequest = UnityWebRequest.Get(Path.Combine(Application.streamingAssetsPath, filename));
            loadingRequest.SendWebRequest();
            while (!loadingRequest.isDone)
            {
                if (loadingRequest.result == UnityWebRequest.Result.ConnectionError ||
                    loadingRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    break;
                }
            }
            if (loadingRequest.result == UnityWebRequest.Result.ConnectionError ||
                loadingRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                return null;
            }
            else
            {
                string str = Encoding.Default.GetString(loadingRequest.downloadHandler.data);
                MemoryStream stream = new MemoryStream();
                StreamWriter writer = new StreamWriter(stream);
                writer.Write(str);
                writer.Flush();
                stream.Position = 0;
                return stream;
            }
        }

        /// <summary>
        /// Cuts the actual file path out of the jar path.
        /// </summary>
        /// <param name="jarPath">The jar file path to the required file</param>
        /// <returns>Actual file path</returns>
        private static string GetFilePath(string jarPath)
        {
            string path = "";
            if (jarPath.StartsWith("jar:file:/"))
            {
                int index = jarPath.IndexOf("assets/");
                path = jarPath.Substring(index + 7);
            }
            return path;
        }

        /// <summary>
        /// Returns true if the file to the given <paramref name="jarPath"/> exists.
        /// </summary>
        /// <remarks>This function assumes that the requested file is contained in <see cref="Application.streamingAssetsPath"/></remarks>
        /// <param name="jarPath">The jar file path to the required file</param>
        /// <returns>true if the file to the given <paramref name="jarPath"/> exists</returns>
        public static bool FileExists(string jarPath)
        {
            string filename = GetFilePath(jarPath);
            UnityWebRequest loadingRequest = UnityWebRequest.Get(Path.Combine(Application.streamingAssetsPath, filename));
            loadingRequest.SendWebRequest();
            while (!loadingRequest.isDone)
            {
                if (loadingRequest.result == UnityWebRequest.Result.ConnectionError ||
                    loadingRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    break;
                }
            }
            return !(loadingRequest.result == UnityWebRequest.Result.ConnectionError
                    || loadingRequest.result == UnityWebRequest.Result.ProtocolError);
        }
    }
}
