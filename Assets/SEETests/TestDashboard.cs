using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using SEE.Net.Dashboard;
using SEE.Net.Dashboard.Model.Issues;
using SEE.Net.Dashboard.Model.Metric;
using UnityEngine;
using UnityEngine.TestTools;

namespace SEETests
{
    /// <summary>
    /// Class which tests the dashboard retrieval, i.e. everything in the <see cref="SEE.Net.Dashboard"/> namespace.
    /// </summary>
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
            GameObject retrieverObject = new GameObject("Retriever");
            retrieverObject.AddComponent<DashboardRetriever>();
        }
        
        [UnityTest]
        public IEnumerator testDashboardVersionCorrect() => UniTask.ToCoroutine(async () =>
        {
            DashboardVersion version = await DashboardRetriever.Instance.GetDashboardVersion();
            Assert.AreEqual(DashboardVersion.SupportedVersion, version);
        });

        [UnityTest]
        public IEnumerator testDashboardSystemEntity() => UniTask.ToCoroutine(async () =>
        {
            EntityList list = await DashboardRetriever.Instance.GetSystemEntity("latest");
            Assert.IsNotNull(list);
            Assert.AreEqual(1, list.entities.Count);
        });

        [UnityTest]
        public IEnumerator testDashboardEntities() => UniTask.ToCoroutine(async () =>
        {
            EntityList list = await DashboardRetriever.Instance.GetEntities("latest");
            Assert.IsNotNull(list);
            Assert.IsNotEmpty(list.entities);
        });

        [UnityTest]
        public IEnumerator testDashboardMetrics() => UniTask.ToCoroutine(async () =>
        {
            MetricList list = await DashboardRetriever.Instance.GetMetrics("latest");
            Assert.IsNotNull(list);
            Assert.IsNotEmpty(list.metrics);
        });

        [UnityTest]
        public IEnumerator testDashboardMetricValue() => UniTask.ToCoroutine(async () =>
        {
            const string entity = "81"; // DesktopNavigationAction->FixedUpdate
            const string metric = "Metric.LOC";
            MetricValueRange range = await DashboardRetriever.Instance.GetMetricValueRange(entity, metric);
            Assert.IsNotNull(range);
            Assert.IsNotEmpty(range.values);
            Assert.AreEqual(range.entity, entity);
            Assert.AreEqual(range.metric, metric);
            Assert.IsFalse(range.values.Contains(null));
        });

        [UnityTest]
        public IEnumerator testDashboardMetricTable() => UniTask.ToCoroutine(async () =>
        {
            MetricValueTable table = await DashboardRetriever.Instance.GetMetricValueTable();
            Assert.IsNotNull(table);
            Assert.IsNotEmpty(table.rows);
        });

        [UnityTest]
        public IEnumerator testDashboardIssueDescription() => UniTask.ToCoroutine(async () =>
        {
            string description = await DashboardRetriever.Instance.GetIssueDescription("SV4");
            Assert.IsNotNull(description);
            Assert.IsTrue(description.StartsWith("This rule"));
        });

        private static IEnumerator testDashboardIssues<T>() where T : Issue, new() => UniTask.ToCoroutine(async () =>
        {
            IssueTable<T> table = await DashboardRetriever.Instance.GetIssues<T>();
            Assert.IsNotNull(table);
        });

        [UnityTest]
        public IEnumerator testDashboardAvIssues() => UniTask.ToCoroutine(async () => await testDashboardIssues<ArchitectureViolationIssue>());

        [UnityTest]
        public IEnumerator testDashboardClIssues() => UniTask.ToCoroutine(async () => await testDashboardIssues<CloneIssue>());

        [UnityTest]
        public IEnumerator testDashboardCyIssues() => UniTask.ToCoroutine(async () => await testDashboardIssues<CycleIssue>());

        [UnityTest]
        public IEnumerator testDashboardDeIssues() => UniTask.ToCoroutine(async () => await testDashboardIssues<DeadEntityIssue>());

        [UnityTest]
        public IEnumerator testDashboardMvIssues() => UniTask.ToCoroutine(async () => await testDashboardIssues<MetricViolationIssue>());

        [UnityTest]
        public IEnumerator testDashboardSvIssues() => UniTask.ToCoroutine(async () => await testDashboardIssues<StyleViolationIssue>());
    }
}