using System.Collections;
using UnityEngine;

namespace Assets.SEE.Game.Drawable
{
    public static class GameMoveRotator
    {
        public static GameObject selectedObj;

        public static bool isActive = false;

        public static int step = 0;

        public static Vector3 oldObjectPosition;

        public static Vector3[] oldLinePositions;

        public static Vector3 newObjectPosition;

        public static Vector3[] newLinePositions;

        public static Vector3 firstPoint;

        public static void SetSelectedLine(GameObject obj)
        {
            selectedObj = obj;
            oldObjectPosition = obj.transform.position;
            LineRenderer selectedRenderer = selectedObj.GetComponent<LineRenderer>();
            oldLinePositions = new Vector3[selectedRenderer.positionCount];
            selectedRenderer.GetPositions(oldLinePositions);

            //FIXME löschen
            newLinePositions = oldLinePositions;
        }

        public static void MoveObject(GameObject line, Vector3 position)
        {
            line.transform.position = position;
        }

        public static void RotateLine(GameObject line, Vector3[] linePositions)
        {
            LineRenderer render = line.GetComponent<LineRenderer>();
            render.SetPositions(linePositions);
        }
    }
}