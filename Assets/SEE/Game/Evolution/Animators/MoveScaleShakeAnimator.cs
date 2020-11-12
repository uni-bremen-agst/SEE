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

using SEE.Layout;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

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
        private Color NewNodeBeamColor = new Color(0, 1, 0.15f, 1);

        /// <summary>
        /// Color of beams for changed nodes
        /// </summary>
        private Color ChangedNodeBeamColor = new Color(0, 1, 0.5f, 1);

        /// <summary>
        /// Moves, scales, and then finally shakes (if <paramref name="wasModified"/>) the animated game object.
        /// At the end of the animation, the method <paramref name="callbackName"/> will be called for the
        /// game object <paramref name="callBackTarget"/> with <paramref name="gameObject"/> as 
        /// parameter if <paramref name="callBackTarget"/> is not null. If <paramref name="callBackTarget"/>
        /// equals null, no callback happens.
        /// </summary>
        /// <param name="gameObject">game object to be animated</param>
        /// <param name="layout">the node transformation to be applied</param>
        /// <param name="wasModified">whether the node attached to <paramref name="gameObject"/> was modified w.r.t. to the previous graph</param>
        /// <param name="callBackTarget">an optional game object that should receive the callback</param>
        /// <param name="callbackName">the method name of the callback</param>
        protected override void AnimateToInternalWithCallback
                  (GameObject gameObject,
                   ILayoutNode layout,
                   bool wasModified,
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

            //Debug.LogFormat("animating {0} from pos={1} scale={2} to pos={3} scale={4}\n",
            //    gameObject.name,
            //    gameObject.transform.position, gameObject.transform.localScale,
            //    position, localScale);

            if (gameObject.transform.localScale != localScale)
            {
                // Scale the object.
                if (mustCallBack)
                {
                    iTween.ScaleTo(gameObject, iTween.Hash(
                        "scale", localScale,
                        "time", MaxAnimationTime,
                        "oncompletetarget", callBackTarget,
                        "oncomplete", callbackName,
                        "oncompleteparams", gameObject
                    ));
                    mustCallBack = false;
                }
                else
                {
                    iTween.ScaleTo(gameObject, iTween.Hash(
                         "scale", localScale,
                         "time", MaxAnimationTime
                    ));
                }
            }

            if (gameObject.transform.position != position)
            {
                // Move the object.
                if (mustCallBack)
                {
                    iTween.MoveTo(gameObject, iTween.Hash(
                        "position", position,
                        "time", MaxAnimationTime,
                        "oncompletetarget", callBackTarget,
                        "oncomplete", callbackName,
                        "oncompleteparams", gameObject
                    ));
                    mustCallBack = false;
                }
                else
                {
                    iTween.MoveTo(gameObject, iTween.Hash("position", position, "time", MaxAnimationTime));
                }
            }

            // Shake the object if it was modified.
            if (wasModified)
            {
                // Changes the modified object's color to blue while animating
                gameObject.GetComponent<Renderer>().material.color = Color.blue;
                CreatePowerBeam(position, ChangedNodeBeamColor);

                if (mustCallBack)
                {
                    iTween.ShakeRotation(gameObject, iTween.Hash(
                         "amount", new Vector3(0, 10, 0),
                         "time", MaxAnimationTime / 2,
                         "delay", MaxAnimationTime / 2,
                         "oncompletetarget", callBackTarget,
                         "oncomplete", callbackName,
                         "oncompleteparams", gameObject
                    ));
                    mustCallBack = false;
                }
                else
                {
                    iTween.ShakeRotation(gameObject, iTween.Hash(
                         "amount", new Vector3(0, 10, 0),
                         "time", MaxAnimationTime / 2,
                         "delay", MaxAnimationTime / 2
                    ));
                }
            }
            if (mustCallBack)
            {
                callback?.Invoke(gameObject);
            }
        }

        /// <summary>
        /// Adds power beams above gameObjects that have been changed
        /// <param name="position">Position of the parent gameObject</param>
        /// <param name="beamColor">Color of the power beam to create</param>
        /// </summary>
        private void CreatePowerBeam(Vector3 position, Color beamColor)
        {
            // Generate power beam above updated objects
            GameObject powerBeam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            powerBeam.tag = "PowerBeam";
            powerBeam.transform.localScale = new Vector3(0.02f, 0f, 0.02f);
            powerBeam.transform.position = new Vector3(position.x, position.y, position.z);
            // Change power beam material color
            powerBeam.GetComponent<Renderer>().material.color = beamColor;
            // Set power beam material to emissive
            powerBeam.GetComponent<Renderer>().material.EnableKeyword("_EMISSION");
            Color emissionColor = beamColor * 7f;
            powerBeam.GetComponent<Renderer>().material.SetColor("_EmissionColor", emissionColor);
            // Remove power beam shadow
            powerBeam.GetComponent<Renderer>().receiveShadows = false;
            powerBeam.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            BeamAnimator.GetInstace().AddPowerBeam(powerBeam);
            powerBeam.AddComponent<BeamAnimatorExecuter>();
        }

        /// <summary>
        /// Removes power beams
        /// </summary>
        public static void DeletePowerBeams()
        {
            BeamAnimator.GetInstace().ClearPowerBeams();
        }

        class BeamAnimator
        {

            /// <summary>
            /// Singleton instance
            /// </summary>
            private static BeamAnimator singleton = null;

            /// <summary>
            /// Get the singleton instance
            /// </summary>
            public static BeamAnimator GetInstace()
            {
                if (singleton == null)
                {
                    singleton = new BeamAnimator();
                }
                return singleton;
            }

            /// <summary>
            /// Power beams, get animated while appearing
            /// </summary>
            private List<GameObject> powerBeams = new List<GameObject>();

            /// <summary>
            /// Power beams that have been removed, get aniamted while disappearing
            /// </summary>
            private GameObject[] removedBeams = new GameObject[0];

            public void Update()
            {
                // Animate new power beams
                foreach (GameObject beam in powerBeams)
                {
                    if (beam.transform.localScale.y < 3f)
                    {
                        beam.transform.localScale = new Vector3(beam.transform.localScale.x, beam.transform.localScale.y + 0.0025f, beam.transform.localScale.z);
                        beam.transform.position = new Vector3(beam.transform.position.x, beam.transform.position.y + 0.0025f, beam.transform.position.z);
                    }
                    else
                    {
                        powerBeams.Remove(beam);
                    }
                }
                // Animate deleted power beams
                foreach (GameObject deleted in removedBeams)
                {
                    if (deleted != null)
                    {
                        if (deleted.transform.localScale.y > 0f)
                        {
                            deleted.transform.localScale = new Vector3(deleted.transform.localScale.x, deleted.transform.localScale.y - 0.0025f, deleted.transform.localScale.z);
                            deleted.transform.position = new Vector3(deleted.transform.position.x, deleted.transform.position.y - 0.0025f, deleted.transform.position.z);
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
                removedBeams = GameObject.FindGameObjectsWithTag("PowerBeam");
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

        class BeamAnimatorExecuter : MonoBehaviour
        {

            private void Awake()
            {
                BeamAnimator.GetInstace();
            }

            private void Update()
            {
                BeamAnimator.GetInstace().Update();
            }
        }
    }
}
    