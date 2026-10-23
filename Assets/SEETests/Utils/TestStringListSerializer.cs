using NUnit.Framework;
using System.Collections.Generic;

namespace SEE.Utils
{
    /// <summary>
    /// Test cases for <see cref="StringListSerializer"/>.
    /// </summary>
    internal class TestStringListSerializer
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
            Assert.That(() => StringListSerializer.Serialize(new List<string>() { null, null }),
                        Throws.ArgumentNullException);
        }

        [Test]
        public void TestEmptyList()
        {
            Check(new List<string>() {});
        }

        [Test]
        public void TestNull()
        {
            Assert.That(() => StringListSerializer.Serialize(null), Throws.ArgumentNullException);
        }

        private static void Check(List<string> stringList)
        {
            string serialized = StringListSerializer.Serialize(stringList);
            List<string> unserialized = StringListSerializer.Unserialize(serialized);
            Assert.That(unserialized, Is.EqualTo(stringList),
                        "Unserializing a serialized list must yield the original list.");
        }
    }
}
