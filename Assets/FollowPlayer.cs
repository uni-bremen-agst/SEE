using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    GameObject player = null;

    // Start is called before the first frame update
    void Start()
    {
        transform.localScale = new Vector3(0.64f,0.48f,-1);
    }

    // Update is called once per frame
    void Update()
    {
        if (player == null)
        {
            //Debug.Log("Player searching, player: " + player);
            player = GameObject.Find("/Local Player 1/DesktopPlayer");
        }
        else {
            //Debug.Log("Player found, player:     " + player);
            transform.position = player.transform.position;
            transform.rotation = player.transform.rotation;
        }
        
    }
}
