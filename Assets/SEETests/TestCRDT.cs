using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static CRDT;

namespace SEETests
{
    public class TestCRDT 
    {
        
        
        [Test]
        public void testAddChar()
        {
            CRDT test = new CRDT(1);
            test.addChar('H', 0);         
            test.addChar('A', 1);            
            test.addChar('L', 2);
            test.addChar('O', 3);
            test.addChar('L', 2);
            test.addChar(' ', 5);
            test.addChar('!', 6);
            print(test);
            Assert.AreEqual(-1, test.ComparePosition(test.getCRDT()[0].GetIdentifier(), test.getCRDT()[1].GetIdentifier()));
            Assert.AreEqual(-1, test.ComparePosition(test.getCRDT()[0].GetIdentifier(), test.getCRDT()[2].GetIdentifier()));
            Assert.AreEqual(-1, test.ComparePosition(test.getCRDT()[0].GetIdentifier(), test.getCRDT()[3].GetIdentifier()));
            Assert.AreEqual(-1, test.ComparePosition(test.getCRDT()[0].GetIdentifier(), test.getCRDT()[4].GetIdentifier()));
            Assert.AreEqual(-1, test.ComparePosition(test.getCRDT()[1].GetIdentifier(), test.getCRDT()[2].GetIdentifier()));
            Assert.AreEqual(-1, test.ComparePosition(test.getCRDT()[1].GetIdentifier(), test.getCRDT()[3].GetIdentifier()));
            Assert.AreEqual(-1, test.ComparePosition(test.getCRDT()[2].GetIdentifier(), test.getCRDT()[3].GetIdentifier()));
            Assert.AreEqual(1, test.ComparePosition(test.getCRDT()[6].GetIdentifier(), test.getCRDT()[1].GetIdentifier()));
            Assert.AreEqual(0, test.ComparePosition(test.getCRDT()[2].GetIdentifier(), test.getCRDT()[2].GetIdentifier()));
        }

        [Test]
        public void testFind()
        {
            CRDT crdt = new CRDT(1);
            crdt.addChar('A', 0);
            crdt.addChar('B', 1);
            crdt.addChar('C', 2);
            Identifier[] wrong = { new Identifier(99, 99), new Identifier(22, 22), new Identifier(77, 77) };

            Assert.AreEqual(0, crdt.Find(crdt.getCRDT()[0].GetIdentifier()).Item1);
            Assert.AreEqual(1, crdt.Find(crdt.getCRDT()[1].GetIdentifier()).Item1);
            Assert.AreEqual(2, crdt.Find(crdt.getCRDT()[2].GetIdentifier()).Item1);
            Assert.AreEqual(-1, crdt.Find(wrong).Item1);

            Assert.AreEqual(crdt.getCRDT()[0], crdt.Find(crdt.getCRDT()[0].GetIdentifier()).Item2);
            Assert.AreEqual(crdt.getCRDT()[1], crdt.Find(crdt.getCRDT()[1].GetIdentifier()).Item2);
            Assert.AreEqual(crdt.getCRDT()[2], crdt.Find(crdt.getCRDT()[2].GetIdentifier()).Item2);
            Assert.AreEqual(null, crdt.Find(wrong).Item2);


            
        }


        [Test]
        public void testRemoteAdd()
        {
            CRDT crdt1 = new CRDT(1);
            CRDT crdt2 = new CRDT(2);

            crdt1.addChar('H', 0);
            crdt2.RemoteAddChar('H', crdt1.getCRDT()[0].GetIdentifier(), null);
            
            crdt1.addChar('A', 1);
            crdt2.RemoteAddChar('A', crdt1.getCRDT()[1].GetIdentifier(), crdt1.getCRDT()[0].GetIdentifier());

            crdt1.addChar('L', 2);
            crdt2.RemoteAddChar('L', crdt1.getCRDT()[2].GetIdentifier(), crdt1.getCRDT()[1].GetIdentifier());

            crdt1.addChar('O', 3);
            crdt2.RemoteAddChar('O', crdt1.getCRDT()[3].GetIdentifier(), crdt1.getCRDT()[2].GetIdentifier());

            //Simulate sync problems
            crdt2.addChar('l', 2);

            crdt1.addChar('!', 4);
            crdt2.RemoteAddChar('!', crdt1.getCRDT()[4].GetIdentifier(), crdt1.getCRDT()[3].GetIdentifier());

            crdt1.RemoteAddChar('l', crdt2.getCRDT()[2].GetIdentifier(), crdt2.getCRDT()[1].GetIdentifier());
            //End sync problem

            print(crdt1);
            print(crdt2);
        }

        private void print(CRDT crdt)
        {
            Debug.LogWarning("CRDT: " + crdt.ToString());
            Debug.LogWarning(crdt.PrintString());
        }
        
    }
}