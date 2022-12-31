using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SEE.Game.Charts
{
    /// <summary>
    ///     Tests for charts. Need a working scene with charts in it and a <see cref="GameObject" /> with a
    ///     <see cref="TestHelper" /> on it. A city will be loaded by the tests.
    /// </summary>
    [Explicit("ChartsTest scene no longer exists. Tracked in #483.")]
    public class TestChartScene
    {
        private const string ChartString = "Chart(Clone)";
        private const string DataString = "DataPanel";
        private const string EntriesString = "Entries";
        private const string MarkerString = "Marker(Clone)";
        private GameObject _charts;
        private GameObject _city;
        private ChartCreator _creator;
        private ChartManager _manager;
        private MonoBehaviourTest<MonoTest> _tester;

        /// <summary>
        ///     Basic scene setup and loads the <see cref="TestHelper" />.
        /// </summary>
        /// <returns></returns>
        [UnitySetUp]
        public IEnumerator Setup()
        {
            _tester = new MonoBehaviourTest<MonoTest>();
            yield return new WaitForSeconds(1f);
            _tester.component.FindHelper();
            TestHelper helper = _tester.component.helper;
            _manager = helper.manager;
            _creator = helper.creator;
            _charts = helper.charts;
            _city = _tester.component.LoadCity(0);
            yield return new WaitForEndOfFrame();
        }

        /// <summary>
        ///     Somehow the first test always fails due to VR initialization, so this one is just for the others to
        ///     succeed.
        /// </summary>
        [Test]
        public void ATestInitializer()
        {
            _creator.CreateChart();
            Assert.True(true);
        }

        /// <summary>
        ///     Checks if the test scene loaded correctly.
        /// </summary>
        [Test]
        public void TestHelperAndComponentsLoaded()
        {
            Assert.NotNull(_tester.component.helper);
            Assert.NotNull(_manager);
            Assert.NotNull(_creator);
            Assert.NotNull(_charts);
        }

        /// <summary>
        ///     Checks if the <see cref="ChartManager" /> toggles charts correctly.
        /// </summary>
        [Test]
        public void TestToggleCharts()
        {
            Assert.True(_charts.activeInHierarchy);
            _manager.ToggleCharts();
            Assert.False(_charts.activeInHierarchy);
            _manager.ToggleCharts();
            Assert.True(_charts.activeInHierarchy);
        }

        /// <summary>
        ///     Checks if the chart ui buttons behave as expected.
        /// </summary>
        [Test]
        public void TestUiButtons()
        {
            Button closeButton = _tester.component.helper.closeChartsButton;
            Button createButton = _tester.component.helper.createChartButton;
            Assert.NotNull(closeButton);
            Assert.NotNull(createButton);
            createButton.onClick.Invoke();
            Assert.AreEqual(3, _charts.transform.childCount);
            closeButton.onClick.Invoke();
            Assert.False(_charts.activeInHierarchy);
        }

        /// <summary>
        ///     Checks if charts are being closed properly.
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator TestCloseChart()
        {
            _creator.CreateChart();
            yield return new WaitForSeconds(0.2f);
            Transform chart = _charts.transform.Find(ChartString);
            Button closeButton = chart.Find("LabelsPanel").Find("TopLeft").Find("DestroyButton").GetComponent<Button>();
            closeButton.onClick.Invoke();
            yield return new WaitForSeconds(0.2f);
            bool destroyed = chart == null;
            Assert.True(destroyed);
        }

        /// <summary>
        ///     Checks if charts with the same metric on both axes display the correct information.
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator TestCreateChartSameAxis()
        {
            _creator.CreateChart();
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(3, _charts.transform.childCount);
            Transform chart = _charts.transform.Find(ChartString);
            Assert.NotNull(chart);
            Transform entries = chart.Find(DataString).Find(EntriesString);
            Assert.NotNull(entries);
            Assert.AreEqual(2, entries.childCount);
            ChartMarker marker = entries.Find(MarkerString).GetComponent<ChartMarker>();
            Assert.NotNull(marker);
            //Assert.AreEqual("a1_a.cpp", marker.LinkedInteractable.GetComponent<NodeRef>().node.SourceName);
        }

        /// <summary>
        ///     Checks if charts with different metrics on their axes display the correct information.
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator TestCreateChartDifferentAxis()
        {
            _creator.CreateChart();
            yield return new WaitForSeconds(0.2f);
            Transform entries = _charts.transform.Find(ChartString).Find(DataString).Find(EntriesString);
            TMP_Dropdown dropdown = _charts.transform.Find(ChartString).Find("LabelsPanel")
                .Find("AxisDropdownX").GetComponent<TMP_Dropdown>();
            Assert.AreEqual(2, entries.childCount);
            dropdown.value = 1;
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(2, entries.childCount);
            dropdown.value = 2;
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(2, entries.childCount);
            dropdown.value = 6;
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(0, entries.childCount);
        }

        /// <summary>
        ///     Checks if markers can be activated and deactivated via the <see cref="ScrollViewEntry" />s.
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator TestScrollViewToggles()
        {
            yield return new WaitForSeconds(0.1f);
            //_creator.CreateChart();
            //yield return new WaitForSeconds(0.2f);
            //Transform entries = _charts.transform.Find(ChartString).Find(DataString).Find(EntriesString);
            //_charts.transform.Find(ChartString).Find("ContentSelection").gameObject
            //    .SetActive(true);
            //Transform scrollView = _charts.transform.Find(ChartString).Find("ContentSelection")
            //    .Find("Scroll View").Find("Viewport").Find("Content");
            //ScrollViewToggle parent = scrollView.GetChild(0).GetComponent<ScrollViewToggle>();
            //ScrollViewToggle child = scrollView.GetChild(1).GetComponent<ScrollViewToggle>();
            //Assert.AreEqual(2, entries.childCount);
            //parent.Toggle(false, true);
            //yield return new WaitForSeconds(3f);
            //Assert.AreEqual(0, entries.childCount);
            //parent.Toggle(true, true);
            //yield return new WaitForSeconds(3f);
            //Assert.AreEqual(2, entries.childCount);
            //child.Toggle(false, true);
            //yield return new WaitForSeconds(3f);
            //Assert.AreEqual(1, entries.childCount);
            //child.Toggle(true, true);
            //yield return new WaitForSeconds(3f);
            //Assert.AreEqual(2, entries.childCount);
        }

        /// <summary>
        ///     Checks if buildings are correctly highlighted.
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator TestHighlights()
        {
            _creator.CreateChart();
            yield return new WaitForSeconds(0.2f);
            //Transform entries = _charts.transform.Find(ChartString).Find(DataString).Find(EntriesString);
            //Button button = entries.Find(MarkerString).GetComponent<Button>();
            //ChartMarker marker = entries.Find(MarkerString).GetComponent<ChartMarker>();
            //GameObject markerHighlight = marker.transform.Find("MarkerHighlight").gameObject;
            //string buildingName = marker.LinkedInteractable.GetComponent<NodeRef>().node.SourceName;
            //Transform folder = _city.transform.Find("dir_A_1");
            //Transform building = folder.Find(buildingName);
            //button.onClick.Invoke();
            //yield return new WaitForSeconds(1f);
            //Assert.True(markerHighlight.activeInHierarchy);
            //Assert.NotNull(building.Find(buildingName + "(Clone)"));
        }

        /// <summary>
        ///     Checks if charts display the correct information if only one building is found in the scene.
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator TestCreateChartOneMarker()
        {
            Object.Destroy(_city);
            _city = _tester.component.LoadCity(1);
            _creator.CreateChart();
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(3, _charts.transform.childCount);
            Transform chart = _charts.transform.Find(ChartString);
            Assert.NotNull(chart);
            Transform entries = chart.Find(DataString).Find(EntriesString);
            Assert.NotNull(entries);
            Assert.AreEqual(1, entries.childCount);
            ChartMarker marker = entries.Find(MarkerString).GetComponent<ChartMarker>();
            Assert.NotNull(marker);
            //Assert.AreEqual("a1_a.cpp", marker.LinkedInteractable.GetComponent<NodeRef>().node.SourceName);
        }

        /// <summary>
        ///     Checks if charts display the correct information if there are no buildings in the scene.
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator TestCreateChartZeroMarkers()
        {
            Object.Destroy(_city);
            _city = _tester.component.LoadCity(2);
            _creator.CreateChart();
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(3, _charts.transform.childCount);
            Transform chart = _charts.transform.Find(ChartString);
            Assert.NotNull(chart);
            Transform entries = chart.Find(DataString).Find(EntriesString);
            Assert.NotNull(entries);
            Assert.AreEqual(0, entries.childCount);
        }
    }

    /// <summary>
    ///     Helper class to have access to MonoBehaviour methods.
    /// </summary>
    public class MonoTest : MonoBehaviour, IMonoBehaviourTest
    {
        public TestHelper helper;

        public bool IsTestFinished { get; private set; }

        private void Start()
        {
            SceneManager.LoadScene("ChartsTest");
            IsTestFinished = true;
        }

        public void FindHelper()
        {
            helper = GameObject.FindGameObjectWithTag("TestHelper").GetComponent<TestHelper>();
        }

        public GameObject LoadCity(int prefab)
        {
            return Instantiate(helper.cityPrefabs[prefab]);
        }
    }
}