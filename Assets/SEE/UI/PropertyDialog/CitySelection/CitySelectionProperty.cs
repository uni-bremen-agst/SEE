using SEE.Controls;
using SEE.Game.City;
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
        /// This input field lets the player pick a city type.
        /// </summary>
        private SelectionProperty selectedCity;

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

            /// Adds the property dialog to the dialog.
            PropertyDialog = Dialog.AddComponent<PropertyDialog>();
            PropertyDialog.Title = "Select a city type";
            PropertyDialog.Description = "Select a city type; then hit the OK button.";
            PropertyDialog.Icon = Resources.Load<Sprite>("Materials/ModernUIPack/Picker");
            PropertyDialog.AddGroup(group);

            PropertyDialog.OnConfirm.AddListener(OnConfirm);
            PropertyDialog.OnCancel.AddListener(Cancel);

            SEEInput.KeyboardShortcutsEnabled = false;
            PropertyDialog.DialogShouldBeShown = true;
        }

        /// <summary>
        /// This method gets called when the player confirms the dialog. It will save the selected city type in a
        /// variable and set <see cref="HolisticMetricsDialog.GotInput"/> to true.
        /// </summary>
        private void OnConfirm()
        {
            SEEInput.KeyboardShortcutsEnabled = true;
            city = (CityTypes)Enum.Parse(typeof(CityTypes), selectedCity.Value);
            GotInput = true;
            Destroyer.Destroy(Dialog);
        }

        /// <summary>
        /// Fetches the city type given by the player.
        /// </summary>
        /// <param name="cityType">If given and not yet fetched, this will be the city type the player selected.
        /// </param>
        /// <returns>The value of <see cref="HolisticMetricsDialog.GotInput"/></returns>
        internal bool TryGetCity(out CityTypes? cityType)
        {
            if (GotInput)
            {
                cityType = city;
                GotInput = false;
                return true;
            }
            cityType = null;
            return false;
        }
    }
}