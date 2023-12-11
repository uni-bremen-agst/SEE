using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Controls
{
    /// <summary>
    /// Defines the key codes for all interaction based on the keyboard in SEE.
    /// </summary>
    internal static class KeyBindings
    {
        // IMPORTANT NOTES:
        // (1) Keep in mind that KeyCodes in Unity map directly to a
        //     physical key on an keyboard with an English layout.
        // (2) Ctrl-Z and Ctrl-Y are reserved for Undo and Redo.
        // (3) The digits 0-9 are reserved for shortcuts for the player menu.

        /// <summary>
        /// Categories for the keyboard shortcuts. Each <see cref="KeyActionDescriptor"/>
        /// is element of exactly one of these categories. These categories are used
        /// to organize the key actions into cohesive groups meaningful to a user.
        /// </summary>
        internal enum Category
        {
            /// <summary>
            /// Actions that are generally applicable in every context.
            /// </summary>
            General,
            /// <summary>
            /// Actions dealing with animations.
            /// </summary>
            Animation,
            /// <summary>
            /// Actions related to architecture verification use case; related to architecture mapping and analysis.
            /// </summary>
            Architecture,
            /// <summary>
            /// Actions related to browsing a code city (panning, zooming, etc.).
            /// </summary>
            Browsing,
            /// <summary>
            /// Actions related to recording a camera (player) path.
            /// </summary>
            CameraPaths,
            /// <summary>
            /// Actions related to text chatting with other remote players.
            /// </summary>
            Chat,
            /// <summary>
            /// Actions related to the source-code viewer.
            /// </summary>
            CodeViewer,
            /// <summary>
            /// Actions related to the use case debugging.
            /// </summary>
            Debugging,
            /// <summary>
            /// Actions related to the use case evolution; observing the series of revisions of a city.
            /// </summary>
            Evolution,
            /// <summary>
            /// Actions related to showing metric charts.
            /// </summary>
            MetricCharts,
            /// <summary>
            /// Actions related to movements of the player within the world.
            /// </summary>
            Movement,
        }

        /// <summary>
        /// The available user actions that can be triggered by a key on the keyboard.
        /// </summary>
        internal enum KeyAction
        {
            /// <summary>
            /// User asks for help.
            /// </summary>
            Help,
            /// <summary>
            /// User toggles the player menu.
            /// </summary>
            ToggleMenu,
            /// <summary>
            /// User toggles the voice chat.
            /// </summary>
            ToggleVoiceInput
        }

        /// <summary>
        /// A descriptor of a user action that can be triggered by a key on the keyboard.
        /// </summary>
        private struct KeyActionDescriptor
        {
            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="name">descriptive name of the action shown to the user</param>
            /// <param name="description">longer description of the action shown to the user</param>
            /// <param name="category">category of the action</param>
            /// <param name="keyCode">key this action is bound to</param>
            public KeyActionDescriptor(string name, string description, Category category, KeyCode keyCode)
            {
                KeyCode = keyCode;
                Name = name;
                Category = category;
                Description = description;
            }

            /// <summary>
            /// The descriptive name of the action. It will be shown to the user. It should be
            /// short and descriptive.
            /// </summary>
            public readonly string Name;
            /// <summary>
            /// A longer description of the action shown to the user. It provides more detail
            /// than <see cref="Name"/>.
            /// </summary>
            public readonly string Description;
            /// <summary>
            /// <summary>
            /// The scope of the action.
            /// </summary>
            public readonly Category Category;
            /// The key on the keyboard this action is bound to. If that key is pressed,
            /// the user wants to trigger this action.
            /// </summary>
            public KeyCode KeyCode;
        }

        /// <summary>
        /// Mapping of every <see cref="KeyAction"/> onto its currently bound <see cref="KeyActionDescriptor"/>.
        /// </summary>
        /// <remarks>Key actions can be re-bound.</remarks>
        private static readonly IDictionary<KeyAction, KeyActionDescriptor> keyBindings
            = new Dictionary<KeyAction, KeyActionDescriptor>();

        /// <summary>
        /// Returns true if the user has pressed a key requesting the given <paramref name="keyAction"/>.
        /// </summary>
        /// <param name="keyAction">the <see cref="KeyAction"/> to check</param>
        /// <returns>true if the user has pressed a key requesting the given <paramref name="keyAction"/></returns>
        internal static bool IsRequested(KeyAction keyAction)
        {
            return keyBindings.TryGetValue(keyAction, out KeyActionDescriptor descriptor)
                && Input.GetKeyDown(descriptor.KeyCode);
        }

        /// <summary>
        /// Registers the given <paramref name="keyCode"/> for the given <paramref name="scope"/>
        /// and the <paramref name="description"/>. If a <paramref name="keyCode"/> is already registered,
        /// an error message will be emitted.
        /// </summary>
        /// <param name="keyCode">the key code to be registered</param>
        /// <param name="name">the name of the action triggered by the <paramref name="keyCode"/>;
        /// <param name="scope">the scope of the key code</param>
        /// <param name="description">the help message for the key code</param>
        /// this name will be presented to our users</param>
        private static void Register(KeyAction keyAction, KeyCode keyCode, string name, Category scope, string description)
        {
            if (keyBindings.TryGetValue(keyAction, out KeyActionDescriptor descriptor))
            {
                Debug.LogError($"Key action {keyAction} is already bound.\n");
            }
            else if (TryGetKeyAction(keyCode, out KeyAction boundKeyAction))
            {
                Debug.LogError($"Key code {keyCode} is already bound to key action {boundKeyAction}.\n");
            }
            else
            {
                keyBindings[keyAction] = new KeyActionDescriptor(name, description, scope, keyCode);
            }
        }

        /// <summary>
        /// Registers all available <see cref="KeyAction"/>s.
        /// </summary>
        static KeyBindings()
        {
            Register(KeyAction.Help, KeyCode.H, "Help", Category.General, "Provides help");
            Register(KeyAction.ToggleMenu, KeyCode.Space, "Toggle menu", Category.General, "Toggles the user-action menu.");
            Register(KeyAction.ToggleVoiceInput, KeyCode.Period, "Toggle voice chat", Category.General, "Toggles voice input on/off.");
            // TODO: Add all other actions
        }

        /// <summary>
        /// Returns the <see cref="KeyAction"/> in <see cref="keyBindings"/> that is
        /// triggered by the given <paramref name="keyCode"/>. If a binding is found,
        /// the <see cref="KeyAction"/> bound to the <paramref name="keyCode"/> is
        /// returned in <paramref name="boundKeyAction"/> and <c>true</c> is returned.
        /// Otherwise <c>false</c> is returned and <paramref name="boundKeyAction"/>
        /// is undefined.
        /// </summary>
        /// <param name="keyCode">a <see cref="KeyCode"/> for which a binding
        /// is to be searched</param>
        /// <param name="boundKeyAction">the <see cref="KeyAction"/> bound
        /// to <paramref name="keyCode"/> if one exists; otherwise undefined</param>
        /// <returns><c>true</c> if and only if there is a <see cref="KeyAction"/>
        /// triggered by <paramref name="keyCode"/></returns>
        private static bool TryGetKeyAction(KeyCode keyCode, out KeyAction boundKeyAction)
        {
            foreach (var binding in keyBindings)
            {
                if (binding.Value.KeyCode == keyCode)
                {
                    boundKeyAction = binding.Key;
                    return true;
                }
            }
            boundKeyAction = default;
            return false;
        }

        /// <summary>
        /// The registered keyboard shortcuts. The value is a help message on the shortcut.
        /// </summary>
        [Obsolete]
        private static readonly IDictionary<KeyCode, string> bindings = new Dictionary<KeyCode, string>();

        /// <summary>
        /// Registers the given <paramref name="keyCode"/> for the given <paramref name="scope"/>
        /// and the <paramref name="description"/>. If a <paramref name="keyCode"/> is already registered,
        /// an error message will be emitted.
        /// </summary>
        /// <param name="keyCode">the key code to be registered</param>
        /// <param name="name">the name of the action triggered by the <paramref name="keyCode"/>;
        /// <param name="scope">the scope of the key code</param>
        /// <param name="description">the help message for the key code</param>
        /// this name will be presented to our users</param>
        /// <returns>a new keyCode that gets added to the bindings if it is not already present
        /// in the bindings</returns>
        [Obsolete("Use the other Register instead")]
        private static KeyCode Register(KeyCode keyCode, string name, Category scope, string description)
        {
            if (bindings.ContainsKey(keyCode))
            {
                Debug.LogError($"Cannot register key {keyCode} for [{scope}] {description}\n");
                Debug.LogError($"Key {keyCode} already bound to {bindings[keyCode]}\n");
            }
            else
            {
                bindings[keyCode] = $"{name} [{scope}] {description}";
            }
            return keyCode;
        }

        /// <summary>
        /// Returns the category of given <paramref name="keyAction"/> if this
        /// <paramref name="keyAction"/> is bound at all; otherwise an exception
        /// is thrown.
        /// </summary>
        /// <param name="keyAction"><see cref="KeyAction"/> whose category is to be retrieved</param>
        /// <returns>category for <paramref name="keyAction"/></returns>
        /// <exception cref="Exception">thrown if <paramref name="keyAction"/> is not bound</exception>
        internal static Category GetCategory(KeyAction keyAction)
        {
            if (keyBindings.TryGetValue(keyAction, out KeyActionDescriptor descriptor))
            {
                return descriptor.Category;
            }
            else
            {
                throw new Exception($"{keyAction} is not bound");
            }
        }

        /// <summary>
        /// Returns the scope of given key-binding description <paramref name="value"/>.
        /// </summary>
        /// <param name="value">the key-binding description from which to extract the scope</param>
        /// <returns>the scope</returns>
        [Obsolete("Use GetCategory instead")]
        public static string GetCategory(string value)
        {
            // Extract the scope part from the string value.
            int startIndex = value.IndexOf("[") + 1;
            int endIndex = value.IndexOf("]");
            return value[startIndex..endIndex];
        }

        /// <summary>
        /// Returns a string array of the binding names.
        /// </summary>
        /// <returns>the binding names</returns>
        public static string[] GetBindingNames()
        {
            // Extract the scope part from the string value.
            List<string> buttons = new();
            foreach (var binding in bindings)
            {
                int endIndex = binding.Value.IndexOf("[") - 1;
                buttons.Add(binding.Value[..endIndex]);
            }
            return buttons.ToArray();
        }

        /// <summary>
        /// Rebinds a binding to another key and updates the <see cref="bindings"/>.
        /// </summary>
        /// <param name="bindingName">the binding, which will be set to a given <param name="keyCode"></param></param>
        /// <returns>false, when the key is already boud to another binding, and true otherwise</returns>
        public static bool SetBindingForKey(string bindingName, KeyCode keyCode)
        {
            if (bindings.ContainsKey(keyCode))
            {
                Debug.LogError($"Cannot register key {keyCode} for {bindingName}\n");
                Debug.LogError($"Key {keyCode} already bound to {bindings[keyCode]}\n");
                return false;
            }
            else
            {
                string bind = bindings.FirstOrDefault(x => x.Value.Contains(bindingName)).Value.ToString();
                KeyCode oldKey = bindings.FirstOrDefault(x => x.Value.Contains(bindingName)).Key;
                bindings.Remove(oldKey);
                bindings[keyCode] = bind;
                SEEInput.UpdateBindings(); // FIXME: Introduces a cyclic dependency.
                return true;
            }
        }

        /// <summary>
        /// Returns a string of the keyName for a binding.
        /// <param name="bindingName">the binding, for which the key is being returned</param>
        /// <returns>returns the key for a given binding.</returns>
        /// </summary>
        public static string GetKeyNameForBinding(string bindingName)
        {
            return bindings.FirstOrDefault(x => x.Value.Contains(bindingName)).Key.ToString();
        }

        /// <summary>
        /// Prints the current key bindings to the debugging console along with their
        /// help message.
        /// </summary>
        internal static void PrintBindings()
        {
            System.Text.StringBuilder sb = new("Key Bindings:\n");
            foreach (var binding in bindings)
            {
                sb.Append($"Key {binding.Key}: {binding.Value}\n");
            }
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Returns the current mapping of keycodes onto their help messages.
        /// </summary>
        /// <returns>current mapping of keycodes onto their help messages</returns>
        public static IDictionary<KeyCode, string> GetBindings()
        {
            return bindings;
        }

        /// <summary>
        /// Returns a string of the current key bindings along with their help message.
        /// </summary>
        /// <returns>current key bindings and their help messages</returns>
        internal static string GetBindingsText()
        {
            var groupedBindings = bindings.GroupBy(pair => GetCategory(pair.Value));
            System.Text.StringBuilder sb = new();
            foreach (var group in groupedBindings)
            {
                // Display the scope
                sb.Append("\n\n\n" + new string(' ', 10) + $"—{group.Key}—\n\n\n" + new string('-', 105) + "\n");

                foreach (var binding in group)
                {
                    int index = binding.Value.IndexOf(']')+1;
                    // Display individual binding details
                    sb.Append($"Key {binding.Key}:\n\n {binding.Value.Substring(index)}\n" + new string('-', 105));
                }
            }
            return sb.ToString();
        }

        //-----------------------------------------------------
        #region General key bindings
        //-----------------------------------------------------

        /// <summary>
        /// Prints help on the key bindings.
        /// </summary>
        internal static readonly KeyCode Help = Register(KeyCode.H, HelpBinding, Category.General, "Prints help on the key bindings.");
        public const string HelpBinding = "Help";
        /// <summary>
        /// Toggles voice input (i.e., for voice commands) on/off.
        /// </summary>
        internal static readonly KeyCode ToggleVoiceInput = Register(KeyCode.Period, ToggleVoiceInputBinding, Category.General,
                                                                     "Toggles voice input on/off.");
        public const string ToggleVoiceInputBinding = "ToggleVoiceInput";

        #endregion

        //-----------------------------------------------------------------
        #region Menu
        // Note: The digits 0-9 are used as short cuts for the menu entries
        //-----------------------------------------------------------------

        /// <summary>
        /// Turns on/off the player-action menu.
        /// </summary>
        internal static readonly KeyCode ToggleMenu = Register(KeyCode.Space, ToggleMenuBinding, Category.General, "Turns on/off the player-action menu.");
        public const string ToggleMenuBinding = "ToggleMenu";

        /// <summary>
        /// Turns on/off the settings menu.
        /// </summary>
        internal static readonly KeyCode ToggleSettings = Register(KeyCode.Pause, ToggleSettingsBinding, Category.General, "Turns on/off the settings menu.");
        public const string ToggleSettingsBinding = "ToggleSettings";

        /// <summary>
        /// Turns on/off the browser.
        /// </summary>
        internal static readonly KeyCode ToggleBrowser = Register(KeyCode.F4, ToggleBrowserBinding, Category.General, "Turns on/off the browser.");
        public const string ToggleBrowserBinding = "ToggleBrowser";

        /// <summary>
        /// Opens the search menu.
        /// </summary>
        internal static readonly KeyCode SearchMenu = Register(KeyCode.F, SearchMenuBinding, Category.General, "Opens the search menu.");
        public const string SearchMenuBinding = "SearchMenu";

        /// <summary>
        /// Undoes the last action.
        /// </summary>
        internal static readonly KeyCode Undo = Register(KeyCode.Z, UndoBinding, Category.General, "Undoes the last action.");
        public const string UndoBinding = "Undo";

        /// <summary>
        /// Re-does the last action.
        /// </summary>
        internal static readonly KeyCode Redo = Register(KeyCode.Y, RedoBinding, Category.General, "Re-does the last action.");
        public const string RedoBinding = "Redo";

        /// <summary>
        /// Opens/closes the configuration menu.
        /// </summary>
        internal static readonly KeyCode ConfigMenu = Register(KeyCode.K, ConfigMenuBinding, Category.General, "Opens/closes the configuration menu.");
        public const string ConfigMenuBinding = "ConfigMenu";

        /// <summary>
        /// Opens/closes the tree view window.
        /// </summary>
        internal static readonly KeyCode TreeView = Register(KeyCode.Tab, TreeViewBinding, Category.General, "Opens/closes the tree view window.");
        public const string TreeViewBinding = "TreeView";

        #endregion

        //-----------------------------------------------------
        #region Camera path recording and playing
        //-----------------------------------------------------

        /// <summary>
        /// Saves the current position when recording paths.
        /// </summary>
        internal static readonly KeyCode SavePathPosition = Register(KeyCode.F11, SavePathPositionBinding, Category.CameraPaths, "Saves the current position when recording paths.");
        public const string SavePathPositionBinding = "SavePathPosition";

        /// <summary>
        /// Starts/stops the automated path replay.
        /// </summary>
        internal static readonly KeyCode TogglePathPlaying = Register(KeyCode.F12, TogglePathPlayingBinding, Category.CameraPaths, "Starts/stops the automated camera movement along a path.");
        public const string TogglePathPlayingBinding = "TogglePathPlaying";

        #endregion

        //-----------------------------------------------------
        #region Metric charts
        //-----------------------------------------------------

        /// <summary>
        /// Turns the metric charts on/off.
        /// </summary>
        internal static KeyCode ToggleCharts = Register(KeyCode.M, ToggleChartsBinding, Category.MetricCharts, "Turns the metric charts on/off.");
        public const string ToggleChartsBinding = "ToggleCharts";

        /// <summary>
        /// Toggles hovering/selection for markers in metric charts.
        /// </summary>
        internal static readonly KeyCode ToggleMetricHoveringSelection = Register(KeyCode.N, ToggleMetricHoveringSelectionBinding, Category.MetricCharts, "Toggles hovering/selection for markers in metric charts.");
        public const string ToggleMetricHoveringSelectionBinding = "ToggleMetricHoveringSelection";

        #endregion


        //-----------------------------------------------------
        #region Navigation in a code city
        //-----------------------------------------------------

        /// <summary>
        /// Toggles the visibility of all edges of a hovered code city.
        /// </summary>
        internal static KeyCode ToggleEdges = Register(KeyCode.V, ToggleEdgesBinding, Category.Browsing, "Toggles the visibility of all edges of a hovered code city.");
        public const string ToggleEdgesBinding = "ToggleEdges";

        /// <summary>
        /// Forgets all currently selected objects.
        /// </summary>
        internal static readonly KeyCode Unselect = Register(KeyCode.U, UnselectBinding, Category.Browsing, "Forgets all currently selected objects.");
        public const string UnselectBinding = "Unselect";

        /// <summary>
        /// Cancels an action.
        /// </summary>
        internal static readonly KeyCode Cancel = Register(KeyCode.Escape, CancelBinding, Category.Browsing, "Cancels an action.");
        public const string CancelBinding = "Cancel";

        /// <summary>
        /// To reset a NavigationAction: resets position/rotation to the original position/rotation.
        /// </summary>
        internal static readonly KeyCode Reset = Register(KeyCode.R, ResetBinding, Category.Browsing, "Resets a code city to its original position and scale.");
        public const string ResetBinding = "Reset";

        /// <summary>
        /// Zooms into a city.
        /// </summary>
        internal static readonly KeyCode ZoomInto = Register(KeyCode.G, ZoomIntoBinding, Category.Browsing, "To zoom into a city.");
        public const string ZoomIntoBinding = "ZoomInto";

        /// <summary>
        /// While moving the city, snaps to one of eight predefined directions.
        /// While rotating the city, rotates in 45 degree steps.
        /// </summary>
        internal static readonly KeyCode Snap = Register(KeyCode.LeftAlt, SnapBinding, Category.Browsing, "Snap move/rotate city.");
        public const string SnapBinding = "Snap";

        /// <summary>
        /// The user drags the city as a whole on the plane.
        /// </summary>
        internal static KeyCode DragHovered = Register(KeyCode.LeftControl, DragHoveredBinding, Category.Browsing, "Drag code city.");
        public const string DragHoveredBinding = "DragHovered";

        /// <summary>
        /// Toggles between the locked and free camera mode.
        /// </summary>
        internal static readonly KeyCode ToggleCameraLock = Register(KeyCode.L, ToggleCameraLockBinding, Category.Browsing, "Toggles between the locked and free camera mode.");
        public const string ToggleCameraLockBinding = "ToggleCameraLock";

        /// <summary>
        /// Toggles between pointing.
        /// </summary>
        internal static readonly KeyCode Pointing = Register(KeyCode.P, PointingBinding, Category.Browsing, "Toggles between Pointing.");
        public const string PointingBinding = "Pointing";

        #endregion

        //-----------------------------------------------------
        #region Player (camera) movements.
        //-----------------------------------------------------

        /// <summary>
        /// Boosts the speed of the player movement. While pressed, movement is faster.
        /// </summary>
        internal static readonly KeyCode BoostCameraSpeed = Register(KeyCode.LeftShift, BoostCameraSpeedBinding, Category.Movement, "Boosts the speed of the player movement. While pressed, movement is faster.");
        public const string BoostCameraSpeedBinding = "BoostCameraSpeed";

        /// <summary>
        /// Move camera (player) forward.
        /// </summary>
        internal static readonly KeyCode MoveForward = Register(KeyCode.W, MoveForwardBinding, Category.Movement, "Move forward.");
        public const string MoveForwardBinding = "MoveForward";

        /// <summary>
        /// Move camera (player) backward.
        /// </summary>
        internal static readonly KeyCode MoveBackward = Register(KeyCode.S, MoveBackwardBinding, Category.Movement, "Move backward.");
        public const string MoveBackwardBinding = "MoveBackward";

        /// <summary>
        /// Move camera (player) to the right.
        /// </summary>
        internal static readonly KeyCode MoveRight = Register(KeyCode.D, MoveRightBinding, Category.Movement, "Move to the right.");
        public const string MoveRightBinding = "MoveRight";

        /// <summary>
        /// Move camera (player) to the left.
        /// </summary>
        internal static readonly KeyCode MoveLeft = Register(KeyCode.A, MoveLeftBinding, Category.Movement, "Move to the left.");
        public const string MoveLeftBinding = "MoveLeft";

        /// <summary>
        /// Move camera (player) up.
        /// </summary>
        internal static readonly KeyCode MoveUp = Register(KeyCode.Q, MoveUpBinding, Category.Movement, "Move up.");
        public const string MoveUpBinding = "MoveUp";

        /// <summary>
        /// Move camera (player) down.
        /// </summary>
        internal static readonly KeyCode MoveDown = Register(KeyCode.E, MoveDownBinding, Category.Movement, "Move down.");
        public const string MoveDownBinding = "MoveDown";

        #endregion

        //--------------------------
        #region Evolution
        //--------------------------

        /// <summary>
        /// Sets a new marker.
        /// </summary>
        internal static readonly KeyCode SetMarker = Register(KeyCode.Insert, SetMarkerBinding, Category.Evolution, "Sets a new marker.");
        public const string SetMarkerBinding = "SetMarker";

        /// <summary>
        /// Deletes a marker.
        /// </summary>
        internal static readonly KeyCode DeleteMarker = Register(KeyCode.Delete, DeleteMarkerBinding, Category.Evolution, "Deletes a marker.");
        public const string DeleteMarkerBinding = "DeleteMarker";

        /// <summary>
        /// Toggles between between the two canvases for the animation and selection of a revision.
        /// </summary>
        internal static readonly KeyCode ToggleEvolutionCanvases = Register(KeyCode.T, ToggleEvolutionCanvasesBinding, Category.Evolution, "Toggles between between the two canvases for the animation and selection of a revision.");
        public const string ToggleEvolutionCanvasesBinding = "ToggleEvolutionCanvases";

        #endregion

        //----------------------------------------------------
        #region Animation (shared by Debugging and Evolution)
        //----------------------------------------------------

        /// <summary>
        /// The previous element in the animation is to be shown.
        /// </summary>
        internal static readonly KeyCode Previous = Register(KeyCode.LeftArrow, PreviousBinding, Category.Animation, "Go to previous element in the animation.");
        public const string PreviousBinding = "Previous";

        /// <summary>
        /// The next element in the animation is to be shown.
        /// </summary>
        internal static readonly KeyCode Next = Register(KeyCode.RightArrow, NextBinding, Category.Animation, "Go to next element in the animation.");
        public const string NextBinding = "Next";

        /// <summary>
        /// Toggles auto play of the animation.
        /// </summary>
        internal static readonly KeyCode ToggleAutoPlay = Register(KeyCode.F9, ToggleAutoPlayBinding, Category.Animation, "Toggles auto play of the animation.");
        public const string ToggleAutoPlayBinding = "ToggleAutoPlay";

        /// <summary>
        /// Double animation speed.
        /// </summary>
        internal static readonly KeyCode IncreaseAnimationSpeed = Register(KeyCode.UpArrow, IncreaseAnimationSpeedBinding, Category.Animation, "Doubles animation speed.");
        public const string IncreaseAnimationSpeedBinding = "IncreaseAnimationSpeed";

        /// <summary>
        /// Halve animation speed.
        /// </summary>
        internal static readonly KeyCode DecreaseAnimationSpeed = Register(KeyCode.DownArrow, DecreaseAnimationSpeedBinding, Category.Animation, "Halves animation speed.");
        public const string DecreaseAnimationSpeedBinding = "DecreaseAnimationSpeed";

        #endregion

        //--------------------------
        #region Debugging
        //--------------------------

        /// <summary>
        /// Toggles execution order (forward/backward).
        /// </summary>
        internal static readonly KeyCode ToggleExecutionOrder = Register(KeyCode.O, ToggleExecutionOrderBinding, Category.Debugging, "Toggles execution order (foward/backward).");
        public const string ToggleExecutionOrderBinding = "ToggleExecutionOrder";

        /// <summary>
        /// Continues execution until next breakpoint is reached.
        /// </summary>
        internal static readonly KeyCode ExecuteToBreakpoint = Register(KeyCode.B, ExecuteToBreakpointBinding, Category.Debugging, "Continues execution until next breakpoint is reached.");
        public const string ExecuteToBreakpointBinding = "ExecuteToBreakpoint";

        /// <summary>
        /// Execution is back to very first statement.
        /// </summary>
        internal static readonly KeyCode FirstStatement = Register(KeyCode.Home, FirstStatementBinding, Category.Debugging, "Execution is back to very first statement.");
        public const string FirstStatementBinding = "FirstStatement";

        #endregion

        //--------------------
        #region Source-code viewer
        //--------------------

        /// <summary>
        /// Toggles the menu of the available windows.
        /// </summary>
        internal static readonly KeyCode ShowWindowMenu = Register(KeyCode.F1, ShowWindowMenuBinding, Category.CodeViewer, "Toggles the menu of the open windows.");
        public const string ShowWindowMenuBinding = "ShowWindowMenu";

        /// <summary>
        /// Undoes an edit in the source-code viewer.
        /// </summary>
        internal static readonly KeyCode CodeWindowUndo = Register(KeyCode.F5, CodeWindowUndoBinding, Category.CodeViewer, "Undoes an edit in the source-code viewer.");
        public const string CodeWindowUndoBinding = "CodeWindowUndo";

        /// <summary>
        /// Redoes an undone edit in the source-code viewer.
        /// </summary>
        internal static readonly KeyCode CodeWindowRedo = Register(KeyCode.F6, CodeWindowRedoBinding, Category.CodeViewer, "Redoes an undone edit in the source-code viewer.");
        public const string CodeWindowRedoBinding = "CodeWindowRedo";

        /// <summary>
        /// Saves the content of the source-code viewer.
        /// </summary>
        internal static readonly KeyCode CodeWindowSave = Register(KeyCode.F7, CodeWindowSaveBinding, Category.CodeViewer, "Saves the content of the source-code viewer.");
        public const string CodeWindowSaveBinding = "CodeWindowSave";

        /// <summary>
        /// Refreshes syntax highlighting in the source-code viewer.
        /// </summary>
        internal static readonly KeyCode RefreshSyntaxHighlighting = Register(KeyCode.F8, RefreshSyntaxHighlightingBinding, Category.CodeViewer, "Refreshes syntax highlighting in the source-code viewer.");
        public const string RefreshSyntaxHighlightingBinding = "RefreshSyntaxHighlighting";

        #endregion

        //-----------------------------------------------------
        #region Text chat to communicate with other remote players
        //-----------------------------------------------------

        /// <summary>
        /// Opens the text chat.
        /// </summary>
        internal static readonly KeyCode OpenTextChat = Register(KeyCode.F2, OpenTextChatBinding, Category.Chat, "Opens the text chat.");
        public const string OpenTextChatBinding = "OpenTextChat";

        #endregion

        //-----------------------------------------------------
        #region Notifications
        //-----------------------------------------------------

        /// <summary>
        /// Closes all open notifications.
        /// </summary>
        internal static readonly KeyCode CloseNotifications = Register(KeyCode.X, CloseNotificationsBinding, Category.General, "Clears all notifications.");
        public const string CloseNotificationsBinding = "CloseNotifications";

        #endregion

        #region FaceCam

        /// <summary>
        /// Toggles the face camera.
        /// </summary>
        internal static readonly KeyCode ToggleFaceCam
            = Register(KeyCode.I, ToggleFaceCamBinding, Category.General, "Toggles the face camera on or off.");
        public const string ToggleFaceCamBinding = "ToggleFaceCam";

        /// <summary>
        /// Toggles the position of the FaceCam on the player's face.
        /// </summary>
        internal static readonly KeyCode ToggleFaceCamPosition
            = Register(KeyCode.F3, ToggleFaceCamPositionBinding, Category.General, "Toggles the position of the FaceCam on the player's face.");
        public const string ToggleFaceCamPositionBinding = "ToggleFaceCamPosition";

        #endregion

        #region Holistic Metric Menu

        //-----------------------------------------------------
        // Holistic metrics menu
        //-----------------------------------------------------

        /// <summary>
        /// Toggles the menu for holistic code metrics.
        /// </summary>
        internal static readonly KeyCode ToggleHolisticMetricsMenu = Register(KeyCode.C, ToggleHolisticMetricsMenuBinding, Category.General,
                                                                              "Toggles the menu for holistic code metrics");
        public const string ToggleHolisticMetricsMenuBinding = "ToggleHolisticMetricsMenu";

        #endregion

    }
}
