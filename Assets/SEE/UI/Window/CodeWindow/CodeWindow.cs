using System;
using System.Linq;
using DG.Tweening;
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
        /// May be <c>null</c> if the code window was filled using <see cref="EnterFromText"/> instead.
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
        public float FontSize = 20f;

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
        /// Path to the code window content prefab.
        /// </summary>
        private const string codeWindowPrefab = "Prefabs/UI/CodeWindowContent";

        /// <summary>
        /// Visually marks the line at the given <paramref name="lineNumber"/> and scrolls to it.
        /// Will also unmark any other line. Sets <see cref="markedLine"/> to
        /// <paramref name="lineNumber"/>.
        /// </summary>
        /// <param name="line">The line number of the line to mark and scroll to (1-indexed)</param>
        private void MarkLine(int lineNumber)
        {
            markedLine = lineNumber;
            string[] allLines = textMesh.text.Split('\n').Select(x => x.EndsWith("</mark>") ? x.Substring(16, x.Length - 16 - 7) : x).ToArray();
            string markLine = $"<mark=#ff000044>{allLines[lineNumber - 1]}</mark>\n";
            textMesh.text = string.Join("", allLines.Select(x => x + "\n").Take(lineNumber - 1).Append(markLine)
                                                    .Concat(allLines.Select(x => x + "\n").Skip(lineNumber).Take(lines - lineNumber - 2)));
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
        /// In our case, this can be calculated by <c>ceil(window_height/line_height)</c>.
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

                    // FIXME: TMP bug: Large files cause issues with highlighting text. This is just a workaround.
                    // See https://github.com/uni-bremen-agst/SEE/issues/250#issuecomment-819653373
                    if (text.Length < 16382)
                    {
                        MarkLine(value);
                    }

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
                    scrollRect.verticalNormalizedPosition = 1 - value / (lines - 1 - excessLines);
                }
            }
        }

        public override void RebuildLayout()
        {
            RecalculateExcessLines();
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
                EnterFromFile(codeValues.Path);
            }
            else if (codeValues.Text != null)
            {
                EnterFromText(codeValues.Text.Split('\n'));
            }
            else
            {
                throw new ArgumentException("Invalid value object. Either FilePath or Text must not be null.");
            }
            ScrolledVisibleLine = codeValues.VisibleLine;
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
        /// If <see cref="FilePath"/> is <c>null</c>, the resulting <see cref="CodeWindowValues"/>
        /// is created with <see cref="text"/>; otherwise with <see cref="FilePath"/>.
        /// </summary>
        /// <returns>The newly created <see cref="CodeWindowValues"/>, matching this class</returns>
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
            /// Text of the code window. May be <c>null</c>, in which case <see cref="Path"/> is not <c>null</c>.
            /// </summary>
            [field: SerializeField]
            public string Text { get; private set; }

            /// <summary>
            /// Path to the file displayed in the code window. May be <c>null</c>, in which case <see cref="Text"/> is not
            /// <c>null</c>.
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
            /// Note that either <paramref name="text"/> or <paramref name="title"/> must not be <c>null</c>.
            /// </summary>
            /// <param name="title">The title of the code window.</param>
            /// <param name="visibleLine">The line currently at the top of the code window which is fully visible.</param>
            /// <param name="attachedTo">Name of the game object the code window is attached to.</param>
            /// <param name="text">The text of the code window. May be <c>null</c>, in which case
            /// <paramref name="path"/> may not be.</param>
            /// <param name="path">The path to the file which should be displayed in the code window.
            /// May be <c>null</c>, in which case <paramref name="text"/> may not.</param>
            /// <exception cref="ArgumentException">Thrown when both <paramref name="path"/> and
            /// <paramref name="text"/> are <c>null</c>.</exception>
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
    }
}
