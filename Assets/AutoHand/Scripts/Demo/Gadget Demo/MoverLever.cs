using UnityEngine;
using Autohand;

namespace Autohand.Demo{
    public class MoverLever : PhysicsGadgetHingeAngleReader{
        public Transform move;
        public Vector3 axis;
        public float speed = 1;
    
        void Update(){
            if(Mathf.Abs(GetValue()) > 0.1f)
                move.position = Vector3.MoveTowards(move.position, move.position-axis, Time.deltaTime*speed*(GetValue()));
        }
    }
}
