using System;
using System.IO;
using UnityEngine;

namespace P3.Validation {
public sealed class EvidencePaths { public string Root,Steps,Unity,Screenshots,SourceReports,Iterations; private static EvidencePaths For(string runId,string prefix,string folder){if(string.IsNullOrWhiteSpace(runId)||!runId.StartsWith(prefix+"_",StringComparison.Ordinal)||runId.Contains("..")||runId.IndexOfAny(new[]{'/','\\'})>=0)throw new ArgumentException("Invalid "+prefix+" RunID");var root=Path.GetFullPath(Path.Combine(Application.dataPath,"../Logs/P3Validation",folder,runId));var p=new EvidencePaths{Root=root,Steps=Path.Combine(root,"steps"),Unity=Path.Combine(root,"unity"),Screenshots=Path.Combine(root,"screenshots"),SourceReports=Path.Combine(root,"source_reports"),Iterations=Path.Combine(root,"iterations")};foreach(var d in new[]{p.Root,p.Steps,p.Unity,p.Screenshots,p.SourceReports,p.Iterations})Directory.CreateDirectory(d);return p;} public static EvidencePaths ForProgramRun(string id)=>For(id,"program","program-runs");public static EvidencePaths ForArtRun(string id)=>For(id,"art","art-runs");public static EvidencePaths ForReleaseRun(string id)=>For(id,"release","release-runs"); }
}
