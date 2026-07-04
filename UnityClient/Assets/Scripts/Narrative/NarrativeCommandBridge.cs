using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class NarrativeCommandExecutionContext {
    public string Context;
    public string SourceNode;
    public string TriggerID;
    public NarrativeEventContext EventContext;

    public static NarrativeCommandExecutionContext FromRequest(NarrativeRequest request) {
        return new NarrativeCommandExecutionContext {
            Context = request?.Context ?? string.Empty,
            SourceNode = request?.YarnNode ?? string.Empty,
            TriggerID = request?.TriggerID ?? string.Empty,
            EventContext = request?.EventContext
        };
    }
}

public sealed class NarrativeCommandBridgeResult {
    public string CommandName;
    public bool Success;
    public string Diagnostic;

    public static NarrativeCommandBridgeResult Passed(string commandName, string diagnostic = null) {
        return new NarrativeCommandBridgeResult {
            CommandName = commandName ?? string.Empty,
            Success = true,
            Diagnostic = diagnostic ?? string.Empty
        };
    }

    public static NarrativeCommandBridgeResult Failed(string commandName, string diagnostic) {
        return new NarrativeCommandBridgeResult {
            CommandName = commandName ?? string.Empty,
            Success = false,
            Diagnostic = diagnostic ?? "Narrative command failed."
        };
    }
}

public sealed class NarrativeCommandBridgeException : Exception {
    public NarrativeCommandBridgeException(string message) : base(message) {
    }
}

public interface INarrativeCommandSink {
    void PlayVisual(NarrativeVisualCommandRequest request);
    void ShowCharacter(NarrativeCharacterCommandRequest request);
    void ShowPrologueAction(NarrativePrologueActionCommandRequest request);
    void ShowStatusCard(NarrativeStatusCardCommandRequest request);
    void UnlockUi(NarrativeUnlockUiCommandRequest request);
    void RequestLayerConfirm(NarrativeLayerConfirmCommandRequest request);
}

public sealed class NarrativeVisualCommandRequest {
    public string CommandName;
    public string Context;
    public string SourceNode;
    public string VisualID;
    public string FallbackVisualID;
    public string Slot;
    public string Transition;
    public bool UseCover;
    public List<string> RawArguments = new List<string>();
}

public sealed class NarrativeCharacterCommandRequest {
    public string Context;
    public string SourceNode;
    public string CharacterID;
    public string Expression;
    public string Pose;
    public string Slot;
    public string VisualID;
    public string FallbackVisualID;
    public List<string> RawArguments = new List<string>();
}

public sealed class NarrativePrologueActionCommandRequest {
    public string Context;
    public string SourceNode;
    public string ActionID;
    public string LabelKey;
    public string VisualID;
    public string FallbackVisualID;
    public List<string> RawArguments = new List<string>();
}

public sealed class NarrativeStatusCardCommandRequest {
    public string Context;
    public string SourceNode;
    public string CardID;
    public string StateID;
    public List<string> RawArguments = new List<string>();
}

public sealed class NarrativeUnlockUiCommandRequest {
    public string Context;
    public string SourceNode;
    public string GateID;
    public string Mode;
    public List<string> RawArguments = new List<string>();
}

public sealed class NarrativeLayerConfirmCommandRequest {
    public string Context;
    public string SourceNode;
    public int LayerID;
    public bool FirstDiveOnly;
    public List<string> RawArguments = new List<string>();
}

public sealed class NarrativeCommandBridge {
    public const int T0FirstDiveLayerID = 1;

    private static readonly HashSet<string> ProtectedCommandNames = new HashSet<string>(StringComparer.Ordinal) {
        "add_money",
        "remove_money",
        "set_money",
        "add_item",
        "remove_item",
        "grant_item",
        "set_hp",
        "set_san",
        "spawn_monster",
        "grant_drop",
        "resolve_combat",
        "start_combat",
        "start_order",
        "charge_rent",
        "apply_maintenance",
        "start_dungeon_layer"
    };

    private readonly NarrativeConfigDatabase _database;
    private readonly NarrativeStateStore _state;
    private readonly INarrativeCommandSink _sink;

    public NarrativeCommandBridge(
        NarrativeConfigDatabase database,
        NarrativeStateStore state,
        INarrativeCommandSink sink) {
        _database = database ?? new NarrativeConfigDatabase();
        _state = state ?? new NarrativeStateStore();
        _sink = sink;
    }

    public NarrativeCommandBridgeResult Execute(
        NarrativeCommandOutput command,
        NarrativeCommandExecutionContext context) {
        string commandName = command?.CommandName ?? string.Empty;
        string commandContext = context?.Context ?? string.Empty;
        List<string> args = command?.Arguments ?? new List<string>();

        if (string.IsNullOrEmpty(commandName)) {
            return Reject(commandName, "Narrative command has no name.");
        }

        if (ProtectedCommandNames.Contains(commandName)) {
            return Reject(commandName, $"Narrative command [{commandName}] is protected gameplay mutation.");
        }

        if (!_database.Commands.TryGetValue(commandName, out NarrativeCommandConfig config)) {
            return Reject(commandName, $"Narrative command [{commandName}] is not whitelisted.");
        }

        if (args.Count < config.MinArgs || args.Count > config.MaxArgs) {
            return Reject(
                commandName,
                $"Narrative command [{commandName}] arg count [{args.Count}] is outside [{config.MinArgs}, {config.MaxArgs}].");
        }

        if (config.AllowedContexts == null
            || config.AllowedContexts.Count == 0
            || !config.AllowedContexts.Contains(commandContext)) {
            return Reject(commandName, $"Narrative command [{commandName}] is not allowed in context [{commandContext}].");
        }

        switch (commandName) {
            case "play_visual":
                return ExecutePlayVisual(args, context);
            case "show_character":
                return ExecuteShowCharacter(args, context);
            case "show_prologue_action":
                return ExecuteShowPrologueAction(args, context);
            case "show_status_card":
                return ExecuteShowStatusCard(args, context);
            case "unlock_ui":
                return ExecuteUnlockUi(args, context);
            case "open_layer_confirm":
                return ExecuteOpenLayerConfirm(args, context);
            case "set_flag":
                return ExecuteSetFlag(args);
            default:
                return Reject(commandName, $"Narrative command [{commandName}] has no bridge handler.");
        }
    }

    private NarrativeCommandBridgeResult ExecutePlayVisual(List<string> args, NarrativeCommandExecutionContext context) {
        _sink?.PlayVisual(new NarrativeVisualCommandRequest {
            CommandName = "play_visual",
            Context = context?.Context ?? string.Empty,
            SourceNode = context?.SourceNode ?? string.Empty,
            VisualID = args[0],
            FallbackVisualID = GetArg(args, 1),
            Slot = GetArg(args, 2),
            Transition = GetArg(args, 3),
            UseCover = true,
            RawArguments = CopyArgs(args)
        });
        return Accept("play_visual");
    }

    private NarrativeCommandBridgeResult ExecuteShowCharacter(List<string> args, NarrativeCommandExecutionContext context) {
        _sink?.ShowCharacter(new NarrativeCharacterCommandRequest {
            Context = context?.Context ?? string.Empty,
            SourceNode = context?.SourceNode ?? string.Empty,
            CharacterID = args[0],
            Expression = GetArg(args, 1),
            Pose = GetArg(args, 2),
            Slot = GetArg(args, 3),
            VisualID = GetArg(args, 4),
            FallbackVisualID = GetArg(args, 5),
            RawArguments = CopyArgs(args)
        });
        return Accept("show_character");
    }

    private NarrativeCommandBridgeResult ExecuteShowPrologueAction(List<string> args, NarrativeCommandExecutionContext context) {
        _sink?.ShowPrologueAction(new NarrativePrologueActionCommandRequest {
            Context = context?.Context ?? string.Empty,
            SourceNode = context?.SourceNode ?? string.Empty,
            ActionID = args[0],
            LabelKey = args[1],
            VisualID = GetArg(args, 2),
            FallbackVisualID = GetArg(args, 3),
            RawArguments = CopyArgs(args)
        });
        return Accept("show_prologue_action");
    }

    private NarrativeCommandBridgeResult ExecuteShowStatusCard(List<string> args, NarrativeCommandExecutionContext context) {
        _sink?.ShowStatusCard(new NarrativeStatusCardCommandRequest {
            Context = context?.Context ?? string.Empty,
            SourceNode = context?.SourceNode ?? string.Empty,
            CardID = args[0],
            StateID = GetArg(args, 1),
            RawArguments = CopyArgs(args)
        });
        return Accept("show_status_card");
    }

    private NarrativeCommandBridgeResult ExecuteUnlockUi(List<string> args, NarrativeCommandExecutionContext context) {
        _sink?.UnlockUi(new NarrativeUnlockUiCommandRequest {
            Context = context?.Context ?? string.Empty,
            SourceNode = context?.SourceNode ?? string.Empty,
            GateID = args[0],
            Mode = GetArg(args, 1),
            RawArguments = CopyArgs(args)
        });
        return Accept("unlock_ui");
    }

    private NarrativeCommandBridgeResult ExecuteOpenLayerConfirm(List<string> args, NarrativeCommandExecutionContext context) {
        if (!int.TryParse(args[0], out int layerID)) {
            return Reject("open_layer_confirm", $"open_layer_confirm layer [{args[0]}] is not a number.");
        }

        if (layerID != T0FirstDiveLayerID) {
            return Reject("open_layer_confirm", $"open_layer_confirm only allows first dive layer [{T0FirstDiveLayerID}], got [{layerID}].");
        }

        _sink?.RequestLayerConfirm(new NarrativeLayerConfirmCommandRequest {
            Context = context?.Context ?? string.Empty,
            SourceNode = context?.SourceNode ?? string.Empty,
            LayerID = layerID,
            FirstDiveOnly = true,
            RawArguments = CopyArgs(args)
        });
        return Accept("open_layer_confirm");
    }

    private NarrativeCommandBridgeResult ExecuteSetFlag(List<string> args) {
        string flagID = args[0];
        if (!_database.Flags.ContainsKey(flagID)) {
            return Reject("set_flag", $"set_flag references missing narrative flag [{flagID}].");
        }

        _state.SetFlag(flagID);
        return Accept("set_flag", $"Narrative flag [{flagID}] set.");
    }

    private NarrativeCommandBridgeResult Accept(string commandName, string diagnostic = null) {
        string message = diagnostic ?? $"Narrative command [{commandName}] accepted.";
        Debug.Log($"[NarrativeCommandBridge] {message}");
        return NarrativeCommandBridgeResult.Passed(commandName, message);
    }

    private NarrativeCommandBridgeResult Reject(string commandName, string diagnostic) {
        Debug.LogError($"[NarrativeCommandBridge] {diagnostic}");
        return NarrativeCommandBridgeResult.Failed(commandName, diagnostic);
    }

    private static string GetArg(List<string> args, int index) {
        return args != null && index >= 0 && index < args.Count ? args[index] : string.Empty;
    }

    private static List<string> CopyArgs(List<string> args) {
        return args == null ? new List<string>() : new List<string>(args);
    }
}
