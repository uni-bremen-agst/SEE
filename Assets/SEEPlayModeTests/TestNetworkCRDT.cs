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
        public void TestDeleteChar()
        {
            CRDT test = new CRDT(new GUID().ToString(), "test");
            test.AddChar('H', 0);
            test.AddChar('A', 1);
            test.AddChar('L', 2);
            test.AddChar('O', 3);
            test.AddChar('L', 2);
            test.AddChar(' ', 5);
            test.AddChar('!', 6);

            test.DeleteChar(2);
            Assert.AreEqual("HALO !", test.PrintString());
            test.DeleteChar(3);
            Assert.AreEqual("HAL !", test.PrintString());
            try
            {
                test.DeleteChar(23234);
                Assert.Fail();
            }
            catch (DeleteNotPossibleException)
            {
            }
        }

        [Test]
        public void TestAddChar()
        {
            CRDT test = new CRDT(new GUID().ToString(), "test");
            test.AddChar('H', 0);
            test.AddChar('A', 1);
            test.AddChar('L', 2);
            test.AddChar('O', 3);
            test.AddChar('L', 2);
            test.AddChar(' ', 5);
            test.AddChar('!', 6);
            test.AddChar(':', 6);
            test.AddChar('(', 6);
            Debug.LogWarning(test.PrintString());
            Assert.AreEqual(-1, test.ComparePosition(test.GetCRDT()[0].GetIdentifier(), test.GetCRDT()[1].GetIdentifier()));
            Assert.AreEqual(-1, test.ComparePosition(test.GetCRDT()[0].GetIdentifier(), test.GetCRDT()[2].GetIdentifier()));
            Assert.AreEqual(-1, test.ComparePosition(test.GetCRDT()[0].GetIdentifier(), test.GetCRDT()[3].GetIdentifier()));
            Assert.AreEqual(-1, test.ComparePosition(test.GetCRDT()[0].GetIdentifier(), test.GetCRDT()[4].GetIdentifier()));
            Assert.AreEqual(-1, test.ComparePosition(test.GetCRDT()[1].GetIdentifier(), test.GetCRDT()[2].GetIdentifier()));
            Assert.AreEqual(-1, test.ComparePosition(test.GetCRDT()[1].GetIdentifier(), test.GetCRDT()[3].GetIdentifier()));
            Assert.AreEqual(-1, test.ComparePosition(test.GetCRDT()[2].GetIdentifier(), test.GetCRDT()[3].GetIdentifier()));
            Assert.AreEqual(1, test.ComparePosition(test.GetCRDT()[6].GetIdentifier(), test.GetCRDT()[1].GetIdentifier()));
            Assert.AreEqual(0, test.ComparePosition(test.GetCRDT()[2].GetIdentifier(), test.GetCRDT()[2].GetIdentifier()));
            Assert.AreEqual("HALLO (:!", test.PrintString());
        }

        [Test]
        public void TestToString()
        {
            CRDT test = new CRDT("1", "test");
            test.AddChar('H', 0);
            test.AddChar('A', 1);
            test.AddChar('L', 2);
            test.AddChar('O', 3);
            test.AddChar('L', 2);
            test.AddChar(' ', 5);
            test.AddChar('!', 6);
            Assert.AreEqual("H [(0, 1), (1, 1)] A [(0, 1), (2, 1)] L [(0, 1), (2, 1), (1, 1)] L [(0, 1), (3, 1)] O [(0, 1), (4, 1)]   [(0, 1), (5, 1)] ! [(0, 1), (6, 1)] ",
                test.ToString());
        }

        [Test]
        public void TestPrintString()
        {
            CRDT test = new CRDT("1", "test");
            test.AddChar('H', 0);
            test.AddChar('A', 1);
            test.AddChar('L', 2);
            test.AddChar('O', 3);
            test.AddChar('L', 2);
            test.AddChar(' ', 5);
            test.AddChar('!', 6);
            Assert.AreEqual("HALLO !", test.PrintString());
        }

        [Test]
        public void TestFind()
        {
            //This test also covers find
            string id = "test";
            CRDT crdt = new CRDT(id, id);
            crdt.AddChar('a', 0);
            crdt.AddChar('b', 1);
            crdt.AddChar('c', 2);
            crdt.AddChar('d', 3);
            crdt.AddChar('e', 4);
            crdt.AddChar('f', 5);
            crdt.AddChar('g', 6);
            crdt.AddChar('h', 7);
            crdt.AddChar('i', 8);

            Identifier[] wrong = { new Identifier(99, "99"), new Identifier(22, "22"), new Identifier(77, "77") };
            Assert.AreEqual(0, crdt.Find(crdt.GetCRDT()[0].GetIdentifier()).Item1);
            Debug.Log("works");
            Assert.AreEqual(1, crdt.Find(crdt.GetCRDT()[1].GetIdentifier()).Item1);
            Assert.AreEqual(2, crdt.Find(crdt.GetCRDT()[2].GetIdentifier()).Item1);
            Debug.Log("works2");
            Assert.AreEqual(3, crdt.Find(crdt.GetCRDT()[3].GetIdentifier()).Item1);
            Assert.AreEqual(7, crdt.Find(crdt.GetCRDT()[7].GetIdentifier()).Item1);
            Assert.AreEqual(8, crdt.Find(crdt.GetCRDT()[8].GetIdentifier()).Item1);
            Debug.Log("works");
            Assert.AreEqual(-1, crdt.Find(wrong).Item1);

            Assert.AreEqual(crdt.GetCRDT()[0], crdt.Find(crdt.GetCRDT()[0].GetIdentifier()).Item2);
            Assert.AreEqual(crdt.GetCRDT()[1], crdt.Find(crdt.GetCRDT()[1].GetIdentifier()).Item2);
            Assert.AreEqual(crdt.GetCRDT()[2], crdt.Find(crdt.GetCRDT()[2].GetIdentifier()).Item2);
            Assert.AreEqual(crdt.GetCRDT()[3], crdt.Find(crdt.GetCRDT()[3].GetIdentifier()).Item2);
            Assert.AreEqual(crdt.GetCRDT()[7], crdt.Find(crdt.GetCRDT()[7].GetIdentifier()).Item2);
            Assert.AreEqual(crdt.GetCRDT()[8], crdt.Find(crdt.GetCRDT()[8].GetIdentifier()).Item2);
            Assert.AreEqual(null, crdt.Find(wrong).Item2);
        }

        [Test]
        public void TestIsEmpty()
        {
            CRDT crdt = new CRDT("test", "test");
            Assert.IsTrue(crdt.IsEmpty());
            crdt.AddChar('c', 0);
            Assert.IsFalse(crdt.IsEmpty());
        }

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
