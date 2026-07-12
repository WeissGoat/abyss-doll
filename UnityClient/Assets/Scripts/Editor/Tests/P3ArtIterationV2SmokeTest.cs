using System;
using Newtonsoft.Json;
using P3.Validation;
using UnityEngine;

public static class P3ArtIterationV2SmokeTest {
 sealed class Fake : IArtPersistAdapter {
  public string TargetId => "workshop_main";
  public ArtPersistResult Apply(ArtIterationRequest request) => new ArtPersistResult { TargetId=TargetId, DurableSource="Assets/Test.prefab", Changed=true };
 }

 public static void Run() {
  ArtPersistAdapterRegistry.ClearForTests();
  try {
   var parsed=JsonConvert.DeserializeObject<ArtIterationRequest>("{\"run_id\":\"r\",\"iteration_id\":\"i\",\"target_id\":\"workshop_main\",\"component_path\":\"WorkshopPanel\",\"action\":\"SetAlpha\",\"float_value\":0.5}");
   if(parsed.RunId!="r"||parsed.Action!=ArtIterationAction.SetAlpha||Math.Abs(parsed.FloatValue-0.5f)>0.001f)throw new Exception("typed iteration payload did not bind");
   var request=new ArtIterationRequest{RunId="art_iter_"+Guid.NewGuid().ToString("N"),IterationId="i1",TargetId="workshop_main"};
   bool rejected=false;
   try{ArtIterationServiceV2.Persist(request);}catch{rejected=true;}
   if(!rejected)throw new Exception("missing adapter accepted");
   ArtPersistAdapterRegistry.Register(new Fake());
   ArtValidationOrchestrator.Start(request.RunId,"art_iteration",new[]{request.TargetId},"test");
   ArtValidationOrchestrator.RecordCapture(request.RunId,ArtCaptureRole.Before);
   ArtIterationServiceV2.Persist(request);
   ArtValidationOrchestrator.RecordCapture(request.RunId,ArtCaptureRole.After);
   rejected=false;
   try{ArtIterationServiceV2.Complete(request);}catch{rejected=true;}
   if(!rejected)throw new Exception("iteration completed without PlayMode reload and reinspect");
   Debug.Log("P3 Art Iteration V2 PASSED");
  } finally {
   ArtPersistAdapterRegistry.ClearForTests();
  }
 }
}
