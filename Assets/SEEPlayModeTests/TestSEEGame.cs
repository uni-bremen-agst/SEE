using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using SEE.UserSettings;

/// <summary>
/// Tests for SEE networking.
/// </summary>
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
        /// <returns>An enumerator that waits for the scene transition.</returns>
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Debug.Log($"[SetUp] Wait until prior scene has been unloaded (if any): {currentlyLoadedScene}...\n");
            // Wait until host is shut down.
            float previousSceneDeadline = Time.realtimeSinceStartup + 10f;
            while (currentlyLoadedScene != string.Empty && Time.realtimeSinceStartup < previousSceneDeadline)
            {
                yield return null;
            }
            Assert.That(currentlyLoadedScene, Is.Empty, "The previous test scene was not unloaded.");
            Debug.Log($"[SetUp] Starting the game by loading {StartScene}...\n");

            SceneManager.LoadScene(StartScene, LoadSceneMode.Single);
            // SceneManager.LoadScene loads the scene in the next frame, that is, it does not load
            // immediately. That is why wait until the next frame.
            yield return new WaitForEndOfFrame();

            // Wait until StartScene is loaded.
            Debug.Log($"[SetUp] Waiting for scene {StartScene} to be loaded...\n");
            float startSceneDeadline = Time.realtimeSinceStartup + 30f;
            while (currentlyLoadedScene != StartScene && Time.realtimeSinceStartup < startSceneDeadline)
            {
                yield return null;
            }
            Assert.That(currentlyLoadedScene, Is.EqualTo(StartScene), "The start scene did not load.");
            Debug.Log($"[SetUp] Scene {StartScene} has been loaded...\n");

            // Make sure we have our network manager.
            Assert.That(Unity.Netcode.NetworkManager.Singleton, Is.Not.Null,
                        $"There is no {nameof(Unity.Netcode.NetworkManager)} in scene {StartScene}.");

            // Simulate that the Host button in the Network Configuration dialog is
            // pressed by the user.
            float menuDeadline = Time.realtimeSinceStartup + 10f;
            while (GameObject.Find(HostButtonPath) == null && Time.realtimeSinceStartup < menuDeadline)
            {
                yield return null;
            }
            Debug.Log($"[SetUp] Pressing button {HostButtonPath}...\n");
            PressButton(HostButtonPath);
            yield return new WaitForEndOfFrame();

            Assert.That(UserSetting.Instance, Is.Not.Null,
                        $"There is no {nameof(UserSetting.Instance)} instance after pressing {HostButtonPath}.");
            Debug.Log($"[SetUp] Finished.\n");
            yield return null;
        }

        /// <summary>
        /// Shuts down the network after each test case and triggers the
        /// unloading of the <see cref="currentlyLoadedScene"/> by loading <see cref="EmptyScene"/>.
        /// <see cref="currentlyLoadedScene"/> will be the empty string after calling this method.
        /// </summary>
        /// <returns>An enumerator that waits for the scene transition.</returns>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Debug.Log($"[TearDown] Started.\n");

            bool shutdownCompleted = true;
            try
            {
                Unity.Netcode.NetworkManager networkManager = Unity.Netcode.NetworkManager.Singleton;
                if (networkManager != null)
                {
                    if (networkManager.IsListening)
                    {
                        networkManager.Shutdown();
                    }
                    float shutdownDeadline = Time.realtimeSinceStartup + 10f;
                    while (networkManager != null && networkManager.ShutdownInProgress
                           && Time.realtimeSinceStartup < shutdownDeadline)
                    {
                        yield return null;
                    }
                    shutdownCompleted = networkManager == null || !networkManager.ShutdownInProgress;
                }
            }
            finally
            {
                SceneManager.LoadScene(EmptyScene, LoadSceneMode.Single);
                currentlyLoadedScene = string.Empty;
            }

            float emptySceneDeadline = Time.realtimeSinceStartup + 30f;
            while (SceneManager.GetActiveScene().name != EmptyScene && Time.realtimeSinceStartup < emptySceneDeadline)
            {
                yield return null;
            }
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(EmptyScene),
                        "The test scene was not unloaded.");
            Assert.That(shutdownCompleted, Is.True, "The network did not shut down before unloading the test scene.");
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
        private const string HostButtonPath = "/UI Canvas/Configuration/Main Content/Content Mask/Content/Menu Entries/Scroll Area/List/Host";

        /// <summary>
        /// Simulates that a user presses the button identified by <paramref name="buttonPath"/>.
        /// </summary>
        /// <param name="buttonPath">the path name of the game object holding a <see cref="Button"/> component</param>
        private static void PressButton(string buttonPath)
        {
            // Retrieve the button
            GameObject buttonObject = GameObject.Find(buttonPath);
            Assert.That(buttonObject, Is.Not.Null, $"Button path {buttonPath} not found.");
            // Make sure the object is really holding a button.
            Assert.That(buttonObject.GetComponent<Button>(), Is.Not.Null,
                        $"Game object {buttonPath} has no {nameof(Button)} component.");
            // Press the button.
            ExecuteEvents.Execute(buttonObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
        }
    }
}
