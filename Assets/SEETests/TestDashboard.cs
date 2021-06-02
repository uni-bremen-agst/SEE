using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using SEE.Net.Dashboard;
using SEE.Net.Dashboard.Model.Issues;
using SEE.Net.Dashboard.Model.Metric;
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
        public void SetupRetriever()
        {
            DashboardRetriever.BaseUrl = "https://stvive.informatik.uni-bremen.de:9443/axivion/projects/SEE/";
            DashboardRetriever.Token = "0.0000000000014.l-qjyU2eTKuNl7v0PsBb4qfI3sVHvtwKGGeOTKV_3eE";
            DashboardRetriever.PublicKey = "3082020A0282020100B20ACB6E1639D673B6AF9E9F36578F66068AFDA50327DC2AB0F804E2F8"
                                           + "3765BCB7AD74FED31EC8812FF9AA9C2461D53F7DC08449C765F0ECFA9C0787B9D1E1AE92F8D"
                                           + "1919EDB6871E70601DB0834FF34389EDBA30BFF48F3EA8D07786E976B04F5232AC3A63D07DA"
                                           + "5EAD5F5450026C9E2FB9294D32FC0172E9F0DFF33CDCB35180DB22E6985C15B02BBFAD02499"
                                           + "D0E52AA916ADD5F9E7A40E22B8EC5427E02E47FD78CFEF30B5A2EDB53EA47E8B70230FB9EAE"
                                           + "57B4B7042BD8829F67F4DCDA0230BB933741AF42992CD9164C4F5E2C126A46DC42AE5BD2268"
                                           + "2C97880F8D0A82FA36FEC89CE9318E0DE2CAA3352F92F6231B18DF29913445AA323931106B0"
                                           + "764066DB4A2F8764CE4FAC2500F5A084AE3133C6C82D18181655FC1050629257A54B44FBACB"
                                           + "1BE43E51C7FA80DD7CE68D2F86AF448CA2E03B3C81A1289AA355E926CF221D881BFCD82BE7B"
                                           + "0FA99F1B04A95D23F9B030B6CDF81E90197868BC72F314E2DFBA5F9965517F8C33BF056C005"
                                           + "0DB08D285C988EFF7F212CC9D652E70B8FE67BC632F17ECFAE57603F5592C831951442F8215"
                                           + "75139D193B4DC4F2EB46FEE09495CCF67259A3F4516873612582B84512019A1157F621B46D4"
                                           + "5BCEE471BCE855C068B701F40C4CBB78F8E11550C83D7E6897967FD0B90C4BD25B0E3884492"
                                           + "66293CF52814112B7F1A95A8EC3D6CBB5567B6B0916A995D5EB8254E31647B2F810203010001";
            DashboardRetriever.StrictMode = false;
        }

        [UnityTest]
        public IEnumerator testDashboardVersionCorrect() => UniTask.ToCoroutine(async () =>
        {
            DashboardVersion version = await DashboardRetriever.Instance.GetDashboardVersion();
            Assert.AreEqual(version, DashboardVersion.SupportedVersion);
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