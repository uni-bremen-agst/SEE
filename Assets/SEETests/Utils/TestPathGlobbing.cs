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

        private readonly List<string> hellos = new() { helloC, "hello.cpp", "hello.cs", "helloc" };

        [Test]
        public void TestFilterSimple()
        {
            Assert.That(PathGlobbing.Filter(hellos, new Globbing() { { "*.c", true } }),
                        Is.EqualTo(new List<string>() { helloC }));
        }

        [Test]
        public void TestContraDictingFilter1()
        {
            Assert.That(PathGlobbing.Filter(hellos, new Globbing() { { "*.c", true }, { helloC, false } }),
                        Is.Empty);
        }

        [Test]
        public void TestContraDictingFilter2()
        {
            Assert.That(PathGlobbing.Filter(hellos, new Globbing() { { "*.c", false }, { helloC, true } }),
                        Is.Empty);
        }

        [Test]
        public void TestContraDictingFilter3()
        {
            Assert.That(PathGlobbing.Filter(hellos, new Globbing() { { helloC, true }, { "*.c", false } }),
                        Is.Empty);
        }

        [Test]
        public void TestContraDictingFilter4()
        {
            Assert.That(PathGlobbing.Filter(hellos, new Globbing() { { helloC, false }, { "*.c", true } }),
                        Is.Empty);
        }

        [Test]
        public void TestNullGlobbing()
        {
            Assert.That(PathGlobbing.Filter(hellos, pathGlobbing: null), Is.EqualTo(hellos));
        }

        [Test]
        public void TestEmptyGlobbing()
        {
            Assert.That(PathGlobbing.Filter(hellos, pathGlobbing: new Globbing()), Is.Empty);
        }

        [Test]
        public void TestNullPaths1()
        {
            Assert.That(() => PathGlobbing.Filter(null, pathGlobbing: new Globbing()),
                        Throws.ArgumentNullException);
        }

        [Test]
        public void TestNullPaths2()
        {
            Assert.That(() => PathGlobbing.Filter(null, pathGlobbing: null),
                        Throws.ArgumentNullException);
        }

        [Test]
        public void TestToMatcher()
        {
            Assert.That(PathGlobbing.Filter
                            (hellos,
                             PathGlobbing.ToMatcher(new Globbing() { { "*.c", true } })),
                        Is.EqualTo(new List<string>() { helloC }));
        }

        [Test]
        public void TestEmptyMatcher()
        {
            Assert.That(PathGlobbing.Filter(hellos, matcher: new()), Is.Empty);
        }

        [Test]
        public void TestNullPathsMatcher1()
        {
            Assert.That(() => PathGlobbing.Filter(null, matcher: new()),
                        Throws.ArgumentNullException);
        }

        [Test]
        public void TestNullPathsMatcher2()
        {
            Assert.That(() => PathGlobbing.Filter(null, matcher: null),
                        Throws.ArgumentNullException);
        }
    }
}
