using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SEE.Net
{
    /// <summary>
    /// Base class for all play-mode tests that need to start the game from
    /// the StartScene and then then the WorldScene. It will also test the network
    /// set up.
    ///
    /// The <see cref="SetUp"/> method will load <see cref="StartScene"/> and then
    /// simulate the user pressing the host configuration in the opening dialog.
    /// Accordingly, the world scene will be loaded.
    ///
    /// The <see cref="TearDown"/> method will unload the world scene, so that the
    /// next test case can run from scratch.
    /// </summary>
    internal class TestSEEGame
    {
        /// <summary>
        /// The scene to be loaded to start the tests. It is the one
        /// in which a user can select the network configuration.
        /// This scene will be loaded in <see cref="SetUp"/> before each
        /// test case.
        /// </summary>
        private const string StartScene = "SEEStart";

        /// <summary>
        /// The name of the empty scene that is used to trigger the unload
        /// the currently loaded scene. There must be always one loaded scene,
        /// that is why we cannot use <see cref="SceneManager.UnloadScene(Scene)"/>.
        /// The trick is to load the <see cref="EmptyScene"/> to replace the
        /// currently loaded scene.
        /// </summary>
        private const string EmptyScene = "Empty";

        /// <summary>
        /// The name of the currently loaded scene.
        /// </summary>
        private string currentlyLoadedScene = string.Empty;

        /// <summary>
        /// A callback called when the <paramref name="scene"/> has been loaded.
        /// Sets <see cref="currentlyLoadedScene"/> to the loaded scene's name
        /// (if its different from <see cref="EmptyScene"/>; the latter is only
        /// loaded to trigger the unload of the currently loaded scene).
        /// </summary>
        /// <param name="scene">the loaded scene</param>
        /// <param name="mode">the mode in which the scene was loaded</param>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[OnSceneLoaded] Scene {scene.name} loaded in mode {mode}.\n");
            if (scene.name != EmptyScene)
            {
                currentlyLoadedScene = scene.name;
            }
        }

        /// <summary>
        /// A callback called when the <paramref name="scene"/> has been unloaded.
        /// Resets <see cref="currentlyLoadedScene"/> to the empty string.
        /// </summary>
        /// <param name="scene">the unloaded scene</param>
        private void OnSceneUnloaded(Scene scene)
        {
            Debug.Log($"[OnSceneUnloaded] Scene {scene.name} unloaded.\n");
            currentlyLoadedScene = string.Empty;
        }

        /// <summary>
        /// Called exactly once before the very first <see cref="SetUp"/>.
        /// Registers <see cref="OnSceneLoaded(Scene, LoadSceneMode)"/>
        /// and <see cref="OnSceneUnloaded(Scene)"/> at the <see cref="SceneManager"/>.
        /// </summary>
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        /// <summary>
        /// Run before every test case. It will wait until the <see cref="currentlyLoadedScene"/>
        /// has been unloaded. Then it will load <see cref="StartScene"/> (and wait until its
        /// loading has been completed) in order to simulate pressing the Host button
        /// (identified by <see cref="HostButtonPath"/>) in the dialog to select the network
        /// configuration and start the game. After that, play-mode tests can be run.
        /// </summary>
        /// <returns>as to whether to continue this co-routine</returns>
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Debug.Log($"[SetUp] Wait until prior scene has been unloaded (if any): {currentlyLoadedScene}...\n");
            // Wait until host is shut down.
            yield return new WaitUntil(() => currentlyLoadedScene == "");
            Debug.Log($"[SetUp] Starting the game by loading {StartScene}...\n");

            SceneManager.LoadScene(StartScene, LoadSceneMode.Single);
            // SceneManager.LoadScene loads the scene in the next frame, that is, it does not load
            // immediately. That is why wait until the next frame.
            yield return new WaitForEndOfFrame();

            // Wait until StartScene is loaded.
            Debug.Log($"[SetUp] Waiting for scene {StartScene} to be loaded...\n");
            yield return new WaitUntil(() => currentlyLoadedScene == StartScene);
            Debug.Log($"[SetUp] Scene {StartScene} has been loaded...\n");

            // Make sure we have our network manager.
            Assert.NotNull(Unity.Netcode.NetworkManager.Singleton);

            // Simulate that the Host button in the Network Configuration dialog is
            // pressed by the user.
            Debug.Log($"[SetUp] Pressing button {HostButtonPath}...\n");
            PressButton(HostButtonPath);
            yield return new WaitForEndOfFrame();

            Assert.NotNull(SEE.Net.Network.Instance);
            Debug.Log($"[SetUp] Finished.\n");
            yield return null;
        }

        /// <summary>
        /// Called after the completion of every single test case. It will trigger the
        /// unloading of the <see cref="currentlyLoadedScene"/> by loading <see cref="EmptyScene"/>.
        /// <see cref="currentlyLoadedScene"/> will be the empty string after calling this method.
        /// </summary>
        /// <returns>as to whether to continue this co-routine</returns>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Debug.Log($"[TearDown] Started.\n");

            // Load an empty scene to trigger the unloading of the currently loaded scene.
            SceneManager.LoadScene(EmptyScene, LoadSceneMode.Single);
            // SceneManager.LoadScene loads the scene in the next frame, that is, it does not load
            // immediately. That is why wait until the next frame.
            yield return new WaitForEndOfFrame();

            currentlyLoadedScene = string.Empty;
            Debug.Log($"[TearDown] Finished.\n");
            yield return null;
        }

        /// <summary>
        /// Called exactly once after the very last <see cref="TearDown"/>.
        /// Unregisters <see cref="OnSceneLoaded(Scene, LoadSceneMode)"/>
        /// and <see cref="OnSceneUnloaded(Scene)"/> from the <see cref="SceneManager"/>.
        /// </summary>
        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        /// <summary>
        /// The full name of the button to select the "Host" network configuration.
        /// </summary>
        private const string HostButtonPath = "/UI Canvas/Network Configuration/Main Content/Content Mask/Content/Menu Entries/Scroll Area/List/Host";

        /// <summary>
        /// Simulates that a user presses the button identified by <paramref name="buttonPath"/>.
        /// </summary>
        /// <param name="buttonPath">the path name of the game object holding a <see cref="Button"/> component</param>
        private static void PressButton(string buttonPath)
        {
            // Retrieve the button
            GameObject buttonObject = GameObject.Find(buttonPath);
            Assert.NotNull(buttonObject);
            // Make sure the object is really holding a button.
            Assert.That(buttonObject.TryGetComponent(out Button _));
            // Press the button.
            ExecuteEvents.Execute(buttonObject.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
        }
    }
}
