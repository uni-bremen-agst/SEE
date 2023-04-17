using System.Diagnostics;
using UnityEngine.Assertions;

namespace Asset_Cleaner {
    static class FLAGS {
        //  cleanup in release
        public const string DEBUG = "DEBUG1";
        public const string M_DISABLE_POOLING = "M_DISABLE_POOLING";
    }

    static class Asr {
#line hidden
        [Conditional(FLAGS.DEBUG)]
        public static void AreEqual(int a, int b) {
            Assert.AreEqual(a, b);
        }

        [Conditional(FLAGS.DEBUG)]
        public static void IsTrue(bool b, string format = null) {
            Assert.IsTrue(b, format);
        }

        [Conditional(FLAGS.DEBUG)]
        public static void IsFalse(bool b, string format = null) {
            Assert.IsFalse(b, format);
        }

        [Conditional(FLAGS.DEBUG)]
        public static void IsNotNull(object target, string format = null) {
            Assert.IsNotNull(target, format);
        }
#line default
    }
}