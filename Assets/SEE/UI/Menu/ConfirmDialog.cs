using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Michsky.UI.ModernUIPack;
using SEE.GO;
using SEE.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SEE.UI.Menu
{
    /// <summary>
    /// Configuration for the confirm dialog.
    /// </summary>
    /// <param name="Description">The description of the dialog, can be up to three lines long.</param>
    /// <param name="Title">The title of the dialog displayed in the top bar. Should be kept short.</param>
    /// <param name="YesText">The text of the confirm button.</param>
    /// <param name="NoText">The text of the cancel button.</param>
    /// <param name="YesIcon">The icon of the confirm button, given as a FontAwesome character (see <see cref="Icons"/>).</param>
    /// <param name="NoIcon">The icon of the cancel button, given as a FontAwesome character (see <see cref="Icons"/>).</param>
    /// <param name="YesColor">The color of the confirm button. Note that the text will be white, so choose a dark color.</param>
    public record ConfirmConfiguration(
        string Description,
        string Title = "Are you sure?",
        string YesText = "Confirm",
        string NoText = "Cancel",
        char YesIcon = Icons.Checkmark,
        char NoIcon = 'X',
        Color? YesColor = null,
        Color? NoColor = null)
    {
        /// <summary>
        /// A pre-made configuration for a delete dialog.
        /// </summary>
        /// <param name="description">The description of the dialog, can be up to three lines long.</param>
        /// <returns>A configuration for a delete dialog.</returns>
        public static ConfirmConfiguration Delete(string description)
        {
            return new ConfirmConfiguration(description, Title: "Really delete?",
                                            YesText: "Delete", YesIcon: Icons.Trash, YesColor: Color.red.Darker());
        }

        public static ConfirmConfiguration YesNo(string description)
        {
            return new ConfirmConfiguration(description,
                                            YesText: "Yes", YesIcon: Icons.Checkmark, YesColor: Color.green.Darker(),
                                            NoText: "No", NoIcon: 'X', NoColor: Color.red.Darker());
        }
    }

    /// <summary>
    /// A simple configurable confirmation dialog with a "confirm" and a "cancel" button.
    /// </summary>
    public class ConfirmDialog : PlatformDependentComponent
    {
        /// <summary>
        /// The duration of the fade animation in seconds.
        /// </summary>
        private const float animationDuration = 1.0f;

        /// <summary>
        /// Prefab for the dialog.
        /// </summary>
        private const string dialogMenuPrefab = UIPrefabFolder + "ConfirmDialog";

        /// <summary>
        /// The default color of the confirm button.
        /// </summary>
        private static readonly Color defaultYesColor = Color.green.Darker();  // Slightly darker green.

        /// <summary>
        /// The default color of the cancel button.
        /// </summary>
        private static readonly Color defaultNoColor = new(74, 78, 152); // The default blue.

        /// <summary>
        /// Whether the dialog should be destroyed after it is closed (and faded out).
        /// </summary>
        private bool oneTime;

        /// <summary>
        /// The dialog menu game object.
        /// </summary>
        [ManagedUI]
        private GameObject Dialog { get; set; }

        /// <summary>
        /// The canvas group of the dialog, used for fading in and out.
        /// </summary>
        private CanvasGroup CanvasGroup { get; set; }

        /// <summary>
        /// The title of the dialog.
        /// </summary>
        private TextMeshProUGUI Title { get; set; }

        /// <summary>
        /// The description of the dialog.
        /// Can be up to three lines long.
        /// </summary>
        private TextMeshProUGUI Description { get; set; }

        /// <summary>
        /// The negative/"cancel" button of the dialog.
        /// </summary>
        private ButtonManagerBasic NoButton { get; set; }

        /// <summary>
        /// The positive/"confirm" button of the dialog.
        /// </summary>
        private ButtonManagerBasic YesButton { get; set; }

        /// <summary>
        /// The image component of the <see cref="YesButton"/> controlling its color.
        /// </summary>
        private Image YesButtonImage { get; set; }

        /// <summary>
        /// The image component of the <see cref="NoButton"/> controlling its color.
        /// </summary>
        private Image NoButtonImage { get; set; }

        /// <summary>
        /// The TextMeshPro component for the icon of the <see cref="NoButton"/>.
        /// </summary>
        private TextMeshProUGUI NoIcon { get; set; }

        /// <summary>
        /// The TextMeshPro component for the icon of the <see cref="YesButton"/>.
        /// </summary>
        private TextMeshProUGUI YesIcon { get; set; }

        /// <summary>
        /// An event that is invoked when the user makes a choice in the dialog (including closing it).
        /// </summary>
        private UnityEvent<bool> OnChoiceMade { get; } = new();

        /// <summary>
        /// The game object under which the dialogs are instantiated.
        /// </summary>
        private static Lazy<GameObject> dialogGameObject;

        /// <summary>
        /// Property of <see cref="dialogGameObject"/>.
        /// This is required because the static variable is not cleared with each
        /// execution of SEE, and therefore, there may be an attempt to access a
        /// <see cref="GameObject"/> that no longer exists.
        /// </summary>
        private static Lazy<GameObject> DialogGameObject
        {
            get
            {
                if (dialogGameObject == null || dialogGameObject.Value == null)
                {
                    dialogGameObject = new(() => new("ConfirmDialogs"));
                }
                return dialogGameObject;
            }
        }

        /// <summary>
        /// The ongoing fade animation of the dialog, if any.
        /// </summary>
        private Tweener existingFade;

        /// <summary>
        /// Initializes the dialog.
        /// </summary>
        protected override void StartDesktop()
        {
            Dialog = PrefabInstantiator.InstantiatePrefab(dialogMenuPrefab, Canvas.transform, false);
            CanvasGroup = Dialog.MustGetComponent<CanvasGroup>();
            NoButton = Dialog.transform.Find("Content/Buttons/Cancel").gameObject.MustGetComponent<ButtonManagerBasic>();
            NoIcon = NoButton.transform.Find("Texts/Icon").gameObject.MustGetComponent<TextMeshProUGUI>();
            YesButton = Dialog.transform.Find("Content/Buttons/Confirm").gameObject.MustGetComponent<ButtonManagerBasic>();
            YesIcon = YesButton.transform.Find("Texts/Icon").gameObject.MustGetComponent<TextMeshProUGUI>();
            YesButtonImage = YesButton.gameObject.MustGetComponent<Image>();
            NoButtonImage = NoButton.gameObject.MustGetComponent<Image>();
            Title = Dialog.transform.Find("Dragger/Text").gameObject.MustGetComponent<TextMeshProUGUI>();
            Description = Dialog.transform.Find("Content/Description").gameObject.MustGetComponent<TextMeshProUGUI>();
            ButtonManagerBasic CloseButton = Dialog.transform.Find("Dragger/CancelDragger").gameObject.MustGetComponent<ButtonManagerBasic>();

            YesButton.clickEvent.AddListener(() => OnChoiceMade.Invoke(true));
            NoButton.clickEvent.AddListener(() => OnChoiceMade.Invoke(false));
            CloseButton.clickEvent.AddListener(() => OnChoiceMade.Invoke(false));
            OnChoiceMade.AddListener(_ => CloseMenu());
        }

        /// <summary>
        /// Sets up the dialog with the given <paramref name="configuration"/> and fades it in.
        /// </summary>
        /// <param name="configuration">The configuration for the dialog.</param>
        private void ShowMenu(ConfirmConfiguration configuration)
        {
            Title.text = configuration.Title;
            Description.text = configuration.Description;
            YesButton.buttonText = configuration.YesText;
            YesButton.UpdateUI();
            NoButton.buttonText = configuration.NoText;
            NoButton.UpdateUI();
            YesIcon.text = configuration.YesIcon.ToString();
            YesButtonImage.color = configuration.YesColor ?? defaultYesColor;
            NoButtonImage.color = configuration.NoColor ?? defaultNoColor;
            NoIcon.text = configuration.NoIcon.ToString();

            Dialog.SetActive(true);
            Dialog.transform.SetAsLastSibling();
            existingFade?.Kill();
            existingFade = CanvasGroup.DOFade(1f, animationDuration);
        }

        /// <summary>
        /// Closes the dialog by fading it out.
        /// </summary>
        private void CloseMenu()
        {
            existingFade?.Kill();
            existingFade = CanvasGroup.DOFade(0f, animationDuration).OnComplete(DisableMenu);
        }

        /// <summary>
        /// Disables/destroys the dialog after it has been faded out.
        /// </summary>
        private void DisableMenu()
        {
            if (oneTime)
            {
                Destroyer.Destroy(this);
            }
            else
            {
                Dialog.SetActive(false);
            }
        }

        /// <summary>
        /// Shows a confirmation dialog with the given <paramref name="configuration"/>
        /// and waits for the user to make a choice. Their choice is returned as a <see cref="bool"/>.
        /// </summary>
        /// <param name="configuration">The configuration for the dialog.</param>
        /// <returns>Whether the user confirmed the dialog.</returns>
        public static async UniTask<bool> ConfirmAsync(ConfirmConfiguration configuration)
        {
            ConfirmDialog dialog = DialogGameObject.Value.AddComponent<ConfirmDialog>();
            dialog.oneTime = true;
            await UniTask.WaitUntil(() => dialog.Dialog != null); // May need to wait for initialization.
            dialog.ShowMenu(configuration);
            return await dialog.OnChoiceMade.OnInvokeAsync(CancellationToken.None);
        }
    }
}
