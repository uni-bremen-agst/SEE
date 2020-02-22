using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Net
{

    public class Player : MonoBehaviour
    {
        Transform cameraTransform;

        void Start()
        {
            if (GetComponent<ViewContainer>().IsOwner())
            {
                cameraTransform = Camera.main.transform ?? throw new System.ArgumentNullException("Main camera must not be null!");
                for (int i = 0; i < transform.childCount; i++)
                {
                    transform.GetChild(i).gameObject.SetActive(false);
                }
            }
            else
            {
                Destroy(this);
            }
        }

        void Update()
        {
            transform.position = cameraTransform.position;
            transform.rotation = cameraTransform.rotation;
        }
    }

}
