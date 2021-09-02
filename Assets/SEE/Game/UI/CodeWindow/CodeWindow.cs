using System;
using System.Linq;
using DG.Tweening;
using SEE.GO;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.Game.UI.CodeWindow
{
    /// <summary>
    /// Represents a movable, scrollable window containing source code.
    /// The source code may either be entered manually or read from a file.
    /// </summary>
    public partial class CodeWindow : PlatformDependentComponent
    {
        /// <summary>
        /// The text displayed in the code window.
        /// </summary>
        private string Text;

        /// <summary>
        /// TextMeshPro component containing the code.
        /// </summary>
        private TextMeshProUGUI TextMesh;

        private TMP_InputField TextMeshInputField;

        /// <summary>
        /// Path to the file whose content is displayed in this code window.
        /// May be <c>null</c> if the code window was filled using <see cref="EnterFromText"/> instead.
        /// </summary>
        public string FilePath;

        /// <summary>
        /// The title (e.g. filename) for the code window.
        /// </summary>
        public string Title;

        /// <summary>
        /// Size of the font used in the code window.
        /// </summary>
        public float FontSize = 20f;

        /// <summary>
        /// Resolution of the code window. By default, this is set to a resolution of 900x500.
        /// </summary>
        public Vector2 Resolution = new Vector2(900, 500);

        /// <summary>
        /// An event which gets called whenever the scrollbar is used to scroll to a different line.
        /// Will be called after the scroll is completed.
        /// </summary>
        public readonly UnityEvent ScrollEvent = new UnityEvent();

        /// <summary>
        /// GameObject containing the actual UI for the code window.
        /// </summary>
        public GameObject codeWindow { get; private set; }

        /// <summary>
        /// Number of lines within the file.
        /// </summary>
        private int lines;

        /// <summary>
        /// Path to the code canvas prefab.
        /// </summary>
        private const string CODE_WINDOW_PREFAB = "Prefabs/UI/CodeWindow";

        /// <summary>
        /// Visually marks the line at the given <paramref name="lineNumber"/> and scrolls to it.
        /// Will also unmark any other line.
        /// </summary>
        /// <param name="line">The line number of the line to mark and scroll to (1-indexed)</param>
        private void MarkLine(int lineNumber)
        {
            string[] allLines = TextMesh.text.Split('\n').Select(x => x.EndsWith("</mark>") ? x.Substring(16, x.Length - 16 - 7) : x).ToArray();
            string markedLine = $"<mark=#ff000044>{allLines[lineNumber - 1]}</mark>\n";
            TextMesh.text = string.Join("", allLines.Select(x => x + "\n").Take(lineNumber - 1).Append(markedLine)
                                                    .Concat(allLines.Select(x => x + "\n").Skip(lineNumber).Take(lines - lineNumber - 2)));
        }

        /// <summary>
        /// Shows or hides the code window, depending on the <see cref="show"/> parameter.
        /// </summary>
        /// <param name="show">Whether the code window should be shown.</param>
        /// <remarks>If this window is used in a <see cref="CodeSpace"/>, this method
        /// needn't (and shouldn't) be used.</remarks>
        public void Show(bool show)
        {
            switch (Platform)
            {
                case PlayerInputType.DesktopPlayer:
                    ShowDesktop(show);
                    break;
                case PlayerInputType.TouchGamepadPlayer:
                    ShowDesktop(show);
                    break;
                case PlayerInputType.VRPlayer:
                    PlatformUnsupported();
                    break;
                case PlayerInputType.HoloLensPlayer:
                    PlatformUnsupported();
                    break;
                case PlayerInputType.None: // nothing needs to be done
                    break;
                default:
                    Debug.LogError($"Platform {Platform} not handled in switch case.\n");
                    break;
            }
        }

        /// <summary>
        /// When disabling the code window, its controlled UI objects will also be disabled.
        /// </summary>
        public void OnDisable()
        {
            if (codeWindow)
            {
                codeWindow.SetActive(false);
            }
        }

        /// <summary>
        /// When enabling the code window, its controlled UI objects will also be enabled.
        /// </summary>
        public void OnEnable()
        {
            if (codeWindow)
            {
                codeWindow.SetActive(true);
            }
        }

        /// <summary>
        /// Inserts a Char at the given index, used for NetworkChanges.
        /// </summary>
        /// <param name="c">The Char that should be added.</param>
        /// <param name="index">The index at which the Char should be added</param>
        public void InsertChar(char c, int index)
        {
            Debug.Log("CHAR " + c + " inde " + index);
            Debug.Log("TMP " + TextMeshInputField.text);
            Debug.Log("TMPs " + TextMeshInputField);
            TextMeshInputField.text.Insert(index, c.ToString());
        }

        /// <summary>
        /// Removes a Char from the CodeWindow, used for NetworkChanges
        /// </summary>
        /// <param name="index">The index at which the Char should be removed</param>
        public void DeletChar(int index)
        {
            TextMeshInputField.text.Remove(index);
        }

        #region Visible Line Calculation

        /// <summary>
        /// The line we're scrolling towards at the moment.
        /// Will be 0 if we're not scrolling towards anything.
        /// </summary>
        private int ScrollingTo;

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
        private float PreStartLine = 1;

        /// <summary>
        /// The line currently at the top of the window.
        /// Will scroll smoothly to the line when changed and mark it visually.
        /// While scrolling to a line, this returns the line we're currently scrolling to.
        /// If a line outside the range of available lines is set, the highest available line number is used instead.
        /// </summary>
        /// <remarks>Only a fully visible line counts. If a line is partially obscured, the next line number
        /// will be returned.</remarks>
        public int VisibleLine
        {
            get => ScrollingTo > 0 ? ScrollingTo : Mathf.CeilToInt(visibleLine) + 1;
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
                    PreStartLine = value;
                }
                else
                {
                    // Animate scroll
                    ScrollingTo = value;
                    DOTween.Sequence().Append(DOTween.To(() => visibleLine, f => visibleLine = f, value - 1, 1f))
                           .AppendCallback(() => ScrollingTo = 0);

                    // FIXME: TMP bug: Large files cause issues with highlighting text. This is just a workaround.
                    // See https://github.com/uni-bremen-agst/SEE/issues/250#issuecomment-819653373
                    if (Text.Length < 16382)
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
        /// Note that the line here is 0-indexed, as opposed to <see cref="VisibleLine"/>, which is 1-indexed.
        /// </summary>
        private float visibleLine
        {
            get => HasStarted ? (1 - scrollRect.verticalNormalizedPosition) * (lines - 1 - excessLines) : PreStartLine;
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
                    PreStartLine = value;
                }
                else
                {
                    scrollRect.verticalNormalizedPosition = 1 - value / (lines - 1 - excessLines);
                }
            }
        }

        #endregion

        #region Value Object

        /// <summary>
        /// Recreates a <see cref="CodeWindow"/> from the given <paramref name="valueObject"/> and attaches it to
        /// the GameObject <paramref name="attachTo"/>.
        /// </summary>
        /// <param name="valueObject">The value object from which the code window should be constructed</param>
        /// <param name="attachTo">The game object the code window should be attached to. If <c>null</c>,
        /// the game object will be attached to the game object with the name specified in the value object.</param>
        /// <returns>The newly re-constructed code window</returns>
        /// <exception cref="ArgumentException">When both Text and Path in the <paramref name="valueObject"/>
        /// are <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">If both <paramref name="attachTo"/> is <c>null</c>
        /// and the game object specified in <paramref name="valueObject"/> can't be found.</exception>
        public static CodeWindow FromValueObject(CodeWindowValues valueObject, GameObject attachTo = null)
        {
            if (attachTo == null)
            {
                attachTo = GameObject.Find(valueObject.AttachedTo);
                if (attachTo == null)
                {
                    throw new InvalidOperationException($"GameObject with name {valueObject} could not be found.\n");
                }
            }

            CodeWindow window = attachTo.AddComponent<CodeWindow>();
            if (valueObject.Path != null)
            {
                window.EnterFromFile(valueObject.Path);
            }
            else if (valueObject.Text != null)
            {
                window.EnterFromText(valueObject.Text.Split('\n'));
            }
            else
            {
                throw new ArgumentException("Invalid value object. Either FilePath or Text must not be null.");
            }

            window.Title = valueObject.Title;
            window.VisibleLine = valueObject.VisibleLine;
            return window;
        }

        /// <summary>
        /// Generates and returns a <see cref="CodeWindowValues"/> struct for this code window.
        /// </summary>
        /// <param name="fulltext">Whether the whole text should be included. Iff false, the filename will be saved
        /// instead of the text.</param>
        /// <returns>The newly created <see cref="CodeWindowValues"/>, matching this class</returns>
        public CodeWindowValues ToValueObject(bool fulltext)
        {
            string attachedTo = gameObject.name;
            return fulltext
                ? new CodeWindowValues(Title, VisibleLine, attachedTo, Text)
                : new CodeWindowValues(Title, VisibleLine, attachedTo, path: FilePath);
        }

        /// <summary>
        /// Represents the values of a code window needed to re-create its content.
        /// Used for serialization when sending a <see cref="CodeWindow"/> over the network.
        /// </summary>
        [Serializable]
        public struct CodeWindowValues
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
            /// Title of the code window.
            /// </summary>
            [field: SerializeField]
            public string Title { get; private set; }

            /// <summary>
            /// The line number which is currently visible in / at the top of the code window.
            /// </summary>
            [field: SerializeField]
            public int VisibleLine { get; private set; }

            [field: SerializeField]
            /// <summary>
            /// Name of the game object this code window was attached to.
            /// </summary>
            public string AttachedTo { get; private set; }

            /// <summary>
            /// Creates a new CodeWindowValues object from the given parameters.
            /// Note that either text or Path must not be <c>null</c>.
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
            internal CodeWindowValues(string title, int visibleLine, string attachedTo = null, string text = null, string path = null)
            {
                if (text == null && path == null)
                {
                    throw new ArgumentException("Either text or filename must not be null!");
                }

                Text = text;
                Path = path;
                AttachedTo = attachedTo;
                Title = title;
                VisibleLine = visibleLine;
            }
        }

        #endregion
    }
}