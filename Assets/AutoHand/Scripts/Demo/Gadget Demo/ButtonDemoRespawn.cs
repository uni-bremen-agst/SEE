using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Autohand.Demo{
    public class ButtonDemoRespawn : MonoBehaviour{
        public Transform root;

        List<Transform> respawns = new List<Transform>();

        List<Vector3> startPos;
        List<Quaternion> startRot;


        void Start(){

            startPos = new List<Vector3>();
            startRot = new List<Quaternion>();

            for (int i = 0; i < root.childCount; i++){
                respawns.Add(root.GetChild(i));
                startPos.Add(root.GetChild(i).transform.position);
                startRot.Add(root.GetChild(i).transform.rotation);
                for (int j = 0; j < root.GetChild(i).childCount; j++){
                    respawns.Add(root.GetChild(i).GetChild(j));
                    startPos.Add(root.GetChild(i).GetChild(j).transform.position);
                    startRot.Add(root.GetChild(i).GetChild(j).transform.rotation);
                }
            }
        }

        public void Respawn() {
            for(int i = 0; i < respawns.Count; i++) {
                try {
                    if (respawns[i].CanGetComponent(out Rigidbody body)){
                        body.velocity = Vector3.zero;
                        body.angularVelocity = Vector3.zero;
                        body.ResetInertiaTensor();
                    }
                    respawns[i].transform.position = startPos[i];
                    respawns[i].transform.rotation = startRot[i];
                }
                catch { }
            }
        }

        public void ReloadScene() {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
}
}