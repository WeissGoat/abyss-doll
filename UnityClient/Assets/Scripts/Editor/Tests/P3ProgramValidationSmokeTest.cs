using System;
using P3.Validation;
using UnityEngine;

public static class P3ProgramValidationSmokeTest {
 public static void Run(){foreach(var id in new[]{"smoke_focus","t0_functional","p0_full"}){var p=ProfileRegistry.GetProgram(id);if(p.ValidationDomain!=ValidationDomain.Program)throw new Exception("domain leak");if(p.RequiredSteps.Exists(x=>x.Contains("screenshot")||x.Contains("art")))throw new Exception("art requirement leaked");ProgramSmokeRegistry.Get(p.SmokeSet);}bool rejected=false;try{ProgramSmokeRegistry.Get("Some.Type.RawMethod");}catch{rejected=true;}if(!rejected)throw new Exception("raw smoke name accepted");Debug.Log("P3 Program Validation PASSED");}
}
