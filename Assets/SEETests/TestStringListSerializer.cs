using NUnit.Framework;
using SEE.Utils;
using System.Collections.Generic;

namespace SEETests
{
    /// <summary>
    /// Test cases for <see cref="SEE.Utils.StringListSerializer"/>.
    /// </summary>
    class TestStringListSerializer
    {
        [Test]
        public void TestMultipleElements()
        {
            Check(new List<string>() { "a", "b", "c" });
        }

        [Test]
        public void TestSingleElement()
        {
            Check(new List<string>() { "a^q@" });
        }

        [Test]
        public void TestEmptyString()
        {
            Check(new List<string>() { "" });
        }

        [Test]
        public void TestNullElements()
        {
            Assert.Throws<System.ArgumentNullException>(() => StringListSerializer.Serialize(new List<string>() { null, null }));
        }

        [Test]
        public void TestEmptyList()
        {
            Check(new List<string>() {});
        }

        [Test]
        public void TestNull()
        {
            Assert.Throws<System.ArgumentNullException>(() => StringListSerializer.Serialize(null));
        }

        private static void Check(List<string> stringList)
        {
            string serialized = StringListSerializer.Serialize(stringList);
            List<string> unserialized = StringListSerializer.Unserialize(serialized);
            Assert.AreEqual(stringList, unserialized);
        }
    }
}
