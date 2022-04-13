using NUnit.Framework;
using SEE.Net;
using UnityEditor;
using UnityEngine;
using static SEE.Utils.CRDT;

namespace SEE.Utils
{
    /// <summary>
    /// Tests the remote edit operations for the CRDT in play mode.
    /// </summary>
    internal class TestNetworkCRDT : TestSEEGame
    {
        [Test]
        public void TestRemoteDeleteChar()
        {
            CRDT test = new CRDT(new GUID().ToString(), "test");
            test.AddChar('H', 0);
            test.AddChar('A', 1);
            test.AddChar('L', 2);
            test.AddChar('O', 3);
            test.AddChar('L', 2);
            test.AddChar(' ', 5);
            test.AddChar('!', 6);

            Assert.AreEqual("HALLO !", test.PrintString());

            test.RemoteDeleteChar(test.GetCRDT()[2].GetIdentifier());
            Assert.AreEqual("HALO !", test.PrintString());
            test.RemoteDeleteChar(test.GetCRDT()[3].GetIdentifier());
            Assert.AreEqual("HAL !", test.PrintString());
            try
            {
                test.RemoteDeleteChar(new Identifier[] { new Identifier(22, "22"), new Identifier(11, "11"), new Identifier(33, "33") });
                Assert.Fail();
            }
            catch (RemoteDeleteNotPossibleException)
            {
            }
        }

        [Test]
        public void RemoteAndAddCharSameTimePos0()
        {
            CRDT crdt1 = new CRDT("1", "test");
            CRDT crdt2 = new CRDT("2", "test");

            crdt1.AddChar('A', 0);
            Assert.AreEqual("A", crdt1.PrintString());

            crdt2.AddChar('a', 0);
            crdt2.AddChar('a', 0);
            Assert.AreEqual("aa", crdt2.PrintString());

            crdt2.RemoteAddChar('A', crdt1.GetCRDT()[0].GetIdentifier());
            crdt1.RemoteAddChar('a', crdt2.GetCRDT()[1].GetIdentifier());
            crdt1.RemoteAddChar('a', crdt2.GetCRDT()[1].GetIdentifier());
            Assert.AreEqual("Aaa", crdt1.PrintString());
            Assert.AreEqual("Aaa", crdt2.PrintString());
        }

        [Test]
        public void TestRemoteAdd()
        {
            CRDT crdt1 = new CRDT("1", "test");
            CRDT crdt2 = new CRDT("2", "test");

            crdt1.AddChar('H', 0);
            crdt2.RemoteAddChar('H', crdt1.GetCRDT()[0].GetIdentifier());

            crdt1.AddChar('A', 1);
            crdt2.RemoteAddChar('A', crdt1.GetCRDT()[1].GetIdentifier());

            crdt1.AddChar('L', 2);
            crdt2.RemoteAddChar('L', crdt1.GetCRDT()[2].GetIdentifier());

            crdt1.AddChar('O', 3);
            crdt2.RemoteAddChar('O', crdt1.GetCRDT()[3].GetIdentifier());

            //Simulate sync problems
            crdt2.AddChar('l', 2);

            crdt1.AddChar('!', 4);
            crdt2.RemoteAddChar('!', crdt1.GetCRDT()[4].GetIdentifier());

            crdt1.RemoteAddChar('l', crdt2.GetCRDT()[2].GetIdentifier());
            //End sync problem
            crdt1.AddChar('_', 0);
            crdt2.RemoteAddChar('_', crdt1.GetCRDT()[0].GetIdentifier());

            // FIXME: There are no asserts here. This is not an automated test.
            Print(crdt1);
            Print(crdt2);
        }

        private void Print(CRDT crdt)
        {
            Debug.Log("CRDT: " + crdt.ToString());
            Debug.Log(crdt.PrintString());
        }
    }
}
