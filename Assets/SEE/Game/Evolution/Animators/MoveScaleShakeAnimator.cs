//Copyright 2020 Florian Garbade

//Permission is hereby granted, free of charge, to any person obtaining a
//copy of this software and associated documentation files (the "Software"),
//to deal in the Software without restriction, including without limitation
//the rights to use, copy, modify, merge, publish, distribute, sublicense,
//and/or sell copies of the Software, and to permit persons to whom the Software
//is furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
//PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
//LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
//USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using SEE.Layout;
using SEE.Utils;
using UnityEngine;
using SEE.Game.Charts;

namespace SEE.Game.Evolution
{
    /// <summary>
    /// Animates the position and scale of a given GameObject over the full <see cref="AbstractAnimator.MaxAnimationTime"/>.
    /// If a node was modified, the GameObject will be shaken to indicate its modification.
    /// </summary>
    public class MoveScaleShakeAnimator : AbstractAnimator
    {

        /// <summary>
        /// Color of beams for newly added nodes
        /// </summary>
        private Color NewNodeBeamColor = AdditionalBeamDetails.newBeamColor;

        /// <summary>
        /// Color of beams for changed nodes
        /// </summary>
        public Color ChangedNodeBeamColor = AdditionalBeamDetails.changedBeamColor;

        /// <summary>
        /// Dimensions of power beams
        /// </summary>
        private Vector3 NodeBeamDimensions = AdditionalBeamDetails.powerBeamDimensions;

        /// <summary>
        /// Moves, scales, and then finally shakes (if <paramref name="difference"/>) the animated game object.
        /// At the end of the animation, the method <paramref name="callbackName"/> will be called for the
        /// game object <paramref name="callBackTarget"/> with <paramref name="gameObject"/> as 
        /// parameter if <paramref name="callBackTarget"/> is not null. If <paramref name="callBackTarget"/>
        /// equals null, no callback happens.
        /// </summary>
        /// <param name="gameObject">game object to be animated</param>
        /// <param name="layout">the node transformation to be applied</param>
        /// <param name="difference">whether the node attached to <paramref name="gameObject"/> was modified w.r.t. to the previous graph</param>
        /// <param name="callBackTarget">an optional game object that should receive the callback</param>
        /// <param name="callbackName">the method name of the callback</param>
        protected override void AnimateToInternalWithCallback
                  (GameObject gameObject,
                   ILayoutNode layout,
                   Difference difference,
                   GameObject callBackTarget,
                   string callbackName,
                   Action<object> callback)
        {
            bool mustCallBack = callBackTarget != null;

            Vector3 position = layout.CenterPosition;

            // layout.scale is in world space, while the animation by iTween
            // is in local space. Our game objects may be nested in other game objects,
            // hence, the two spaces may be different.
            // We may need to transform nodeTransform.scale from world space to local space.
            Vector3 localScale = gameObject.transform.parent == null ?
                                     layout.LocalScale
                                   : gameObject.transform.parent.InverseTransformVector(layout.LocalScale);

            if (gameObject.transform.localScale != localScale)
            {
                // Scale the object.
                if (mustCallBack)
                {
                    Tweens.Scale(gameObject, localScale, MaxAnimationTime);
                    callback?.Invoke(gameObject);
                    mustCallBack = false;
                }
                else
                {
                    Tweens.Scale(gameObject, localScale, MaxAnimationTime);
                }
            }

            if (gameObject.transform.position != position)
            {
                // Move the object.
                if (mustCallBack)
                {
                    Tweens.Move(gameObject, position, MaxAnimationTime);
                    callback?.Invoke(gameObject);
                    mustCallBack = false;
                }
                else
                {
                    Tweens.Move(gameObject, position, MaxAnimationTime);
                }
            }

            // Shake the object if it was modified.
            if (difference == Difference.Changed)
            {
                NodeChangesBuffer.GetSingleton().changedNodeIDs.Add(gameObject.name);
                // Changes the modified object's color to blue while animating
                gameObject.GetComponent<Renderer>().material.color = Color.blue;
                // Refetch values, necessary because this gets loaded before other scripts
                NewNodeBeamColor = AdditionalBeamDetails.newBeamColor;
                ChangedNodeBeamColor = AdditionalBeamDetails.changedBeamColor;
                NodeBeamDimensions = AdditionalBeamDetails.powerBeamDimensions;
                // Create a new power beam
                BeamAnimator.GetInstance().CreatePowerBeam(position, ChangedNodeBeamColor, NodeBeamDimensions);

                if (mustCallBack)
                {
                    Tweens.ShakeRotate(gameObject, MaxAnimationTime / 2, new Vector3(0, 10, 0));
                    callback?.Invoke(gameObject);
                    mustCallBack = false;
                }
                else
                {
                    Tweens.ShakeRotate(gameObject, MaxAnimationTime / 2, new Vector3(0, 10, 0));
                }
            }
            if (mustCallBack)
            {
                callback?.Invoke(gameObject);
            }
        }

        /// <summary>
        /// Removes power beams
        /// </summary>
        public static void DeletePowerBeams()
        {
            BeamAnimator.GetInstance().ClearPowerBeams();
        }

        /// <summary>
        /// Singleton that stores all newly created / deleted power beams
        /// </summary>
        public class BeamAnimator
        {

            /// <summary>
            /// Adds power beam above gameObject that has been changed
            /// <param name="position">Position of the parent gameObject</param>
            /// <param name="beamColor">Color of the power beam to create</param>
            /// </summary>
            public void CreatePowerBeam(Vector3 position, Color beamColor, Vector3 NodeBeamDimensions)
            {
                // Generate power beam above updated objects
                GameObject powerBeam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                GameObject.DestroyImmediate(powerBeam.GetComponent<Collider>());
                powerBeam.tag = DataModel.Tags.PowerBeam;
                Renderer powerBeamRenderer = powerBeam.GetComponent<Renderer>();
                powerBeam.transform.localScale = new Vector3(NodeBeamDimensions.x, 0, NodeBeamDimensions.z);
                BeamAnimator.GetInstance().BeamHeight = NodeBeamDimensions.y;
                powerBeam.transform.position = position;
                // Change power beam material color
                powerBeamRenderer.material.color = beamColor;
                // Set power beam material to emissive
                powerBeamRenderer.material.EnableKeyword("_EMISSION");
                Color emissionColor = beamColor * 7f;
                powerBeamRenderer.material.SetColor("_EmissionColor", emissionColor);
                // Remove power beam shadow
                powerBeamRenderer.receiveShadows = false;
                powerBeamRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                BeamAnimator.GetInstance().AddPowerBeam(powerBeam);
                powerBeam.AddComponent<BeamAnimatorExecuter>();
            }

            /// <summary>
            /// Singleton instance
            /// </summary>
            private static BeamAnimator Instance = null;

            /// <summary>
            /// Get the singleton instance
            /// </summary>
            public static BeamAnimator GetInstance()
            {
                if (Instance == null)
                {
                    Instance = new BeamAnimator();
                }
                return Instance;
            }

            /// <summary>
            /// Power beam height getter/setter
            /// </summary>
            public float BeamHeight
            {
                get; set;
            }

            /// <summary>
            /// Power beams, get animated while appearing
            /// </summary>
            private readonly List<GameObject> powerBeams = new List<GameObject>();

            /// <summary>
            /// Power beams that have been removed, get animated while disappearing
            /// </summary>
            private readonly GameObject[] removedBeams = new GameObject[0];

            /// <summary>
            /// Deleted power beams, updated whenever Update is called
            /// </summary>
            private readonly List<GameObject> deletedBeams = new List<GameObject>();

            /// <summary>
            /// Beam appearing magic constant
            /// </summary>
            private const float appearingMagicNumber = 0.0025f;

            /// <summary>
            /// Beam disappearing magic number
            /// </summary>
            private const float disappearingMagicNumber = 0.0005f;

            /// <summary>
            /// Animation update function
            /// </summary>
            public void Update()
            {
                deletedBeams.Clear();

                if (BeamHeight <= 0)
                {
                    BeamHeight = 3f;
                }
                // Animate new power beams
                foreach (GameObject beam in powerBeams)
                {
                    if (beam.transform.localScale.y < BeamHeight)
                    {
                        beam.transform.localScale = new Vector3(beam.transform.localScale.x, beam.transform.localScale.y + appearingMagicNumber, beam.transform.localScale.z);
                        beam.transform.position = new Vector3(beam.transform.position.x, beam.transform.position.y + appearingMagicNumber, beam.transform.position.z);
                    }
                    else
                    {
                        deletedBeams.Add(beam);
                    }
                }
                foreach (GameObject beam in deletedBeams) {
                    powerBeams.Remove(beam);
                }
                // Animate deleted power beams
                foreach (GameObject deleted in removedBeams)
                {
                    if (deleted != null)
                    {
                        if (deleted.transform.localScale.y > 0f)
                        {
                            deleted.transform.localScale = new Vector3(deleted.transform.localScale.x, deleted.transform.localScale.y - disappearingMagicNumber, deleted.transform.localScale.z);
                            deleted.transform.position = new Vector3(deleted.transform.position.x, deleted.transform.position.y - disappearingMagicNumber, deleted.transform.position.z);
                        }
                        else
                        {
                            GameObject.Destroy(deleted);
                        }
                    }
                }
            }

            /// <summary>
            /// Clear power beams
            /// </summary>
            public void ClearPowerBeams()
            {
                removedBeams = GameObject.FindGameObjectsWithTag(DataModel.Tags.PowerBeam);
                powerBeams.Clear();
            }

            /// <summary>
            /// Add a new power beam
            /// <param name="beam">Power beam to add</param>
            /// </summary>
            public void AddPowerBeam(GameObject beam)
            {
                powerBeams.Add(beam);
            }
        }

        /// <summary>
        /// Attached to power beam GameObjects to call update from BeamAnimator class
        /// </summary>
        class BeamAnimatorExecuter : MonoBehaviour
        {

            private void Update()
            {
                BeamAnimator.GetInstance().Update();
            }
        }
    }
}
    
