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
        public void testInput()
        {
            CRDT test = new CRDT(1);
            print(test);
            test.addChar('H', 0);
            
            test.addChar('A', 1);
            
            test.addChar('L', 2);
            print(test);
            test.addChar('O', 3);
            print(test);
            test.addChar('L', 2);
            print(test);
            Debug.LogWarning(test.text[4].ToString());
            test.addChar(' ', 5);
            print(test);
            test.addChar('!', 6);
            print(test);
            Assert.AreEqual(-1, test.ComparePosition(test.text[0].GetIdentifier(), test.text[1].GetIdentifier()));
            Assert.AreEqual(-1, test.ComparePosition(test.text[0].GetIdentifier(), test.text[2].GetIdentifier()));
            Assert.AreEqual(-1, test.ComparePosition(test.text[0].GetIdentifier(), test.text[3].GetIdentifier()));
            Assert.AreEqual(-1, test.ComparePosition(test.text[0].GetIdentifier(), test.text[4].GetIdentifier()));
            Assert.AreEqual(-1, test.ComparePosition(test.text[1].GetIdentifier(), test.text[2].GetIdentifier()));
            Assert.AreEqual(-1, test.ComparePosition(test.text[1].GetIdentifier(), test.text[3].GetIdentifier()));
            Assert.AreEqual(-1, test.ComparePosition(test.text[2].GetIdentifier(), test.text[3].GetIdentifier()));




        }

        [Test]
        public void testKommutativ()
        {
            CRDT crdt1 = new CRDT();
        }

        private void print(CRDT crdt)
        {
            string ret = "";
            if(crdt.text == null)
            {
                Debug.LogWarning("CRDT EMPTY");
                return;
            }
            foreach(CharObj elm in crdt.text)
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