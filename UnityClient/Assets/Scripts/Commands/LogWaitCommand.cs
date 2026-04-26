using System;
using System.Collections;
using UnityEngine;

public class LogWaitCommand : IVisualCommand {
    private string _logMessage;
    private float _waitTime;

    public LogWaitCommand(string logMessage, float waitTime = 0.5f) {
        _logMessage = logMessage;
        _waitTime = waitTime;
    }

    public IEnumerator Execute() {
        if (!string.IsNullOrEmpty(_logMessage)) {
            Debug.Log($"<color=cyan>[VisualQueue]</color> {_logMessage}");
        }
        
        if (_waitTime > 0) {
            yield return new WaitForSeconds(_waitTime);
        } else {
            yield return null;
        }
    }
}