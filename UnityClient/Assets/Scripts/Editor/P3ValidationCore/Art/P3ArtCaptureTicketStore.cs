using System;
using System.IO;
using Newtonsoft.Json;

namespace P3.Validation {
public static class ArtCaptureTicketStore {
 static string Dir(string run){var d=Path.Combine(EvidencePaths.ForArtRun(run).Root,"capture-tickets");Directory.CreateDirectory(d);return d;}
 static string PathFor(string run,string ticket)=>Path.Combine(Dir(run),ticket+".json");
 public static ArtCaptureTicket Prepare(string run,string profile,string targetId,ArtCaptureRole role,string iterationId){
  ProfileRegistry.GetArt(profile);ArtTargetRegistry.Get(targetId);
  var id="capture_"+Guid.NewGuid().ToString("N");var staging=Path.Combine(EvidencePaths.ForArtRun(run).Root,"mcp-capture-staging",id);Directory.CreateDirectory(staging);
  var ticket=new ArtCaptureTicket{TicketId=id,RunId=run,ProfileId=profile,TargetId=targetId,Role=role,IterationId=iterationId,OutputFolder=staging,ScreenshotFileName=id+".png",ExpectedAbsolutePath=Path.Combine(staging,id+".png"),CreatedAt=DateTime.UtcNow.ToString("o"),Status="Prepared"};
  File.WriteAllText(PathFor(run,id),JsonConvert.SerializeObject(ticket,Formatting.Indented));return ticket;
 }
 public static ArtCaptureTicket Load(string run,string id){if(string.IsNullOrWhiteSpace(id)||id.Contains("..")||id.IndexOfAny(new[]{'/','\\'})>=0)throw new ArgumentException("art_blocked:invalid_capture_ticket");var p=PathFor(run,id);if(!File.Exists(p))throw new ArgumentException("art_blocked:unknown_capture_ticket");return JsonConvert.DeserializeObject<ArtCaptureTicket>(File.ReadAllText(p));}
 public static void Save(ArtCaptureTicket t)=>File.WriteAllText(PathFor(t.RunId,t.TicketId),JsonConvert.SerializeObject(t,Formatting.Indented));
}
}
