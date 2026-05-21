using Cysharp.Threading.Tasks;
using NUnit.Framework;
using SEE.Utils.Paths;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.TestTools;

namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// Tests of <see cref="MetricImporter"/>.
    /// </summary>
    internal class TestMetricImporter
    {
        /// <summary>
        /// Tests <see cref="MetricImporter.LoadCsvAsync(Graph, DataPath, char)"/>
        /// and its interplay with <see cref="DataPath"/>. We download an arbitrary
        /// CSV file from the web. It will not have our expected format, but that
        /// is not our test focus. As long as it downloads the file, everything
        /// is fine.
        /// </summary>
        /// <returns>enumerator to continue</returns>
        [UnityTest]
        public IEnumerator TestLoadCsvAsyncMethod() =>
            UniTask.ToCoroutine(async () =>
            {
                LogAssert.Expect(LogType.Error, new Regex(".*There is no SEE.User.UserSettings component in the current scene!.*"));

                DataPath path = new()
                {
                    Root = DataPath.RootKind.Url,
                    Path = "https://www.stats.govt.nz/assets/Uploads/Research-and-development-survey/Research-and-development-survey-2022/"
                           + "Download-data/research-and-development-survey-2022-csv-notes.csv"
                };

                try
                {
                    await MetricImporter.LoadCsvAsync(new Graph(""), path);
                }
                catch (System.IO.IOException ex)
                {
                    Assert.AreEqual($"First header column in {path.Path} is not ID.", ex.Message);
                    // This is expected.
                    // Note: I couldn't use the following:
                    // Assert.ThrowsAsync<IOException>(async () => await MetricImporter.LoadCsvAsync(new Graph(""), path));
                    // It never terminated.
                }
            });
    }
}
