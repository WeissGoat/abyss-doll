using System;
using System.IO;
using System.Security.Cryptography;
using Newtonsoft.Json;
using UnityEngine;

namespace P3.Validation {
public static class ArtEvidenceService {
 static string RoleName(ArtCaptureRole r)=>r.ToString().ToLowerInvariant();
 public static Artifact Finalize(string run,string ticketId){
  var ticket=ArtCaptureTicketStore.Load(run,ticketId);if(ticket.Status!="Prepared")throw new InvalidOperationException("art_blocked:capture_ticket_not_prepared");
  if(!File.Exists(ticket.ExpectedAbsolutePath))throw new FileNotFoundException("art_blocked:mcp_capture_missing");
  var bytes=File.ReadAllBytes(ticket.ExpectedAbsolutePath);if(bytes.Length==0)throw new InvalidDataException("art_blocked:mcp_capture_empty");
  var tex=new Texture2D(2,2);if(!tex.LoadImage(bytes))throw new InvalidDataException("art_failed:invalid_png");var target=ArtTargetRegistry.Get(ticket.TargetId);
  if(tex.width!=target.ReferenceWidth||tex.height!=target.ReferenceHeight)throw new InvalidDataException("art_failed:unexpected_capture_dimensions:"+tex.width+"x"+tex.height);
  UnityEngine.Object.DestroyImmediate(tex);
  var folder=string.IsNullOrWhiteSpace(ticket.IterationId)?Path.Combine(EvidencePaths.ForArtRun(run).Screenshots,ticket.TargetId):Path.Combine(EvidencePaths.ForArtRun(run).Iterations,ticket.IterationId);
  Directory.CreateDirectory(folder);var dest=Path.Combine(folder,RoleName(ticket.Role)+".png");File.Copy(ticket.ExpectedAbsolutePath,dest,true);
  string hash;using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();
  var artifact=new Artifact{Path=dest,SourcePath=ticket.ExpectedAbsolutePath,Sha256=hash,Size=bytes.Length,CapturedAt=DateTime.UtcNow.ToString("o"),MimeType="image/png"};
  ticket.Status="Finalized";ArtCaptureTicketStore.Save(ticket);File.Delete(ticket.ExpectedAbsolutePath);var stagingDirectory=Path.GetDirectoryName(ticket.ExpectedAbsolutePath);if(Directory.Exists(stagingDirectory)&&Directory.GetFileSystemEntries(stagingDirectory).Length==0)Directory.Delete(stagingDirectory);var step=ticket.Role==ArtCaptureRole.Seal?"seal_capture":RoleName(ticket.Role)+"_capture";var dir=Path.Combine(EvidencePaths.ForArtRun(run).Steps,step);Directory.CreateDirectory(dir);
  var result=new StepResult{ValidationDomain=ValidationDomain.Art,RunId=run,ProfileId=ticket.ProfileId,StepId=step,Required=true,Status=StepStatus.Passed,Code="art_passed:mcp_game_view_capture"};result.Artifacts.Add(artifact);File.WriteAllText(Path.Combine(dir,"result.json"),JsonConvert.SerializeObject(result,Formatting.Indented));return artifact;
 }
}
}
