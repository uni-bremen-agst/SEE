using NUnit.Framework;
using System.Collections.Generic;

namespace SEE.Utils
{
    /// <summary>
    /// Tests for <see cref="PathGlobbing"/>.
    /// </summary>
    internal class TestPathGlobbing
    {
        private const string helloC = "hello.c";

        private static List<string> EmptyList => new();

        private readonly List<string> hellos = new() { helloC, "hello.cpp", "hello.cs", "helloc" };

        [Test]
        public void TestFilterSimple()
        {
            Assert.AreEqual(new List<string>() { helloC },
                            PathGlobbing.Filter(hellos, new Globbing() { { "*.c", true } }));
        }

        [Test]
        public void TestContraDictingFilter1()
        {
            Assert.AreEqual(new List<string>(),
                            PathGlobbing.Filter(hellos, new Globbing() { { "*.c", true }, { helloC, false } }));
        }

        [Test]
        public void TestContraDictingFilter2()
        {
            Assert.AreEqual(EmptyList,
                            PathGlobbing.Filter(hellos, new Globbing() { { "*.c", false }, { helloC, true } }));
        }

        [Test]
        public void TestContraDictingFilter3()
        {
            Assert.AreEqual(EmptyList,
                            PathGlobbing.Filter(hellos, new Globbing() { { helloC, true }, { "*.c", false } }));
        }

        [Test]
        public void TestContraDictingFilter4()
        {
            Assert.AreEqual(EmptyList,
                            PathGlobbing.Filter(hellos, new Globbing() { { helloC, false }, { "*.c", true } }));
        }

        [Test]
        public void TestNullGlobbing()
        {
            Assert.AreEqual(hellos,
                            PathGlobbing.Filter(hellos, pathGlobbing: null));
        }

        [Test]
        public void TestEmptyGlobbing()
        {
            Assert.AreEqual(EmptyList,
                            PathGlobbing.Filter(hellos, pathGlobbing: new Globbing()));
        }

        [Test]
        public void TestNullPaths1()
        {
            Assert.Throws<System.ArgumentNullException>
                (() => PathGlobbing.Filter(null, pathGlobbing: new Globbing()));
        }

        [Test]
        public void TestNullPaths2()
        {
            Assert.Throws<System.ArgumentNullException>
                (() => PathGlobbing.Filter(null, pathGlobbing: null));
        }

        [Test]
        public void TestToMatcher()
        {
            Assert.AreEqual(new List<string>() { helloC },
                            PathGlobbing.Filter
                               (hellos,
                                PathGlobbing.ToMatcher(new Globbing() { { "*.c", true } })));
        }

        [Test]
        public void TestEmptyMatcher()
        {
            Assert.AreEqual(EmptyList,
                            PathGlobbing.Filter(hellos, matcher: new()));
        }

        [Test]
        public void TestNullPathsMatcher1()
        {
            Assert.Throws<System.ArgumentNullException>
                (() => PathGlobbing.Filter(null, matcher: new ()));
        }

        [Test]
        public void TestNullPathsMatcher2()
        {
            Assert.Throws<System.ArgumentNullException>
                (() => PathGlobbing.Filter(null, matcher: null));
        }
    }
}
