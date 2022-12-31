using NUnit.Framework;
using UnityEngine;

namespace SEE.Net
{
    /// <summary>
    /// Tests for starting the network configuration.
    /// </summary>
    internal class TestNetwork : TestSEEGame
    {
        /// <summary>
        /// Tests the selection of the network configuration and starting a host.
        /// We will first start the game once (via <see cref="TestInitialStart"/>
        /// and then re-start it once more (via <see cref="TestRestart"/> in order
        /// to check that we can move from one test case to the next one without
        /// any problem. This is basically a sanity check to see whether we can
        /// move from one test case to the next one without any problem of
        /// the set-up and tear-down in <see cref="TestSEEGame"/>.
        /// </summary>
        [Test]
        public void TestInitialStart()
        {
            Debug.Log($"[TestInitialStart] Started.\n");
            // All the checked assertions are already in SetUp. There is
            // nothing be checked here.
            Debug.Log("[TestInitialStart] Finished.\n");

        }

        /// <summary>
        /// Tests the re-entering of the play mode.
        /// </summary>
        [Test]
        public void TestRestart()
        {
            Debug.Log($"[TestRestart] Started.\n");
            // All the checked assertions are already in SetUp. There is
            // nothing be checked here.
            Debug.Log("[TestRestart] Finished\n");
        }
    }
}
