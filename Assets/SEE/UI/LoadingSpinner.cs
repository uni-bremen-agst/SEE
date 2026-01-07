using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using SEE.GO;
using SEE.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace SEE.UI
{
    /// <summary>
    /// A loading spinner that can be shown and hidden for both indeterminate and determinate processes.
    ///
    /// A message can also be specified that will be shown below the spinner.
    /// Note that the message must be unique to your loading process, otherwise another loading process
    /// with the same message may interfere with your spinner.
    /// The spinner will be shown as long as there is at least one active loading process.
    ///
    /// The recommended way to use this is in a using block.
    /// For example, to show an indeterminate spinner (i.e., no progress updates):
    /// <code>
    /// using (LoadingSpinner.ShowIndeterminate("Loading message"))  // Message must be unique to your loading process.
    /// {
    ///     // Do something that takes a long time.
    ///     // The loading spinner will be shown while this is running,
    ///     // and hidden once control flow leaves this block.
    /// }
    /// </code>
    ///
    /// Or, to show a determinate spinner (i.e., with incremental progress updates):
    /// <code>
    /// using (LoadingSpinner.ShowDeterminate("Loading message", out Action&lt;float&gt; updateProgress))
    /// {
    ///     // Do something that takes a long time.
    ///     // Call updateProgress with a float from 0 to 1 to update the progress of the spinner,
    ///     // for example, updateProgress(0.5f) will set the progress to 50%.
    ///     // The loading spinner will be shown until control flow leaves this block.
    /// }
    /// </code>
    /// Alternatively, you can call <see cref="IDisposable.Dispose"/> manually on the object
    /// returned by <see cref="ShowIndeterminate"/> or <see cref="ShowDeterminate"/> to mark the
    /// end of your loading process and hide the spinner.
    /// You can also call <see cref="Hide"/> manually with your unique message to hide the spinner.
    /// </summary>
    public class LoadingSpinner : PlatformDependentComponent
    {
        /// <summary>
        /// The path to the prefab for the loading spinner game object.
        /// </summary>
        private const string loadingSpinnerPrefab = UIPrefabFolder + "LoadingSpinner";

        /// <summary>
        /// The initial color (i.e., at 0%) of the determinate spinner.
        /// </summary>
        private static readonly Color initialDeterminateColor = Color.red;

        /// <summary>
        /// The final color (i.e., at 100%) of the determinate spinner.
        /// </summary>
        private static readonly Color finalDeterminateColor = Color.white;

        /// <summary>
        /// The duration of the animation when the determinate spinner changes.
        /// </summary>
        private const float determinateChangeDuration = 0.3f;

        /// <summary>
        /// The game object containing the loading spinner.
        /// </summary>
        private static GameObject loadingSpinner;

        /// <summary>
        /// The image containing the indeterminate spinner.
        /// </summary>
        private static GameObject indeterminateSpinner;

        /// <summary>
        /// The game object containing the determinate spinner.
        /// </summary>
        private static GameObject determinateSpinner;

        /// <summary>
        /// The image containing the determinate spinner's progress circle.
        /// </summary>
        private static Image determinateSpinnerProgress;

        /// <summary>
        /// The TextMeshPro containing the number of active loading processes.
        /// </summary>
        private static TextMeshProUGUI processCountText;

        /// <summary>
        /// The TextMeshPro containing the information about the active loading processes.
        /// </summary>
        private static TextMeshProUGUI processInfoText;

        /// <summary>
        /// The set of active indeterminate loading processes.
        /// </summary>
        private static readonly ISet<string> indeterminateProcesses = new HashSet<string>();

        /// <summary>
        /// The set of active determinate loading processes and their progress (from 0 to 1).
        /// </summary>
        private static readonly IDictionary<string, float> determinateProcesses = new ConcurrentDictionary<string, float>();

        protected override void StartDesktop()
        {
            // This should be a singleton.
            if (!ReferenceEquals(loadingSpinner, null))
            {
                throw new InvalidOperationException("There can only be one loading spinner.");
            }

            // We initialize the loading spinner so that it can be quickly shown when needed.
            loadingSpinner = PrefabInstantiator.InstantiatePrefab(loadingSpinnerPrefab, Canvas.transform, false);
            processCountText = loadingSpinner.transform.Find("Counter").gameObject.MustGetComponent<TextMeshProUGUI>();
            processInfoText = loadingSpinner.transform.Find("Info").gameObject.MustGetComponent<TextMeshProUGUI>();
            indeterminateSpinner = loadingSpinner.transform.Find("Indeterminate").gameObject;
            determinateSpinner = loadingSpinner.transform.Find("Determinate").gameObject;
            determinateSpinnerProgress = determinateSpinner.transform.Find("Progress").gameObject.MustGetComponent<Image>();

            loadingSpinner.SetActive(indeterminateProcesses.Count + determinateProcesses.Count > 0);
            UpdateLoadingText();
        }

        /// <summary>
        /// The text that shall be shown below the spinner.
        /// </summary>
        private static string InfoText
        {
            get
            {
                StringBuilder infoText = new();
                if (determinateProcesses.Count > 0)
                {
                    if (determinateProcesses.Count > 1)
                    {
                        // Display progress along with the process name if there are multiple determinate processes
                        // (the progress display inside the indicator only shows the average).
                        infoText.AppendJoin('\n', determinateProcesses.Select(x => $"{x.Key} [{x.Value:P0}]"));
                    }
                    else
                    {
                        infoText.Append(determinateProcesses.Keys.First());
                    }
                    infoText.Append('\n');
                }
                infoText.AppendJoin('\n', indeterminateProcesses);
                return infoText.ToString();
            }
        }

        /// <summary>
        /// Displays the indeterminate (i.e., no progress updates) loading spinner
        /// with the given <paramref name="processMessage"/>, which <b>must be unique</b> to your loading process.
        ///
        /// It is recommended to call this method in a using block.
        /// Refer to the class documentation for more information on how to use this.
        /// </summary>
        /// <param name="processMessage">The unique message for the loading process.</param>
        /// <seealso cref="LoadingSpinner"/>
        /// <returns>An <see cref="IDisposable"/> that can be used to hide the spinner.</returns>
        public static IDisposable ShowIndeterminate(string processMessage)
        {
            if (processMessage == null)
            {
                throw new ArgumentNullException(nameof(processMessage));
            }

            if (!AsyncUtils.IsRunningOnMainThread)
            {
                // If the game is not running, we'll just use a simple log message.
                Debug.Log($"Running: {processMessage}\n");
            }

            if (indeterminateProcesses.Add(processMessage) && !ReferenceEquals(loadingSpinner, null))
            {
                loadingSpinner.SetActive(true);
                UpdateLoadingText();
                if (determinateProcesses.Count == 0)
                {
                    // Determinate spinner is not shown, so we'll show the indeterminate spinner.
                    determinateSpinner.SetActive(false);
                    indeterminateSpinner.SetActive(true);
                }
            }
            return new LoadingSpinnerDisposable(processMessage);
        }

        /// <summary>
        /// Displays the determinate (i.e., with incremental progress updates) loading spinner
        /// with the given <paramref name="processMessage"/>, which <b>must be unique</b> to your loading process.
        /// <paramref name="updateProgress"/> is a callback that can be used to update the progress of the spinner.
        ///
        /// It is recommended to call this method in a using block.
        /// Refer to the class documentation for more information on how to use this.
        /// </summary>
        /// <param name="processMessage">The unique message for the loading process.</param>
        /// <param name="updateProgress">A callback that can be used to update the progress of the spinner.
        /// Takes a float from 0 to 1, where 0 is no progress and 1 is complete.</param>
        /// <seealso cref="LoadingSpinner"/>
        /// <returns>An <see cref="IDisposable"/> that can be used to hide the spinner.</returns>
        public static IDisposable ShowDeterminate(string processMessage, out Action<float> updateProgress)
        {
            Assert.IsNotNull(processMessage, nameof(processMessage));

            if (!AsyncUtils.IsRunningOnMainThread)
            {
                // If the game is not running, we'll just use a simple log message.
                Debug.Log($"Running: {processMessage}.\n");
            }

            determinateProcesses[processMessage] = 0;
            if (!ReferenceEquals(loadingSpinner, null))
            {
                loadingSpinner.SetActive(true);
                UpdateLoadingText();
                // Determinate spinner takes precedence over indeterminate spinner.
                indeterminateSpinner.gameObject.SetActive(false);
                determinateSpinner.SetActive(true);

                determinateSpinnerProgress.fillAmount = 0;
                determinateSpinnerProgress.color = initialDeterminateColor;
            }

            updateProgress = progress => UpdateProgressAsync(processMessage, progress).Forget();

            return new LoadingSpinnerDisposable(processMessage);
        }

        /// <summary>
        /// Updates the progress of the determinate spinner for the given <paramref name="processMessage"/>
        /// to the given <paramref name="progress"/>.
        /// </summary>
        /// <param name="processMessage">The unique message for the loading process.</param>
        /// <param name="progress">The progress of the loading process, from 0 to 1.</param>
        private static async UniTaskVoid UpdateProgressAsync(string processMessage, float progress)
        {
            if (ReferenceEquals(loadingSpinner, null) || !determinateProcesses.ContainsKey(processMessage))
            {
                // Not yet initialized or already hidden.
                return;
            }
            determinateProcesses[processMessage] = progress;
            // The update method may be called from outside the main thread, so we need to switch to the main thread.
            await AsyncUtils.RunOnMainThreadAsync(() =>
            {
                if (determinateProcesses.TryGetValue(processMessage, out float endValue))
                {
                    determinateSpinnerProgress.DOFillAmount(endValue, determinateChangeDuration).Play();
                    determinateSpinnerProgress.DOColor
                       (Color.Lerp(initialDeterminateColor, finalDeterminateColor, endValue),
                        determinateChangeDuration).Play();
                    UpdateLoadingText();
                }
                else
                {
                    Debug.LogError($"[{nameof(LoadingSpinner)}] unknown process message: {processMessage}.\n");
                }
            });
        }


        /// <summary>
        /// Hides the loading spinner with the given <paramref name="processMessage"/>.
        /// You do not need to call this explicitly if you used a using block to show the spinner.
        /// </summary>
        /// <param name="processMessage">The unique message for the loading process.</param>
        public static void Hide(string processMessage)
        {
            if (!AsyncUtils.IsRunningOnMainThread)
            {
                // If the game is not running, we'll just use a simple log message.
                Debug.Log($"Finished: {processMessage}.\n");
            }

            if ((!determinateProcesses.Remove(processMessage) && !indeterminateProcesses.Remove(processMessage))
                || ReferenceEquals(loadingSpinner, null))
            {
                return;
            }

            UpdateLoadingText();
            if (determinateProcesses.Count + indeterminateProcesses.Count == 0)
            {
                loadingSpinner.SetActive(false);
            }
            else if (determinateProcesses.Count == 0)
            {
                determinateSpinner.SetActive(false);
            }
            else
            {
                // Determinate spinner takes precedence over indeterminate spinner.
                indeterminateSpinner.SetActive(false);
            }
        }

        /// <summary>
        /// Updates the <see cref="processCountText"/> to reflect the
        /// number of active loading processes (if indeterminate) or the progress (if determinate).
        ///
        /// Also updates the <see cref="processInfoText"/> to reflect the names of the active loading processes.
        /// </summary>
        private static void UpdateLoadingText()
        {
            if (processCountText == null)
            {
                return;
            }
            if (determinateProcesses.Count > 0)
            {
                processCountText.text = $"{determinateProcesses.Values.Average():P0}";
            }
            else
            {
                processCountText.text = indeterminateProcesses.Count <= 1 ? "" : $"{indeterminateProcesses.Count}";
            }
            processInfoText.text = InfoText;
        }

        /// <summary>
        /// Destroys <see cref="loadingSpinner"/>. Field <see cref="loadingSpinner"/>
        /// will be null afterwards.
        /// </summary>
        /// <remarks>Called by Unity.</remarks>
        protected override void OnDisable()
        {
            // AXIVION Routine C#-MethodShouldBeDeclaredStatic: This method is interpreted by Unity.
            Destroyer.Destroy(loadingSpinner);
            loadingSpinner = null;
            base.OnDisable();
        }

        /// <summary>
        /// A disposable that hides the loading spinner when disposed.
        /// </summary>
        /// <param name="ProcessMessage">The unique message for the loading process that this disposable represents.</param>
        private record LoadingSpinnerDisposable(string ProcessMessage) : IDisposable
        {
            /// <summary>
            /// Disposes this disposable, hiding the loading spinner for the given <paramref name="ProcessName"/>.
            /// </summary>
            public void Dispose() => HideSpinnerAsync().Forget();

            /// <summary>
            /// Hides the spinner on the main thread.
            ///
            /// This is necessary in case the spinner is disposed on a background thread.
            /// </summary>
            private async UniTask HideSpinnerAsync()
            {
                if (!AsyncUtils.IsRunningOnMainThread)
                {
                    await UniTask.SwitchToMainThread();
                }
                Hide(ProcessMessage);
            }
        }
    }
}
