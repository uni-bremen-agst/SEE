using System.Collections.Generic;
using SEE.Controls.Actions;
using SEE.Utils;

public class UpdateCityFieldAction : AbstractPlayerAction
{
    private object settingsField;
    private object oldValue;
    private object newValue;

    public override HashSet<string> GetChangedObjects()
    {
        return new HashSet<string>();
    }

    public override ActionStateType GetActionStateType()
    {
        // TODO: Is this needed?
        return ActionStateType.Hide;
    }

    public override ReversibleAction NewInstance()
    {
        return CreateReversibleAction();
    }
    
    public static ReversibleAction CreateReversibleAction()
    {
        return new UpdateCityFieldAction();
    }

    public override bool Update()
    {
        oldValue = settingsField;
        settingsField = newValue;
        return true;
    }

    public override void Undo()
    {
        settingsField = oldValue;
    }

    public override void Redo()
    {
        settingsField = newValue;
    }
}
