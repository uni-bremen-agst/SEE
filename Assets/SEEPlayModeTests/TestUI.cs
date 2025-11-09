using System.Collections;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using NUnit.Framework.Interfaces;
using NUnit.Framework;
using UnityEngine;

namespace SEE.UI
{
    /// <summary>
    /// Abstract super class of all UI tests. It takes care of the setup
    /// and tear down of all tests.
    /// </summary>
    internal abstract class TestUI
    {
        /// <summary>
        /// Setup for a test.
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
        public IEnumerator SetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            User.UserSettings.Instance.InputType = GO.PlayerInputType.DesktopPlayer;
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

        /// <summary>
        /// An attribute (annotation) that can be specified for a test method
        /// to load a scene before the test is run.
        /// </summary>
        protected class LoadSceneAttribute : NUnitAttribute, IOuterUnityTestAction
        {
            /// <summary>
            /// Folder containing the test scene.
            /// </summary>
            private const string SceneFolder = "Assets/Scenes/";
            /// <summary>
            /// Unity's file extension for scene files.
            /// </summary>
            private const string SceneFileExtension = ".unity";
            /// <summary>
            /// The name of the scene given by the constructor.
            /// </summary>
            private readonly string scene;

            /// <summary>
            /// Makes sure that the given <paramref name="scene"/> is loaded before
            /// the test is run. The actual scene is loaded from the path
            /// <see cref="SceneFolder"/> + <paramref name="scene"/> + <see cref="SceneFileExtension"/>.
            ///
            /// Assumptions: <paramref name="scene"/> is contained in folder
            /// <see cref="SceneFolder"/> and does not have the <see cref="SceneFileExtension"/>
            /// (that extension will be added automatically).
            /// </summary>
            /// <param name="scene">name of the test scene to be loaded</param>
            public LoadSceneAttribute(string scene = "Empty") => this.scene = SceneFolder + scene + SceneFileExtension;

            /// <summary>
            /// Run before the <paramref name="test"/> to load the scene.
            /// </summary>
            /// <param name="test">test to be run</param>
            /// <returns>An <see cref="AsyncOperation"/> determining whether the
            /// scene was loaded</returns>
            IEnumerator IOuterUnityTestAction.BeforeTest(ITest test)
            {
                yield return EditorSceneManager.LoadSceneAsyncInPlayMode(scene, new LoadSceneParameters(LoadSceneMode.Single));
            }

            /// <summary>
            /// Run before the <paramref name="test"/>. Does not do anything.
            /// </summary>
            /// <param name="test">test to be run</param>
            /// <returns>alway <c>null</c></returns>
            IEnumerator IOuterUnityTestAction.AfterTest(ITest test)
            {
                yield return null;
            }
        }
    }
}
