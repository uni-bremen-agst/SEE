using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using SEE.Net.Dashboard.Model.Issues;
using SEE.Net.Dashboard.Model.Metric;
using UnityEngine;
using UnityEngine.TestTools;

namespace SEE.Net.Dashboard
{
    /// <summary>
    /// Class which tests the dashboard retrieval, i.e. everything in the <see cref="SEE.Net.Dashboard"/> namespace.
    /// </summary>
    [Category("SkipOnCI")]
    public class TestDashboard
    {
        /**
         * NOTE: Tests in here are extremely basic and prototypical, they only ascertain whether some of the API calls
         * work at all (i.e. cause no error). Before more useful tests can be implemented, a project with constant
         * properties has to be created, because the currently existing SEE project is too dynamic to reliably test.
         */

        [SetUp]
        public void SetUp()
        {
            GameObject retrieverObject = new("Retriever");
            retrieverObject.AddComponent<DashboardRetriever>();
        }

        [UnityTest]
        public IEnumerator TestDashboardVersionCorrect() => UniTask.ToCoroutine(async () =>
        {
            DashboardVersion version = await DashboardRetriever.Instance.GetDashboardVersionAsync();
            Assert.AreEqual(DashboardVersion.SupportedVersion.MajorVersion, version.MajorVersion);
            Assert.AreEqual(DashboardVersion.SupportedVersion.MinorVersion, version.MinorVersion);
        });

        [UnityTest]
        public IEnumerator TestDashboardSystemEntity() => UniTask.ToCoroutine(async () =>
        {
            EntityList list = await DashboardRetriever.Instance.GetSystemEntityAsync("latest");
            Assert.IsNotNull(list);
            Assert.AreEqual(1, list.Entities.Count);
        });

        [UnityTest]
        public IEnumerator TestDashboardEntities() => UniTask.ToCoroutine(async () =>
        {
            EntityList list = await DashboardRetriever.Instance.GetEntitiesAsync("latest");
            Assert.IsNotNull(list);
            Assert.IsNotEmpty(list.Entities);
        });

        [UnityTest]
        public IEnumerator TestDashboardMetrics() => UniTask.ToCoroutine(async () =>
        {
            MetricList list = await DashboardRetriever.Instance.GetMetricsAsync("latest");
            Assert.IsNotNull(list);
            Assert.IsNotEmpty(list.Metrics);
        });

        [UnityTest]
        public IEnumerator TestDashboardMetricValue() => UniTask.ToCoroutine(async () =>
        {
            const string entity = "81"; // This entity does not exist.
            const string metric = SEE.DataModel.DG.Metrics.Prefix + "LOC";
            MetricValueRange range = await DashboardRetriever.Instance.GetMetricValueRangeAsync(entity, metric);
            Assert.IsNotNull(range);
            Assert.IsNotEmpty(range.Values);
            Assert.AreEqual(range.Entity, entity);
            Assert.AreEqual(range.Metric, metric);
            Assert.IsTrue(range.Values.Contains(null));
        });

        [UnityTest]
        public IEnumerator TestDashboardMetricTable() => UniTask.ToCoroutine(async () =>
        {
            MetricValueTable table = await DashboardRetriever.Instance.GetMetricValueTableAsync();
            Assert.IsNotNull(table);
            Assert.IsNotEmpty(table.Rows);
        });

        [UnityTest]
        public IEnumerator TestDashboardIssueDescription() => UniTask.ToCoroutine(async () =>
        {
            string description = await DashboardRetriever.Instance.GetIssueDescriptionAsync("SV4");
            Assert.IsNotNull(description);
            Assert.IsTrue(description.StartsWith("This rule"));
        });

        private static IEnumerator TestDashboardIssues<T>() where T : Issue, new() => UniTask.ToCoroutine(async () =>
        {
            IssueTable<T> table = await DashboardRetriever.Instance.GetIssuesAsync<T>();
            Assert.IsNotNull(table);
        });

        [UnityTest]
        public IEnumerator TestDashboardAvIssues() => UniTask.ToCoroutine(async () => await TestDashboardIssues<ArchitectureViolationIssue>());

        [UnityTest]
        public IEnumerator TestDashboardClIssues() => UniTask.ToCoroutine(async () => await TestDashboardIssues<CloneIssue>());

        [UnityTest]
        public IEnumerator TestDashboardCyIssues() => UniTask.ToCoroutine(async () => await TestDashboardIssues<CycleIssue>());

        [UnityTest]
        public IEnumerator TestDashboardDeIssues() => UniTask.ToCoroutine(async () => await TestDashboardIssues<DeadEntityIssue>());

        [UnityTest]
        public IEnumerator TestDashboardMvIssues() => UniTask.ToCoroutine(async () => await TestDashboardIssues<MetricViolationIssue>());

        [UnityTest]
        public IEnumerator TestDashboardSvIssues() => UniTask.ToCoroutine(async () => await TestDashboardIssues<StyleViolationIssue>());
    }
}
