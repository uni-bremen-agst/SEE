using System;
using System.IO;
using System.Linq;
using DG.Tweening;
using SEE.GO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.Game.UI.CodeWindow
{
    /// <summary>
    /// Represents a movable, scrollable window containing source code.
    /// The source code may either be entered manually or read from a file.
    /// </summary>
    public partial class CodeWindow: PlatformDependentComponent
    {
        /// <summary>
        /// The text displayed in the code window.
        /// </summary>
        private string Text;

        /// <summary>
        /// TextMeshPro component containing the code.
        /// </summary>
        private TextMeshProUGUI TextMesh;

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
        /// </summary>
        public ScrollRect.ScrollRectEvent ScrollEvent;

        /// <summary>
        /// The line currently at the top of the window.
        /// Will scroll smoothly to the line when changed and mark it visually.
        /// If a line outside the range of available lines is set, the highest available line number is used instead.
        /// </summary>
        /// <remarks>Only a fully visible line counts. If a line is partially obscured, the next line number
        /// will be returned.</remarks>
        public int VisibleLine
        {
            get => Mathf.CeilToInt(visibleLine)+1;
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
                if (scrollRect == null)
                {
                    PreStartLine = value;
                }
                else
                {
                    DOTween.To(() => visibleLine+1, f => visibleLine = f-1, value, 1f);
                    // Mark selected line
                    string[] allLines = TextMesh.text.Split('\n');
                    string markedLine = $"<mark=#ff000044>{allLines[value]}</mark>\n";
                    TextMesh.text = string.Join("", allLines.Select(x => x+"\n").Take(value).Append(markedLine)
                                                            .Concat(allLines.Select(x => x + "\n").Skip(value+1)));
                }
            }
        }

        /// <summary>
        /// Holds the desired visible line before <see cref="Start"/> is called, because <see cref="scrollbar"/> will
        /// be undefined until then.
        /// </summary>
        private float PreStartLine = 1;
        
        /// <summary>
        /// The line currently at the top of the window.
        /// Will immediately set the line.
        /// Note that the line here is 0-indexed, as opposed to <see cref="VisibleLine"/>, which is 1-indexed.
        /// </summary>
        private float visibleLine
        {
            get => scrollRect != null ? (1-scrollRect.verticalNormalizedPosition) * (lines-excessLines) : PreStartLine;
            set
            {
                if (VisibleLine > lines)
                {
                    throw new ArgumentOutOfRangeException();
                }
                // If this is called before Start() has been called, scrollbar will be null, so we have to cache
                // the desired visible line.
                if (scrollRect == null)
                {
                    PreStartLine = value;
                }
                else
                {
                    scrollRect.verticalNormalizedPosition = 1 - (value) / (lines-excessLines);
                }
            }
        }

        /// <summary>
        /// GameObject containing the actual UI for the code window.
        /// </summary>
        public GameObject codeWindow { get; private set; }

        /// <summary>
        /// Number of lines within the file.
        /// </summary>
        private int lines;

        /// <summary>
        /// Number of "excess lines" within this code window.
        /// Excess lines are defined as lines which can't be accessed by the scrollbar, so
        /// they're all lines which are visible when scrolling to the lowest point of the window (except for the
        /// first line, as that one is still accessible by the scrollbar).
        /// In our case, this can be calculated by <c>ceil(window_height/line_height)</c>.
        /// </summary>
        private int excessLines;

        /// <summary>
        /// Path to the code canvas prefab.
        /// </summary>
        private const string CODE_WINDOW_PREFAB = "Prefabs/UI/CodeWindow";
        
        /// <summary>
        /// Populates the code window with the contents of the given file.
        /// This will overwrite any existing text.
        /// </summary>
        /// <param name="text">An array of lines to use for the code window.</param>
        /// <exception cref="ArgumentException">If <paramref name="text"/> is empty or <c>null</c></exception>
        public void EnterFromText(string[] text)
        {
            if (text == null || text.Length <= 0)
            {
                throw new ArgumentException("Given text must not be empty or null.\n");
            }

            int neededPadding = $"{text.Length}".Length;
            Text = "";
            for (int i = 0; i < text.Length; i++)
            {
                // Add whitespace next to line number so it's consistent
                Text += string.Join("", Enumerable.Repeat(" ", neededPadding-$"{i+1}".Length));
                // Line number will be typeset in yellow to distinguish it from the rest
                Text += $"<color=\"yellow\">{i+1}</color> <noparse>{text[i]}</noparse>\n";
            }
            lines = text.Length;
        }

        /// <summary>
        /// Populates the code window with the contents of the given file.
        /// This will overwrite any existing text.
        /// </summary>
        /// <param name="filename">The filename for the file to read.</param>
        public void EnterFromFile(string filename)
        {
            // Try to read the file, otherwise display the error message.
            if (!File.Exists(filename))
            {
                Text = $"<color=\"red\"><b>Couldn't find file '<noparse>{filename}</noparse>'.</b></color>";
                Debug.LogError($"Couldn't find file {filename}");
                return;
            }
            try
            {
                EnterFromText(File.ReadAllLines(filename));
            }
            catch (IOException exception)
            {
                Text = $"<color=\"red\"><noparse>{exception}</noparse></color>";
                Debug.LogError(exception);
            }
        }

        /// <summary>
        /// Shows or hides the code window, depending on the <see cref="show"/> parameter.
        /// </summary>
        /// <param name="show">Whether the code window should be shown.</param>
        /// <remarks>If this window is used in a <see cref="CodeWindowSpace"/>, this method
        /// needn't (and shouldn't) be used.</remarks>
        public void Show(bool show)
        {
            switch (Platform)
            {
                case PlayerInputType.DesktopPlayer: ShowDesktop(show);
                    break;
                case PlayerInputType.TouchGamepadPlayer: ShowDesktop(show);
                    break;
                case PlayerInputType.VRPlayer: PlatformUnsupported();
                    break;
                case PlayerInputType.HoloLensPlayer: PlatformUnsupported();
                    break;
                case PlayerInputType.None:  // nothing needs to be done
                    break;
                default: Debug.LogError($"Platform {Platform} not handled in switch case.\n");
                    break;
            }
        }

        public void OnDisable()
        {
            if (codeWindow)
            {
                codeWindow.SetActive(false);
            }
        }

        public void OnEnable()
        {
            if (codeWindow)
            {
                codeWindow.SetActive(true);
            }
        }
    }
}