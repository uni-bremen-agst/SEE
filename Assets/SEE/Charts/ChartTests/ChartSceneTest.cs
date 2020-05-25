using System.Collections;
using NUnit.Framework;
using SEE.Charts.Scripts;
using SEE.GO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SEE.Charts.ChartTests
{
	/// <summary>
	/// Tests for charts. Need a working scene with charts in it and a <see cref="GameObject" /> with a
	/// <see cref="TestHelper" /> on it. A city will be loaded by the tests.
	/// </summary>
	public class ChartSceneTest
	{
		private MonoBehaviourTest<MonoTest> _tester;
		private GameObject _city;
		private ChartManager _manager;
		private ChartCreator _creator;
		private GameObject _charts;

		private const string ChartString = "Chart(Clone)";
		private const string DataString = "DataPanel";
		private const string EntriesString = "Entries";

		/// <summary>
		/// Basic scene setup and loads the <see cref="TestHelper" />.
		/// </summary>
		/// <returns></returns>
		[UnitySetUp]
		public IEnumerator Setup()
		{
			_tester = new MonoBehaviourTest<MonoTest>();
			yield return new WaitForSeconds(1f);
			_tester.component.FindHelper();
			var helper = _tester.component.helper;
			_manager = helper.manager;
			_creator = helper.creator;
			_charts = helper.charts;
			_city = _tester.component.LoadCity(0);
			yield return new WaitForEndOfFrame();
		}

		/// <summary>
		/// Somehow the first test always fails due to VR initialization, so this one is just for the others to
		/// succeed.
		/// </summary>
		[Test]
		public void ATestInitializer()
		{
			_creator.CreateChart();
			Assert.True(true);
		}

		/// <summary>
		/// Checks if the test scene loaded correctly.
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
		/// Checks if the <see cref="ChartManager" /> toggles charts correctly.
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
		/// Checks if the chart ui buttons behave as expected.
		/// </summary>
		[Test]
		public void TestUiButtons()
		{
			var closeButton = _tester.component.helper.closeChartsButton;
			var createButton = _tester.component.helper.createChartButton;
			Assert.NotNull(closeButton);
			Assert.NotNull(createButton);
			createButton.onClick.Invoke();
			Assert.AreEqual(3, _charts.transform.childCount);
			closeButton.onClick.Invoke();
			Assert.False(_charts.activeInHierarchy);
		}

		/// <summary>
		/// Checks if charts with the same metric on both axes display the correct information.
		/// </summary>
		/// <returns></returns>
		[UnityTest]
		public IEnumerator TestCreateChartSameAxis()
		{
			_creator.CreateChart();
			yield return new WaitForSeconds(0.2f);
			Assert.AreEqual(3, _charts.transform.childCount);
			var chart = _charts.transform.Find(ChartString);
			Assert.NotNull(chart);
			var entries = chart.Find(DataString).Find(EntriesString);
			Assert.NotNull(entries);
			Assert.AreEqual(2, entries.childCount);
			var marker = entries.Find("Marker(Clone)").GetComponent<ChartMarker>();
			Assert.NotNull(marker);
			Assert.AreEqual("a1_a.cpp",
				marker.linkedObject.GetComponent<NodeRef>().node.SourceName);
		}

		/// <summary>
		/// Checks if charts with different metrics on their axes display the correct information.
		/// </summary>
		/// <returns></returns>
		[UnityTest]
		public IEnumerator TestCreateChartDifferentAxis()
		{
			_creator.CreateChart();
			yield return new WaitForSeconds(0.2f);
			var entries = _charts.transform.Find(ChartString).Find(DataString).Find(EntriesString);
			var dropdown = _charts.transform.Find(ChartString).Find("LabelsPanel")
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
		/// Checks if charts display the correct information if only one building is found in the scene.
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
			var chart = _charts.transform.Find(ChartString);
			Assert.NotNull(chart);
			var entries = chart.Find(DataString).Find(EntriesString);
			Assert.NotNull(entries);
			Assert.AreEqual(1, entries.childCount);
			var marker = entries.Find("Marker(Clone)").GetComponent<ChartMarker>();
			Assert.NotNull(marker);
			Assert.AreEqual("a1_b.cpp",
				marker.linkedObject.GetComponent<NodeRef>().node.SourceName);
		}

		/// <summary>
		/// Checks if charts display the correct information if there are no buildings in the scene.
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
			var chart = _charts.transform.Find(ChartString);
			Assert.NotNull(chart);
			var entries = chart.Find(DataString).Find(EntriesString);
			Assert.NotNull(entries);
			Assert.AreEqual(0, entries.childCount);
		}

		/// <summary>
		/// Checks if markers can be activated and deactivated via the <see cref="ScrollViewToggle" />s.
		/// </summary>
		/// <returns></returns>
		[UnityTest]
		public IEnumerator TestScrollViewToggles()
		{
			_creator.CreateChart();
			yield return new WaitForSeconds(0.2f);
			var entries = _charts.transform.Find(ChartString).Find(DataString).Find(EntriesString);
			_charts.transform.Find(ChartString).Find("ContentSelection").gameObject
				.SetActive(true);
			var scrollView = _charts.transform.Find(ChartString).Find("ContentSelection")
				.Find("Scroll View").Find("Viewport").Find("Content");
			var parent = scrollView.GetChild(0).GetComponent<ScrollViewToggle>();
			var child = scrollView.GetChild(1).GetComponent<ScrollViewToggle>();
			Assert.AreEqual(2, entries.childCount);
			parent.Toggle(false);
			yield return new WaitForSeconds(3f);
			Assert.AreEqual(0, entries.childCount);
			parent.Toggle(true);
			yield return new WaitForSeconds(3f);
			Assert.AreEqual(2, entries.childCount);
			child.Toggle(false);
			yield return new WaitForSeconds(3f);
			Assert.AreEqual(1, entries.childCount);
			child.Toggle(true);
			yield return new WaitForSeconds(3f);
			Assert.AreEqual(2, entries.childCount);
		}

		//Test Highlights

		//Test 
	}

	/// <summary>
	/// Helper class to have access to MonoBehaviour methods.
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