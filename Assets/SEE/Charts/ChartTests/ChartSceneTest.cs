using System.Collections;
using NUnit.Framework;
using SEE.Charts.Scripts;
using SEE.GO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SEE.Charts.ChartTests
{
	public class ChartSceneTest
	{
		private MonoBehaviourTest<MonoTest> _tester;
		private GameObject _city;
		private ChartManager _manager;
		private ChartCreator _creator;
		private GameObject _charts;

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

		[Test]
		public void TestHelperAndComponentsLoaded()
		{
			Assert.NotNull(_tester.component.helper);
			Assert.NotNull(_manager);
			Assert.NotNull(_creator);
			Assert.NotNull(_charts);
		}

		[Test]
		public void TestToggleCharts()
		{
			Assert.True(_charts.activeInHierarchy);
			_manager.ToggleCharts();
			Assert.False(_charts.activeInHierarchy);
			_manager.ToggleCharts();
			Assert.True(_charts.activeInHierarchy);
		}

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

		[UnityTest]
		public IEnumerator TestCreateChartSameAxis()
		{
			_creator.CreateChart();
			yield return new WaitForSeconds(0.2f);
			Assert.AreEqual(3, _charts.transform.childCount);
			var chart = _charts.transform.Find("Chart(Clone)");
			Assert.NotNull(chart);
			var entries = chart.Find("DataPanel").Find("Entries");
			Assert.NotNull(entries);
			Assert.AreEqual(2, entries.childCount);
			var marker = entries.Find("Marker(Clone)").gameObject.GetComponent<ChartMarker>();
			Assert.NotNull(marker);
			Assert.AreEqual("a1_a.cpp",
				marker.linkedObject.GetComponent<NodeRef>().node.SourceName);
		}

		[UnityTest]
		public IEnumerator TestCreateChartDifferentAxis()
		{
			_creator.CreateChart();
			yield return new WaitForSeconds(0.2f);

		}

		[Test]
		public void TestCreateChartOneMarker()
		{
			Object.Destroy(_city);
			_city = _tester.component.LoadCity(1);
		}

		[Test]
		public void TestCreateChartZeroMarkers()
		{
			Object.Destroy(_city);
			_city = _tester.component.LoadCity(2);
		}

		[Test]
		public void TestScrollViewToggles()
		{
			//Scroll view toggles testen -> wie viele marker bleiben aktiv?
		}
	}

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