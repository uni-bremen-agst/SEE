using NUnit.Framework;
using System.Reflection;
using UnityEngine;

/// <summary>
/// User interface components and their tests.
/// </summary>
namespace SEE.UI
{
    /// <summary>
    /// Tests startup reentrancy and callbacks received after the menu has been destroyed.
    /// </summary>
    internal class TestOpeningDialog
    {
        /// <summary>
        /// The inactive test object, which never starts a real network connection.
        /// </summary>
        private GameObject dialogObject;
        /// <summary>
        /// The controller under test.
        /// </summary>
        private OpeningDialog dialog;
        /// <summary>
        /// The binding flags used to exercise private Unity lifecycle callbacks.
        /// </summary>
        private const BindingFlags instancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;
        /// <summary>
        /// Creates an inactive controller without running its scene-dependent Awake or Start methods.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            dialogObject = new("Opening dialog test");
            dialogObject.SetActive(false);
            dialog = dialogObject.AddComponent<OpeningDialog>();
        }

        /// <summary>
        /// Removes the inactive controller created by the test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(dialogObject);
        }

        /// <summary>
        /// Ignores a second startup request while an earlier request is pending.
        /// </summary>
        [Test]
        public void IgnoresDuplicateStartup()
        {
            FieldInfo starting = typeof(OpeningDialog).GetField("starting", instancePrivate);
            MethodInfo startNetwork = typeof(OpeningDialog).GetMethod("StartNetwork", instancePrivate);
            starting.SetValue(dialog, true);
            Assert.That(() => startNetwork.Invoke(dialog, new object[] { true }), Throws.Nothing);
            Assert.That(starting.GetValue(dialog), Is.True);
        }

        /// <summary>
        /// Safely completes a pending request if the view no longer exists.
        /// </summary>
        /// <param name="success">The result reported by the network service.</param>
        [TestCase(true)]
        [TestCase(false)]
        public void IgnoresLateCallbackWithoutView(bool success)
        {
            FieldInfo starting = typeof(OpeningDialog).GetField("starting", instancePrivate);
            MethodInfo callback = typeof(OpeningDialog).GetMethod("NetworkCallBack", instancePrivate);
            starting.SetValue(dialog, true);
            Assert.That(() => callback.Invoke(dialog, new object[] { success, "Test result" }), Throws.Nothing);
            Assert.That(starting.GetValue(dialog), Is.False);
        }
    }
}
