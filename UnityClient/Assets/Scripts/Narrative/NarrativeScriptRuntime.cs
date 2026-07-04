using System;
using System.Collections.Generic;

public interface INarrativeScriptRuntime {
    string RuntimeName { get; }
    bool HasNode(string scriptSource, string nodeName);
    NarrativePlaybackResult PlayNode(string scriptSource, string nodeName, Action<NarrativeCommandOutput> onCommand);
}

public sealed class NarrativeLineOutput {
    public string SpeakerID;
    public string Text;
    public string LineKey;
    public string RawLine;
}

public sealed class NarrativeCommandOutput {
    public string CommandName;
    public List<string> Arguments = new List<string>();
    public string RawCommand;
}

public enum NarrativePlaybackStepType {
    Line,
    Command
}

public sealed class NarrativePlaybackStep {
    public NarrativePlaybackStepType StepType;
    public NarrativeLineOutput Line;
    public NarrativeCommandOutput Command;
}

public sealed class NarrativePlaybackResult {
    public string RuntimeName;
    public string NodeName;
    public List<NarrativePlaybackStep> Steps = new List<NarrativePlaybackStep>();
    public List<NarrativeLineOutput> Lines = new List<NarrativeLineOutput>();
    public List<NarrativeCommandOutput> Commands = new List<NarrativeCommandOutput>();
    public List<string> Errors = new List<string>();

    public bool Success {
        get { return Errors.Count == 0; }
    }
}

public sealed class P3YarnLikeNarrativeRuntime : INarrativeScriptRuntime {
    public string RuntimeName {
        get { return NarrativeRuntimeSelection.CurrentSpikeRuntimeName; }
    }

    public bool HasNode(string scriptSource, string nodeName) {
        return TryGetNodeLines(scriptSource, nodeName, out _);
    }

    public NarrativePlaybackResult PlayNode(string scriptSource, string nodeName, Action<NarrativeCommandOutput> onCommand) {
        NarrativePlaybackResult result = new NarrativePlaybackResult {
            RuntimeName = RuntimeName,
            NodeName = nodeName
        };

        if (!TryGetNodeLines(scriptSource, nodeName, out List<string> nodeLines)) {
            result.Errors.Add($"Narrative node not found: {nodeName}");
            return result;
        }

        for (int i = 0; i < nodeLines.Count; i++) {
            string trimmed = nodeLines[i].Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("//", StringComparison.Ordinal)) {
                continue;
            }

            if (TryParseCommand(trimmed, out NarrativeCommandOutput command)) {
                result.Commands.Add(command);
                result.Steps.Add(new NarrativePlaybackStep {
                    StepType = NarrativePlaybackStepType.Command,
                    Command = command
                });
                onCommand?.Invoke(command);
                continue;
            }

            NarrativeLineOutput line = ParseLine(trimmed);
            if (!string.IsNullOrEmpty(line.Text)) {
                result.Lines.Add(line);
                result.Steps.Add(new NarrativePlaybackStep {
                    StepType = NarrativePlaybackStepType.Line,
                    Line = line
                });
            }
        }

        return result;
    }

    private static bool TryGetNodeLines(string scriptSource, string nodeName, out List<string> nodeLines) {
        nodeLines = new List<string>();
        if (string.IsNullOrEmpty(scriptSource) || string.IsNullOrEmpty(nodeName)) {
            return false;
        }

        string[] lines = scriptSource.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        bool foundTitle = false;
        bool inBody = false;

        for (int i = 0; i < lines.Length; i++) {
            string raw = lines[i];
            string trimmed = raw.Trim();

            if (trimmed.StartsWith("title:", StringComparison.OrdinalIgnoreCase)) {
                string title = trimmed.Substring("title:".Length).Trim();
                foundTitle = string.Equals(title, nodeName, StringComparison.Ordinal);
                inBody = false;
                nodeLines.Clear();
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
                return true;
            }

            if (inBody) {
                nodeLines.Add(raw);
            }
        }

        return foundTitle && inBody;
    }

    private static bool TryParseCommand(string line, out NarrativeCommandOutput command) {
        command = null;
        if (!line.StartsWith("<<", StringComparison.Ordinal) || !line.EndsWith(">>", StringComparison.Ordinal)) {
            return false;
        }

        string body = line.Substring(2, line.Length - 4).Trim();
        if (string.IsNullOrEmpty(body)) {
            return false;
        }

        List<string> parts = SplitCommand(body);
        if (parts.Count == 0) {
            return false;
        }

        command = new NarrativeCommandOutput {
            CommandName = parts[0],
            RawCommand = body
        };

        for (int i = 1; i < parts.Count; i++) {
            command.Arguments.Add(parts[i]);
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
            output.SpeakerID = string.Empty;
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
}
