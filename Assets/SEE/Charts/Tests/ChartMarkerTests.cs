using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tests
{
    public class ChartMarkerTests
    {
	    [SetUp]
	    public void Setup()
	    {
		    SceneManager.LoadScene("ChartsTest");

	    }

	    [UnityTest]
        public IEnumerator ChartMarkerTestsWithEnumeratorPasses()
        {
	        yield return null;
        }
    }
}
