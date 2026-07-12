using System;
using System.IO;
using System.Security.Cryptography;
using Newtonsoft.Json;

public static class P3ValidationEvidenceWriter {
    public static string Write(P3ValidationStepResult result) { var target=P3ValidationEvidencePaths.ForRun(result.RunId).StepResult(result.StepId); var temp=target+".tmp"; File.WriteAllText(temp,JsonConvert.SerializeObject(result,Formatting.Indented)); if(File.Exists(target))File.Delete(target); File.Move(temp,target); return target; }
    public static P3ValidationArtifact Copy(string source,string destinationRoot,string relative) { if(!File.Exists(source))throw new FileNotFoundException(source); var dest=Path.Combine(destinationRoot,relative); Directory.CreateDirectory(Path.GetDirectoryName(dest)); File.Copy(source,dest,true); using(var sha=SHA256.Create())using(var stream=File.OpenRead(dest)){ return new P3ValidationArtifact{Path=relative.Replace('\\','/'),SourcePath=source,Sha256=BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant(),Size=new FileInfo(dest).Length,CapturedAt=DateTime.UtcNow.ToString("o"),MimeType=Path.GetExtension(dest).Equals(".png",StringComparison.OrdinalIgnoreCase)?"image/png":"application/octet-stream"}; } }
}
