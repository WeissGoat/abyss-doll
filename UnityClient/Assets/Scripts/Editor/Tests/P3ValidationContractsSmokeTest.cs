using System;
using System.Linq;
using MCPForUnity.Editor.Tools;
using UnityEngine;

public static class P3ValidationContractsSmokeTest {
    public static void Run(){ var r=P3ValidationStepResult.Passed("run_1","smoke_focus","compile_gate",true); if(r.SchemaVersion!="p3-validation/step-result@1"||r.Status!=P3ValidationStepStatus.Passed)throw new Exception("contract mismatch"); var ids=P3ValidationProfileRegistry.All().Select(p=>p.Id).ToArray(); foreach(var id in new[]{"smoke_focus","art_runtime","t0_seal","p0_full"})if(!ids.Contains(id))throw new Exception("profile missing: "+id); bool rejected=false;try{P3ValidationEvidencePaths.ForRun("../bad");}catch{rejected=true;}if(!rejected)throw new Exception("unsafe RunID accepted"); Debug.Log("P3 Validation Contracts PASSED"); }
}

public static class P3ValidationMcpToolsSmokeTest {
    public static void Run(){ var types=new[]{typeof(P3UnityReadinessTool),typeof(P3CollectUnityEvidenceTool),typeof(P3RunSmokeProfileTool),typeof(P3RunArtAcceptanceTool),typeof(P3CaptureT0Tool),typeof(P3RunUnityProfileTool)}; foreach(var type in types){var attr=(McpForUnityToolAttribute)Attribute.GetCustomAttribute(type,typeof(McpForUnityToolAttribute));if(attr==null)throw new Exception("MCP attribute missing: "+type.Name);} var snapshot=P3ValidationEditorSnapshot.Capture(); if(snapshot==null)throw new Exception("snapshot missing"); Debug.Log("P3 Validation MCP Tools PASSED"); }
}

public static class P3ValidationJobStateSmokeTest {
    public static void Run(){ var id="job_test_"+Guid.NewGuid().ToString("N");var instance="instance_"+Guid.NewGuid().ToString("N");var state=new P3ValidationJobState{RunId=id,Status="Running",CurrentStep="test"};P3ValidationJobStore.Save(state);if(P3ValidationJobStore.Load(id)?.CurrentStep!="test")throw new Exception("job persistence failed");string active;if(!P3ValidationInstanceLock.TryAcquire(instance,id,out active))throw new Exception("lock acquire failed");if(P3ValidationInstanceLock.TryAcquire(instance,id+"b",out active))throw new Exception("collision accepted");P3ValidationInstanceLock.Release(instance,id);P3ValidationJobStore.Clear(id);Debug.Log("P3 Validation Job State PASSED"); }
}

public static class P3ValidationConsoleDeltaSmokeTest {
    public static void Run(){var id="console_test_"+Guid.NewGuid().ToString("N");var marker=P3ValidationConsoleTracker.Begin(id);var warning="p3-warning-"+id;Debug.LogWarning(warning);var entries=P3ValidationConsoleTracker.Complete(marker);if(!entries.Any(e=>e.condition==warning))throw new Exception("console delta missing");Debug.Log("P3 Validation Console Delta PASSED");}
}

public static class P3ValidationFoundationSmokeSuite {
    public static void Run(){P3ValidationContractsSmokeTest.Run();P3ValidationMcpToolsSmokeTest.Run();P3ValidationJobStateSmokeTest.Run();P3ValidationConsoleDeltaSmokeTest.Run();Debug.Log("P3 Validation Foundation Suite PASSED");}
}
