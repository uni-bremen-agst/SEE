using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Autohand{
    [HelpURL("https://app.gitbook.com/s/5zKO0EvOjzUDeT2aiFk3/auto-hand/teleportation")]
    public class Teleporter : MonoBehaviour{
        [Header("Teleport")]
        [Tooltip("The object to teleport")]
        public GameObject teleportObject;
        [Tooltip("Can be left empty - Used for if there is a container that should be teleported in addition to the main teleport object")]
        public Transform[] additionalTeleports;

        [Header("Aim Settings")]
        [Tooltip("The Object to Shoot the Beam From")]
        public Transform aimer;
        [Tooltip("Layers You Can Teleport On")]
        public LayerMask layer;
        [Tooltip("The Maximum Slope You Can Teleport On")]
        public float maxSurfaceAngle = 45;
        [Min(0)]
        public float distanceMultiplyer = 1;
        [Min(0)]
        public float curveStrength = 1;
        [Tooltip("Use Worldspace Must be True")]
        public LineRenderer line;
        [Tooltip("Maximum Length of The Teleport Line")]
        public int lineSegments = 50;
    
        [Header("Line Settings")]
        public Gradient canTeleportColor = new Gradient(){ colorKeys = new GradientColorKey[] { new GradientColorKey(){ color = Color.green, time = 0 } } };
        public Gradient cantTeleportColor = new Gradient(){ colorKeys = new GradientColorKey[] { new GradientColorKey(){ color = Color.red, time = 0 } } };

        [Tooltip("This gameobject will match the position of the teleport point when aiming")]
        public GameObject indicator;

        [Header("Unity Events")]
        public UnityEvent OnStartTeleport;
        public UnityEvent OnStopTeleport;
        public UnityEvent OnTeleport;

        Vector3[] lineArr;
        bool aiming;
        bool hitting;
        RaycastHit aimHit;
        HandTeleportGuard[] teleportGuards;
        AutoHandPlayer playerBody;

        private void Start() {
            playerBody = FindObjectOfType<AutoHandPlayer>();
            if (playerBody != null && playerBody.transform.gameObject == teleportObject)
                teleportObject = null;

            lineArr = new Vector3[lineSegments];
            teleportGuards = FindObjectsOfType<HandTeleportGuard>();
        }

        void Update(){
            if(aiming)
                CalculateTeleport();
            else
                line.positionCount = 0;

            DrawIndicator();
        }

        void CalculateTeleport() {
            line.colorGradient = cantTeleportColor;
            var lineList = new List<Vector3>();
            int i;
            hitting = false;
            for(i = 0; i < lineSegments; i++) {
                var time = i/60f;
                lineArr[i] = aimer.transform.position;
                lineArr[i] += transform.forward*time*distanceMultiplyer*15;
                lineArr[i].y += curveStrength * (time - Mathf.Pow(9.8f*0.5f*time, 2));
                lineList.Add(lineArr[i]);
                if(i != 0) {
                    if(Physics.Raycast(lineArr[i-1], lineArr[i]-lineArr[i-1], out aimHit, Vector3.Distance(lineArr[i], lineArr[i-1]), ~Hand.GetHandsLayerMask(), QueryTriggerInteraction.Ignore)) {
                        //Makes sure the angle isnt too steep
                        if(Vector3.Angle(aimHit.normal, Vector3.up) <= maxSurfaceAngle && layer == (layer | (1 << aimHit.collider.gameObject.layer))) {
                            line.colorGradient = canTeleportColor;
                            lineList.Add(aimHit.point);
                            hitting = true;
                            break;
                        }
                        break;
                    }
                }
            }
            line.positionCount = i;
            line.SetPositions(lineArr);
            
        }

        void DrawIndicator(){
            if(indicator != null){
                if(hitting){
                    indicator.gameObject.SetActive(true);
                    indicator.transform.position = aimHit.point;
                    indicator.transform.up = aimHit.normal;
                }
                else
                    indicator.gameObject.SetActive(false);
            }
        }

        public void StartTeleport(){
            aiming = true;
            OnStartTeleport?.Invoke();
        }

        public void CancelTeleport(){
            line.positionCount = 0;
            hitting = false;
            aiming = false;
            OnStopTeleport?.Invoke();
        }

        public void Teleport(){
            Queue<Vector3> fromPos = new Queue<Vector3>();
            foreach(var guard in teleportGuards) {
                if(guard.gameObject.activeInHierarchy)
                    fromPos.Enqueue(guard.transform.position);
            }

            if(hitting) {
                if (teleportObject != null){
                    var diff = aimHit.point - teleportObject.transform.position;
                    teleportObject.transform.position = aimHit.point;
                    foreach (var teleport in additionalTeleports){
                        teleport.position += diff;
                    }
                }
                playerBody?.SetPosition(aimHit.point);

               OnTeleport?.Invoke();


                foreach(var guard in teleportGuards) {
                    if(guard.gameObject.activeInHierarchy) {
                        guard.TeleportProtection(fromPos.Dequeue(), guard.transform.position);
                    }
                }
            }
            
            CancelTeleport();
        }
    }
}
