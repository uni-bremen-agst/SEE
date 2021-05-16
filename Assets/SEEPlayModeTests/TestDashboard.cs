using System.Collections;
using SEE.Net.Dashboard;
using UnityEngine;
using UnityEngine.TestTools;

namespace SEEPlayModeTests
{
    internal class TestDashboard
    {
        private DashboardRetriever retriever;
        
        [UnitySetUp]
        public IEnumerator Setup()
        {
            LogAssert.ignoreFailingMessages = true;
            // A player-settings object must be present in the scene.
            GameObject retrieverGO = new GameObject("Dashboard Retriever");
            retriever = retrieverGO.AddComponent<DashboardRetriever>();
            yield return null;
        }

        [UnityTest]
        public IEnumerator TestDialog()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return new WaitForSeconds(10f);
        }
    }
}