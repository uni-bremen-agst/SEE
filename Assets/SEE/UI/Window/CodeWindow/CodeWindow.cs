using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using MoreLinq;
using SEE.Tools.LSP;
using SEE.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.UI.Window.CodeWindow
{
    /// <summary>
    /// Represents a movable, scrollable window containing source code.
    /// The source code may either be entered manually or read from a file.
    /// </summary>
    public partial class CodeWindow : BaseWindow
    {
        /// <summary>
        /// The text displayed in the code window.
        /// </summary>
        private string text;

        /// <summary>
        /// TextMeshPro component containing the code.
        /// </summary>
        private TextMeshProUGUI textMesh;

        /// <summary>
        /// Path to the file whose content is displayed in this code window.
        /// May be null if the code window was filled using <see cref="EnterFromText"/> instead.
        /// </summary>
        public string FilePath;

        /// <summary>
        /// The line that was marked (1-indexed). Unlike <see cref="ScrolledVisibleLine"/>,
        /// this line is independent of scrolling.
        /// </summary>
        private int markedLine = 1;

        /// <summary>
        /// Size of the font used in the code window.
        /// </summary>
        public float FontSize = 16f;

        /// <summary>
        /// An event which gets called whenever the scrollbar is used to scroll to a different line.
        /// Will be called after the scroll is completed.
        /// </summary>
        public readonly UnityEvent ScrollEvent = new();

        /// <summary>
        /// Number of lines within the file.
        /// </summary>
        private int lines;

        /// <summary>
        /// The LSP handler for this code window.
        ///
        /// Will only be set if the LSP feature is enabled and active for this code window.
        /// </summary>
        private LSPHandler lspHandler;

        /// <summary>
        /// The handler for the context menu of this code window, which provides various navigation options,
        /// such as "Go to Definition" or "Find References".
        /// </summary>
        private ContextMenuHandler contextMenu;

        /// <summary>
        /// Path to the code window content prefab.
        /// </summary>
        private const string codeWindowPrefab = "Prefabs/UI/CodeWindowContent";

        /// <summary>
        /// Path to the breakpoint prefab.
        /// </summary>
        private const string breakpointPrefab = "Prefabs/UI/BreakpointButton";

        /// <summary>
        /// The color for active breakpoints.
        /// </summary>
        private static readonly Color breakpointColorActive = Color.red.WithAlpha(0.5f);

        /// <summary>
        /// The color for inactive breakpoints.
        /// </summary>
        private static readonly Color breakpointColorInactive = Color.gray.WithAlpha(0.25f);

        /// <summary>
        /// The user begins to hover over a word.
        /// </summary>
        public static event Action<CodeWindow, TMP_WordInfo> OnWordHoverBegin;

        /// <summary>
        /// The user stops to hover over a word.
        /// </summary>
        public static event Action<CodeWindow, TMP_WordInfo> OnWordHoverEnd;

        /// <summary>
        /// The word that was hovered last frame.
        /// </summary>
        private static TMP_WordInfo? lastHoveredWord;

        /// <summary>
        /// Whether the code window contains text.
        /// </summary>
        public bool ContainsText => text != null;

        /// <summary>
        /// Visually highlights the line number at the given <paramref name="lineNumber"/> and scrolls to it.
        /// Will also unhighlight any other line. Sets <see cref="markedLine"/> to <paramref name="lineNumber"/>.
        /// Clears the markers for line numbers smaller than 1.
        /// </summary>
        /// <param name="line">The line number of the line to highlight and scroll to (1-indexed).</param>
        public void MarkLine(int lineNumber)
        {
            const string markColor = "<color=#FF0000>";
            int markColorLength = markColor.Length;
            markedLine = lineNumber;
            string[] allLines = textMesh.text.Split('\n')
                                        .Select(x => x.StartsWith(markColor) ? $"<color=#CCCCCC>{x[markColorLength..]}" : x)
                                        .ToArray();
            if (lineNumber < 1)
            {
                text = string.Join("\n", allLines);
            }
            else
            {
                string markLine = $"{markColor}{allLines[lineNumber - 1][markColorLength..]}";
                text = string.Join("\n", allLines.Exclude(lineNumber - 1, 1).Insert(new[] { markLine }, lineNumber - 1));
            }
            textMesh.text = text;
        }

        #region Visible Line Calculation

        /// <summary>
        /// The line we're scrolling towards at the moment.
        /// Will be 0 if we're not scrolling towards anything.
        /// </summary>
        private int scrollingTo;

        /// <summary>
        /// Number of "excess lines" within this code window.
        /// Excess lines are defined as lines which can't be accessed by the scrollbar, so
        /// they're all lines which are visible when scrolling to the lowest point of the window (except for the
        /// first line, as that one is still accessible by the scrollbar).
        /// In our case, this can be calculated by ceil(window_height/line_height).
        /// </summary>
        private int excessLines;

        /// <summary>
        /// Holds the desired visible line before <see cref="Start"/> is called, because <see cref="scrollbar"/> will
        /// be undefined until then.
        /// </summary>
        private float preStartLine = 1;

        /// <summary>
        /// The line currently at the top of the window.
        /// Will scroll smoothly to the line when changed and mark it visually.
        /// While scrolling to a line, this returns the line we're currently scrolling to.
        /// If a line outside the range of available lines is set, the highest available line number is used instead.
        /// </summary>
        /// <remarks>Only a fully visible line counts. If a line is partially obscured, the next line number
        /// will be returned.</remarks>
        public int ScrolledVisibleLine
        {
            get => scrollingTo > 0 ? scrollingTo : Mathf.CeilToInt(ImmediateVisibleLine) + 1;
            set
            {
                if (value > lines || value < 1)
                {
                    Debug.LogError($"Specified line number {value} is outside the range of lines 1-{lines}. "
                                   + $"Using maximum line number {lines} instead.");
                    value = lines;
                }

                // If this is called before Start() has been called, scrollbar will be null, so we have to cache
                // the desired visible line.
                if (!HasStarted)
                {
                    preStartLine = value;
                }
                else
                {
                    // Animate scroll
                    scrollingTo = value;
                    DOTween.Sequence().Append(DOTween.To(() => ImmediateVisibleLine, f => ImmediateVisibleLine = f, value - 1, 1f))
                           .AppendCallback(() => scrollingTo = 0);

                    MarkLine(value);
                    ScrollEvent.Invoke();
                }
            }
        }

        /// <summary>
        /// The line currently at the top of the window.
        /// Will immediately set the line.
        /// Note that the line here is 0-indexed, as opposed to <see cref="ScrolledVisibleLine"/>, which is 1-indexed.
        /// </summary>
        private float ImmediateVisibleLine
        {
            get => HasStarted ? (1 - scrollRect.verticalNormalizedPosition) * (lines - 1 - excessLines) : preStartLine;
            set
            {
                if (value > lines - 1 || value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                // If this is called before Start() has been called, scrollbar will be null, so we have to cache
                // the desired visible line.
                if (!HasStarted)
                {
                    preStartLine = value;
                }
                else
                {
                    scrollRect.verticalNormalizedPosition = 1 - (value-1) / (lines - 1 - excessLines);
                    scrollRect.horizontalNormalizedPosition = 0;
                }
            }
        }

        public override void RebuildLayout()
        {
            RecalculateExcessLines();
            SetupBreakpoints();
        }

        #endregion

        #region Value Object

        protected override void InitializeFromValueObject(WindowValues valueObject)
        {
            if (valueObject is not CodeWindowValues codeValues)
            {
                throw new UnsupportedTypeException(typeof(CodeWindowValues), valueObject.GetType());
            }

            if (codeValues.Path != null)
            {
                EnterFromFileAsync(codeValues.Path).ContinueWith(() => ScrolledVisibleLine = codeValues.VisibleLine).Forget();
            }
            else if (codeValues.Text != null)
            {
                EnterFromText(codeValues.Text.Split('\n'));
                ScrolledVisibleLine = codeValues.VisibleLine;
            }
            else
            {
                throw new ArgumentException("Invalid value object. Either FilePath or Text must not be null.");
            }
        }

        public override void UpdateFromNetworkValueObject(WindowValues valueObject)
        {
            if (valueObject is not CodeWindowValues codeValues)
            {
                throw new UnsupportedTypeException(typeof(CodeWindowValues), valueObject.GetType());
            }
            ScrolledVisibleLine = codeValues.VisibleLine;
            // TODO: Text merge between windowValue.Text and window.Text
        }

        /// <summary>
        /// Generates and returns a <see cref="CodeWindowValues"/> struct for this code window.
        /// If <see cref="FilePath"/> is null, the resulting <see cref="CodeWindowValues"/>
        /// is created with <see cref="text"/>; otherwise with <see cref="FilePath"/>.
        /// </summary>
        /// <returns>The newly created <see cref="CodeWindowValues"/>, matching this class.</returns>
        public override WindowValues ToValueObject()
        {
            string attachedTo = gameObject.name;
            return FilePath == null
                ? new CodeWindowValues(Title, ScrolledVisibleLine, attachedTo, text)
                : new CodeWindowValues(Title, ScrolledVisibleLine, attachedTo, path: FilePath);
        }

        /// <summary>
        /// Represents the values of a code window needed to re-create its content.
        /// Used for serialization when sending a <see cref="CodeWindow"/> over the network.
        /// </summary>
        [Serializable]
        public class CodeWindowValues : WindowValues
        {
            /// <summary>
            /// Text of the code window. May be null, in which case <see cref="Path"/> is not null.
            /// </summary>
            [field: SerializeField]
            public string Text { get; private set; }

            /// <summary>
            /// Path to the file displayed in the code window. May be null, in which case <see cref="Text"/> is not
            /// null.
            /// </summary>
            [field: SerializeField]
            public string Path { get; private set; }

            /// <summary>
            /// The line number which is currently visible in / at the top of the code window.
            /// </summary>
            [field: SerializeField]
            public int VisibleLine { get; private set; }

            /// <summary>
            /// Creates a new CodeWindowValues object from the given parameters.
            /// Note that either <paramref name="text"/> or <paramref name="title"/> must not be null.
            /// </summary>
            /// <param name="title">The title of the code window.</param>
            /// <param name="visibleLine">The line currently at the top of the code window which is fully visible.</param>
            /// <param name="attachedTo">Name of the game object the code window is attached to.</param>
            /// <param name="text">The text of the code window. May be null, in which case
            /// <paramref name="path"/> may not be.</param>
            /// <param name="path">The path to the file which should be displayed in the code window.
            /// May be null, in which case <paramref name="text"/> may not.</param>
            /// <exception cref="ArgumentException">Thrown when both <paramref name="path"/> and
            /// <paramref name="text"/> are null.</exception>
            internal CodeWindowValues(string title, int visibleLine, string attachedTo = null, string text = null, string path = null) : base(title, attachedTo)
            {
                if (text == null && path == null)
                {
                    throw new ArgumentException("Either text or filename must not be null!");
                }

                Text = text;
                Path = path;
                VisibleLine = visibleLine;
            }
        }

        #endregion

        /// <summary>
        /// Data container for a word hover event
        /// </summary>
        /// <param name="Word">The hovered word.</param>
        /// <param name="CodeWindow">The code window containing the hovered word.</param>
        /// <param name="FilePath">The file path of the code window.</param>
        /// <param name="Line">The line of the hovered word.</param>
        /// <param name="Column">The column of the hovered word.</param>
        public record WordHoverEvent(string Word, CodeWindow CodeWindow, string FilePath, int Line, int Column);
    }
}
