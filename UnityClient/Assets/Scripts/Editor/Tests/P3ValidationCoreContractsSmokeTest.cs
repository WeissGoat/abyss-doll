using System;
using P3.Validation;
using UnityEngine;

public static class P3ValidationCoreContractsSmokeTest { public static void Run(){if(ProfileRegistry.GetProgram("p0_full").ValidationDomain!=ValidationDomain.Program)throw new Exception("program domain");if(ProfileRegistry.GetArt("art_runtime").ValidationDomain!=ValidationDomain.Art)throw new Exception("art domain");bool rejected=false;try{ProfileRegistry.GetProgram("art_runtime");}catch{rejected=true;}if(!rejected)throw new Exception("cross registry accepted");rejected=false;try{EvidencePaths.ForProgramRun("art_bad");}catch{rejected=true;}if(!rejected)throw new Exception("cross run accepted");var result=new StepResult{ValidationDomain=ValidationDomain.Program,RunId="program_test",ProfileId="smoke_focus",StepId="smoke",Required=true,Status=StepStatus.Passed};if(result.SchemaVersion!="p3-validation/step-result@2")throw new Exception("schema");Debug.Log("P3 Validation Core Contracts PASSED");} }
