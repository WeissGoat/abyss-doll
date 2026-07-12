using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

public sealed class P3ConsoleEntry { public string log_type,condition,stack_trace,fingerprint,captured_at; }
public sealed class P3ConsoleMarker { public string RunId; public DateTime StartedAt; internal List<P3ConsoleEntry> Entries=new List<P3ConsoleEntry>(); internal Application.LogCallback Handler; }
public static class P3ValidationConsoleTracker {
    public static P3ConsoleMarker Begin(string runId) { var m=new P3ConsoleMarker{RunId=runId,StartedAt=DateTime.UtcNow}; int n=0; m.Handler=(c,s,t)=>m.Entries.Add(new P3ConsoleEntry{log_type=t.ToString(),condition=c,stack_trace=s,fingerprint=t+"|"+c+"|"+s+"|"+(n++),captured_at=DateTime.UtcNow.ToString("o")}); Application.logMessageReceived+=m.Handler; return m; }
    public static IReadOnlyList<P3ConsoleEntry> Complete(P3ConsoleMarker marker) { Application.logMessageReceived-=marker.Handler; var path=P3ValidationEvidencePaths.ForRun(marker.RunId); File.WriteAllText(Path.Combine(path.Unity,"console-delta.json"),JsonConvert.SerializeObject(marker.Entries,Formatting.Indented)); return marker.Entries; }
}
