using System;
using System.Collections.Generic;

public sealed class NarrativeEventContext {
    public string EventType;
    public string Context;
    public readonly Dictionary<string, string> Values = new Dictionary<string, string>(StringComparer.Ordinal);

    public static NarrativeEventContext Create(string eventType, string context = null) {
        return new NarrativeEventContext {
            EventType = eventType,
            Context = context ?? string.Empty
        };
    }

    public NarrativeEventContext WithValue(string key, string value) {
        if (!string.IsNullOrEmpty(key)) {
            Values[key] = value ?? string.Empty;
        }

        return this;
    }

    public bool TryGetValue(string key, out string value) {
        if (string.IsNullOrEmpty(key)) {
            value = string.Empty;
            return false;
        }

        return Values.TryGetValue(key, out value);
    }
}

public sealed class NarrativeRequest {
    public string TriggerID;
    public string YarnNode;
    public string BlockingMode;
    public string IfBusyPolicy;
    public string Context;
    public int Priority;
    public NarrativeTriggerConfig TriggerConfig;
    public NarrativeEventContext EventContext;
}

public sealed class NarrativeSeenRecord {
    public int Count;
    public string LastContext;
}

public sealed class NarrativeStateStore {
    private readonly Dictionary<string, bool> _flags = new Dictionary<string, bool>(StringComparer.Ordinal);
    private readonly Dictionary<string, NarrativeSeenRecord> _seenTriggers = new Dictionary<string, NarrativeSeenRecord>(StringComparer.Ordinal);
    private readonly Dictionary<string, NarrativeSeenRecord> _seenNodes = new Dictionary<string, NarrativeSeenRecord>(StringComparer.Ordinal);
    private readonly Queue<NarrativeRequest> _pendingQueue = new Queue<NarrativeRequest>();

    public bool IsInputLocked { get; private set; }
    public string LockedContext { get; private set; } = string.Empty;

    public int PendingCount {
        get { return _pendingQueue.Count; }
    }

    public bool GetFlag(string flagID) {
        return !string.IsNullOrEmpty(flagID) && _flags.TryGetValue(flagID, out bool value) && value;
    }

    public void SetFlag(string flagID, bool value = true) {
        if (!string.IsNullOrEmpty(flagID)) {
            _flags[flagID] = value;
        }
    }

    public bool HasSeenTrigger(string triggerID) {
        return !string.IsNullOrEmpty(triggerID) && _seenTriggers.ContainsKey(triggerID);
    }

    public bool HasSeenNode(string nodeID) {
        return !string.IsNullOrEmpty(nodeID) && _seenNodes.ContainsKey(nodeID);
    }

    public int GetTriggerSeenCount(string triggerID) {
        return _seenTriggers.TryGetValue(triggerID ?? string.Empty, out NarrativeSeenRecord record) ? record.Count : 0;
    }

    public int GetNodeSeenCount(string nodeID) {
        return _seenNodes.TryGetValue(nodeID ?? string.Empty, out NarrativeSeenRecord record) ? record.Count : 0;
    }

    public void MarkTriggerSeen(string triggerID, string context) {
        MarkSeen(_seenTriggers, triggerID, context);
    }

    public void MarkNodeSeen(string nodeID, string context) {
        MarkSeen(_seenNodes, nodeID, context);
    }

    public void LockInput(string context) {
        IsInputLocked = true;
        LockedContext = context ?? string.Empty;
    }

    public void UnlockInput() {
        IsInputLocked = false;
        LockedContext = string.Empty;
    }

    public void Enqueue(NarrativeRequest request) {
        if (request != null) {
            _pendingQueue.Enqueue(request);
        }
    }

    public bool HasPendingTrigger(string triggerID) {
        if (string.IsNullOrEmpty(triggerID)) {
            return false;
        }

        foreach (NarrativeRequest request in _pendingQueue) {
            if (request != null && string.Equals(request.TriggerID, triggerID, StringComparison.Ordinal)) {
                return true;
            }
        }

        return false;
    }

    public bool TryDequeue(out NarrativeRequest request) {
        if (_pendingQueue.Count == 0) {
            request = null;
            return false;
        }

        request = _pendingQueue.Dequeue();
        return true;
    }

    private static void MarkSeen(Dictionary<string, NarrativeSeenRecord> records, string id, string context) {
        if (string.IsNullOrEmpty(id)) {
            return;
        }

        if (!records.TryGetValue(id, out NarrativeSeenRecord record)) {
            record = new NarrativeSeenRecord();
            records[id] = record;
        }

        record.Count++;
        record.LastContext = context ?? string.Empty;
    }
}

public sealed class NarrativeTriggerService {
    private readonly NarrativeConfigDatabase _database;
    private readonly NarrativeStateStore _state;

    public NarrativeTriggerService(NarrativeConfigDatabase database, NarrativeStateStore state) {
        _database = database ?? new NarrativeConfigDatabase();
        _state = state ?? new NarrativeStateStore();
    }

    public List<NarrativeRequest> Evaluate(NarrativeEventContext eventContext) {
        List<NarrativeRequest> requests = new List<NarrativeRequest>();
        if (eventContext == null || string.IsNullOrEmpty(eventContext.EventType)) {
            return requests;
        }

        foreach (NarrativeTriggerConfig trigger in _database.Triggers.Values) {
            if (!CanTrigger(trigger, eventContext)) {
                continue;
            }

            requests.Add(new NarrativeRequest {
                TriggerID = trigger.TriggerID,
                YarnNode = trigger.YarnNode,
                BlockingMode = trigger.BlockingMode,
                IfBusyPolicy = trigger.IfBusyPolicy,
                Context = trigger.Context,
                Priority = trigger.Priority,
                TriggerConfig = trigger,
                EventContext = eventContext
            });
        }

        requests.Sort((left, right) => right.Priority.CompareTo(left.Priority));
        return requests;
    }

    private bool CanTrigger(NarrativeTriggerConfig trigger, NarrativeEventContext eventContext) {
        if (trigger == null || !string.Equals(trigger.EventType, eventContext.EventType, StringComparison.Ordinal)) {
            return false;
        }

        if (!string.IsNullOrEmpty(eventContext.Context)
            && !string.IsNullOrEmpty(trigger.Context)
            && !string.Equals(trigger.Context, eventContext.Context, StringComparison.Ordinal)) {
            return false;
        }

        if (trigger.Once && _state.HasSeenTrigger(trigger.TriggerID)) {
            return false;
        }

        if (trigger.Chance <= 0f) {
            return false;
        }

        foreach (string flag in SafeList(trigger.RequiredFlags)) {
            if (!_state.GetFlag(flag)) {
                return false;
            }
        }

        foreach (string flag in SafeList(trigger.ForbiddenFlags)) {
            if (_state.GetFlag(flag)) {
                return false;
            }
        }

        foreach (NarrativeConditionConfig condition in SafeList(trigger.Conditions)) {
            if (!EvaluateCondition(condition, eventContext)) {
                return false;
            }
        }

        return _database.Nodes.ContainsKey(trigger.YarnNode);
    }

    private bool EvaluateCondition(NarrativeConditionConfig condition, NarrativeEventContext eventContext) {
        if (condition == null || string.IsNullOrEmpty(condition.Type)) {
            return true;
        }

        switch (condition.Type) {
            case "FlagEquals":
                return _state.GetFlag(condition.Key) == ParseBool(condition.Value);
            case "FlagNotSet":
                return !_state.GetFlag(condition.Key);
            case "ActionEquals":
                return eventContext.TryGetValue(condition.Key, out string action)
                    && string.Equals(action, condition.Value, StringComparison.Ordinal);
            case "LayerEquals":
                return eventContext.TryGetValue(condition.Key ?? "layer", out string layer)
                    && string.Equals(layer, condition.Value, StringComparison.Ordinal);
            default:
                return false;
        }
    }

    private static bool ParseBool(string value) {
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "1", StringComparison.Ordinal);
    }

    private static List<T> SafeList<T>(List<T> items) {
        return items ?? new List<T>();
    }
}

public enum NarrativeScheduleResult {
    Started,
    Queued,
    Dropped,
    Replaced,
    Rejected
}

public sealed class NarrativeScheduler {
    private readonly NarrativeStateStore _state;
    private NarrativeRequest _activeRequest;

    public NarrativeScheduler(NarrativeStateStore state) {
        _state = state ?? new NarrativeStateStore();
    }

    public bool IsBusy {
        get { return _activeRequest != null; }
    }

    public NarrativeRequest ActiveRequest {
        get { return _activeRequest; }
    }

    public NarrativeScheduleResult Submit(NarrativeRequest request) {
        if (request == null) {
            return NarrativeScheduleResult.Rejected;
        }

        if (_activeRequest == null) {
            Start(request);
            return NarrativeScheduleResult.Started;
        }

        switch (request.IfBusyPolicy) {
            case "drop":
                return NarrativeScheduleResult.Dropped;
            case "replace_lower_priority":
                if (request.Priority > _activeRequest.Priority) {
                    Start(request);
                    return NarrativeScheduleResult.Replaced;
                }

                return NarrativeScheduleResult.Dropped;
            case "queue_once":
                if (string.Equals(request.TriggerID, _activeRequest.TriggerID, StringComparison.Ordinal)
                    || _state.HasPendingTrigger(request.TriggerID)) {
                    return NarrativeScheduleResult.Dropped;
                }

                _state.Enqueue(request);
                return NarrativeScheduleResult.Queued;
            case "defer":
                _state.Enqueue(request);
                return NarrativeScheduleResult.Queued;
            default:
                return NarrativeScheduleResult.Rejected;
        }
    }

    public bool CompleteActive() {
        _activeRequest = null;
        _state.UnlockInput();
        if (_state.TryDequeue(out NarrativeRequest next)) {
            Start(next);
            return true;
        }

        return false;
    }

    private void Start(NarrativeRequest request) {
        _activeRequest = request;
        if (request.BlockingMode == "modal" || request.BlockingMode == "modal_light") {
            _state.LockInput(request.Context);
        }
    }
}

public sealed class NarrativePlaybackService {
    private readonly NarrativeConfigDatabase _database;
    private readonly INarrativeScriptRuntime _runtime;
    private readonly NarrativeStateStore _state;
    private readonly NarrativeScheduler _scheduler;
    private readonly NarrativeCommandBridge _commandBridge;

    public NarrativePlaybackService(
        NarrativeConfigDatabase database,
        INarrativeScriptRuntime runtime,
        NarrativeStateStore state,
        NarrativeScheduler scheduler,
        NarrativeCommandBridge commandBridge = null) {
        _database = database ?? new NarrativeConfigDatabase();
        _runtime = runtime ?? new P3YarnLikeNarrativeRuntime();
        _state = state ?? new NarrativeStateStore();
        _scheduler = scheduler;
        _commandBridge = commandBridge;
    }

    public NarrativePlaybackResult PlayActive() {
        return Play(_scheduler?.ActiveRequest);
    }

    public NarrativePlaybackResult Play(NarrativeRequest request) {
        NarrativePlaybackResult result = new NarrativePlaybackResult {
            RuntimeName = _runtime.RuntimeName,
            NodeName = request?.YarnNode ?? string.Empty
        };

        if (request == null) {
            result.Errors.Add("Narrative request is null.");
            return result;
        }

        if (!_database.Nodes.TryGetValue(request.YarnNode, out NarrativeNodeConfig node)) {
            result.Errors.Add($"Narrative node config not found: {request.YarnNode}");
            return result;
        }

        string scriptSource = _database.GetScriptSource(node.ScriptFile);
        List<string> commandErrors = new List<string>();
        Action<NarrativeCommandOutput> onCommand = null;
        if (_commandBridge != null) {
            NarrativeCommandExecutionContext executionContext = NarrativeCommandExecutionContext.FromRequest(request);
            onCommand = command => {
                NarrativeCommandBridgeResult commandResult = _commandBridge.Execute(command, executionContext);
                if (!commandResult.Success) {
                    commandErrors.Add(commandResult.Diagnostic);
                    throw new NarrativeCommandBridgeException(commandResult.Diagnostic);
                }
            };
        }

        try {
            result = _runtime.PlayNode(scriptSource, request.YarnNode, onCommand);
        } catch (NarrativeCommandBridgeException ex) {
            result.Errors.Add(ex.Message);
            return result;
        }

        foreach (string error in commandErrors) {
            result.Errors.Add(error);
        }

        if (!result.Success) {
            return result;
        }

        _state.MarkTriggerSeen(request.TriggerID, request.Context);
        _state.MarkNodeSeen(request.YarnNode, request.Context);
        foreach (string flag in SafeList(request.TriggerConfig?.SetFlagsOnComplete)) {
            _state.SetFlag(flag);
        }

        _scheduler?.CompleteActive();
        return result;
    }

    private static List<T> SafeList<T>(List<T> items) {
        return items ?? new List<T>();
    }
}
