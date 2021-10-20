using NUnit.Framework;
using SEE.Utils;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static SEE.Utils.CRDT;

namespace SEETests
{
    public class TestCRDT
    {
        [Test]
        public void testPositionToString()
        {
            CRDT crdt = new CRDT(new GUID().ToString(), "test");
            Identifier[] pos = new Identifier[] { new Identifier(1, "1"), new Identifier(2, "1"), new Identifier(3, "1") };
            Assert.AreEqual("(1, 1), (2, 1), (3, 1)", crdt.PositionToString(pos));
        }

        [Test]
        public void testStringToPosition()
        {
            CRDT crdt = new CRDT(new GUID().ToString(), "test");
            Identifier[] pos = new Identifier[] {new Identifier(1, "1"), new Identifier(2, "1"), new Identifier(3, "1") };
            Debug.LogWarning(pos[0] + " to string "  + crdt.StringToPosition("(1, 1), (2, 1), (3, 1)")[0]);

            Assert.AreEqual(0, crdt.ComparePosition(pos, crdt.StringToPosition("(1, 1), (2, 1), (3, 1)")));
        }

        [Test]
        public void testDeleteChar()
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
        public void testRemoteDeleteChar()
        {
            CRDT test = new CRDT(new GUID().ToString(), "test");
            test.AddChar('H', 0);
            test.AddChar('A', 1);
            test.AddChar('L', 2);
            test.AddChar('O', 3);
            test.AddChar('L', 2);
            test.AddChar(' ', 5);
            test.AddChar('!', 6);

            test.RemoteDeleteChar(test.getCRDT()[2].GetIdentifier());
            Assert.AreEqual("HALO !", test.PrintString());
            test.RemoteDeleteChar(test.getCRDT()[3].GetIdentifier());
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
            crdt2.AddChar('a', 0);
            crdt2.AddChar('a', 0);

            crdt2.RemoteAddChar('A', crdt1.getCRDT()[0].GetIdentifier(), null);
            crdt1.RemoteAddChar('a', crdt2.getCRDT()[1].GetIdentifier(), null);
            crdt1.RemoteAddChar('a', crdt2.getCRDT()[1].GetIdentifier(), null);
            Assert.AreEqual("Aaa", crdt1.PrintString());
            Assert.AreEqual("Aaa", crdt2.PrintString());
        }

        [Test]
        public void testAddChar()
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
            Assert.AreEqual(-1, test.ComparePosition(test.getCRDT()[0].GetIdentifier(), test.getCRDT()[1].GetIdentifier()));
            Assert.AreEqual(-1, test.ComparePosition(test.getCRDT()[0].GetIdentifier(), test.getCRDT()[2].GetIdentifier()));
            Assert.AreEqual(-1, test.ComparePosition(test.getCRDT()[0].GetIdentifier(), test.getCRDT()[3].GetIdentifier()));
            Assert.AreEqual(-1, test.ComparePosition(test.getCRDT()[0].GetIdentifier(), test.getCRDT()[4].GetIdentifier()));
            Assert.AreEqual(-1, test.ComparePosition(test.getCRDT()[1].GetIdentifier(), test.getCRDT()[2].GetIdentifier()));
            Assert.AreEqual(-1, test.ComparePosition(test.getCRDT()[1].GetIdentifier(), test.getCRDT()[3].GetIdentifier()));
            Assert.AreEqual(-1, test.ComparePosition(test.getCRDT()[2].GetIdentifier(), test.getCRDT()[3].GetIdentifier()));
            Assert.AreEqual(1, test.ComparePosition(test.getCRDT()[6].GetIdentifier(), test.getCRDT()[1].GetIdentifier()));
            Assert.AreEqual(0, test.ComparePosition(test.getCRDT()[2].GetIdentifier(), test.getCRDT()[2].GetIdentifier()));
            Assert.AreEqual("HALLO (:!", test.PrintString());
        }

        [Test]
        public void testRemoteAdd()
        {
            CRDT crdt1 = new CRDT("1", "test");
            CRDT crdt2 = new CRDT("2", "test");

            crdt1.AddChar('H', 0);
            crdt2.RemoteAddChar('H', crdt1.getCRDT()[0].GetIdentifier(), null);

            crdt1.AddChar('A', 1);
            crdt2.RemoteAddChar('A', crdt1.getCRDT()[1].GetIdentifier(), crdt1.getCRDT()[0].GetIdentifier());

            crdt1.AddChar('L', 2);
            crdt2.RemoteAddChar('L', crdt1.getCRDT()[2].GetIdentifier(), crdt1.getCRDT()[1].GetIdentifier());

            crdt1.AddChar('O', 3);
            crdt2.RemoteAddChar('O', crdt1.getCRDT()[3].GetIdentifier(), crdt1.getCRDT()[2].GetIdentifier());

            //Simulate sync problems
            crdt2.AddChar('l', 2);

            crdt1.AddChar('!', 4);
            crdt2.RemoteAddChar('!', crdt1.getCRDT()[4].GetIdentifier(), crdt1.getCRDT()[3].GetIdentifier());

            crdt1.RemoteAddChar('l', crdt2.getCRDT()[2].GetIdentifier(), crdt2.getCRDT()[1].GetIdentifier());
            //End sync problem
            crdt1.AddChar('_', 0);
            crdt2.RemoteAddChar('_', crdt1.getCRDT()[0].GetIdentifier(), null);


            print(crdt1);
            print(crdt2);
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
            Assert.AreEqual(0, crdt.Find(crdt.getCRDT()[0].GetIdentifier()).Item1);
            Debug.Log("works");
            Assert.AreEqual(1, crdt.Find(crdt.getCRDT()[1].GetIdentifier()).Item1);
            Assert.AreEqual(2, crdt.Find(crdt.getCRDT()[2].GetIdentifier()).Item1);
            Debug.Log("works2");
            Assert.AreEqual(3, crdt.Find(crdt.getCRDT()[3].GetIdentifier()).Item1);
            Assert.AreEqual(7, crdt.Find(crdt.getCRDT()[7].GetIdentifier()).Item1);
            Assert.AreEqual(8, crdt.Find(crdt.getCRDT()[8].GetIdentifier()).Item1);
            Debug.Log("works");
            Assert.AreEqual(-1, crdt.Find(wrong).Item1);

            Assert.AreEqual(crdt.getCRDT()[0], crdt.Find(crdt.getCRDT()[0].GetIdentifier()).Item2);
            Assert.AreEqual(crdt.getCRDT()[1], crdt.Find(crdt.getCRDT()[1].GetIdentifier()).Item2);
            Assert.AreEqual(crdt.getCRDT()[2], crdt.Find(crdt.getCRDT()[2].GetIdentifier()).Item2);
            Assert.AreEqual(crdt.getCRDT()[3], crdt.Find(crdt.getCRDT()[3].GetIdentifier()).Item2);
            Assert.AreEqual(crdt.getCRDT()[7], crdt.Find(crdt.getCRDT()[7].GetIdentifier()).Item2);
            Assert.AreEqual(crdt.getCRDT()[8], crdt.Find(crdt.getCRDT()[8].GetIdentifier()).Item2);
            Assert.AreEqual(null, crdt.Find(wrong).Item2);
        }

        [Test]
        public void testIsEmpty()
        {
            CRDT crdt = new CRDT("test", "test");
            Assert.IsTrue(crdt.IsEmpty());
            crdt.AddChar('c', 0);
            Assert.IsFalse(crdt.IsEmpty());
        }

        private void print(CRDT crdt)
        {
            Debug.LogWarning("CRDT: " + crdt.ToString());
            Debug.LogWarning(crdt.PrintString());
        }



    }
}