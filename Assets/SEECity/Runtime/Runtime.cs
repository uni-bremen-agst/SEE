using System.Collections.Generic;
using UnityEngine;
using SEE.DataModel;
using SEE.Layout;
using SEE.GO;

namespace SEE.Runtime
{

    /// <summary>
    /// Is responsible for initializing some parts of call tree and managing the dynamic visualizations.
    /// </summary>
    [System.Serializable]
    public class Runtime : MonoBehaviour
    {
        /// <summary>
        /// Time in seconds for building animation loop.
        /// </summary>
        private const float LOOP_TIME = 1.0f;

        /// <summary>
        /// The first function call to be visualized. The call to main will not be
        /// visualized.
        /// </summary>
        private const int FIRST_FUNCTION_CALL = 1;

        /// <summary>
        /// The call tree to be visualized.
        /// </summary>
        [SerializeField]
        public CallTree callTree;

        /// <summary>
        /// States whether the visualization is currently running or not.
        /// </summary>
        private bool running = false;

        /// <summary>
        /// The currently visualized function call.
        /// </summary>
        private int currentFunctionCall = FIRST_FUNCTION_CALL;

        /// <summary>
        /// The current time in the loop.
        /// </summary>
        private float currentTimeInLoop = 0.0f;

        /// <summary>
        /// The list of the simulators.
        /// </summary>
        private List<FunctionCallSimulator> functionCalls = new List<FunctionCallSimulator>();

        /// <summary>
        /// States whether of not the left trigger is currently pressed down.
        /// </summary>
        private bool isLeftTriggerDown = false;

        /// <summary>
        /// Initializes non serializable part of call tree and general initialization.
        /// </summary>
        void Awake()
        {
            if (callTree == null)
            {
                Debug.LogError("'callTree' was null!");
            }
            
            // The buildings representing function calls are all tagged with these tags.
            // Currently, these are solely Tags.Building.
            string[] tags = new string[] { Tags.Building };

            // Generates non serializable parts of call tree.
            callTree.GenerateTree();

            // Finds GameObjects, that represent functions and maps them onto call tree.
            List<KeyValuePair<string, GameObject>> gameObjects = new List<KeyValuePair<string, GameObject>>(FindObjectsOfType<GameObject>().Length);
            for (int i = 0; i < tags.Length; i++)
            {
                GameObject[] gameObjectsWithTag = GameObject.FindGameObjectsWithTag(tags[i]);
                for (int j = 0; j < gameObjectsWithTag.Length; j++)
                {
                    NodeRef nodeRef = gameObjectsWithTag[j].GetComponent<NodeRef>();
                    if (nodeRef != null && nodeRef.node != null)
                    {
                        gameObjectsWithTag[j].GetComponentInChildren<MeshRenderer>().material.color = Color.black;
                        if (!gameObjects.TrueForAll((p) => !p.Key.Equals(nodeRef.node.LinkName)))
                        {
                            Debug.LogWarning("Contains '" + nodeRef.node + "' already!");
                        }
                        gameObjects.Add(new KeyValuePair<string, GameObject>(nodeRef.node.LinkName, gameObjectsWithTag[j]));
                    }
                }
            }
            callTree.MapGameObjects(gameObjects);
        }

        /// <summary>
        /// Checks for user input and updates simulation accordingly.
        /// </summary>
        void Update()
        {
            bool leftTrackpadPressed = false;//Input.GetButtonDown("LeftVRTrackpadPress");
            float leftTrackpadHorizontalMovement = 0.0f;//Input.GetAxis("LeftVRTrackpadHorizontalMovement");

            //float leftTriggerValue = Input.GetAxis("LeftVRTrigger");
            bool leftTriggerPressed = false;//leftTriggerValue > 0.5f && !isLeftTriggerDown;
            isLeftTriggerDown = false;//leftTriggerValue > 0.5f;

            if (Input.GetKeyDown(KeyCode.Return) || leftTriggerPressed)
            {
                running = !running;
                if (!running)
                {
                    ClearFunctionCalls();
                }
                else
                {
                    GenerateFunctionCalls();
                }
            }

            if (running)
            {
                if (Input.GetKeyDown(KeyCode.Equals) || leftTrackpadPressed && leftTrackpadHorizontalMovement > 0.0f) // For some reason, KeyCode.Plus is actually KeyCode.Equals.
                {
                    Forwards();
                }
                if (Input.GetKeyDown(KeyCode.Minus) || leftTrackpadPressed && leftTrackpadHorizontalMovement < 0.0f)
                {
                    Backwards();
                }
            }

            functionCalls.ForEach(e => e.UpdateSimulation(currentTimeInLoop / LOOP_TIME));
            currentTimeInLoop = (currentTimeInLoop + Time.deltaTime) % LOOP_TIME;
        }

        /// <summary>
        /// Moves visualization to the next function call. If current function call is
        /// already last function call, nothing happens.
        /// </summary>
        private void Forwards()
        {
            if (currentFunctionCall == callTree.FunctionCallCount - 1)
            {
                return;
            }
            currentFunctionCall = Mathf.Min(currentFunctionCall + 1, callTree.FunctionCallCount - 1);
            ClearFunctionCalls();
            GenerateFunctionCalls();
        }

        /// <summary>
        /// Moves visualization to the previous function call. If current function call
        /// is already first function call, nothing happens.
        /// </summary>
        private void Backwards()
        {
            if (currentFunctionCall == FIRST_FUNCTION_CALL)
            {
                return;
            }
            currentFunctionCall = Mathf.Max(currentFunctionCall - 1, FIRST_FUNCTION_CALL);
            ClearFunctionCalls();
            GenerateFunctionCalls();
        }

        /// <summary>
        /// Generates function call visualizations for <see cref="currentFunctionCall"/>.
        /// </summary>
        private void GenerateFunctionCalls()
        {
            CallTreeFunctionCall call = callTree.GetFunctionCall(currentFunctionCall);
            while (call != null)
            {
                if (call.predecessor != null)
                {
                    GameObject go = new GameObject("FunctionCall: " + call.GetAttributeForCategory(CallTree.LINKAGE_NAME), typeof(FunctionCallSimulator))
                    {
                        tag = Tags.FunctionCall
                    };
                    FunctionCallSimulator sim = go.GetComponent<FunctionCallSimulator>();
                    sim.Initialize(call.predecessor.node, call.node, currentTimeInLoop);
                    functionCalls.Add(sim);
                }
                call = call.predecessor;
            }
        }

        /// <summary>
        /// Removes all visualized function calls.
        /// </summary>
        private void ClearFunctionCalls()
        {
            for (int i = 0; i < functionCalls.Count; i++)
            {
                functionCalls[i].Shutdown();
            }
            functionCalls.ForEach(e => e.Shutdown());
            functionCalls.ForEach(e => Destroy(e.gameObject));
            functionCalls.Clear();
        }
    }

}
