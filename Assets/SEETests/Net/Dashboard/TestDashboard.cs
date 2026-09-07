using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using SEE.Net.Dashboard.Model.Issues;
using SEE.Net.Dashboard.Model.Metric;
using SEE.Utils;
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

        /// <summary>
        /// The maximal amount of time to wait for a response from the dashboard.
        /// </summary>
        private const float timeout = 2f;

        /// <summary>
        /// The game object holding a <see cref="DashboardRetriever"/> component,
        /// which is used to retrieve data from the dashboard. This object will
        /// be created and destroyed for each test.
        /// </summary>
        private GameObject retrieverObject;

        [SetUp]
        public void SetUp()
        {
            retrieverObject = new("Retriever");
            DashboardRetriever retriever = retrieverObject.AddComponent<DashboardRetriever>();
            // Set timeout to 2 seconds to speed up tests.
            retriever.TimeoutSeconds = timeout;
        }

        [TearDown]
        public void TearDown()
        {
            Destroyer.Destroy(retrieverObject);
        }

        [UnityTest]
        public IEnumerator TestDashboardVersionCorrect() => UniTask.ToCoroutine(async () =>
        {
            DashboardVersion version = await DashboardRetriever.Instance.GetDashboardVersionAsync();
            Assert.That(version.MajorVersion, Is.EqualTo(DashboardVersion.SupportedVersion.MajorVersion));
            Assert.That(version.MinorVersion, Is.EqualTo(DashboardVersion.SupportedVersion.MinorVersion));
        });

        [UnityTest]
        public IEnumerator TestDashboardSystemEntity() => UniTask.ToCoroutine(async () =>
        {
            EntityList list = await DashboardRetriever.Instance.GetSystemEntityAsync("latest");
            Assert.That(list, Is.Not.Null, "The system entity list must have been retrieved.");
            Assert.That(list.Entities.Count, Is.EqualTo(1));
        });

        [UnityTest]
        public IEnumerator TestDashboardEntities() => UniTask.ToCoroutine(async () =>
        {
            EntityList list = await DashboardRetriever.Instance.GetEntitiesAsync("latest");
            Assert.That(list, Is.Not.Null, "The entity list must have been retrieved.");
            Assert.That(list.Entities, Is.Not.Empty);
        });

        [UnityTest]
        public IEnumerator TestDashboardMetrics() => UniTask.ToCoroutine(async () =>
        {
            MetricList list = await DashboardRetriever.Instance.GetMetricsAsync("latest");
            Assert.That(list, Is.Not.Null, "The metric list must have been retrieved.");
            Assert.That(list.Metrics, Is.Not.Empty);
        });

        [UnityTest]
        public IEnumerator TestDashboardMetricValue() => UniTask.ToCoroutine(async () =>
        {
            const string entity = "81"; // This entity does not exist.
            const string metric = SEE.DataModel.DG.Metrics.Prefix + "LOC";
            MetricValueRange range = await DashboardRetriever.Instance.GetMetricValueRangeAsync(entity, metric);
            Assert.That(range, Is.Not.Null, "The metric value range must have been retrieved.");
            Assert.That(range.Values, Is.Not.Empty);
            Assert.That(range.Entity, Is.EqualTo(entity));
            Assert.That(range.Metric, Is.EqualTo(metric));
            // The entity does not exist, hence there is no value for it.
            Assert.That(range.Values, Has.Member(null));
        });

        [UnityTest]
        public IEnumerator TestDashboardMetricTable() => UniTask.ToCoroutine(async () =>
        {
            MetricValueTable table = await DashboardRetriever.Instance.GetMetricValueTableAsync();
            Assert.That(table, Is.Not.Null, "The metric value table must have been retrieved.");
            Assert.That(table.Rows, Is.Not.Empty);
        });

        [UnityTest]
        public IEnumerator TestDashboardIssueDescription() => UniTask.ToCoroutine(async () =>
        {
            string description = await DashboardRetriever.Instance.GetIssueDescriptionAsync("SV4");
            Assert.That(description, Is.Not.Null, "The description of issue SV4 must have been retrieved.");
            Assert.That(description, Does.StartWith("This rule"));
        });

        private static IEnumerator TestDashboardIssues<T>() where T : Issue, new() => UniTask.ToCoroutine(async () =>
        {
            IssueTable<T> table = await DashboardRetriever.Instance.GetIssuesAsync<T>();
            Assert.That(table, Is.Not.Null, $"The issue table for {typeof(T).Name} must have been retrieved.");
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
