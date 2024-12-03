using Cysharp.Threading.Tasks;
using MathNet.Numerics;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using SEE.Controls;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.Game.City;
using SEE.Game.SceneManipulation;
using SEE.GO;
using SEE.Net.Actions;
using SEE.Tools.ReflexionAnalysis;
using SEE.UI.DebugAdapterProtocol.DebugAdapter;
using SEE.UI.Notification;
using SEE.UI.PropertyDialog.CitySelection;
using SEE.UI.RuntimeConfigMenu;
using SEE.Utils;
using System;
using System.Collections.Generic;
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
            RelfexionCity,
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
        /// Adds the game object to which the <see cref="AbstractSEECity"/> comonent
        /// will be attached and his table to the list of all such objects.
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
                    citysHolder.Cities.Add((gameObject, transform.parent.gameObject));
                }
            }
        }

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
                    if (citySelectionProperty.TryGetCity(out CityTypes? cityType, out string cityName) || typeOfNetworkExecution != null)
                    {
                        if (cityType != null)
                        {
                            new AddCityNetAction(transform.parent.name, cityType.Value, cityName).Execute();
                        } else
                        {
                            cityType = typeOfNetworkExecution;
                            cityName = nameOfNetworkExecution;
                            // If the variable is not reset, there will be multiple attempts to add the city, which leads to errors.
                            typeOfNetworkExecution = null;
                            nameOfNetworkExecution = null;
                        }
                        gameObject.name = cityName;
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
                                ShowNotification.Warn("Error", "Error in city selection; The city cannot be added.");
                                progressState = ProgressState.Start;
                                return;
                        }
                    }
                    if (citySelectionProperty.WasCanceled())
                    {
                        progressState = ProgressState.Start;
                    }
                    break;

                case ProgressState.RelfexionCity:
                    if (gameObject.GetComponent<SEEReflexionCity>() != null
                        && gameObject.IsCodeCityDrawn())
                    {
                        SEEReflexionCity reflexionCity = gameObject.GetComponent<SEEReflexionCity>();
                        FitInitalReflexionCity(reflexionCity);
                        Rename(reflexionCity);
                        progressState = ProgressState.Finish;
                    }
                    break;

                case ProgressState.Finish:
                    //AddCityToRuntimeMenu();
                    CityComponentsSettings();
                    progressState = ProgressState.Start;
                    break;

                default:
                    ShowNotification.Warn("Error", "Error in city selection");
                    break;
            }
        }

        /// <summary>
        /// Adds the created city to the <see cref="RuntimeConfigMenu"/>
        /// </summary>
        private void AddCityToRuntimeMenu()
        {
            if (LocalPlayer.TryGetRuntimeConfigMenu(out RuntimeConfigMenu runtimeConfigMenu))
            {
                runtimeConfigMenu.BuildTabMenus();
            }
            else
            {
                ShowNotification.Warn("Error", "Error the city can't be added to the runtime config menu.");
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
            progressState = ProgressState.ChoseCity;
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

            reflexionCity.ConfigurationPath = new(reflexionCityPath);
            reflexionCity.LoadConfiguration();
            await reflexionCity.LoadDataAsync();
            reflexionCity.DrawGraph();
            progressState = ProgressState.RelfexionCity;
        }

        /// <summary>
        /// Ensures that the architecture root is always positioned on the right side
        /// and the implementation root on the left side.
        /// Additionally, the roots are scaled to a ratio of 60 (architecture)
        /// to 40 (implementation).
        /// </summary>
        /// <param name="city">The reflexion city.</param>
        /// <exception cref="ArgumentException">If the city is null.</exception>
        private void FitInitalReflexionCity(SEEReflexionCity city)
        {
            if (city == null)
            {
                throw new ArgumentException("The city is null.");
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
            float currenScale = 0.5f;
            float targetArchScale = 0.6f;
            float targetImplScale = 0.4f;
            ApplyScale(arch, targetArchScale / currenScale);
            ApplyScale(impl, targetImplScale / currenScale);

            return;
            void ApplyScale(GameObject obj, float factor)
            {
                Vector3 oldScale = obj.transform.localScale;
                Vector3 newScale = new (oldScale.x, oldScale.y, oldScale.z * factor);
                float diff = oldScale.z - newScale.z;
                diff = diff < 0 ? diff : -diff;
                Vector3 newPosition = obj.transform.position + new Vector3(0, 0, diff) * 1.5f;
                obj.NodeOperator().ResizeTo(newScale, newPosition, 0, reparentChildren: false);
            }
        }

        /// <summary>
        /// Replaces the keyword "initial" in the root node with the selected city name.
        /// Currently, the <see cref="Node.ID"/> and the <see cref="GameObject.name"> are not updated.
        /// This would be necessary to load the initial <see cref="SEEReflexionCity"/> twice.
        /// </summary>
        /// <param name="city">The reflexion city.</param>
        private void Rename(SEEReflexionCity city)
        {
            string cityName = gameObject.name;
            foreach (Node node in GatherRoots())
            {
                GameNodeEditor.ChangeName(node, node.SourceName.Replace("initial", gameObject.name));
            }
            return;
            List<Node> GatherRoots()
            {
                ReflexionGraph graph = city.ReflexionGraph;
                List<Node> roots = graph.GetRoots();
                roots.Add(graph.GetNode(graph.ArchitectureRoot.ID));
                roots.Add(graph.GetNode(graph.ImplementationRoot.ID));
                return roots;
            }
        }
        #endregion
    }
}