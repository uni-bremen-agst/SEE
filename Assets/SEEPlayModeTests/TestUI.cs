using NUnit.Framework;
using NUnit.Framework.Interfaces;
using System.Collections;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using SEE.UserSettings;

/// <summary>
/// User interface components and their tests.
/// </summary>
namespace SEE.UI
{
    /// <summary>
    /// Sets up desktop input for UI tests running in Play Mode.
    /// </summary>
    internal abstract class TestUI
    {
        /// <summary>
        /// The settings used by the current test.
        /// </summary>
        private UserSetting userSetting;

        /// <summary>
        /// The input type restored after a test using existing settings.
        /// </summary>
        private PlayerInputType previousInputType;

        /// <summary>
        /// The settings object created when the test scene has no settings.
        /// </summary>
        private GameObject createdSettings;

        /// <summary>
        /// The UI canvas created by this test when no scene canvas exists.
        /// </summary>
        private GameObject createdUICanvas;

        /// <summary>
        /// Whether the current test owns a UI canvas that must be
        /// removed during teardown.
        /// </summary>
        protected bool OwnsUICanvas => createdUICanvas != null;

        /// <summary>
        /// Ensures that a UI canvas is available for drawable menus.
        /// An existing scene canvas is reused and is never owned
        /// or destroyed by this test fixture.
        /// </summary>
        protected void EnsureUICanvas()
        {
            if (GameObject.Find("UI Canvas") == null)
            {
                createdUICanvas = new GameObject(
                    "UI Canvas",
                    typeof(RectTransform),
                    typeof(Canvas));
            }
        }

        /// <summary>
        /// Sets up desktop input in the Play Mode already entered by the test runner.
        /// </summary>
        /// <returns>An enumerator that waits one frame for UI initialization.</returns>
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            userSetting = Object.FindAnyObjectByType<UserSetting>();
            if (userSetting == null)
            {
                createdSettings = new("UI test settings");
                userSetting = createdSettings.AddComponent<UserSetting>();
                userSetting.enabled = false;
            }
            previousInputType = userSetting.InputType;
            userSetting.InputType = PlayerInputType.DesktopPlayer;
            yield return null;
        }

        /// <summary>
        /// Restores user settings and removes objects created by the
        /// shared UI test setup, including an optional UI canvas.
        /// Derived fixtures must release their menus before this
        /// teardown destroys an owned canvas.
        /// </summary>
        /// <returns>
        /// An enumerator that waits for object destruction.
        /// </returns>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (createdUICanvas != null)
            {
                Object.Destroy(createdUICanvas);
                createdUICanvas = null;
            }

            if (createdSettings != null)
            {
                Object.Destroy(createdSettings);
                createdSettings = null;
            }
            else if (userSetting != null)
            {
                userSetting.InputType = previousInputType;
            }

            yield return null;
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
            private const string sceneFolder = "Assets/Scenes/";
            /// <summary>
            /// Unity's file extension for scene files.
            /// </summary>
            private const string sceneFileExtension = ".unity";
            /// <summary>
            /// The name of the scene given by the constructor.
            /// </summary>
            private readonly string scene;

            /// <summary>
            /// Makes sure that the given <paramref name="scene"/> is loaded before
            /// the test is run. The actual scene is loaded from the path
            /// <see cref="sceneFolder"/> + <paramref name="scene"/> + <see cref="sceneFileExtension"/>.
            ///
            /// Assumptions: <paramref name="scene"/> is contained in folder
            /// <see cref="sceneFolder"/> and does not have the <see cref="sceneFileExtension"/>
            /// (that extension will be added automatically).
            /// </summary>
            /// <param name="scene">Name of the test scene to be loaded.</param>
            public LoadSceneAttribute(string scene = "Empty") => this.scene = sceneFolder + scene + sceneFileExtension;

            /// <summary>
            /// Run before the <paramref name="test"/> to load the scene.
            /// </summary>
            /// <param name="test">Test to be run.</param>
            /// <returns>An <see cref="AsyncOperation"/> determining whether the
            /// scene was loaded.</returns>
            IEnumerator IOuterUnityTestAction.BeforeTest(ITest test)
            {
                yield return EditorSceneManager.LoadSceneAsyncInPlayMode(scene, new LoadSceneParameters(LoadSceneMode.Single));
            }

            /// <summary>
            /// Waits one frame after the <paramref name="test"/>.
            /// </summary>
            /// <param name="test">Test to be run.</param>
            /// <returns>An enumerator that waits one frame.</returns>
            IEnumerator IOuterUnityTestAction.AfterTest(ITest test)
            {
                yield return null;
            }
        }
    }
}
