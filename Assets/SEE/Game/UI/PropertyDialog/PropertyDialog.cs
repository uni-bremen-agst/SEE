using SEE.GO;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.Game.UI.PropertyDialog
{
    /// <summary>
    /// A UI dialog consisting of several groups, each with configurable properties.
    /// </summary>
    public partial class PropertyDialog: PlatformDependentComponent
    {
        /// <summary>
        /// The title of the dialog.
        /// </summary>
        public string Title;

        /// <summary>
        /// The description of the dialog.
        /// </summary>
        public string Description;

        /// <summary>
        /// The icon providing a visual clue on the dialog's purpose.
        /// </summary>
        public Sprite Icon;

        /// <summary>
        /// The list of cohesive groups of properties of the dialog. The properties of
        /// a group should be shown together because they are semantically related.
        /// A group could, for instance, be shown as a foldout or tab. The exact
        /// visual representation depends on the implementation.
        /// </summary>
        private readonly List<PropertyGroup> groups = new List<PropertyGroup>();

        /// <summary>
        /// A read-only wrapper around the list of groups for this dialog.
        /// </summary>
        public IList<PropertyGroup> Groups => groups.AsReadOnly();

        /// <summary>
        /// Adds a <paramref name="group"/> to this dialog's <see cref="entries"/>.
        /// </summary>
        /// <param name="group">The group to add to this dialog.</param>
        public void AddGroup(PropertyGroup group)
        {
            if (groups.Any(x => x.Name == group.Name))
            {
                throw new InvalidOperationException($"Group with the given name '{group.Name}' already exists!\n");
            }
            groups.Add(group);
        }

        /// <summary>
        /// If true, the dialog is currently shown.
        /// This value may differ from <see cref="DialogShouldBeShown"/>. The latter is only the
        /// request to show the dialog, where the actual occurence of the dialog can be delayed
        /// somewhat.
        /// </summary>
        private bool dialogIsShown = false;
        /// <summary>
        /// Whether the dialog should be shown.
        /// </summary>
        public bool DialogShouldBeShown { get; set; } = false;

        /// <summary>
        /// If <paramref name="allow"/> is true, the dialog is allowed to handle its
        /// own closing. If <paramref name="allow"/> is false, the client must handle
        /// the closing by calling <see cref="Close"/>.
        /// </summary>
        /// <param name="allow">whether the dialog may handle its own closing</param>
        public void AllowClosing(bool allow)
        {
            switch (Platform)
            {
                case PlayerInputType.DesktopPlayer:
                    AllowClosingDesktop(allow);
                    break;
                case PlayerInputType.TouchGamepadPlayer:
                    break;
                case PlayerInputType.VRPlayer:
                    break;
                case PlayerInputType.HoloLensPlayer:
                    break;
                case PlayerInputType.None: // no UI has to be rendered
                    break;
                default:
                    PlatformUnsupported();
                    break;
            }
        }

        /// <summary>
        /// Closes this dialog. Should be called only if self-managed closing of
        /// this dialog was disabled via <see cref="AllowClosing(bool)"/>.
        /// </summary>
        public void Close()
        {
            switch (Platform)
            {
                case PlayerInputType.DesktopPlayer:
                    CloseDesktop();
                    break;
                case PlayerInputType.TouchGamepadPlayer:
                    break;
                case PlayerInputType.VRPlayer:
                    break;
                case PlayerInputType.HoloLensPlayer:
                    break;
                case PlayerInputType.None: // no UI has to be rendered
                    break;
                default:
                    PlatformUnsupported();
                    break;
            }
        }

        /// <summary>
        /// Event triggered when the user presses the OK button. Clients can
        /// register on this event to receive a notification when this happens.
        /// </summary>
        public UnityEvent OnConfirm = new UnityEvent();
        /// <summary>
        /// Event triggered when the user presses the Cancel button. Clients can
        /// register on this event to receive a notification when this happens.
        /// </summary>
        public UnityEvent OnCancel = new UnityEvent();

        /// <summary>
        /// Destroys <see cref="dialog"/>.
        /// Called by Unity when an instance of <see cref="PropertyDialog"/> is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            Destroy(dialog);
        }

        /// <summary>
        /// Enables <see cref="dialog"/> and all <see cref="groups"/>.
        /// Called by Unity when an instance of <see cref="PropertyDialog"/> is enabled.
        /// </summary>
        private void OnEnable()
        {
            Enable(true);
        }

        /// <summary>
        /// Disables <see cref="dialog"/> and all <see cref="groups"/>.
        /// Called by Unity when an instance of <see cref="PropertyDialog"/> is disabled.
        /// </summary>
        private void OnDisable()
        {
            Enable(false);
        }

        /// <summary>
        /// Enables/disables <see cref="dialog"/> and all <see cref="groups"/>
        /// depending on <paramref name="value"/>.
        /// </summary>
        /// <param name="value">whether enabling or disabling is requested</param>
        private void Enable(bool value)
        {
            if (dialog != null)
            {
                dialog.SetActive(value);
            }

            foreach (PropertyGroup group in groups)
            {
                group.enabled = value;
            }
        }
    }
}
