using SEE.Controls;
using SEE.Game;
using SEE.Game.City;
using SEE.GameObjects;
using SEE.UI.Notification;
using SEE.UI.PropertyDialog.HolisticMetrics;
using SEE.Utils;
using System;
using UnityEngine;

namespace SEE.UI.PropertyDialog.CitySelection
{
    /// <summary>
    /// This class manages the dialog for adding a city.
    /// </summary>
    internal class CitySelectionProperty : HolisticMetricsDialog
    {
        /// <summary>
        /// The city which the player selected.
        /// </summary>
        private static CityTypes city;

        /// <summary>
        /// The city name which the player entered.
        /// </summary>
        private static string name;

        /// <summary>
        /// This input field lets the player pick a city type.
        /// </summary>
        private SelectionProperty selectedCity;

        /// <summary>
        /// this input field where the player can enter a city name.
        /// </summary>
        private StringProperty selectedName;

        /// <summary>
        /// This method instantiates the dialog and then displays it to the player.
        /// </summary>
        public void Open()
        {
            Dialog = new GameObject("Add city dialog");
            PropertyGroup group = Dialog.AddComponent<PropertyGroup>();
            group.Name = "Add city dialog";

            selectedCity = Dialog.AddComponent<SelectionProperty>();
            selectedCity.Name = "Select a city type.";
            selectedCity.Description = "Select a city type to be added.";
            selectedCity.AddOptions(Enum.GetNames(typeof(CityTypes)));
            selectedCity.Value = Enum.GetValues(typeof(CityTypes)).GetValue(0).ToString();
            group.AddProperty(selectedCity);

            selectedName = Dialog.AddComponent<StringProperty>();
            selectedName.Name = "City Name";
            selectedName.Description = "Enter a name for the city.";
            group.AddProperty(selectedName);

            /// Adds the property dialog to the dialog.
            PropertyDialog = Dialog.AddComponent<PropertyDialog>();
            PropertyDialog.Title = "Select a city type";
            PropertyDialog.Description = "Select a city type; then hit the OK button.";
            PropertyDialog.Icon = Resources.Load<Sprite>("Materials/ModernUIPack/Picker");
            PropertyDialog.AddGroup(group);

            PropertyDialog.OnConfirm.AddListener(OnConfirm);
            PropertyDialog.OnCancel.AddListener(Cancel);
            /// Prevents the dialog from closing automatically upon confirmation.
            PropertyDialog.AllowClosing(false);

            SEEInput.KeyboardShortcutsEnabled = false;
            PropertyDialog.DialogShouldBeShown = true;
        }

        /// <summary>
        /// This method gets called when the player confirms the dialog. It will save the selected city type in a
        /// variable and set <see cref="HolisticMetricsDialog.GotInput"/> to true.
        /// </summary>
        private void OnConfirm()
        {
            if (string.IsNullOrEmpty(selectedName.Value.Trim()))
            {
                // TODO show validation failed.
                ShowNotification.Warn("Name is empty", "You need to enter an name for the city.");
            }
            else if (LocalPlayer.TryGetCitiesHolder(out CitiesHolder citiesHolder) && citiesHolder.IsNameAlreadyUsed(selectedName.Value))
            {
                // TODO is name already in use.
                ShowNotification.Warn("Name is already in use", "You need to enter an not used name for the city.");
            }
            else
            {
                SEEInput.KeyboardShortcutsEnabled = true;
                city = (CityTypes)Enum.Parse(typeof(CityTypes), selectedCity.Value);
                name = selectedName.Value;
                GotInput = true;
                Destroyer.Destroy(Dialog);
            }
        }

        /// <summary>
        /// Fetches the city type given by the player.
        /// </summary>
        /// <param name="cityType">If given and not yet fetched, this will be the city type the player selected.</param>
        /// <param name="cityName">If given and not yet fetched, this will be the city name the player chosen.</param>
        /// <returns>The value of <see cref="HolisticMetricsDialog.GotInput"/></returns>
        internal bool TryGetCity(out CityTypes? cityType, out string cityName)
        {
            if (GotInput)
            {
                cityType = city;
                cityName = name;
                GotInput = false;
                return true;
            }
            cityType = null;
            cityName = null;
            return false;
        }
    }
}