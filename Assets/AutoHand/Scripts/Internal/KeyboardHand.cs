using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Autohand
{
    public class KeyboardHand : MonoBehaviour
    {
        public Hand hand;
        public float speed = 1;
        public float flySpeed = 1;
        public bool useLocal = true;

        void Update() {
            float yMove = 0;

            if(Input.GetKey(KeyCode.Space))
                yMove = 1;

            if(Input.GetKey(KeyCode.LeftShift))
                yMove = -1;

            if(Input.GetKey(KeyCode.E))
                transform.Rotate(new Vector3(speed * 90 * Time.deltaTime, 0, 0));

            if(Input.GetKey(KeyCode.Q))
                transform.Rotate(new Vector3(-speed * 90 * Time.deltaTime, 0, 0));


            if(useLocal) {
                Vector3 move = new Vector3(yMove * flySpeed, -Input.GetAxis("Horizontal") * speed, Input.GetAxis("Vertical") * speed);
                transform.position += transform.rotation * move * Time.deltaTime;
            }
            else {
                Vector3 move = new Vector3(Input.GetAxis("Horizontal") * speed, yMove * flySpeed, Input.GetAxis("Vertical") * speed);
                transform.position += move * Time.deltaTime;
            }
            if(Input.GetKeyDown(KeyCode.Mouse0))
                hand.Grab();

            if(Input.GetKeyUp(KeyCode.Mouse0))
                hand.Release();
        }
    }
}
