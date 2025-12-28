using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.Game.Drawable.ValueHolders;
using SEE.Net.Actions.Drawable;
using SEE.UI.Notification;
using SEE.UI.PopupMenu;
using SEE.Utils;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace SEE.UI.Drawable
{
    /// <summary>
    /// Controller for managing the buttons used to switch pages of a surface.
    /// </summary>
    public class SurfacePageController : MonoBehaviour
    {
        /// <summary>
        /// Button for increasing the currently selected page.
        /// </summary>
        private ButtonManagerBasic forward;

        /// <summary>
        /// Button for decreasing the currently selected page.
        /// </summary>
        private ButtonManagerBasic backward;

        /// <summary>
        /// Button for opening a <see cref="PopupMenu"/> to select a page.
        /// </summary>
        private ButtonManagerBasic display;

        /// <summary>
        /// Text for displaying the currentl< selected page.
        /// </summary>
        private TextMeshProUGUI displayMesh;

        /// <summary>
        /// The popup menu for chosing a page.
        /// </summary>
        private PopupMenu.PopupMenu popupMenu;

        /// <summary>
        /// The drawable holder of the depending surface.
        /// </summary>
        private DrawableHolder holder;

        /// <summary>
        /// Initializes the Surface Page Controller.
        /// </summary>
        private void Awake()
        {
            forward = transform.Find("ForwardButton").GetComponent<ButtonManagerBasic>();
            backward = transform.Find("BackButton").GetComponent<ButtonManagerBasic>();
            display = transform.Find("CurrentButton").GetComponent<ButtonManagerBasic>();
            displayMesh = transform.Find("CurrentButton").GetComponentInChildren<TextMeshProUGUI>();
            popupMenu = gameObject.AddComponent<PopupMenu.PopupMenu>();
            holder = GameFinder.GetDrawableSurface(gameObject).GetComponent<DrawableHolder>();

            displayMesh.text = holder.CurrentPage.ToString();

            /// Register handler for the back button.
            backward.clickEvent.AddListener(() =>
            {
                int page;
                if (holder.CurrentPage > 0)
                {
                    page = holder.CurrentPage - 1;
                } else
                {
                    page = holder.MaxPageSize - 1;
                };
                Notification();
                GameDrawableManager.ChangeCurrentPage(gameObject, page);
                new SynchronizeSurface(DrawableConfigManager.GetDrawableConfig(GameFinder.GetDrawableSurface(gameObject))).Execute();
            });

            /// Register handler for the forward button.
            forward.clickEvent.AddListener(() =>
            {
                int page;
                if (holder.CurrentPage < holder.MaxPageSize-1)
                {
                    page = holder.CurrentPage+ 1;
                } else
                {
                    page = 0;
                }
                Notification();
                GameDrawableManager.ChangeCurrentPage(gameObject, page);
                new SynchronizeSurface(DrawableConfigManager.GetDrawableConfig(GameFinder.GetDrawableSurface(gameObject))).Execute();
            });

            /// Register handler for the middle button (popup menu button)
            display.clickEvent.AddListener(PopupMenu);
        }

        /// <summary>
        /// Displays a notification indicating that an additional page must be added before the page can be switched.
        /// </summary>
        private void Notification()
        {
            if (holder.MaxPageSize == 1)
            {
                ShowNotification.Info("Add a new page",
                    "Currently, this surface has only one page. " +
                    "\nFirst, add a new page via the popup menu. " +
                    "\nYou can open this menu by clicking the middle button from the page selection.");
            }
        }

        /// <summary>
        /// Prepares the popup menu and makes it available.
        /// </summary>
        private void PopupMenu()
        {
            List<PopupMenuEntry> entries = new();

            List<int> pages = new();
            for (int i = 0; i < holder.MaxPageSize; i++)
            {
                pages.Add(i);
            }
            entries.AddRange(pages.Select(CreatePopupEntries));
            entries.Add(new PopupMenuAction("+", () =>
            {
                holder.MaxPageSize++;
                new SynchronizeSurface(DrawableConfigManager.GetDrawableConfig(GameFinder.GetDrawableSurface(gameObject))).Execute();
                PopupMenu();
            }, ' ', CloseAfterClick: false));

            popupMenu.ClearEntries();
            popupMenu.AddEntries(entries);
            popupMenu.ShowWith(position: UICanvas.Canvas.transform.position);
            UICanvas.Refresh();
        }

        /// <summary>
        /// Creates a <see cref="PopupMenuAction"/> for each page.
        /// </summary>
        /// <param name="pageNumber">The page number.</param>
        /// <returns>The created <see cref="PopupMenuAction"/> for the page number entry.</returns>
        private PopupMenuAction CreatePopupEntries(int pageNumber)
        {
            return new PopupMenuAction(pageNumber.ToString(), () => SetPage(pageNumber), GetIcon(pageNumber), CloseAfterClick: true);
        }

        /// <summary>
        /// Gets the depending icon for the entry.
        /// </summary>
        /// <param name="i">The entry number.</param>
        /// <returns>The corresponding icon.</returns>
        private char GetIcon(int i)
        {
            return i == holder.CurrentPage ? Icons.CheckedRadio : Icons.EmptyRadio;
        }

        /// <summary>
        /// Sets the page of the surface.
        /// </summary>
        /// <param name="page">The page to switch to.</param>
        public void SetPage(int page)
        {
            GameDrawableManager.ChangeCurrentPage(gameObject, page);
            new SynchronizeSurface(DrawableConfigManager.GetDrawableConfig(GameFinder.GetDrawableSurface(gameObject))).Execute();
        }

        /// <summary>
        /// Updates the displayed page number.
        /// </summary>
        public void UpdatePage()
        {
            displayMesh.text = holder.CurrentPage.ToString();
        }
    }
}
