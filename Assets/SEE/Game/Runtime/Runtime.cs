﻿using System;
using System.Collections.Generic;
using Sirenix.Serialization;
using SEE.DataModel.DG;
using SEE.DataModel.Runtime;
using SEE.GO;
using SEE.Utils;
using UnityEngine;
using Sirenix.OdinInspector;

namespace SEE.Game.Runtime
{
    /// <summary>
    /// Is responsible for initializing some parts of call tree and managing the dynamic visualizations.
    /// </summary>
    public class Runtime : SerializedMonoBehaviour
    {
        /// <summary>
        /// Time in seconds for building animation loop.
        /// </summary>
        private const float loopTime = 1.0f;

        /// <summary>
        /// The first function call to be visualized. The call to main will not be
        /// visualized.
        /// </summary>
        private const int firstFunctionCall = 1;

        /// <summary>
        /// The call tree to be visualized.
        /// </summary>
        [NonSerialized, OdinSerialize]
        public CallTree CallTree;

        /// <summary>
        /// States whether the visualization is currently running or not.
        /// </summary>
        private bool running = false;

        /// <summary>
        /// The currently visualized function call.
        /// </summary>
        private int currentFunctionCall = firstFunctionCall;

        /// <summary>
        /// The current time in the loop.
        /// </summary>
        private float currentTimeInLoop = 0.0f;

        /// <summary>
        /// The list of the simulators.
        /// </summary>
        [NonSerialized, OdinSerialize]
        private readonly List<FunctionCallSimulator> functionCalls = new List<FunctionCallSimulator>();

        /// <summary>
        /// Initializes non serializable part of call tree and general initialization.
        /// </summary>
        private void Start()
        {
            if (CallTree == null)
            {
                throw new Exception("'callTree' is null!");
            }

            // The buildings representing function calls are all tagged with these tags.
            // Currently, these are solely Tags.Building.
            string[] tags = new string[] { Tags.Node };

            // Generates non serializable parts of call tree.
            CallTree.GenerateTree();

            // Finds GameObjects, that represent functions and maps them onto call tree.
            Dictionary<string, GameObject> gameObjects = new Dictionary<string, GameObject>(FindObjectsOfType<GameObject>().Length);
            for (int i = 0; i < tags.Length; i++)
            {
                GameObject[] gameObjectsWithTag = GameObject.FindGameObjectsWithTag(tags[i]);
                for (int j = 0; j < gameObjectsWithTag.Length; j++)
                {
                    NodeRef nodeRef = gameObjectsWithTag[j].GetComponent<NodeRef>();
                    if (nodeRef != null && nodeRef.Value != null)
                    {
                        // We retrieve the linkname because that contains the encoding of the signature
                        // of a method specifying its parameters and return type.
                        // FIXME: This makes assumptions about the information content of a linkname
                        // that may not necessarily hold.
                        if (nodeRef.Value.TryGetString(Node.LinknameAttribute, out string linkname))
                        {
                            gameObjectsWithTag[j].GetComponentInChildren<MeshRenderer>().material.color = Color.black;
                            if (gameObjects.ContainsKey(linkname))
                            {
                                Debug.LogWarning($"Contains '{nodeRef.Value.ID}' already!");
                            }
                            else
                            {
                                gameObjects[linkname] = gameObjectsWithTag[j];
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"Node with ID {nodeRef.Value.ID} has no linkname.\n");
                        }
                    }
                }
            }
            CallTree.MapGameObjects(gameObjects);
        }

        /// <summary>
        /// Checks for user input and updates simulation accordingly.
        ///
        /// Key Return => toggle simulation
        /// Key +      => one step forward
        /// Key -      => one step backward
        /// </summary>
        private void Update()
        {
            bool leftTrackpadPressed = false;//Input.GetButtonDown("LeftVRTrackpadPress");
            float leftTrackpadHorizontalMovement = 0.0f;//Input.GetAxis("LeftVRTrackpadHorizontalMovement");
            bool leftTriggerPressed = false;

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

            functionCalls.ForEach(e => e.UpdateSimulation(currentTimeInLoop / loopTime));
            currentTimeInLoop = (currentTimeInLoop + Time.deltaTime) % loopTime;
        }

        /// <summary>
        /// Moves visualization to the next function call. If current function call is
        /// already last function call, nothing happens.
        /// </summary>
        private void Forwards()
        {
            if (currentFunctionCall == CallTree.FunctionCallCount - 1)
            {
                return;
            }
            currentFunctionCall = Mathf.Min(currentFunctionCall + 1, CallTree.FunctionCallCount - 1);
            ClearFunctionCalls();
            GenerateFunctionCalls();
        }

        /// <summary>
        /// Moves visualization to the previous function call. If current function call
        /// is already first function call, nothing happens.
        /// </summary>
        private void Backwards()
        {
            if (currentFunctionCall == firstFunctionCall)
            {
                return;
            }
            currentFunctionCall = Mathf.Max(currentFunctionCall - 1, firstFunctionCall);
            ClearFunctionCalls();
            GenerateFunctionCalls();
        }

        /// <summary>
        /// Generates function call visualizations for <see cref="currentFunctionCall"/>.
        /// </summary>
        private void GenerateFunctionCalls()
        {
            CallTreeFunctionCall call = CallTree.GetFunctionCall(currentFunctionCall);
            while (call != null)
            {
                if (call.Predecessor != null)
                {
                    GameObject go = new GameObject("FunctionCall: " + call.GetAttributeForCategory(CallTree.LinkageName), typeof(FunctionCallSimulator))
                    {
                        tag = Tags.FunctionCall
                    };
                    FunctionCallSimulator sim = go.GetComponent<FunctionCallSimulator>();
                    sim.Initialize(call.Predecessor.Node, call.Node, currentTimeInLoop);
                    functionCalls.Add(sim);
                }
                call = call.Predecessor;
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
            functionCalls.ForEach(e => Destroyer.Destroy(e.gameObject));
            functionCalls.Clear();
        }
    }

}
