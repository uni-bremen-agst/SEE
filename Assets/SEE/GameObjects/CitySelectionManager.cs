using Cysharp.Threading.Tasks;
using SEE.Controls;
using SEE.Game;
using SEE.Game.City;
using SEE.GO;
using SEE.Net.Actions;
using SEE.UI.Notification;
using SEE.UI.PropertyDialog.CitySelection;
using SEE.Utils;
using System;
using UnityEngine;

namespace SEE.GameObjects
{
    /// <summary>
    /// Component for selecting a city to be added to the table.
    /// </summary>
    public class CitySelectionManager : MonoBehaviour
    {
        #region Initial Cities Configuration Paths
        /// <summary>
        /// Path to the initial reflexion city.
        /// </summary>
        public static readonly string InitialReflexionCityPath
            = Application.streamingAssetsPath + "/reflexion/initial/Reflexion.cfg";
        #endregion

        /// <summary>
        /// Constant for the word "initial", required to replace this keyword with the city name.
        /// The initial reflexion city graph uses this keyword as a placeholder for the selected city name in its files.
        /// </summary>
        public const string Initial = "initial";

        /// <summary>
        /// The progress states for selecting and adding a city to a table.
        /// </summary>
        private enum ProgressState
        {
            Start,
            ChooseCity,
            ReflexionCity,
            Finish,
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
        /// The type of city that should be created via a network execution.
        /// </summary>
        private CityTypes? typeOfNetworkExecution = null;

        /// <summary>
        /// The name for the city that should be created via a network execution.
        /// </summary>
        private string nameOfNetworkExecution = null;

        /// <summary>
        /// Adds the game object to which the <see cref="AbstractSEECity"/> component
        /// will be attached and this table to the tuple list <see cref="CitiesHolder.Cities"/>.
        /// Is required to create the component across the network.
        /// </summary>
        void Awake()
        {
            WaitForLocalPlayerInstantiation().Forget();
            return;

            async UniTask WaitForLocalPlayerInstantiation()
            {
                await UniTask.WaitUntil(() => LocalPlayer.Instance != null);
                if (LocalPlayer.TryGetCitiesHolder(out CitiesHolder citysHolder))
                {
                    citysHolder.Cities.Add(transform.parent.name, gameObject);
                }
            }
        }

        /// <summary>
        /// Provides the selection and addition of a city.
        /// </summary>
        private void Update()
        {
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
                    if (citySelectionProperty.TryGetCity(out CityTypes? cityType, out string cityName) || typeOfNetworkExecution != null)
                    {
                        if (cityType != null)
                        {
                            new AddCityNetAction(transform.parent.name, cityType.Value, cityName).Execute();
                        }
                        else
                        {
                            cityType = typeOfNetworkExecution;
                            cityName = nameOfNetworkExecution;
                            // If the variable is not reset, there will be multiple attempts to add the city, which leads to errors.
                            typeOfNetworkExecution = null;
                            nameOfNetworkExecution = null;
                        }
                        gameObject.name = cityName;
                        /// Delete existing <see cref="AbstractSEECity"/> component.
                        if (GetComponent<AbstractSEECity>() != null)
                        {
                            Destroyer.Destroy(GetComponent<AbstractSEECity>());
                        }
                        switch (cityType)
                        {
                            case CityTypes.ReflexionCity:
                                CreateReflexionCityAsync().Forget();
                                break;
                            case CityTypes.CodeCity:
                            case CityTypes.DiffCity:
                            case CityTypes.EvolutionCity:
                            case CityTypes.BranchCity:
                            case CityTypes.DynamicCity:
                                ShowNotification.Warn("Not available", "This type of city cannot be added yet.");
                                progressState = ProgressState.Start;
                                break;
                            default:
                                ShowNotification.Warn("City type is not supported",
                                    "The selected city type is not supported and cannot be added yet.");
                                progressState = ProgressState.Start;
                                return;
                        }
                    }
                    if (citySelectionProperty.WasCanceled())
                    {
                        progressState = ProgressState.Start;
                    }
                    break;

                case ProgressState.ReflexionCity:
                    if (gameObject.GetComponent<SEEReflexionCity>() != null
                        && gameObject.IsCodeCityDrawn())
                    {
                        SEEReflexionCity reflexionCity = gameObject.GetComponent<SEEReflexionCity>();
                        FitInitalReflexionCity(reflexionCity);
                        progressState = ProgressState.Finish;
                    }
                    break;

                case ProgressState.Finish:
                    CityComponentsSettings();
                    progressState = ProgressState.Start;
                    break;

                default:
                    ShowNotification.Warn("Unexpected progress state", "An unexpected progress state occurred during execution.");
                    break;
            }
        }

        /// <summary>
        /// Creates a city through a network execution.
        /// </summary>
        /// <param name="cityType">The type of city that should be added.</param>
        internal void CreateCity(CityTypes cityType, string cityName)
        {
            typeOfNetworkExecution = cityType;
            nameOfNetworkExecution = cityName;
            progressState = ProgressState.ChooseCity;
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

        #region Reflexion City
        /// <summary>
        /// Creates and loads an initial reflexion city.
        /// </summary>
        /// <returns>Needed for asynchrony.</returns>
        private async UniTask CreateReflexionCityAsync()
        {
            SEEReflexionCity reflexionCity = gameObject.AddComponent<SEEReflexionCity>();
            gameObject.AddComponent<ReflexionVisualization>();
            gameObject.AddComponent<EdgeMeshScheduler>();

            reflexionCity.ConfigurationPath = new(InitialReflexionCityPath);
            reflexionCity.LoadConfiguration();
            await reflexionCity.LoadDataAsync();
            reflexionCity.DrawGraph();
            progressState = ProgressState.ReflexionCity;
        }

        /// <summary>
        /// Ensures that the architecture root is always positioned on the right side
        /// and the implementation root on the left side.
        /// Additionally, the roots are scaled to a ratio of 60 (architecture)
        /// to 40 (implementation).
        /// </summary>
        /// <param name="city">The reflexion city.</param>
        /// <exception cref="ArgumentNullException">If the <paramref name="city"/> is null.</exception>
        private void FitInitalReflexionCity(SEEReflexionCity city)
        {
            if (city == null)
            {
                throw new ArgumentNullException(nameof(city));
            }

            GameObject arch = city.ReflexionGraph.ArchitectureRoot.GameObject(true);
            GameObject impl = city.ReflexionGraph.ImplementationRoot.GameObject(true);

            /// Changes the position of the architecture and implementation roots.
            /// The result is that the architecture root is on the right side,
            /// and the implementation root is on the left side.
            if (arch.transform.position.z < impl.transform.position.z)
            {
                Vector3 oldPosition = arch.transform.position;
                arch.transform.position = impl.transform.position;
                impl.transform.position = oldPosition;
            }

            /// Adjusting the initial size.
            /// The architecture root should occupy approximately 60% of the table,
            /// and the implementation root 40%.
            /// FIXME(#816): Update once branch 816-layouts-for-reflexion-modeling is merged.
            float currentScale = 0.5f;
            float targetArchScale = 0.6f;
            float targetImplScale = 1 - targetArchScale;
            ApplyScale(arch, targetArchScale / currentScale);
            ApplyScale(impl, targetImplScale / currentScale);

            return;

            static void ApplyScale(GameObject obj, float factor)
            {
                Vector3 oldScale = obj.transform.localScale;
                Vector3 newScale = new (oldScale.x, oldScale.y, oldScale.z * factor);
                float diff = oldScale.z - newScale.z;
                diff = diff < 0 ? diff : -diff;
                Vector3 newPosition = obj.transform.position + new Vector3(0, 0, diff) * 1.5f;
                obj.NodeOperator().ResizeTo(newScale, newPosition, 0, reparentChildren: false);
            }
        }
        #endregion
    }
}
