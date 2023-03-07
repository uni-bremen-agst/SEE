using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Autohand.Demo{
    public class Spinner : MonoBehaviour{
        public Vector3 rotationSpeed;

        void FixedUpdate()
        {
            transform.Rotate(rotationSpeed * Time.fixedDeltaTime/2f);
        }

        void Update()
        {
            transform.Rotate(rotationSpeed * Time.deltaTime/2f);
        }
    }
}
