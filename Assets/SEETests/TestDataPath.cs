using Cysharp.Threading.Tasks;
using NUnit.Framework;
using SEE.User;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
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
        /// Test for downloading a file from a foreign server based on a URL.
        /// </summary>
        /// <returns>enumerator to continue</returns>
        [UnityTest]
        public IEnumerator LoadFromForeignServer() =>
            UniTask.ToCoroutine(async () =>
            {
                LogAssert.Expect(LogType.Error, new Regex($".*There is no {typeof(UserSettings)} component in the current scene!.*"));

                const string filename = "psnfss2e.pdf";
                DataPath dataPath = new()
                {
                    Root = DataPath.RootKind.Url,
                    Path = $"https://mirror.physik.tu-berlin.de/pub/CTAN/macros/latex/required/psnfss/{filename}"
                };
                Assert.That(dataPath.Root, Is.EqualTo(DataPath.RootKind.Url));
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
        /// Test for downloading a file from our own data backend server based on a URL.
        /// </summary>
        /// <returns>enumerator to continue</returns>
        /// <remarks>This test can only be run when our backend server is running.</remarks>
        [UnityTest]
        [Category("SkipOnCI")]
        public IEnumerator LoadFromOurBackend() =>
            UniTask.ToCoroutine(async () =>
            {
                LogAssert.Expect(LogType.Error, new Regex($".*There is no {typeof(UserSettings)} component in the current scene!.*"));

                const string filename = "solution.sln";
                DataPath dataPath = new()
                {
                    Root = DataPath.RootKind.Url,
                    Path = "http://localhost/api/v1/file/client/solution/serverId=&roomPassword=password"
                };
                Assert.That(dataPath.Root, Is.EqualTo(DataPath.RootKind.Url));
                try
                {
                    using Stream stream = await dataPath.LoadAsync();
                    Debug.Log($"Content length in bytes: {stream.Length}\n");
                    using (FileStream fileStream = File.Create(filename))
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                        Debug.Log($"Saving to {filename}.\n");
                        await stream.CopyToAsync(fileStream);
                    }
                    FileIO.DeleteIfExists(filename);
                }
                catch (System.Net.Http.HttpRequestException _)
                {
                    Debug.LogError("Apparently, our backend server is not running. This test cannot be run then.\n");
                }
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
                Assert.That(dataPath.Root, Is.EqualTo(DataPath.RootKind.Absolute));
                using Stream stream = await dataPath.LoadAsync();
                Assert.That(Read(stream), Is.EqualTo(content), "The loaded content must equal what was written.");
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
