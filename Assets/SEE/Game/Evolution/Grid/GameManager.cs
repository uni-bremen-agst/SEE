using Hypertonic.GridPlacement;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Hypertonic.GridPlacement.Enums;

namespace SEE.Game.Evolution
{
    [System.Serializable]
    internal class GridObjectSpawnData
    {
        public GameObject GridObject;
        public Vector2Int GridCellIndex;
        public ObjectAlignment ObjectAlignment;
        public Vector3 ObjectRotation;
    }

    public class GameManager : MonoBehaviour
    {
        [SerializeField]
        private GridSettings _gridSettings;

        [SerializeField]
        private List<GridObjectSpawnData> _gridObjectSpawnDatas;

        void Start()
        {
            CreateGridManager();
            _ = AddObjectsToGrid();
        }

        private void CreateGridManager()
        {
            GameObject gridManagerObject = new GameObject("Grid Manager");
            GridManager gridManager = gridManagerObject.AddComponent<GridManager>();
            gridManager.Setup(_gridSettings);
        }

        private async Task AddObjectsToGrid()
        {
            if(_gridObjectSpawnDatas == null || _gridObjectSpawnDatas.Count == 0)
            {
                Debug.LogError("The object spawn data is null or empty.");
                return;
            }

            for(int i = 0; i < _gridObjectSpawnDatas.Count; i++)
            {
                GridObjectSpawnData gridObjectPositionData = _gridObjectSpawnDatas[i];
                GameObject gridObject = Instantiate(gridObjectPositionData.GridObject);
                gridObject.transform.localRotation = Quaternion.Euler(gridObjectPositionData.ObjectRotation);

                await GridManagerAccessor.GridManager.AddObjectToGrid(gridObject, gridObjectPositionData.GridCellIndex, gridObjectPositionData.ObjectAlignment);
            }
        }
    }
}