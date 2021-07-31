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
        public void testKommutativ()
        {
            CRDT crdt1 = new CRDT(1);
            CRDT cradt2 = new CRDT(2);
        }

        private void print(CRDT crdt)
        {
            string ret = "";
            if(crdt.getCRDT() == null)
            {
                Debug.LogWarning("CRDT EMPTY");
                return;
            }
            foreach(CharObj elm in crdt.getCRDT())
            {
                if(elm != null)
                {
                    ret += elm.ToString();
                }
            }
            Debug.LogWarning("CRDT: " + ret);
        }
        
    }
}