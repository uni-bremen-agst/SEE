using Cysharp.Threading.Tasks;
using SEE.Controls;
using SEE.Game.City;
using SEE.Game.CityRendering;
using SEE.GO;
using SEE.Layout.NodeLayouts;
using SEE.Layout;
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
        #region Initial Cities Configuration Paths
        private const string reflexionCityPath = "Assets/StreamingAssets/reflexion/initial/Reflexion.cfg";

        #endregion

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
                                SEEReflexionCity reflexionCity = gameObject.AddComponent<SEEReflexionCity>();
                                gameObject.AddComponent<ReflexionVisualization>();
                                gameObject.AddComponent<EdgeMeshScheduler>();
                                gameObject.GetComponent<CityCursor>().enabled = true;

                                reflexionCity.ConfigurationPath = new(reflexionCityPath);
                                reflexionCity.LoadConfiguration();
                                CreateReflexionCityAsync(reflexionCity).Forget();
                                //reflexionCity.LoadAndDrawGraphAsync().Forget();
                                enabled = false;
                                break;
                            case CityTypes.CodeCity:
                            case CityTypes.DiffCity:
                            case CityTypes.EvolutionCity:
                            case CityTypes.BranchCity:
                            case CityTypes.DynamicCity:
                                ShowNotification.Warn("Not available", "This type of city cannot be added yet.");
                                break;
                            default:
                                ShowNotification.Warn("Error", "Error in city selection; The city cannot be added.");
                                progressState = ProgressState.Start;
                                return;
                        }
                        progressState = ProgressState.Start;
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

        private async UniTask CreateReflexionCityAsync(SEEReflexionCity city)
        {
            await city.LoadDataAsync();
            city.DrawGraph();
        }
    }
}