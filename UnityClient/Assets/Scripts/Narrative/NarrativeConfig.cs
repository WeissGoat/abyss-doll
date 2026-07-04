using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

[Serializable]
public sealed class NarrativeConfigDatabase {
    public readonly Dictionary<string, NarrativeNodeConfig> Nodes = new Dictionary<string, NarrativeNodeConfig>(StringComparer.Ordinal);
    public readonly Dictionary<string, NarrativeTriggerConfig> Triggers = new Dictionary<string, NarrativeTriggerConfig>(StringComparer.Ordinal);
    public readonly Dictionary<string, NarrativeSpeakerConfig> Speakers = new Dictionary<string, NarrativeSpeakerConfig>(StringComparer.Ordinal);
    public readonly Dictionary<string, NarrativeFlagConfig> Flags = new Dictionary<string, NarrativeFlagConfig>(StringComparer.Ordinal);
    public readonly Dictionary<string, NarrativeCommandConfig> Commands = new Dictionary<string, NarrativeCommandConfig>(StringComparer.Ordinal);
    public readonly Dictionary<string, string> ScriptSources = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public readonly Dictionary<string, HashSet<string>> ScriptNodes = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
    public readonly Dictionary<string, HashSet<string>> SourceTableLineKeys = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
    public string RootPath;

    public int NodeCount { get { return Nodes.Count; } }
    public int TriggerCount { get { return Triggers.Count; } }

    public static NarrativeConfigDatabase LoadFromDirectory(string rootPath) {
        NarrativeConfigDatabase database = new NarrativeConfigDatabase {
            RootPath = rootPath
        };

        if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath)) {
            return database;
        }

        LoadArray(Path.Combine(rootPath, "narrative_nodes.json"), database.Nodes, item => item.NodeID);
        LoadArray(Path.Combine(rootPath, "narrative_triggers.json"), database.Triggers, item => item.TriggerID);
        LoadArray(Path.Combine(rootPath, "narrative_speakers.json"), database.Speakers, item => item.SpeakerID);
        LoadArray(Path.Combine(rootPath, "narrative_flags.json"), database.Flags, item => item.FlagID);
        LoadArray(Path.Combine(rootPath, "narrative_commands.json"), database.Commands, item => item.CommandName);
        database.LoadScripts(rootPath);
        database.LoadSourceTables(rootPath);
        return database;
    }

    public bool HasYarnNode(string scriptFile, string nodeID) {
        if (string.IsNullOrEmpty(scriptFile) || string.IsNullOrEmpty(nodeID)) {
            return false;
        }

        return ScriptNodes.TryGetValue(NormalizeRelativePath(scriptFile), out HashSet<string> nodes)
            && nodes.Contains(nodeID);
    }

    public string GetScriptSource(string scriptFile) {
        if (string.IsNullOrEmpty(scriptFile)) {
            return string.Empty;
        }

        return ScriptSources.TryGetValue(NormalizeRelativePath(scriptFile), out string source) ? source : string.Empty;
    }

    public bool SourceTableHasLineKey(string sourceTable, string lineKey) {
        if (string.IsNullOrEmpty(sourceTable) || string.IsNullOrEmpty(lineKey)) {
            return false;
        }

        return SourceTableLineKeys.TryGetValue(NormalizeRelativePath(sourceTable), out HashSet<string> keys)
            && keys.Contains(lineKey);
    }

    private void LoadScripts(string rootPath) {
        string scriptsPath = Path.Combine(rootPath, "scripts");
        if (!Directory.Exists(scriptsPath)) {
            return;
        }

        foreach (string file in Directory.GetFiles(scriptsPath, "*.yarn", SearchOption.AllDirectories)) {
            string relativePath = NormalizeRelativePath(GetRelativePath(rootPath, file));
            string source = File.ReadAllText(file);
            ScriptSources[relativePath] = source;
            ScriptNodes[relativePath] = ParseYarnNodeTitles(source);
        }
    }

    private void LoadSourceTables(string rootPath) {
        string tablesPath = Path.Combine(rootPath, "source_tables");
        if (!Directory.Exists(tablesPath)) {
            return;
        }

        foreach (string file in Directory.GetFiles(tablesPath, "*.csv", SearchOption.AllDirectories)) {
            string relativePath = NormalizeRelativePath(GetRelativePath(rootPath, file));
            SourceTableLineKeys[relativePath] = ParseCsvLineKeys(file);
        }
    }

    private static void LoadArray<T>(string path, Dictionary<string, T> output, Func<T, string> keySelector) {
        if (!File.Exists(path)) {
            return;
        }

        List<T> items = JsonConvert.DeserializeObject<List<T>>(File.ReadAllText(path));
        if (items == null) {
            return;
        }

        foreach (T item in items) {
            if (item == null) {
                continue;
            }

            string key = keySelector(item);
            if (!string.IsNullOrEmpty(key) && !output.ContainsKey(key)) {
                output.Add(key, item);
            }
        }
    }

    private static HashSet<string> ParseYarnNodeTitles(string source) {
        HashSet<string> nodes = new HashSet<string>(StringComparer.Ordinal);
        if (string.IsNullOrEmpty(source)) {
            return nodes;
        }

        string[] lines = source.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        for (int i = 0; i < lines.Length; i++) {
            string trimmed = lines[i].Trim();
            if (trimmed.StartsWith("title:", StringComparison.OrdinalIgnoreCase)) {
                string title = trimmed.Substring("title:".Length).Trim();
                if (!string.IsNullOrEmpty(title)) {
                    nodes.Add(title);
                }
            }
        }

        return nodes;
    }

    private static HashSet<string> ParseCsvLineKeys(string path) {
        HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);
        string[] lines = File.ReadAllLines(path);
        if (lines.Length == 0) {
            return keys;
        }

        string[] headers = SplitCsvLine(lines[0]);
        int lineKeyIndex = Array.FindIndex(headers, header => string.Equals(header, "line_key", StringComparison.OrdinalIgnoreCase));
        if (lineKeyIndex < 0) {
            return keys;
        }

        for (int i = 1; i < lines.Length; i++) {
            string[] columns = SplitCsvLine(lines[i]);
            if (lineKeyIndex < columns.Length && !string.IsNullOrEmpty(columns[lineKeyIndex])) {
                keys.Add(columns[lineKeyIndex]);
            }
        }

        return keys;
    }

    private static string[] SplitCsvLine(string line) {
        List<string> columns = new List<string>();
        bool inQuotes = false;
        string current = string.Empty;

        for (int i = 0; i < line.Length; i++) {
            char c = line[i];
            if (c == '"') {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"') {
                    current += '"';
                    i++;
                    continue;
                }

                inQuotes = !inQuotes;
                continue;
            }

            if (c == ',' && !inQuotes) {
                columns.Add(current);
                current = string.Empty;
                continue;
            }

            current += c;
        }

        columns.Add(current);
        return columns.ToArray();
    }

    private static string NormalizeRelativePath(string path) {
        return string.IsNullOrEmpty(path) ? string.Empty : path.Replace('\\', '/').TrimStart('/');
    }

    private static string GetRelativePath(string rootPath, string path) {
        Uri rootUri = new Uri(AppendDirectorySeparatorChar(rootPath));
        Uri pathUri = new Uri(path);
        return Uri.UnescapeDataString(rootUri.MakeRelativeUri(pathUri).ToString());
    }

    private static string AppendDirectorySeparatorChar(string path) {
        if (string.IsNullOrEmpty(path)) {
            return string.Empty;
        }

        return path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
            ? path
            : path + Path.DirectorySeparatorChar;
    }
}

[Serializable]
public sealed class NarrativeNodeConfig {
    public string NodeID;
    public string ScriptFile;
    public string SourceTable;
    public string Context;
    public string ContentType;
    public string ImplementationState;
    public List<string> SpeakerIDs = new List<string>();
    public List<string> LineKeys = new List<string>();
    public List<string> CommandNames = new List<string>();
    public List<NarrativeVisualIntentConfig> VisualIntents = new List<NarrativeVisualIntentConfig>();
}

[Serializable]
public sealed class NarrativeVisualIntentConfig {
    public string VisualID;
    public string FallbackVisualID;
    public bool Required;
}

[Serializable]
public sealed class NarrativeTriggerConfig {
    public string TriggerID;
    public string EventType;
    public string YarnNode;
    public int Priority;
    public string BlockingMode;
    public string IfBusyPolicy;
    public string Context;
    public bool Once;
    public float Chance = 1f;
    public List<string> RequiredFlags = new List<string>();
    public List<string> ForbiddenFlags = new List<string>();
    public List<NarrativeConditionConfig> Conditions = new List<NarrativeConditionConfig>();
    public List<string> SetFlagsOnComplete = new List<string>();
}

[Serializable]
public sealed class NarrativeConditionConfig {
    public string Type;
    public string Key;
    public string Value;
}

[Serializable]
public sealed class NarrativeSpeakerConfig {
    public string SpeakerID;
    public string DisplayNameKey;
    public string DefaultPortraitVisualID;
    public string FallbackVisualID;
    public string TextStyle;
}

[Serializable]
public sealed class NarrativeFlagConfig {
    public string FlagID;
    public string Scope;
    public string Description;
}

[Serializable]
public sealed class NarrativeCommandConfig {
    public string CommandName;
    public int MinArgs;
    public int MaxArgs;
    public List<string> AllowedContexts = new List<string>();
    public string Description;
}

public static class NarrativeConfigValidator {
    private static readonly HashSet<string> AllowedEventTypes = new HashSet<string>(StringComparer.Ordinal) {
        "PrologueStarted",
        "NarrativeNodeCompleted",
        "PrologueActionClicked",
        "NarrativeFlagSet",
        "DayStarted",
        "DollInteractionResolved",
        "DungeonRunStarted",
        "DungeonNodeEntered",
        "CombatPhaseChanged"
    };

    private static readonly HashSet<string> AllowedBlockingModes = new HashSet<string>(StringComparer.Ordinal) {
        "modal",
        "modal_light",
        "bark",
        "background"
    };

    private static readonly HashSet<string> AllowedBusyPolicies = new HashSet<string>(StringComparer.Ordinal) {
        "defer",
        "drop",
        "queue_once",
        "replace_lower_priority"
    };

    private static readonly HashSet<string> ProtectedCommandNames = new HashSet<string>(StringComparer.Ordinal) {
        "add_money",
        "remove_money",
        "add_item",
        "remove_item",
        "set_hp",
        "set_san",
        "spawn_monster",
        "grant_drop",
        "resolve_combat",
        "start_order",
        "charge_rent",
        "apply_maintenance"
    };

    public static void Validate(NarrativeConfigDatabase database, ConfigValidationReport report) {
        if (database == null || string.IsNullOrEmpty(database.RootPath) || !Directory.Exists(database.RootPath)) {
            report.AddError("Narrative config domain is missing. Expected Configs/Narrative generated from 配置表(JSON)/Narrative.");
            return;
        }

        ValidateCommands(database, report);
        ValidateSpeakers(database, report);
        ValidateNodes(database, report);
        ValidateTriggers(database, report);
    }

    public static ConfigValidationReport ValidateStandalone(NarrativeConfigDatabase database) {
        ConfigValidationReport report = new ConfigValidationReport();
        Validate(database, report);
        return report;
    }

    private static void ValidateCommands(NarrativeConfigDatabase database, ConfigValidationReport report) {
        if (database.Commands.Count == 0) {
            report.AddError("Narrative commands are empty.");
            return;
        }

        foreach (var kvp in database.Commands) {
            NarrativeCommandConfig command = kvp.Value;
            if (string.IsNullOrEmpty(command.CommandName)) {
                report.AddError("Narrative command with empty CommandName.");
                continue;
            }

            if (ProtectedCommandNames.Contains(command.CommandName)) {
                report.AddError($"Narrative command [{command.CommandName}] is protected gameplay state mutation and cannot be whitelisted.");
            }

            if (command.MinArgs < 0 || command.MaxArgs < command.MinArgs) {
                report.AddError($"Narrative command [{command.CommandName}] has invalid arg range [{command.MinArgs}, {command.MaxArgs}].");
            }

            if (command.AllowedContexts == null || command.AllowedContexts.Count == 0) {
                report.AddError($"Narrative command [{command.CommandName}] must define AllowedContexts.");
            }
        }
    }

    private static void ValidateSpeakers(NarrativeConfigDatabase database, ConfigValidationReport report) {
        foreach (var kvp in database.Speakers) {
            NarrativeSpeakerConfig speaker = kvp.Value;
            if (string.IsNullOrEmpty(speaker.SpeakerID)) {
                report.AddError("Narrative speaker with empty SpeakerID.");
            }

            if (string.IsNullOrEmpty(speaker.DisplayNameKey)) {
                report.AddError($"Narrative speaker [{speaker.SpeakerID}] is missing DisplayNameKey.");
            }

            if (string.IsNullOrEmpty(speaker.DefaultPortraitVisualID) && string.IsNullOrEmpty(speaker.FallbackVisualID)) {
                report.AddError($"Narrative speaker [{speaker.SpeakerID}] must define DefaultPortraitVisualID or FallbackVisualID.");
            }
        }
    }

    private static void ValidateNodes(NarrativeConfigDatabase database, ConfigValidationReport report) {
        if (database.Nodes.Count == 0) {
            report.AddError("Narrative nodes are empty.");
            return;
        }

        HashSet<string> triggerNodes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var trigger in database.Triggers.Values) {
            if (!string.IsNullOrEmpty(trigger.YarnNode)) {
                triggerNodes.Add(trigger.YarnNode);
            }
        }

        foreach (var kvp in database.Nodes) {
            NarrativeNodeConfig node = kvp.Value;
            string owner = $"Narrative node [{node.NodeID}]";

            if (string.IsNullOrEmpty(node.NodeID)) {
                report.AddError("Narrative node with empty NodeID.");
                continue;
            }

            if (string.IsNullOrEmpty(node.ScriptFile) || !database.ScriptSources.ContainsKey(Normalize(node.ScriptFile))) {
                report.AddError($"{owner} references missing ScriptFile [{node.ScriptFile}].");
            } else if (!database.HasYarnNode(node.ScriptFile, node.NodeID)) {
                report.AddError($"{owner} does not exist in Yarn script [{node.ScriptFile}].");
            }

            if (!string.IsNullOrEmpty(node.SourceTable) && !database.SourceTableLineKeys.ContainsKey(Normalize(node.SourceTable))) {
                report.AddError($"{owner} references missing SourceTable [{node.SourceTable}].");
            }

            if (!triggerNodes.Contains(node.NodeID)) {
                report.AddError($"{owner} has no narrative trigger.");
            }

            ValidateNodeSpeakers(database, report, node, owner);
            ValidateNodeLineKeys(database, report, node, owner);
            ValidateNodeCommands(database, report, node, owner);
            ValidateNodeVisualIntents(report, node, owner);
            ValidateScriptBody(database, report, node, owner);
        }
    }

    private static void ValidateNodeSpeakers(NarrativeConfigDatabase database, ConfigValidationReport report, NarrativeNodeConfig node, string owner) {
        foreach (string speakerID in SafeList(node.SpeakerIDs)) {
            if (string.IsNullOrEmpty(speakerID) || !database.Speakers.ContainsKey(speakerID)) {
                report.AddError($"{owner} references missing speaker [{speakerID}].");
            }
        }
    }

    private static void ValidateNodeLineKeys(NarrativeConfigDatabase database, ConfigValidationReport report, NarrativeNodeConfig node, string owner) {
        HashSet<string> keys = ExtractLineKeys(database.GetScriptSource(node.ScriptFile), node.NodeID);
        foreach (string lineKey in SafeList(node.LineKeys)) {
            if (string.IsNullOrEmpty(lineKey)) {
                report.AddError($"{owner} has empty line_key.");
                continue;
            }

            if (!keys.Contains(lineKey)) {
                report.AddError($"{owner} declares line_key [{lineKey}] but Yarn node does not contain it.");
            }

            if (!string.IsNullOrEmpty(node.SourceTable) && !database.SourceTableHasLineKey(node.SourceTable, lineKey)) {
                report.AddError($"{owner} line_key [{lineKey}] is missing from SourceTable [{node.SourceTable}].");
            }
        }
    }

    private static void ValidateNodeCommands(NarrativeConfigDatabase database, ConfigValidationReport report, NarrativeNodeConfig node, string owner) {
        foreach (string commandName in SafeList(node.CommandNames)) {
            if (string.IsNullOrEmpty(commandName) || !database.Commands.ContainsKey(commandName)) {
                report.AddError($"{owner} references missing command [{commandName}].");
            }
        }
    }

    private static void ValidateNodeVisualIntents(ConfigValidationReport report, NarrativeNodeConfig node, string owner) {
        bool locked = string.Equals(node.ImplementationState, "locked_for_implementation", StringComparison.Ordinal);
        foreach (NarrativeVisualIntentConfig visual in SafeList(node.VisualIntents)) {
            if (visual == null) {
                report.AddError($"{owner} has null visual intent.");
                continue;
            }

            if (visual.Required && string.IsNullOrEmpty(visual.VisualID) && string.IsNullOrEmpty(visual.FallbackVisualID)) {
                report.AddError($"{owner} has required visual intent without VisualID or FallbackVisualID.");
            }

            if (locked && visual.Required && string.IsNullOrEmpty(visual.FallbackVisualID)) {
                report.AddError($"{owner} is locked_for_implementation and required VisualID [{visual.VisualID}] has no fallback.");
            }
        }

        if (locked && (node.VisualIntents == null || node.VisualIntents.Count == 0)) {
            report.AddError($"{owner} is locked_for_implementation but has no VisualIntents / fallback.");
        }
    }

    private static void ValidateScriptBody(NarrativeConfigDatabase database, ConfigValidationReport report, NarrativeNodeConfig node, string owner) {
        string script = database.GetScriptSource(node.ScriptFile);
        List<string> nodeLines = ExtractNodeBody(script, node.NodeID);
        foreach (string rawLine in nodeLines) {
            string trimmed = rawLine.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("//", StringComparison.Ordinal)) {
                continue;
            }

            if (TryParseCommand(trimmed, out string commandName, out List<string> args)) {
                if (!database.Commands.TryGetValue(commandName, out NarrativeCommandConfig command)) {
                    report.AddError($"{owner} uses illegal command [{commandName}].");
                    continue;
                }

                if (args.Count < command.MinArgs || args.Count > command.MaxArgs) {
                    report.AddError($"{owner} command [{commandName}] arg count [{args.Count}] is outside [{command.MinArgs}, {command.MaxArgs}].");
                }

                if (command.AllowedContexts != null && command.AllowedContexts.Count > 0 && !command.AllowedContexts.Contains(node.Context)) {
                    report.AddError($"{owner} command [{commandName}] is not allowed in context [{node.Context}].");
                }

                if (string.Equals(commandName, "set_flag", StringComparison.Ordinal) && args.Count > 0 && !database.Flags.ContainsKey(args[0])) {
                    report.AddError($"{owner} command set_flag references missing flag [{args[0]}].");
                }

                continue;
            }

            NarrativeLineOutput line = ParseLine(trimmed);
            if (!string.IsNullOrEmpty(line.SpeakerID) && !database.Speakers.ContainsKey(line.SpeakerID)) {
                report.AddError($"{owner} Yarn line references missing speaker [{line.SpeakerID}].");
            }

            if (string.IsNullOrEmpty(line.LineKey) && !string.IsNullOrEmpty(line.Text)) {
                report.AddError($"{owner} Yarn line [{line.Text}] is missing #line tag.");
            }
        }
    }

    private static void ValidateTriggers(NarrativeConfigDatabase database, ConfigValidationReport report) {
        if (database.Triggers.Count == 0) {
            report.AddError("Narrative triggers are empty.");
            return;
        }

        foreach (var kvp in database.Triggers) {
            NarrativeTriggerConfig trigger = kvp.Value;
            string owner = $"Narrative trigger [{trigger.TriggerID}]";

            if (string.IsNullOrEmpty(trigger.TriggerID)) {
                report.AddError("Narrative trigger with empty TriggerID.");
                continue;
            }

            if (!AllowedEventTypes.Contains(trigger.EventType)) {
                report.AddError($"{owner} has unsupported EventType [{trigger.EventType}].");
            }

            if (string.IsNullOrEmpty(trigger.YarnNode) || !database.Nodes.ContainsKey(trigger.YarnNode)) {
                report.AddError($"{owner} references missing YarnNode [{trigger.YarnNode}].");
            }

            if (!AllowedBlockingModes.Contains(trigger.BlockingMode)) {
                report.AddError($"{owner} has unsupported BlockingMode [{trigger.BlockingMode}].");
            }

            if (!AllowedBusyPolicies.Contains(trigger.IfBusyPolicy)) {
                report.AddError($"{owner} has unsupported IfBusyPolicy [{trigger.IfBusyPolicy}].");
            }

            if (trigger.Priority < 0 || trigger.Priority > 100) {
                report.AddError($"{owner} Priority [{trigger.Priority}] must be between 0 and 100.");
            }

            if (trigger.Chance < 0f || trigger.Chance > 1f) {
                report.AddError($"{owner} Chance [{trigger.Chance}] must be between 0 and 1.");
            }

            ValidateFlagRefs(database, report, owner, "RequiredFlags", trigger.RequiredFlags);
            ValidateFlagRefs(database, report, owner, "ForbiddenFlags", trigger.ForbiddenFlags);
            ValidateFlagRefs(database, report, owner, "SetFlagsOnComplete", trigger.SetFlagsOnComplete);
        }
    }

    private static void ValidateFlagRefs(NarrativeConfigDatabase database, ConfigValidationReport report, string owner, string fieldName, List<string> flags) {
        foreach (string flagID in SafeList(flags)) {
            if (string.IsNullOrEmpty(flagID) || !database.Flags.ContainsKey(flagID)) {
                report.AddError($"{owner} {fieldName} references missing flag [{flagID}].");
            }
        }
    }

    private static List<string> ExtractNodeBody(string script, string nodeID) {
        List<string> body = new List<string>();
        if (string.IsNullOrEmpty(script) || string.IsNullOrEmpty(nodeID)) {
            return body;
        }

        string[] lines = script.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        bool foundTitle = false;
        bool inBody = false;

        for (int i = 0; i < lines.Length; i++) {
            string trimmed = lines[i].Trim();
            if (trimmed.StartsWith("title:", StringComparison.OrdinalIgnoreCase)) {
                string title = trimmed.Substring("title:".Length).Trim();
                foundTitle = string.Equals(title, nodeID, StringComparison.Ordinal);
                inBody = false;
                continue;
            }

            if (!foundTitle) {
                continue;
            }

            if (trimmed == "---") {
                inBody = true;
                continue;
            }

            if (trimmed == "===") {
                break;
            }

            if (inBody) {
                body.Add(lines[i]);
            }
        }

        return body;
    }

    private static HashSet<string> ExtractLineKeys(string script, string nodeID) {
        HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (string line in ExtractNodeBody(script, nodeID)) {
            int index = line.IndexOf("#line:", StringComparison.Ordinal);
            if (index >= 0) {
                keys.Add(line.Substring(index + "#line:".Length).Trim());
            }
        }

        return keys;
    }

    private static bool TryParseCommand(string line, out string commandName, out List<string> args) {
        commandName = string.Empty;
        args = new List<string>();
        if (!line.StartsWith("<<", StringComparison.Ordinal) || !line.EndsWith(">>", StringComparison.Ordinal)) {
            return false;
        }

        string body = line.Substring(2, line.Length - 4).Trim();
        List<string> parts = SplitCommand(body);
        if (parts.Count == 0) {
            return false;
        }

        commandName = parts[0];
        for (int i = 1; i < parts.Count; i++) {
            args.Add(parts[i]);
        }

        return true;
    }

    private static NarrativeLineOutput ParseLine(string line) {
        NarrativeLineOutput output = new NarrativeLineOutput {
            RawLine = line
        };

        int tagIndex = line.IndexOf("#line:", StringComparison.Ordinal);
        if (tagIndex >= 0) {
            output.LineKey = line.Substring(tagIndex + "#line:".Length).Trim();
            line = line.Substring(0, tagIndex).TrimEnd();
        }

        int speakerSeparator = line.IndexOf(':');
        if (speakerSeparator > 0) {
            output.SpeakerID = line.Substring(0, speakerSeparator).Trim();
            output.Text = line.Substring(speakerSeparator + 1).Trim();
        } else {
            output.Text = line.Trim();
        }

        return output;
    }

    private static List<string> SplitCommand(string commandBody) {
        List<string> parts = new List<string>();
        bool inQuotes = false;
        string current = string.Empty;

        for (int i = 0; i < commandBody.Length; i++) {
            char c = commandBody[i];
            if (c == '"') {
                inQuotes = !inQuotes;
                continue;
            }

            if (char.IsWhiteSpace(c) && !inQuotes) {
                if (current.Length > 0) {
                    parts.Add(current);
                    current = string.Empty;
                }
                continue;
            }

            current += c;
        }

        if (current.Length > 0) {
            parts.Add(current);
        }

        return parts;
    }

    private static List<T> SafeList<T>(List<T> items) {
        return items ?? new List<T>();
    }

    private static string Normalize(string path) {
        return string.IsNullOrEmpty(path) ? string.Empty : path.Replace('\\', '/').TrimStart('/');
    }
}
