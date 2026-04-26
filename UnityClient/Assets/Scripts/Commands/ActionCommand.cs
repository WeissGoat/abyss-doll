using System;
using System.Collections;

public class ActionCommand : IVisualCommand {
    private Action _action;

    public ActionCommand(Action action) {
        _action = action;
    }

    public IEnumerator Execute() {
        _action?.Invoke();
        yield break;
    }
}