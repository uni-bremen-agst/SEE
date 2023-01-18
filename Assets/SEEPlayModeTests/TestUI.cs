using SEE.Controls;
using System.Collections;
using UnityEngine.TestTools;

namespace SEE.Game.UI
{
    /// <summary>
    /// Abstract super class of all UI tests. It takes for the setup and tear down of
    /// all tests.
    /// </summary>
    internal abstract class TestUI
    {
        /// <summary>
        /// Set up for a test.
        /// The playmode will be entered.
        /// The <see cref="SceneSettings.InputType"/> will be <see cref="SEE.GO.PlayerInputType.DesktopPlayer"/>.
        ///
        /// Note: Subclasses may have their own method tagged by UnitySetUp,
        /// which will then be called after this method.
        ///
        /// </summary>
        /// <returns><see cref="EnterPlayMode"/></returns>
        /// <remarks>Called before each test. See
        /// https://docs.unity3d.com/Packages/com.unity.test-framework@1.1/manual/reference-unitysetup-and-unityteardown.html
        ///
        /// Method must be public. Otherwise it will not be called by the test framework.
        /// </remarks>
        [UnitySetUp]
        public IEnumerator Setup()
        {
            LogAssert.ignoreFailingMessages = true;
            SceneSettings.InputType = SEE.GO.PlayerInputType.DesktopPlayer;
            yield return new EnterPlayMode();
        }

        /// <summary>
        /// Tear down after a test.
        /// The playmode will be left.
        ///
        /// Note: Subclasses may have their own method tagged by UnityTearDown,
        /// which will then be called before this method.
        /// </summary>
        /// <returns><see cref="ExitPlayMode"/></returns>
        /// <remarks>Called before each test. See
        /// https://docs.unity3d.com/Packages/com.unity.test-framework@1.1/manual/reference-unitysetup-and-unityteardown.html
        ///
        /// Method must be public. Otherwise it will not be called by the test framework.
        /// </remarks>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return new ExitPlayMode();
        }
    }
}
