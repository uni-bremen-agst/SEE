using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Autohand.Demo{
public class TomatoSpawner : MonoBehaviour{
    public GameObject[] tomatoes;
    List<GameObject> copies;

    void Start(){
        copies = new List<GameObject>();
        foreach(var tomato in tomatoes) {
            var newTomato = Instantiate(tomato, tomato.transform.position, tomato.transform.rotation);
            newTomato.transform.position += new Vector3(0, 0.2f, 0);
            newTomato.SetActive(false);
            copies.Add(newTomato);
            
        }
    }

    public void SpawnTomato() {
        int i = Random.Range(0, copies.Count-1);
        Instantiate(copies[i], copies[i].transform.position, copies[i].transform.rotation).SetActive(true);
    }
}
}
