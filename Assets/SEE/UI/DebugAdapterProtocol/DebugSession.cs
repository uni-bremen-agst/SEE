using SEE.Controls;
using SEE.UI;
using SEE.UI.DebugAdapterProtocol.DebugAdapter;
using SEE.UI.Window;
using SEE.UI.Window.ConsoleWindow;
using SEE.Utils;
using UnityEngine;

public class DebugSession : PlatformDependentComponent
{
    public DebugAdapter debugAdapter;

    private ConsoleWindow consoleWindow;

    // Start is called before the first frame update
    void Start()
    {
        if (debugAdapter == null)
        {
            Debug.LogError("Debug adapter not set.");
            Destroyer.Destroy(this);
            return;
        }
        consoleWindow = Canvas.AddComponent<ConsoleWindow>();
        consoleWindow.AddMessage("Hello");
        consoleWindow.AddMessage("World");
        WindowSpace manager = WindowSpaceManager.ManagerInstance[WindowSpaceManager.LocalPlayer];
        manager.AddWindow(consoleWindow);
        manager.ActiveWindow = consoleWindow;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    protected override void StartDesktop()
    {
    }
}
