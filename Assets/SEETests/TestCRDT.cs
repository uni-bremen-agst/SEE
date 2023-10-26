using NUnit.Framework;
using SEE.Utils;
using UnityEditor;
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
            Identifier[] pos = { new Identifier(1, "1"), new Identifier(2, "1"), new Identifier(3, "1") };
            Assert.AreEqual("(1, 1), (2, 1), (3, 1)", CRDT.PositionToString(pos));
        }

        [Test]
        public void TestStringToPosition()
        {
            CRDT crdt = new(new GUID().ToString(), "test");
            Identifier[] pos = {new Identifier(1, "1"), new Identifier(2, "1"), new Identifier(3, "1") };
            Assert.AreEqual(0, crdt.ComparePosition(pos, CRDT.StringToPosition("(1, 1), (2, 1), (3, 1)")));
        }
    }
}
