using SEE.Controls;
using SEE.Game.City;
using SEE.UI.Notification;
using SEE.UI.PropertyDialog;
using SEE.Utils;
using UnityEngine;

namespace SEE.GameObjects
{
    /// <summary>
    /// Component for selecting a city to be added to the table.
    /// </summary>
    public class CitySelectionManager : MonoBehaviour
    {
        /// <summary>
        /// The progress states for selecting and adding a city to a table.
        /// </summary>
        private enum ProgressState
        {
            Start,
            ChoseCity,
        }

        /// <summary>
        /// The current progress state.
        /// </summary>
        private ProgressState progressState = ProgressState.Start;

        /// <summary>
        /// The dialog for selecting a city type.
        /// </summary>
        private readonly CitySelectionProperty citySelectionProperty = new();

        /// <summary>
        /// Provides the selection and addition of a city.
        /// </summary>
        void Update()
        {
            switch (progressState)
            {
                case ProgressState.Start:
                    if (SEEInput.Select()
                        && Raycasting.RaycastAnything(out RaycastHit raycastHit)
                        && raycastHit.collider.gameObject == gameObject)
                    {
                        citySelectionProperty.Open();
                        progressState = ProgressState.ChoseCity;
                    }
                    break;
                case ProgressState.ChoseCity:
                    if (citySelectionProperty.TryGetCity(out CityTypes? cityType))
                    {
                        switch(cityType)
                        {
                            case CityTypes.ReflexionCity:
                            case CityTypes.CodeCity:
                            case CityTypes.DiffCity:
                            case CityTypes.EvolutionCity:
                            case CityTypes.BranchCity:
                            case CityTypes.DynamicCity:
                                ShowNotification.Info("City selected", $"{cityType} was selected");
                                progressState = ProgressState.Start;
                                enabled = false;
                                break;
                            default:
                                ShowNotification.Warn("Error", "Error in city selection; The city cannot be added.");
                                break;
                        }
                    }
                    if (citySelectionProperty.WasCanceled())
                    {
                        progressState = ProgressState.Start;
                    }
                    break;
                default:
                    ShowNotification.Warn("Error", "Error in city selection");
                    break;
            }
        }
    }
}