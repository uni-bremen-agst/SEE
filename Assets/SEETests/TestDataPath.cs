using Cysharp.Threading.Tasks;
using NUnit.Framework;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.TestTools;

namespace SEE.Utils.Paths
{
    /// <summary>
    /// Tests <see cref="DataPath"/>.
    /// </summary>
    internal class TestDataPath
    {
        /// <summary>
        /// Test for downloading a file from a server based on a URL.
        /// </summary>
        /// <returns>enumerator to continue</returns>
        [UnityTest]
        public IEnumerator LoadFromServer() =>
            UniTask.ToCoroutine(async () =>
            {
                const string filename = "psnfss2e.pdf";
                DataPath dataPath = new()
                {
                    Root = DataPath.RootKind.Url,
                    Path = $"https://mirror.physik.tu-berlin.de/pub/CTAN/macros/latex/required/psnfss/{filename}"
                };
                Assert.AreEqual(DataPath.RootKind.Url, dataPath.Root);
                using Stream stream = await dataPath.LoadAsync();
                Debug.Log($"Content length in bytes: {stream.Length}\n");
                using (FileStream fileStream = File.Create(filename))
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    Debug.Log($"Saving to {filename}.\n");
                    await stream.CopyToAsync(fileStream);
                }
                FileIO.DeleteIfExists(filename);
            });

        /// <summary>
        /// Test for loading a file based on a disk path.
        /// </summary>
        /// <returns>enumerator to continue</returns>
        [UnityTest]
        public IEnumerator LoadFromFile() =>
            UniTask.ToCoroutine(async () =>
            {
                // Write the file.
                string filename = Path.GetTempFileName();
                const string content = "Hello, world!";
                File.WriteAllText(filename, content);

                // Read the file.
                DataPath dataPath = new()
                {
                    Root = DataPath.RootKind.Absolute,
                    Path = filename
                };
                Assert.AreEqual(DataPath.RootKind.Absolute, dataPath.Root);
                using Stream stream = await dataPath.LoadAsync();
                Assert.AreEqual(content, Read(stream));
                FileIO.DeleteIfExists(filename);
            });

        /// <summary>
        /// Returns the content of <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">where to read</param>
        /// <returns>content of <paramref name="stream"/></returns>
        private static string Read(Stream stream)
        {
            using StreamReader sr = new(stream);
            stream.Seek(0, SeekOrigin.Begin);
            return sr.ReadToEnd();
        }
    }
}
