using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEditor;
using System.Linq;

namespace CScape
{
    [ExecuteInEditMode]
    public class BuildingReplacer : MonoBehaviour
    {

        public GameObject rootHolder;
        public bool execute = false;
        public bool guessRotation = false;
        public DistrictStyle districtStyle;


        void Update()
        {
            CityRandomizer cityRandomizer = gameObject.GetComponent<CityRandomizer>();

            if (execute)
            {
                foreach (Transform go in rootHolder.transform.Cast<Transform>().Reverse())
                {
                    GuessWorldRotation(go.gameObject);
                    // we have to reset reference object rotation to be able to calculate real bounding box, here we are storing a real rotation
                    Quaternion oldTransform = go.rotation;

                    //reseting rotation
                    go.eulerAngles = new Vector3(go.eulerAngles.x, 0, go.eulerAngles.z);

                    //calculate bounding box and apporximate building size in CScape building units
                    Vector3 center = go.gameObject.GetComponent<Renderer>().bounds.center;
                    Vector3 bounds = go.gameObject.GetComponent<Renderer>().bounds.extents;
                    Vector3 CScapeCoordinates = new Vector3(center.x - bounds.x, center.y - bounds.y, center.z - bounds.z);
                    int depth = Mathf.FloorToInt((bounds.z * 2f) / 3f);
                    int width = Mathf.FloorToInt((bounds.x * 2f) / 3f);
                    int height = Mathf.FloorToInt((bounds.y * 2f) / 3f);




                    //take random building objects from a CityRandomizer array of buildings ad choose only those that can fit reference buildings
                    GameObject prefabToInstantiate = null;
                    int iterations = districtStyle.prefabs.Length;
                    bool validate = false;
                    int prefabChoice = Random.Range(0, districtStyle.prefabs.Length);

                    while (validate == false)
                    {
                        prefabToInstantiate = districtStyle.prefabs[prefabChoice];
  //                      BuildingModifier buildingToInstantiate = prefabToInstantiate.GetComponent<BuildingModifier>();
                        if (prefabToInstantiate.GetComponent<BuildingModifier>().prefabDepth <= depth && prefabToInstantiate.GetComponent<BuildingModifier>().prefabDepth <= depth)
                        {
                            validate = true;
                        }
                        else prefabChoice++;

                        if (iterations > districtStyle.prefabs.Length)
                        {
                            prefabToInstantiate = districtStyle.prefabs[0];
                            validate = true;
                        }

                        iterations++;
                    }

                    //Generate building
                    GameObject cloneH = Instantiate(prefabToInstantiate, CScapeCoordinates, transform.rotation) as GameObject;
                    //parent to reference building
                    cloneH.transform.parent = go.transform;
                    //restore original object rotation
                    go.rotation = oldTransform;
                    //unparent from reference building, and parent to Cscape city
                    cloneH.transform.parent = cityRandomizer.buildings.transform;
                    BuildingModifier buildingModifier = cloneH.GetComponent(typeof(BuildingModifier)) as BuildingModifier;
                    buildingModifier.buildingDepth = depth;
                    buildingModifier.buildingWidth = width;
                    buildingModifier.floorNumber = height;

                    buildingModifier.AwakeCity();
                    buildingModifier.UpdateCity();

                }


                execute = false;
            }
        }

        float GuessWorldRotation(GameObject go)
        {
            float rotation = 0;
            Mesh mesh = go.GetComponent<MeshFilter>().mesh;
            Debug.Log(mesh.triangles.Length + "triangles" + mesh.vertices.Length);
            for (int i = 0; i < mesh.triangles.Length; i = i + 3)
            {
               // Debug.Log(i);
                //if (mesh.normals[i].y != 0)
                //{
                //    continue;
                //}
                int chooseVerticalEdge = 0;
                int chooseEdge = 0;

                if (CompareFloats(go.transform.TransformPoint(mesh.vertices[mesh.triangles[i]]).x , go.transform.TransformPoint(mesh.vertices[mesh.triangles[i + 1]]).x) && CompareFloats(go.transform.TransformPoint(mesh.vertices[mesh.triangles[i]]).z, go.transform.TransformPoint(mesh.vertices[mesh.triangles[i + 1]]).z))
                {
                    chooseVerticalEdge = 0;
                }
                else if (CompareFloats(go.transform.TransformPoint(mesh.vertices[mesh.triangles[i + 1]]).x, go.transform.TransformPoint(mesh.vertices[mesh.triangles[i + 2]]).x) && CompareFloats(go.transform.TransformPoint(mesh.vertices[mesh.triangles[i + 1]]).z, go.transform.TransformPoint(mesh.vertices[mesh.triangles[i + 2]]).z))
                {
                    chooseVerticalEdge = 1;
                }
                else if (CompareFloats(go.transform.TransformPoint(mesh.vertices[mesh.triangles[i + 2]]).x, go.transform.TransformPoint(mesh.vertices[mesh.triangles[i]]).x) && CompareFloats(go.transform.TransformPoint(mesh.vertices[mesh.triangles[i + 2]]).z, go.transform.TransformPoint(mesh.vertices[mesh.triangles[i]]).z))
                {
                    chooseVerticalEdge = 2;
                }

                else continue;

                Vector2 a = new Vector2 (go.transform.TransformPoint(mesh.vertices[mesh.triangles[i]]).x, go.transform.TransformPoint(mesh.vertices[mesh.triangles[i]]).z);
                Vector2 b = new Vector2(go.transform.TransformPoint(mesh.vertices[mesh.triangles[i+1]]).x, go.transform.TransformPoint(mesh.vertices[mesh.triangles[i+1]]).z);
                Vector2 c = new Vector2(go.transform.TransformPoint(mesh.vertices[mesh.triangles[i + 2]]).x, go.transform.TransformPoint(mesh.vertices[mesh.triangles[i + 2]]).z);
                Vector2 zero = new Vector2(1, 0);

                if (CompareFloats(go.transform.TransformPoint(mesh.vertices[mesh.triangles[i]]).y, go.transform.TransformPoint(mesh.vertices[mesh.triangles[i + 1]]).y))
                    rotation = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
                else if (CompareFloats(go.transform.TransformPoint(mesh.vertices[mesh.triangles[i + 1]]).y, go.transform.TransformPoint(mesh.vertices[mesh.triangles[i + 2]]).y))
                    rotation = Mathf.Atan2(c.y - b.y, c.x - b.x) * Mathf.Rad2Deg;
                else if (CompareFloats(go.transform.TransformPoint(mesh.vertices[mesh.triangles[i + 2]]).y, go.transform.TransformPoint(mesh.vertices[mesh.triangles[i]]).y))
                    rotation = Mathf.Atan2(a.y - c.y, a.x - c.x) * Mathf.Rad2Deg;



                else continue;
                Debug.Log(rotation);



            }

            //   Debug.Log(rotation);
            return rotation;
        }
        bool CompareFloats(float vec1, float vec2)
        {
            float treshold = 0.05f;
            bool result = false;
            if ((vec1 + treshold) >= vec2 && vec2 >= (vec1 - treshold))       
                result = true;
            return result;
        }
    }
}
