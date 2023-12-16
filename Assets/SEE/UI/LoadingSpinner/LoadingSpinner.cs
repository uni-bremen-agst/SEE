using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SEE.GO;
using SEE.Utils;
using TMPro;
using UnityEngine;

namespace SEE.UI.LoadingSpinner
{
    /// <summary>
    /// A loading spinner that can be shown and hidden.
    ///
    /// A message can also be specified that will be shown to the user when they hover over the spinner.
    /// Note that the message must be unique to your loading process, otherwise another loading process
    /// with the same message may interfere with your spinner.
    /// The spinner will be shown as long as there is at least one active loading process.
    ///
    /// The recommended way to use this is in a <c>using</c> block:
    /// <code>
    /// using (LoadingSpinner.Show("Loading message"))  // The message must be unique to your loading process.
    /// {
    ///     // Do something that takes a long time.
    ///     // The loading spinner will be shown while this is running,
    ///     // and hidden once control flow leaves this block.
    /// }
    /// </code>
    /// Alternatively, you can call <see cref="IDisposable.Dispose"/> manually on the object
    /// returned by <see cref="Show"/> to mark the end of your loading process.
    /// You can also call <see cref="Hide"/> manually with your unique message to hide the spinner.
    /// </summary>
    public class LoadingSpinner: PlatformDependentComponent
    {
        /// <summary>
        /// The path to the prefab for the loading spinner game object.
        /// </summary>
        private const string loadingSpinnerPrefab = UIPrefabFolder + "LoadingSpinner";

        /// <summary>
        /// The game object containing the loading spinner.
        /// </summary>
        private static GameObject loadingSpinner;

        /// <summary>
        /// The TextMeshPro containing the number of active loading processes.
        /// </summary>
        private static TextMeshProUGUI processCountText;

        /// <summary>
        /// The tooltip containing the <see cref="TooltipText"/> of this <see cref="LoadingSpinner"/>, which will
        /// be displayed when hovering above it.
        /// </summary>
        private static Tooltip.Tooltip tooltip;

        /// <summary>
        /// The set of active loading processes.
        /// </summary>
        private static readonly ISet<string> loadingProcesses = new HashSet<string>();

        protected override void StartDesktop()
        {
            // This should be a singleton.
            if (!ReferenceEquals(loadingSpinner, null))
            {
                throw new InvalidOperationException("There can only be one loading spinner.");
            }

            // We initialize the loading spinner so that it can be quickly shown when needed.
            loadingSpinner = PrefabInstantiator.InstantiatePrefab(loadingSpinnerPrefab, Canvas.transform, false);
            PointerHelper pointerHelper = loadingSpinner.MustGetComponent<PointerHelper>();
            processCountText = loadingSpinner.GetComponentInChildren<TextMeshProUGUI>();
            tooltip = loadingSpinner.AddOrGetComponent<Tooltip.Tooltip>();
            pointerHelper.EnterEvent.AddListener(_ => tooltip.Show(TooltipText));
            pointerHelper.ExitEvent.AddListener(_ => tooltip.Hide());

            loadingSpinner.SetActive(loadingProcesses.Count > 0);
            UpdateLoadingText();
        }

        /// <summary>
        /// The text that shall be shown in the loading spinner's tooltip.
        /// </summary>
        private static string TooltipText => string.Join("\n", loadingProcesses);

        /// <summary>
        /// Displays the loading spinner with the given <paramref name="processMessage"/>,
        /// which <b>must be unique</b> to your loading process.
        /// It is recommended to call this method in a <c>using</c> block.
        ///
        /// Refer to the class documentation for more information on how to use this.
        /// </summary>
        /// <param name="processMessage">The unique message for the loading process</param>
        /// <seealso cref="LoadingSpinner"/>
        /// <returns>An <see cref="IDisposable"/> that can be used to hide the spinner.</returns>
        public static IDisposable Show(string processMessage = null)
        {
            // If the process name is not specified, we generate a random one.
            // TODO (@koschke): However, does it ever make sense to not specify a process name?
            processMessage ??= $"Unnamed loading process [{Guid.NewGuid().ToString()}]";

            if (loadingProcesses.Add(processMessage) && !ReferenceEquals(loadingSpinner, null))
            {
                loadingSpinner.SetActive(true);
                UpdateLoadingText();
            }
            return new LoadingSpinnerDisposable(processMessage);
        }

        /// <summary>
        /// Hides the loading spinner with the given <paramref name="processMessage"/>.
        /// You do not need to call this explicitly if you used a <c>using</c> block to show the spinner.
        /// </summary>
        /// <param name="processMessage">The unique message for the loading process</param>
        public static void Hide(string processMessage)
        {
            if (!loadingProcesses.Remove(processMessage) || ReferenceEquals(loadingSpinner, null))
            {
                return;
            }

            UpdateLoadingText();
            if (loadingProcesses.Count == 0)
            {
                loadingSpinner.SetActive(false);
            }
        }

        /// <summary>
        /// Updates the <see cref="processCountText"/> to reflect the number of active loading processes.
        /// </summary>
        private static void UpdateLoadingText()
        {
            if (processCountText == null)
            {
                return;
            }
            processCountText.text = loadingProcesses.Count <= 1 ? "" : $"{loadingProcesses.Count}";
        }

        /// <summary>
        /// A disposable that hides the loading spinner when disposed.
        /// </summary>
        /// <param name="processName">The unique message for the loading process that this disposable represents</param>
        private record LoadingSpinnerDisposable(string processName): IDisposable
        {
            /// <summary>
            /// Disposes this disposable, hiding the loading spinner for the given <paramref name="processName"/>.
            /// </summary>
            public void Dispose() => Hide(processName);
        }
    }
}
