using SEE.Command;
using UnityEngine;

public class HistoryTest : MonoBehaviour
{
    void Start()
    {
        Random.InitState(13031995);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            new CreateBlockCommand(new Vector3(Random.Range(-10.0f, 10.0f), Random.Range(-10.0f, 10.0f), Random.Range(-10.0f, 10.0f))).Execute();
        }

        if (Input.GetKeyDown(KeyCode.U))
        {
            CommandHistory.Undo();
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            CommandHistory.Redo();
        }
    }
}
