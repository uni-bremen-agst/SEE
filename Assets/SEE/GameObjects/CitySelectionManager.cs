using Cysharp.Threading.Tasks;
using SEE.Controls;
using SEE.Game;
using SEE.Game.City;
using SEE.GO;
using SEE.Net.Actions.City;
using SEE.UI.Notification;
using SEE.UI.PropertyDialog.CitySelection;
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
            ChooseCity,
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
        /// Adds the game object to which the <see cref="AbstractSEECity"/> component
        /// will be attached and this table to the tuple list <see cref="CitiesHolder.Cities"/>.
        /// Is required to create the component across the network.
        /// </summary>
        private void Awake()
        {
            WaitForLocalPlayerInstantiation().Forget();
            return;

            async UniTask WaitForLocalPlayerInstantiation()
            {
                await UniTask.WaitUntil(() => LocalPlayer.Instance != null);
                if (LocalPlayer.TryGetCitiesHolder(out CitiesHolder citiesHolder)
                    && !citiesHolder.Cities.ContainsKey(transform.parent.name))
                {
                    citiesHolder.Cities.Add(transform.parent.name, gameObject);
                }
            }
        }

        /// <summary>
        /// Provides the selection and addition of a city.
        /// </summary>
        private void Update()
        {
            if (gameObject.IsCodeCityDrawn())
            {
                CityComponentsSettings();
            }
            switch (progressState)
            {
                case ProgressState.Start:
                    if (SEEInput.Select()
                        && Raycasting.RaycastAnything(out RaycastHit raycastHit)
                        && raycastHit.collider.gameObject == gameObject)
                    {
                        citySelectionProperty.Open();
                        progressState = ProgressState.ChooseCity;
                    }
                    break;

                case ProgressState.ChooseCity:
                    if (citySelectionProperty.TryGetCity(out CityTypes? cityType, out string cityName))
                    {
                        switch (cityType)
                        {
                            case CityTypes.ReflexionCity:
                                CityAdder.CreateReflexionCityAsync(gameObject, cityName).Forget();
                                new AddReflexionCityNetAction(transform.parent.name, cityName).Execute();
                                break;
                            case CityTypes.CodeCity:
                            case CityTypes.DiffCity:
                            case CityTypes.EvolutionCity:
                            case CityTypes.BranchCity:
                            case CityTypes.DynamicCity:
                                ShowNotification.Warn("Not available", "This type of city cannot be added yet.");
                                break;
                            default:
                                ShowNotification.Warn("City type is not supported",
                                    "The selected city type is not supported and cannot be added yet.");
                                break;
                        }
                        progressState = ProgressState.Start;
                    }
                    if (citySelectionProperty.WasCanceled())
                    {
                        progressState = ProgressState.Start;
                    }
                    break;

                default:
                    ShowNotification.Warn("Unexpected progress state", "An unexpected progress state occurred during execution.");
                    progressState = ProgressState.Start;
                    break;
            }
        }

        /// <summary>
        /// Activation and deactivation of the additionally required components of the
        /// city game object.
        /// </summary>
        private void CityComponentsSettings()
        {
            gameObject.GetComponent<CityCursor>().enabled = true;
            enabled = false;
        }
    }
}
