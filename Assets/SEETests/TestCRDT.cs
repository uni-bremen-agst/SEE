using NUnit.Framework;
using SEE.Utils;
using UnityEditor;
using UnityEngine;
using static SEE.Utils.CRDT;

namespace SEETests
{

    /// <summary>
    /// Tests functions of CRDT that do not require a working network connection.
    /// </summary>
    public class TestCRDT
    {
        [Test]
        public void TestPositionToString()
        {
            CRDT crdt = new CRDT(new GUID().ToString(), "test");
            Identifier[] pos = new Identifier[] { new Identifier(1, "1"), new Identifier(2, "1"), new Identifier(3, "1") };
            Assert.AreEqual("(1, 1), (2, 1), (3, 1)", crdt.PositionToString(pos));
        }

        [Test]
        public void TestStringToPosition()
        {
            CRDT crdt = new CRDT(new GUID().ToString(), "test");
            Identifier[] pos = new Identifier[] {new Identifier(1, "1"), new Identifier(2, "1"), new Identifier(3, "1") };
            Debug.LogWarning(pos[0] + " to string "  + crdt.StringToPosition("(1, 1), (2, 1), (3, 1)")[0]);

            Assert.AreEqual(0, crdt.ComparePosition(pos, crdt.StringToPosition("(1, 1), (2, 1), (3, 1)")));
        }
    }
}