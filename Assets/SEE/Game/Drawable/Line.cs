using SEE.Game;
using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;

namespace Assets.SEE.Game.Drawable
{
    public class Line
    {
        public GameObject gameObject;

        public string id;

        public Vector3 position;

        public Vector3[] rendererPositions;

        public Vector3 parentEulerAngles;

        public Color color;

        public int orderInLayer;

        public float thickness;

        public static Line GetLine(GameObject lineGameObject)
        { 
            Line line = null;
            if (lineGameObject != null && lineGameObject.CompareTag(Tags.Line)) {
                line = new();
                line.gameObject = lineGameObject;
                line.id = lineGameObject.name;
                line.position = lineGameObject.transform.position;
                LineRenderer renderer = lineGameObject.GetComponent<LineRenderer>();
                line.rendererPositions = new Vector3[renderer.positionCount];
                renderer.GetPositions(line.rendererPositions);
                line.parentEulerAngles = lineGameObject.transform.parent.localEulerAngles;
                line.color = renderer.material.color;
                line.orderInLayer = renderer.sortingOrder;
                line.thickness = renderer.startWidth;
            }
            return  line;
        }
    }
}