using Michsky.UI.ModernUIPack;
using SEE.GO;
using SEE.Utils;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.Game.UI.PropertyDialog
{
    /// <summary>
    /// A UI dialog consisting of several groups, each representing a list of configurable properties.
    ///
    /// This file contains the implementation for the desktop environment.
    /// </summary>
    public partial class PropertyDialog
    {
        /// <summary>
        /// The path of the prefab to instantiate the dialog.
        /// </summary>
        private const string DialogPrefab = "Prefabs/UI/PropertyDialog";
        /// <summary>
        /// Name of the ascendant in the Modern UI Pack dialog relative to the instantiated <see cref="dialog"/>.
        /// </summary>
        private const string ContentChildName = "Main Content/Content Mask/Content/InputFields/Scroll Area/List";

        /// <summary>
        /// The instantiation of <see cref="DialogPrefab"/>.
        /// </summary>
        private GameObject dialog;
        /// <summary>
        /// The component <see cref="ModalWindowManager"/> attached to the instantiated <see cref="dialog"/>.
        /// This component allows us to set the icon, title, description of the dialog and to retrieve
        /// the OK and Cancel buttons we need to react to.
        /// </summary>
        private ModalWindowManager modal;

        /// <summary>
        /// Whether the dialog may handle its own closing.
        /// </summary>
        private bool allowClosing = true;

        /// <summary>
        /// Instantiates <see cref="dialog"/> from <see cref="DialogPrefab"/>,
        /// and makes it a child of the <see cref="Canvas"/>.
        /// In addition, a <see cref="ModalWindowManager"/> is attached to it (<see cref="modal"/>)
        /// with the attributes <see cref="Title"/> and <see cref="Description"/> and the <see cref="Icon"/>.
        /// Every <see cref="groups"/> element is created and added as child to the dialog's ascendant
        /// named <see cref="ContentChildName"/>.
        /// </summary>
        protected override void StartDesktop()
        {
            try
            {
                dialog = PrefabInstantiator.InstantiatePrefab(DialogPrefab, Canvas.transform, false);
            }
            catch (Exception e)
            {
                Debug.LogError($"The dialog {Title} could not be instantiated using the prefab {DialogPrefab}: {e.Message}.\n");
                enabled = false;
                return;
            }
            dialog.name = Title;
            if (dialog.TryGetComponent(out modal))
            {
                modal.titleText = Title;
                if (Icon != null)
                {
                    modal.icon = Icon;
                }
                modal.descriptionText = Description;

                Transform contentChild = dialog.transform.Find(ContentChildName);
                foreach (PropertyGroup group in groups)
                {
                    group.SetParent(contentChild.gameObject);
                }

                // Client notifications for the buttons
                modal.onConfirm.AddListener(() => OnConfirm.Invoke());
                modal.onCancel.AddListener(() => OnCancel.Invoke());

                // To close the dialog if any of the buttons are pressed.
                if (allowClosing)
                {
                    modal.onConfirm.AddListener(() => DialogShouldBeShown = false);
                    modal.onCancel.AddListener(() => DialogShouldBeShown = false);
                }

                EnableOnClick(modal.confirmButton, allowClosing);
                EnableOnClick(modal.cancelButton, allowClosing);

                if (groups.Any())
                {
                    if (contentChild.parent.gameObject.TryGetComponentOrLog(out ScrollRect rect))
                    {
                        rect.content = (RectTransform)groups[0].PropertyGroupUIObject.transform;
                    }
                }
            }
            else
            {
                Debug.LogError($"The dialog {dialog.name} does not have a {nameof(ModalWindowManager)} component attached to it.\n");
                enabled = false;
            }
        }

        /// <summary>
        /// Enables or disables the dialog depending on <see cref="DialogShouldBeShown"/>.
        /// </summary>
        protected override void UpdateDesktop()
        {
            if (DialogShouldBeShown != dialogIsShown)
            {
                if (DialogShouldBeShown)
                {
                    // Move window to the top of the hierarchy (which, confusingly, is actually at the bottom)
                    // so that this dialog is rendered over any other potentially existing item on the UI canvas.
                    modal.transform.SetAsLastSibling();
                    GetReady();
                    modal.OpenWindow();
                }
                else
                {
                    modal.CloseWindow();
                }
                dialogIsShown = !dialogIsShown;
            }
        }

        /// <summary>
        /// Calls <see cref="PropertyGroup.GetReady()"/> for each group in <see cref="groups"/>.
        /// </summary>
        private void GetReady()
        {
            foreach (PropertyGroup group in groups)
            {
                group.GetReady();
            }
        }

        /// <summary>
        /// Implementation of <see cref="Close()"/> for <see cref="PlayerInputType.DesktopPlayer"/>.
        /// Closes the modal window.
        /// </summary>
        private void CloseDesktop()
        {
            modal?.CloseWindow();
        }

        /// <summary>
        /// Implementation of <see cref="AllowClosing(bool)"/> for <see cref="PlayerInputType.DesktopPlayer"/>.
        /// Disables/enables the OnClick callback of the two buttons (OK and Cancel).
        /// </summary>
        /// <param name="allow">whether the dialog may handle its own closing</param>
        private void AllowClosingDesktop(bool allow)
        {
            /// We cannot act on the OK and cancel button yet, because they may not exist
            /// when this method is called. We only save the client's wish here and handle
            /// the necessary modifications in <see cref="StartDesktop"/>.
            allowClosing = allow;
        }

        /// <summary>
        /// Adds or removes <see cref="CloseDesktop"/> from the <paramref name="button"/>'s
        /// onClick event dependent upon <paramref name="add"/>.
        /// </summary>
        /// <param name="button">the button for which to register/unregister <see cref="CloseDesktop"/></param>
        /// <param name="add">if true, <see cref="CloseDesktop"/> is registered on onClick,
        /// otherwise unregistered</param>
        private void EnableOnClick(Button button, bool add)
        {
            if (add)
            {
                button.onClick.AddListener(CloseDesktop);
            }
            else
            {
                button.onClick.RemoveListener(CloseDesktop);
            }
        }
    }
}
